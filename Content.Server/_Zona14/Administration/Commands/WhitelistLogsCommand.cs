// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
///     Opens the admin logs UI pre-filtered to whitelist changes.
/// </summary>
[AdminCommand(AdminFlags.Logs)]
public sealed class WhitelistLogsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "whitelistlogs";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminLogsEui();
        _euiManager.OpenEui(ui, player);

        var types = new HashSet<LogType> { LogType.AdminMessage };
        ui.SetLogFilter(search: "whitelist", types: types);
    }
}
