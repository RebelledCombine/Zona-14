// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server._Zona14.Administration.UI.Dashboard;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14AdminDashboardCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "z14dashboard";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command must be run by a player.");
            return;
        }

        _euiManager.OpenEui(new Z14AdminDashboardEui(), player);
    }
}
