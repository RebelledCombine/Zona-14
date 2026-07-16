// SPDX-License-Identifier: MIT

using Content.Server._Stalker.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._Zona14.MutantLair;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;

namespace Content.Server._Zona14.MutantLair;

/// <summary>
/// Station event that spawns <see cref="Z14MutantLairComponent"/> nests at random
/// <see cref="Z14MutantLairZoneComponent"/> markers, respecting safe zones and per-map limits.
/// </summary>
public sealed class Z14MutantLairRuleSystem : StationEventSystem<Z14MutantLairRuleComponent>
{
    [Dependency] private readonly IMapManager _mapManager = default!;

    protected override void Started(EntityUid uid, Z14MutantLairRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var zones = new List<Entity<Z14MutantLairZoneComponent>>();

        var query = AllEntityQuery<Z14MutantLairZoneComponent, TransformComponent>();
        while (query.MoveNext(out var zoneUid, out var zoneComp, out var xform))
        {
            var mapUid = _mapManager.GetMapEntityId(xform.MapID);
            if (HasComp<StalkerSafeZoneComponent>(mapUid))
                continue;

            var gridUid = xform.Coordinates.GetGridUid(EntityManager);
            if (gridUid is { } grid && HasComp<StalkerSafeZoneComponent>(grid))
                continue;

            zones.Add(new Entity<Z14MutantLairZoneComponent>(zoneUid, zoneComp));
        }

        if (zones.Count == 0)
        {
            Sawmill.Warning("No eligible Z14MutantLairZone markers found.");
            ForceEndSelf(uid, gameRule);
            return;
        }

        var lairCounts = new Dictionary<MapId, int>();
        var lairQuery = AllEntityQuery<Z14MutantLairComponent, TransformComponent>();
        while (lairQuery.MoveNext(out _, out _, out var lairXform))
        {
            var mapId = lairXform.MapID;
            lairCounts[mapId] = lairCounts.TryGetValue(mapId, out var count) ? count + 1 : 1;
        }

        RobustRandom.Shuffle(zones);

        var spawned = 0;
        foreach (var (zoneUid, zoneComp) in zones)
        {
            if (spawned >= component.MaxLairsPerEvent)
                break;

            var xform = Transform(zoneUid);
            var mapId = xform.MapID;

            if (lairCounts.TryGetValue(mapId, out var current) && current >= component.MaxLairsPerMap)
                continue;

            Spawn(zoneComp.LairPrototype, xform.Coordinates);
            lairCounts[mapId] = lairCounts.TryGetValue(mapId, out var existing) ? existing + 1 : 1;
            spawned++;
        }

        if (spawned == 0)
        {
            Sawmill.Warning("All Z14MutantLairZone markers are at their per-map lair limit.");
            ForceEndSelf(uid, gameRule);
            return;
        }

        AdminLogManager.Add(LogType.Z14MutantLair, LogImpact.High, $"Z14 mutant lair event spawned {spawned} lair(s).");
    }
}
