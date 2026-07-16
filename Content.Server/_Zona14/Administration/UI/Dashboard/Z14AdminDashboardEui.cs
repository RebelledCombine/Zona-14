// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server._Zona14.Administration.Logs;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;

using Content.Shared._Zona14.Administration.Dashboard;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Eui;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using static Content.Shared._Zona14.Administration.Dashboard.Z14AdminDashboardEuiMsg;

namespace Content.Server._Zona14.Administration.UI.Dashboard;

/// <summary>
/// Server-side EUI for the Zona-14 central admin dashboard.
/// </summary>
public sealed class Z14AdminDashboardEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;
    private readonly List<SharedAdminLog> _recentEvents = new();
    private readonly Dictionary<string, int> _eventCounts = new();

    private GameTicker _gameTicker = default!;
    private GameRunLevel _lastRunLevel;
    private int _lastRoundId;
    private TimeSpan _lastRoundStart;
    private TimeSpan? _lastEventUpdate;
    private bool _loading = true;

    public override void Opened()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _logManager.GetSawmill("admin.z14_dashboard");

        base.Opened();

        _adminManager.OnPermsChanged += OnPermsChanged;
        _adminLog.OnAdminLogAdded += OnAdminLogAdded;
        _gameTicker = _entity.System<GameTicker>();

        _lastRunLevel = _gameTicker.RunLevel;
        _lastRoundId = _gameTicker.RoundId;
        _lastRoundStart = _gameTicker.RoundStartTimeSpan;

        StateDirty();

        // Send a fresh initial event feed immediately.
        _ = RefreshEventsAsync();
    }

    public override void Closed()
    {
        base.Closed();
        _adminManager.OnPermsChanged -= OnPermsChanged;
        _adminLog.OnAdminLogAdded -= OnAdminLogAdded;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        _loading = false;
        var state = BuildState();
        return state;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        switch (msg)
        {
            case Refresh:
                _ = RefreshEventsAsync();
                StateDirty();
                break;

            case PlayerAction playerAction:
                _ = HandlePlayerActionAsync(playerAction);
                break;

            case FeatureCommand feature:
                HandleFeatureCommand(feature.Command);
                break;
        }
    }

    private void OnAdminLogAdded(object? sender, AdminLogAddedEventArgs e)
    {
        if (!IsInterestingEvent(e.Type))
            return;

        var log = ToSharedLog(e.Log);
        _recentEvents.Add(log);
        if (_recentEvents.Count > 250)
            _recentEvents.RemoveAt(0);

        var key = e.Type.ToString();
        _eventCounts[key] = _eventCounts.GetValueOrDefault(key) + 1;

        // Throttle rapid bursts to one update per second.
        var now = _timing.CurTime;
        if (_lastEventUpdate != null && (now - _lastEventUpdate.Value).TotalSeconds < 1.0)
            return;
        _lastEventUpdate = now;

        SendMessage(new NewEvents(new List<SharedAdminLog> { log }, replace: false));
        StateDirty();
    }

    private async Task RefreshEventsAsync()
    {
        var filter = new LogFilter
        {
            Types = GetInterestingLogTypes().ToHashSet(),
            CancellationToken = default,
            Limit = 250,
            Round = _gameTicker.RoundId,
            DateOrder = DateOrder.Descending
        };

        var logs = await Task.Run(async () => await _adminLog.All(filter), filter.CancellationToken);
        logs.Reverse();
        _recentEvents.Clear();
        _recentEvents.AddRange(logs);

        _eventCounts.Clear();
        foreach (var log in _recentEvents)
        {
            var key = log.Type.ToString();
            _eventCounts[key] = _eventCounts.GetValueOrDefault(key) + 1;
        }

        SendMessage(new NewEvents(new List<SharedAdminLog>(_recentEvents), replace: true));
        StateDirty();
    }

    private Z14AdminDashboardState BuildState()
    {
        var runLevel = _gameTicker.RunLevel;
        var roundId = _gameTicker.RoundId;
        var start = _gameTicker.RoundStartTimeSpan;

        var players = _playerManager.Sessions
            .Select(s => new Z14AdminDashboardPlayer(
                s.UserId,
                s.Name,
                s.AttachedEntity != null ? _entity.GetComponent<MetaDataComponent>(s.AttachedEntity.Value).EntityName : null,
                null,
                _adminManager.IsAdmin(s)))
            .OrderBy(p => p.Name)
            .ToList();

        var maps = _mapManager.GetAllMapIds()
            .Where(id => id != MapId.Nullspace)
            .Select(id =>
            {
                var uid = _mapManager.GetMapEntityId(id);
                var name = _entity.TryGetComponent<MetaDataComponent>(uid, out var meta) ? meta.EntityName : $"Map {id}";
                var gridCount = _mapManager.GetAllMapGrids(id).Count();
                return new Z14AdminDashboardMap((int)id, name, gridCount, 0);
            })
            .ToList();

        var flags = _adminManager.GetAdminData(Player)?.Flags ?? AdminFlags.None;

        return new Z14AdminDashboardState
        {
            RoundId = roundId,
            RoundDuration = _timing.CurTime - start,
            RunLevel = runLevel.ToString(),
            PlayerCount = players.Count,
            AdminCount = players.Count(p => p.IsAdmin),
            Players = players,
            Maps = maps,
            EventCounts = new Dictionary<string, int>(_eventCounts),
            RecentEvents = new List<SharedAdminLog>(_recentEvents),
            Flags = (uint)flags,
            ServerLoading = _loading,
            AllowedCommands = BuildAllowedCommands()
        };
    }

    private List<Z14AdminDashboardCommandInfo> BuildAllowedCommands()
    {
        var list = new List<Z14AdminDashboardCommandInfo>();

        foreach (var (name, cmd) in _consoleHost.AvailableCommands)
        {
            if (!_adminManager.TryGetCommandFlags(name, out var flags))
                continue;

            if (flags != null && flags.Length > 0 && !flags.Any(f => _adminManager.HasAdminFlag(Player, f)))
                continue;

            list.Add(new Z14AdminDashboardCommandInfo(
                name,
                cmd.Description,
                cmd.Help,
                flags?.Select(f => (uint)f).ToArray() ?? Array.Empty<uint>()));
        }

        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return list;
    }

    private async Task HandlePlayerActionAsync(PlayerAction action)
    {
        var target = await _playerLocator.LookupIdAsync(action.UserId);
        if (target == null)
        {
            _sawmill.Warning($"Dashboard player action failed: unable to locate {action.UserId} ({action.TargetName})");
            return;
        }

        switch (action.Action)
        {
            case Z14AdminDashboardAction.OpenPlayerPanel:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
                    return;
                var panel = new PlayerPanelEui(target);
                _euiManager.OpenEui(panel, Player);
                panel.SetPlayerState();
                break;

            case Z14AdminDashboardAction.OpenPlayerLogs:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Logs))
                    return;
                var logs = new AdminLogsEui();
                _euiManager.OpenEui(logs, Player);
                logs.SetLogFilter(players: new HashSet<Guid> { action.UserId.UserId });
                break;

            case Z14AdminDashboardAction.OpenBanPanel:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Ban))
                    return;
                _euiManager.OpenEui(new BanPanelEui(), Player);
                break;

            case Z14AdminDashboardAction.WipeStash:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Ban))
                    return;
                _consoleHost.ExecuteCommand(Player, $"clear_stash \"{action.TargetName}\"");
                break;

            case Z14AdminDashboardAction.WhitelistAdd:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Ban))
                    return;
                _consoleHost.ExecuteCommand(Player, $"whitelistadd \"{action.TargetName}\"");
                break;

            case Z14AdminDashboardAction.WhitelistRemove:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Ban))
                    return;
                _consoleHost.ExecuteCommand(Player, $"whitelistremove \"{action.TargetName}\"");
                break;

            case Z14AdminDashboardAction.JobWhitelistAdd:
            case Z14AdminDashboardAction.JobWhitelistRemove:
                if (!_adminManager.HasAdminFlag(Player, AdminFlags.Ban))
                    return;
                if (string.IsNullOrWhiteSpace(action.Extra))
                    return;
                var cmd = action.Action == Z14AdminDashboardAction.JobWhitelistAdd ? "jobwhitelistadd" : "jobwhitelistremove";
                _consoleHost.ExecuteCommand(Player, $"{cmd} \"{action.TargetName}\" \"{action.Extra}\"");
                break;
        }
    }

    private void HandleFeatureCommand(string command)
    {
        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Admin))
        {
            Close();
            return;
        }

        var firstWord = command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.IsNullOrEmpty(firstWord))
        {
            _sawmill.Warning($"Dashboard blocked empty command");
            return;
        }

        if (!_adminManager.TryGetCommandFlags(firstWord, out var flags))
        {
            _sawmill.Warning($"Dashboard blocked unknown command: {firstWord}");
            return;
        }

        if (flags != null && flags.Length > 0 && !flags.Any(f => _adminManager.HasAdminFlag(Player, f)))
        {
            _sawmill.Warning($"Dashboard blocked command {firstWord} due to permissions");
            return;
        }

        _consoleHost.ExecuteCommand(Player, command);
    }

    private static bool IsInterestingEvent(LogType type)
    {
        return GetInterestingLogTypes().Contains(type);
    }

    private static HashSet<LogType> GetInterestingLogTypes()
    {
        return new HashSet<LogType>
        {
            LogType.Kill,
            LogType.AdminAlert,
            LogType.Z14Door,
            LogType.Z14MutantLair,
            LogType.Z14AnomalyMigration,
            LogType.Z14SupplyDrop,
            LogType.Z14PersonalCache,
            LogType.Z14MapRadiation,
            LogType.STBandPatchIdentified,
            LogType.AdminMessage,
            LogType.MentorHelp,
            LogType.Z14Inventory,
            LogType.Respawn,
            LogType.ShuttleCalled,
            LogType.ShuttleRecalled,
            LogType.Action
        };
    }

    private static SharedAdminLog ToSharedLog(AdminLog log)
    {
        return new SharedAdminLog(
            log.Id,
            log.Type,
            log.Impact,
            log.Date,
            log.Message,
            log.Players.Select(p => p.PlayerUserId).ToArray());
    }
}
