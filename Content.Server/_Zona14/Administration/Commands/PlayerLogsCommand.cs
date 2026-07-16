// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class PlayerLogsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override string Command => Cmd;
    public const string Cmd = "playerlogs";

    public override string Description => "Open the admin logs panel filtered to a specific player by username or user ID.";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length == 0)
        {
            shell.WriteError("Usage: playerlogs <username or userId>");
            return;
        }

        var arg = args[0];

        NetUserId userId;
        if (Guid.TryParse(arg, out var guid))
        {
            userId = new NetUserId(guid);
        }
        else if (!_playerManager.TryGetUserId(arg, out userId))
        {
            shell.WriteError($"Could not find a player with the name '{arg}'.");
            return;
        }

        var ui = new AdminLogsEui();
        _euiManager.OpenEui(ui, player);
        ui.SetLogFilter(players: new HashSet<Guid> { userId.UserId });
    }
}
