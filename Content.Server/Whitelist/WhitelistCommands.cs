using System.Collections.Generic;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Database;
using Content.Shared._Zona14.Administration.Logs;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server.Whitelist;

[AdminCommand(AdminFlags.Ban)]
public sealed class AddWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    public override string Command => "whitelistadd";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _dbManager.GetWhitelistStatusAsync(guid);
            if (isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistadd-existing", ("username", data.Username)));
                return;
            }

            await _dbManager.AddToWhitelistAsync(guid);

            // Zona14: log whitelist addition
            var addAdmin = shell.Player;
            if (addAdmin is { } addAdminSession)
            {
                _adminLog.Add(LogType.AdminMessage, LogImpact.Medium,
                    $"{addAdminSession:player} added {new AdminLogPlayerValue(data.UserId, data.Username):subject} to whitelist");
            }
            else
            {
                _adminLog.Add(LogType.AdminMessage, LogImpact.Medium,
                    $"System added {new AdminLogPlayerValue(data.UserId, data.Username):subject} to whitelist");
            }

            shell.WriteLine(Loc.GetString("cmd-whitelistadd-added", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistadd-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistadd-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class RemoveWhitelistCommand : LocalizedCommands
{
    [Dependency] private readonly IPlayerLocator _locator = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override string Command => "whitelistremove";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Loc.GetString("shell-need-minimum-one-argument"));
            shell.WriteLine(Help);
            return;
        }

        var name = string.Join(' ', args).Trim();
        var data = await _locator.LookupIdByNameOrIdAsync(name);

        if (data != null)
        {
            var guid = data.UserId;
            var isWhitelisted = await _dbManager.GetWhitelistStatusAsync(guid);
            if (!isWhitelisted)
            {
                shell.WriteLine(Loc.GetString("cmd-whitelistremove-existing", ("username", data.Username)));
                return;
            }

            await _dbManager.RemoveFromWhitelistAsync(guid);

            // Zona14: log whitelist removal
            var removeAdmin = shell.Player;
            if (removeAdmin is { } removeAdminSession)
            {
                _adminLog.Add(LogType.AdminMessage, LogImpact.Medium,
                    $"{removeAdminSession:player} removed {new AdminLogPlayerValue(data.UserId, data.Username):subject} from whitelist");
            }
            else
            {
                _adminLog.Add(LogType.AdminMessage, LogImpact.Medium,
                    $"System removed {new AdminLogPlayerValue(data.UserId, data.Username):subject} from whitelist");
            }

            shell.WriteLine(Loc.GetString("cmd-whitelistremove-removed", ("username", data.Username)));
            return;
        }

        shell.WriteError(Loc.GetString("cmd-whitelistremove-not-found", ("username", args[0])));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-whitelistremove-arg-player"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Ban)]
public sealed class KickNonWhitelistedCommand : LocalizedCommands
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override string Command => "kicknonwhitelisted";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 0)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 0), ("currentAmount", args.Length)));
            shell.WriteLine(Help);
            return;
        }

        if (!_configManager.GetCVar(CCVars.WhitelistEnabled))
            return;

        var kicked = new List<string>();
        foreach (var session in _playerManager.NetworkedSessions)
        {
            if (await _dbManager.GetAdminDataForAsync(session.UserId) is not null)
                continue;

            if (!await _dbManager.GetWhitelistStatusAsync(session.UserId))
            {
                _netManager.DisconnectChannel(session.Channel, Loc.GetString("whitelist-not-whitelisted"));
                kicked.Add(session.Name);
            }
        }

        // Zona14: log non-whitelisted kick sweep
        var kickAdmin = shell.Player;
        var kickMessage = $"kicked {kicked.Count} non-whitelisted players{(kicked.Count > 0 ? ": " + string.Join(", ", kicked) : "")}";
        if (kickAdmin is { } kickAdminSession)
        {
            _adminLog.Add(LogType.AdminMessage, LogImpact.Extreme,
                $"{kickAdminSession:player} {kickMessage}");
        }
        else
        {
            _adminLog.Add(LogType.AdminMessage, LogImpact.Extreme,
                $"System {kickMessage}");
        }
    }
}
