// SPDX-License-Identifier: MIT

using System;
using System.Globalization;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server._Stalker.Map;
using Content.Server._Zona14.MapRadiation;
using Content.Shared.Administration;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class Z14MapRadiationCommand : IConsoleCommand
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public string Command => "z14mapradiation";
    public string Description => "Inspect or modify ambient map radiation.";
    public string Help => "Usage: z14mapradiation list | z14mapradiation <mapId|mapKey> [enabled <true|false>|interval <seconds>|damage <type> <amount>]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Help);
            return;
        }

        if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            ListRadiation(shell);
            return;
        }

        if (args.Length < 2)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!TryResolveMap(args[0], out var mapUid, out var mapName))
        {
            shell.WriteError($"No map found for '{args[0]}'.");
            return;
        }

        if (!_entManager.TryGetComponent(mapUid, out MapRadiationComponent? mapRad))
        {
            shell.WriteError($"Map {mapName} has no MapRadiation component.");
            return;
        }

        var subcommand = args[1].ToLowerInvariant();
        switch (subcommand)
        {
            case "enabled":
                SetEnabled(shell, mapUid, mapRad, mapName, args);
                return;
            case "interval":
                SetInterval(shell, mapUid, mapRad, mapName, args);
                return;
            case "damage":
                SetDamage(shell, mapUid, mapRad, mapName, args);
                return;
        }

        shell.WriteLine(Help);
    }

    private void ListRadiation(IConsoleShell shell)
    {
        var query = _entManager.EntityQueryEnumerator<MapRadiationComponent, MapComponent, STMapKeyComponent>();
        var found = false;
        while (query.MoveNext(out var mapUid, out var mapRad, out _, out var keyComp))
        {
            found = true;
            var damage = mapRad.Damage.GetTotal().Float();
            shell.WriteLine($"Map {keyComp.Value}: enabled={mapRad.Enabled}, interval={mapRad.Interval}s, total damage={damage:F2}");
        }

        if (!found)
            shell.WriteLine("No maps with MapRadiation found.");
    }

    private void SetEnabled(IConsoleShell shell, EntityUid mapUid, MapRadiationComponent mapRad, string mapName, string[] args)
    {
        if (args.Length < 3 || !bool.TryParse(args[2], out var enabled))
        {
            shell.WriteError("Usage: z14mapradiation <map> enabled <true|false>");
            return;
        }

        mapRad.Enabled = enabled;
        _adminLog.Add(LogType.Z14MapRadiation, LogImpact.Extreme,
            $"{shell.Player?.Name ?? "Server console"} set map radiation on {mapName} enabled={enabled}");
        shell.WriteLine($"Map radiation on {mapName} enabled set to {enabled}.");
    }

    private void SetInterval(IConsoleShell shell, EntityUid mapUid, MapRadiationComponent mapRad, string mapName, string[] args)
    {
        if (args.Length < 3 || !float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var interval))
        {
            shell.WriteError("Usage: z14mapradiation <map> interval <seconds>");
            return;
        }

        mapRad.Interval = interval;
        _adminLog.Add(LogType.Z14MapRadiation, LogImpact.Extreme,
            $"{shell.Player?.Name ?? "Server console"} set map radiation on {mapName} interval to {interval}s");
        shell.WriteLine($"Map radiation on {mapName} interval set to {interval}s.");
    }

    private void SetDamage(IConsoleShell shell, EntityUid mapUid, MapRadiationComponent mapRad, string mapName, string[] args)
    {
        if (args.Length < 4
            || !float.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
        {
            shell.WriteError("Usage: z14mapradiation <map> damage <type> <amount>");
            return;
        }

        var damageType = args[2];
        mapRad.Damage = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                { damageType, amount }
            }
        };
        _adminLog.Add(LogType.Z14MapRadiation, LogImpact.Extreme,
            $"{shell.Player?.Name ?? "Server console"} set map radiation on {mapName} damage {damageType}={amount}");
        shell.WriteLine($"Map radiation on {mapName} damage set to {damageType}={amount}.");
    }

    private bool TryResolveMap(string arg, out EntityUid mapUid, out string mapName)
    {
        mapUid = EntityUid.Invalid;
        mapName = string.Empty;

        if (int.TryParse(arg, out var id))
        {
            var mapId = new MapId(id);
            if (!_mapManager.MapExists(mapId))
                return false;

            mapUid = _mapManager.GetMapEntityId(mapId);
            mapName = GetMapName(mapUid);
            return true;
        }

        var query = _entManager.EntityQueryEnumerator<MapComponent, STMapKeyComponent>();
        while (query.MoveNext(out var uid, out _, out var keyComp))
        {
            if (keyComp.Value.Equals(arg, StringComparison.OrdinalIgnoreCase))
            {
                mapUid = uid;
                mapName = keyComp.Value;
                return true;
            }
        }

        return false;
    }

    private string GetMapName(EntityUid mapUid)
    {
        if (_entManager.TryGetComponent<STMapKeyComponent>(mapUid, out var keyComp))
            return keyComp.Value;

        return _entManager.GetComponent<MetaDataComponent>(mapUid).EntityName;
    }
}
