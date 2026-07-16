// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;

namespace Content.Server._Zona14.Administration.Commands;

/// <summary>
/// Opens the admin logs UI pre-filtered to notable admin actions and events.
/// </summary>
[AdminCommand(AdminFlags.Logs)]
public sealed class AdminActivityCommand : LocalizedEntityCommands
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override string Command => "adminactivity";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new AdminLogsEui();
        _euiManager.OpenEui(ui, player);

        var types = new HashSet<LogType>
        {
            LogType.Verb,
            LogType.AdminMessage,
            LogType.AdminCommands,
            LogType.Action,
            LogType.EntitySpawn,
            LogType.EntityDelete,
            LogType.Teleport,
            LogType.Mind,
            LogType.Respawn,
            LogType.Vote,
            LogType.EventStarted,
            LogType.EventAnnounced,
            LogType.EventRan,
            LogType.EventStopped,
            LogType.ShuttleCalled,
            LogType.ShuttleRecalled,
            LogType.STStorage,
            LogType.STShop,
            LogType.STBand,
            LogType.STFactionRelation,
            LogType.STCharacterRank,
            LogType.STLoadout,
            LogType.STSponsor,
            LogType.STWarZone,
            LogType.Z14MutantLair,
            LogType.Z14AnomalyMigration,
            LogType.Z14SupplyDrop,
            LogType.Z14PersonalCache,
            LogType.Z14MapRadiation,
        };

        var impacts = new HashSet<LogImpact>
        {
            LogImpact.High,
            LogImpact.Extreme,
        };

        ui.SetLogFilter(types: types, impacts: impacts);
    }
}
