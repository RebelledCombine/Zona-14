// SPDX-License-Identifier: MIT

using System;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server._Zona14.AnomalyMigration;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server._Zona14.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class Z14AnomalyMigrateCommand : IConsoleCommand
{
    public string Command => "z14anomigrate";
    public string Description => "Manually trigger an anomaly migration on a map.";
    public string Help => "Usage: z14anomigrate [all|mapKey|mapId] [count]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var system = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<Z14AnomalyMigrationRuleSystem>();
        var adminLog = IoCManager.Resolve<IAdminLogManager>();

        if (args.Length == 0)
        {
            system.Trigger();
            adminLog.Add(LogType.Z14AnomalyMigration, LogImpact.Extreme,
                $"{shell.Player?.Name ?? "Server console"} triggered random anomaly migration");
            shell.WriteLine("Triggered random anomaly migration.");
            return;
        }

        if (args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            system.Trigger(true);
            adminLog.Add(LogType.Z14AnomalyMigration, LogImpact.Extreme,
                $"{shell.Player?.Name ?? "Server console"} triggered anomaly migration on all valid maps");
            shell.WriteLine("Triggered anomaly migration on all valid maps.");
            return;
        }

        MapId? mapId = null;
        string? mapKey = null;

        if (int.TryParse(args[0], out var id) && IoCManager.Resolve<IMapManager>().MapExists(new MapId(id)))
        {
            mapId = new MapId(id);
        }
        else
        {
            mapKey = args[0];
        }

        var count = 0;
        if (args.Length > 1 && int.TryParse(args[1], out var parsedCount))
            count = parsedCount;

        if (!system.TryResolveMigrationTarget(mapKey, mapId, out var targetMapId, out var optionsId, out var mapName))
        {
            shell.WriteLine($"No valid anomaly migration target found for '{args[0]}'.");
            return;
        }

        system.Trigger(targetMapId, optionsId, count);
        adminLog.Add(LogType.Z14AnomalyMigration, LogImpact.Extreme,
            $"{shell.Player?.Name ?? "Server console"} triggered anomaly migration on {mapName} (count {count})");
        shell.WriteLine($"Triggered anomaly migration on {mapName}.");
    }
}
