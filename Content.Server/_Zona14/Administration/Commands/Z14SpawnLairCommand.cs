// SPDX-License-Identifier: MIT

using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Shared._Zona14.MutantLair;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Player;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14SpawnLairCommand : IConsoleCommand
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    public string Command => "z14spawnlair";
    public string Description => "Spawns a mutant lair at the current player position or at a specified Z14MutantLairZone marker.";
    public string Help => "Usage: z14spawnlair [here|<zone entity uid>]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteError(Help);
            return;
        }

        var arg = args[0].ToLowerInvariant();

        if (arg == "here")
        {
            var player = shell.Player?.AttachedEntity;
            if (player is not { } playerUid)
            {
                shell.WriteError(Loc.GetString("shell-must-be-attached-to-entity"));
                return;
            }

            if (!_entManager.TryGetComponent(playerUid, out TransformComponent? xform))
            {
                shell.WriteError("Player has no transform.");
                return;
            }

            var adminName = shell.Player?.Name ?? "Server console";
            _entManager.SpawnAtPosition("Z14MutantLair", xform.Coordinates);
            _adminLog.Add(LogType.Z14MutantLair, LogImpact.Extreme,
                $"{adminName} spawned Z14MutantLair at {xform.MapPosition:coords}");
            shell.WriteLine("Spawned a mutant lair at your position.");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !_entManager.TryGetEntity(netEntity, out var zoneEntity))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_entManager.TryGetComponent(zoneEntity, out Z14MutantLairZoneComponent? zoneComp) ||
            !_entManager.TryGetComponent(zoneEntity, out TransformComponent? zoneXform))
        {
            shell.WriteError("Entity is not a valid Z14MutantLairZone marker.");
            return;
        }

        var adminName2 = shell.Player?.Name ?? "Server console";
        _entManager.SpawnAtPosition(zoneComp.LairPrototype, zoneXform.Coordinates);
        _adminLog.Add(LogType.Z14MutantLair, LogImpact.Extreme,
            $"{adminName2} spawned {zoneComp.LairPrototype} at {zoneXform.MapPosition:coords}");
        shell.WriteLine($"Spawned {zoneComp.LairPrototype} at {zoneComp.Tier} zone.");
    }
}
