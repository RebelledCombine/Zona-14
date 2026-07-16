// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server._Zona14.Administration.Logs;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared._Zona14.Administration.Logs;
using Content.Shared._Zona14.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Zona14.Administration.Systems;

/// <summary>
/// Detects rapid kills, mass door destruction, mass item spawning, and impossible movement.
/// </summary>
public sealed class Z14CheatDetectionSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private bool _enabled;
    private bool _movementEnabled;
    private bool _killsEnabled;
    private bool _doorsEnabled;
    private bool _spawnsEnabled;

    private double _movementMinTime;
    private TimeSpan _movementSampleWindow;
    private float _movementMaxDistance;
    private float _movementMaxSpeed;

    private float _killsWindow;
    private int _killsThreshold;

    private float _doorsWindow;
    private int _doorsThreshold;

    private float _spawnsWindow;
    private int _spawnsThreshold;

    private TimeSpan _alertCooldown;

    private readonly Dictionary<EntityUid, (TimeSpan Time, MapCoordinates Pos)> _lastMove = new();
    private readonly Dictionary<EntityUid, (TimeSpan Time, MapCoordinates Pos)> _lastSample = new();

    private readonly Dictionary<NetUserId, Queue<TimeSpan>> _killQueue = new();
    private readonly Dictionary<NetUserId, Queue<TimeSpan>> _doorQueue = new();
    private readonly Dictionary<NetUserId, Queue<TimeSpan>> _spawnQueue = new();

    private readonly Dictionary<NetUserId, TimeSpan> _lastKillAlert = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastDoorAlert = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastSpawnAlert = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastMovementAlert = new();

    public override void Initialize()
    {
        base.Initialize();
        LoadCVars();
        _cfg.OnCVarValueChanged += OnCVarValueChanged;
        ((IAdminLogManager)_adminLog).OnAdminLogAdded += OnAdminLogAdded;

        SubscribeLocalEvent<ActorComponent, MoveEvent>(OnActorMove);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<EntityTerminatingEvent>(OnEntityTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _cfg.OnCVarValueChanged -= OnCVarValueChanged;
        ((IAdminLogManager)_adminLog).OnAdminLogAdded -= OnAdminLogAdded;
    }

    private void LoadCVars()
    {
        _enabled = _cfg.GetCVar(Zona14CVars.CheatDetectionEnabled);
        _movementEnabled = _cfg.GetCVar(Zona14CVars.CheatDetectionMovementEnabled);
        _killsEnabled = _cfg.GetCVar(Zona14CVars.CheatDetectionKillsEnabled);
        _doorsEnabled = _cfg.GetCVar(Zona14CVars.CheatDetectionDoorsEnabled);
        _spawnsEnabled = _cfg.GetCVar(Zona14CVars.CheatDetectionSpawnsEnabled);

        _movementMinTime = _cfg.GetCVar(Zona14CVars.CheatDetectionMovementMinTime);
        _movementSampleWindow = TimeSpan.FromSeconds(_cfg.GetCVar(Zona14CVars.CheatDetectionMovementSampleWindow));
        _movementMaxDistance = _cfg.GetCVar(Zona14CVars.CheatDetectionMovementMaxDistance);
        _movementMaxSpeed = _cfg.GetCVar(Zona14CVars.CheatDetectionMovementMaxSpeed);

        _killsWindow = _cfg.GetCVar(Zona14CVars.CheatDetectionKillsWindow);
        _killsThreshold = _cfg.GetCVar(Zona14CVars.CheatDetectionKillsThreshold);

        _doorsWindow = _cfg.GetCVar(Zona14CVars.CheatDetectionDoorsWindow);
        _doorsThreshold = _cfg.GetCVar(Zona14CVars.CheatDetectionDoorsThreshold);

        _spawnsWindow = _cfg.GetCVar(Zona14CVars.CheatDetectionSpawnsWindow);
        _spawnsThreshold = _cfg.GetCVar(Zona14CVars.CheatDetectionSpawnsThreshold);

        _alertCooldown = TimeSpan.FromSeconds(_cfg.GetCVar(Zona14CVars.CheatDetectionAlertCooldown));
    }

    private void OnCVarValueChanged(CVarChangeInfo info)
    {
        if (info.Name.StartsWith("zona14.cheat_detection_"))
            LoadCVars();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent args)
    {
        _lastMove.Clear();
        _lastSample.Clear();
        _killQueue.Clear();
        _doorQueue.Clear();
        _spawnQueue.Clear();
        _lastKillAlert.Clear();
        _lastDoorAlert.Clear();
        _lastSpawnAlert.Clear();
        _lastMovementAlert.Clear();
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        _lastMove.Remove(ev.Entity);
        _lastSample.Remove(ev.Entity);
    }

    private void OnEntityTerminating(ref EntityTerminatingEvent args)
    {
        _lastMove.Remove(args.Entity.Owner);
        _lastSample.Remove(args.Entity.Owner);
    }

    private void OnAdminLogAdded(object? sender, AdminLogAddedEventArgs e)
    {
        if (!_enabled)
            return;

        if (_killsEnabled && e.Log.Type == LogType.Kill)
            ProcessKill(e);

        if (_doorsEnabled && e.Log.Type == LogType.Z14Door)
            ProcessDoor(e);

        if (_spawnsEnabled && e.Log.Type == LogType.EntitySpawn && e.Log.Impact == LogImpact.Low)
            ProcessSpawn(e);
    }

    private void ProcessKill(AdminLogAddedEventArgs e)
    {
        if (e.Log.Impact != LogImpact.High)
            return;

        if (!TryGetActorFromKey(e.Values, "actor", out var actor))
            return;

        AddEvent(actor, _killQueue, _killsWindow, _killsThreshold, "rapid killing", _lastKillAlert);
    }

    private void ProcessDoor(AdminLogAddedEventArgs e)
    {
        if (e.Log.Impact != LogImpact.Medium)
            return;

        if (!TryGetActorFromKey(e.Values, "actor", out var actor))
            return;

        AddEvent(actor, _doorQueue, _doorsWindow, _doorsThreshold, "mass door destruction", _lastDoorAlert);
    }

    private void ProcessSpawn(AdminLogAddedEventArgs e)
    {
        if (e.Log.Impact != LogImpact.Low || e.Players.Count == 0)
            return;

        var userId = new NetUserId(e.Players[0].PlayerUserId);
        var name = GetPlayerName(userId);

        AddEvent(new ActorInfo(userId, name), _spawnQueue, _spawnsWindow, _spawnsThreshold, "mass item spawning", _lastSpawnAlert);
    }

    private void OnActorMove(EntityUid uid, ActorComponent component, ref MoveEvent args)
    {
        if (!_enabled || !_movementEnabled)
            return;

        if (HasComp<GhostComponent>(uid))
            return;

        if (component.PlayerSession is not { } session)
            return;

        if (_adminManager.IsAdmin(session))
            return;

        // Skip parent moves / rotations where the local position did not change.
        if (args.OldPosition == args.NewPosition)
            return;

        var oldMap = _transform.ToMapCoordinates(args.OldPosition, logError: false);
        var newMap = _transform.ToMapCoordinates(args.NewPosition, logError: false);

        if (newMap.MapId == MapId.Nullspace || oldMap.MapId != newMap.MapId)
            return;

        if (args.ParentChanged)
        {
            _lastMove[uid] = (_timing.CurTime, newMap);
            _lastSample[uid] = (_timing.CurTime, newMap);
            return;
        }

        var now = _timing.CurTime;

        if (!_lastMove.TryGetValue(uid, out var lastMove))
        {
            _lastMove[uid] = (now, newMap);
            _lastSample[uid] = (now, newMap);
            return;
        }

        var deltaTime = now - lastMove.Time;
        _lastMove[uid] = (now, newMap);

        if (deltaTime > _movementSampleWindow)
        {
            _lastSample[uid] = (now, newMap);
            return;
        }

        if (!_lastSample.TryGetValue(uid, out var lastSample))
        {
            _lastSample[uid] = (now, newMap);
            return;
        }

        var sampleDelta = (now - lastSample.Time).TotalSeconds;
        if (sampleDelta < _movementMinTime)
            return;

        var distance = (double)(newMap.Position - lastSample.Pos.Position).Length();

        if (distance > _movementMaxDistance)
        {
            _lastSample[uid] = (now, newMap);
            return;
        }

        var speed = distance / sampleDelta;
        if (speed > _movementMaxSpeed &&
            CanAlert(session.UserId, _lastMovementAlert, _alertCooldown))
        {
            _lastMovementAlert[session.UserId] = now;
            RaiseMovementAlert(session.UserId, session.Name, speed, distance, newMap);
        }

        _lastSample[uid] = (now, newMap);
    }

    private void AddEvent(ActorInfo actor, Dictionary<NetUserId, Queue<TimeSpan>> queueDict, float windowSeconds, int threshold, string typeName, Dictionary<NetUserId, TimeSpan> lastAlertDict)
    {
        var now = _timing.CurTime;
        var window = TimeSpan.FromSeconds(windowSeconds);

        if (!queueDict.TryGetValue(actor.UserId, out var queue))
        {
            queue = new Queue<TimeSpan>();
            queueDict[actor.UserId] = queue;
        }

        while (queue.Count > 0 && now - queue.Peek() > window)
            queue.Dequeue();

        queue.Enqueue(now);

        var count = queue.Count;
        if (count >= threshold && CanAlert(actor.UserId, lastAlertDict, _alertCooldown))
        {
            lastAlertDict[actor.UserId] = now;
            RaiseAlert(typeName, actor.UserId, actor.Name, count, windowSeconds);
        }
    }

    private bool CanAlert(NetUserId userId, Dictionary<NetUserId, TimeSpan> lastAlert, TimeSpan cooldown)
    {
        if (!lastAlert.TryGetValue(userId, out var last))
            return true;

        return (_timing.CurTime - last) >= cooldown;
    }

    private void RaiseAlert(string typeName, NetUserId userId, string? name, int count, float windowSeconds)
    {
        var player = new AdminLogPlayerValue(userId, name);
        var windowStr = windowSeconds.ToString("F1");
        _adminLog.Add(LogType.AdminAlert, LogImpact.Extreme,
            $"Potential {typeName} by {player:player}: {count} events in {windowStr:windowSeconds}s");
    }

    private void RaiseMovementAlert(NetUserId userId, string? name, double speed, double distance, MapCoordinates map)
    {
        var player = new AdminLogPlayerValue(userId, name);
        var speedStr = speed.ToString("F1");
        var distanceStr = distance.ToString("F1");
        var mapStr = map.ToString();
        _adminLog.Add(LogType.AdminAlert, LogImpact.Extreme,
            $"Potential impossible movement by {player:player}: {speedStr:speed} tiles/s over {distanceStr:distance} tiles at {mapStr:map}");
    }

    private bool TryGetActorFromKey(IReadOnlyDictionary<string, object?> values, string key, out ActorInfo actor)
    {
        actor = default;
        if (!values.TryGetValue(key, out var value) || value is null)
            return false;

        switch (value)
        {
            case EntityStringRepresentation rep:
                if (rep.Session is not { } session)
                    return false;
                actor = new ActorInfo(session.UserId, session.Name);
                return true;

            case SerializablePlayer player:
            {
                var userId = new NetUserId(player.UserId);
                actor = new ActorInfo(userId, GetPlayerName(userId) ?? player.Name);
                return true;
            }

            case IAdminLogsPlayerValue playerValue:
            {
                var user = playerValue.Players.FirstOrDefault();
                if (user == default)
                    return false;
                actor = new ActorInfo(user, GetPlayerName(user) ?? playerValue.ToString());
                return true;
            }

            default:
                return false;
        }
    }

    private string? GetPlayerName(NetUserId userId)
    {
        if (_playerManager.TryGetPlayerData(userId, out var data))
            return data.UserName;
        return null;
    }

    private readonly record struct ActorInfo(NetUserId UserId, string? Name);
}
