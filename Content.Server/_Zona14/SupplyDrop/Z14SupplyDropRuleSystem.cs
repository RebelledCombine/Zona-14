// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Numerics;
using Content.Server._Stalker.Map;
using Content.Server._Stalker.StationEvents.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Events;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Zona14.SupplyDrop;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Station.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Zona14.SupplyDrop;

public sealed class Z14SupplyDropRuleSystem : StationEventSystem<Z14SupplyDropRuleComponent>
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Added(EntityUid uid, Z14SupplyDropRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        // Zona14: handled in Started() so we can choose a zone and use local PVS warnings.
    }

    protected override void Started(EntityUid uid, Z14SupplyDropRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var zone = component.ForcedZone;
        var variant = component.ForcedVariant;
        var user = component.ForcedUser;

        component.ForcedZone = null;
        component.ForcedVariant = null;
        component.ForcedUser = null;

        if (!RunSequence(component, zone, variant, user))
            ForceEndSelf(uid, gameRule);
    }

    protected override void Ended(EntityUid uid, Z14SupplyDropRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
    }

    public void Trigger(Z14SupplyDropVariant? variant = null, EntityUid? zone = null, EntityUid? user = null)
    {
        var ent = GameTicker.AddGameRule("Z14SupplyDropRule");
        if (!TryComp<Z14SupplyDropRuleComponent>(ent, out var comp))
            return;

        comp.ForcedVariant = variant;
        comp.ForcedZone = zone;
        comp.ForcedUser = user;

        GameTicker.StartGameRule(ent);
    }

    private bool RunSequence(Z14SupplyDropRuleComponent comp, EntityUid? zone, Z14SupplyDropVariant? variant, EntityUid? user)
    {
        zone ??= TryPickZone(comp);
        if (!zone.HasValue || !TryComp<Z14SupplyDropZoneComponent>(zone.Value, out var zoneComp))
        {
            Sawmill.Warning("No valid Z14SupplyDropZone found for supply drop");
            return false;
        }

        var zoneXform = Transform(zone.Value);
        var mapCoords = _transform.GetMapCoordinates(zone.Value, zoneXform);
        var mapName = GetMapName(mapCoords.MapId);

        var filter = Filter.Empty().AddInRange(mapCoords, zoneComp.WarningRadius);
        var x = (int) mapCoords.Position.X;
        var y = (int) mapCoords.Position.Y;

        ChatSystem.DispatchFilteredAnnouncement(filter,
            Loc.GetString("z14-supplydrop-warning", ("map", mapName), ("x", x), ("y", y)),
            playSound: false);

        Audio.PlayGlobal(zoneComp.WarningSound, filter, true);

        Timer.Spawn(zoneComp.WarningDelay, () =>
        {
            if (Deleted(zone))
            {
                Sawmill.Warning("Z14SupplyDropZone was deleted before impact");
                return;
            }

            SpawnImpact(zone.Value, zoneComp, comp, variant, user, mapName, x, y);
        });

        return true;
    }

    private void SpawnImpact(EntityUid zone, Z14SupplyDropZoneComponent zoneComp, Z14SupplyDropRuleComponent comp,
        Z14SupplyDropVariant? variant, EntityUid? user, string mapName, int x, int y)
    {
        var zoneXform = Transform(zone);
        var mapCoords = _transform.GetMapCoordinates(zone, zoneXform);

        _explosion.QueueExplosion(mapCoords,
            zoneComp.ExplosionPrototype,
            totalIntensity: 5,
            slope: 1,
            maxTileIntensity: 5,
            cause: zone,
            canCreateVacuum: false,
            addLog: true);

        Audio.PlayPvs(zoneComp.ImpactSound, zoneXform.Coordinates);

        ChatSystem.DispatchFilteredAnnouncement(Filter.Empty().AddInRange(mapCoords, zoneComp.WarningRadius),
            Loc.GetString("z14-supplydrop-impact", ("map", mapName), ("x", x), ("y", y)),
            playSound: false);

        Timer.Spawn(TimeSpan.FromSeconds(0.5), () =>
        {
            if (!Deleted(zone))
                TrySpawnDrop(zone, zoneComp, comp, variant, user);
        });
    }

    private void TrySpawnDrop(EntityUid zone, Z14SupplyDropZoneComponent zoneComp, Z14SupplyDropRuleComponent comp,
        Z14SupplyDropVariant? variant, EntityUid? user)
    {
        var zoneXform = Transform(zone);
        var mapCoords = _transform.GetMapCoordinates(zone, zoneXform);
        var mapId = mapCoords.MapId;
        var rotation = zoneXform.LocalRotation;

        // Resolve vehicle variant
        var allowed = zoneComp.AllowedVariants.Count == 0
            ? comp.VehiclePrototypes.Keys.ToList()
            : zoneComp.AllowedVariants;

        if (allowed.Contains(Z14SupplyDropVariant.Any))
            allowed = comp.VehiclePrototypes.Keys.ToList();

        var validVariants = allowed
            .Where(v => comp.VehiclePrototypes.ContainsKey(v))
            .ToList();

        if (validVariants.Count == 0)
        {
            Sawmill.Warning("Z14SupplyDropZone has no valid vehicle variants");
            return;
        }

        var chosenVariant = variant ?? RobustRandom.Pick(validVariants);
        if (!comp.VehiclePrototypes.TryGetValue(chosenVariant, out var vehicleProto))
        {
            Sawmill.Warning("Chosen vehicle variant {0} has no prototype", chosenVariant);
            return;
        }

        Spawn(vehicleProto, mapCoords, rotation: rotation);

        var cratePos = TryGetValidPosition(mapCoords, zoneComp.CrateMinRadius, zoneComp.CrateMaxRadius, mapId, comp.MaxSpawnRetries);
        if (cratePos == null)
            cratePos = mapCoords;

        var crate = Spawn(comp.CrateProto, cratePos.Value, rotation: rotation);

        if (TryComp<EntityStorageComponent>(crate, out var storage))
        {
            foreach (var item in _entityTable.GetSpawns(PrototypeManager.Index<EntityTablePrototype>(comp.LootTable), RobustRandom.GetRandom()))
            {
                var itemUid = Spawn(item, MapCoordinates.Nullspace);
                _entityStorage.Insert(itemUid, crate, storage);
            }
        }

        var spawns = _entityTable.GetSpawns(PrototypeManager.Index<EntityTablePrototype>(comp.GuardianTable), RobustRandom.GetRandom()).ToList();
        foreach (var guardProto in spawns)
        {
            var pos = TryGetValidPosition(mapCoords, zoneComp.GuardianMinRadius, zoneComp.GuardianMaxRadius, mapId, comp.MaxSpawnRetries);
            if (pos != null)
                Spawn(guardProto, pos.Value);
        }

        if (zoneComp.DeleteAfterSpawn)
            QueueDel(zone);

        var mapName = GetMapName(mapId);
        var userName = user is { } u ? ToPrettyString(u) : "system";
        AdminLogManager.Add(LogType.Z14SupplyDrop, LogImpact.High,
            $"Z14 supply drop ({chosenVariant}) spawned at {mapName} {mapCoords.Position:coords} by {userName:player}");
    }

    private EntityUid? TryPickZone(Z14SupplyDropRuleComponent comp)
    {
        var valid = new List<EntityUid>();
        var query = EntityQueryEnumerator<Z14SupplyDropZoneComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == null)
                continue;

            if (comp.SkipSafeMaps && HasComp<StalkerSafeZoneComponent>(xform.MapUid.Value))
                continue;

            valid.Add(uid);
        }

        if (valid.Count > 0)
            return RobustRandom.Pick(valid);

        if (!comp.AllowFallback)
            return null;

        return TryCreateFallbackZone(comp);
    }

    private EntityUid? TryCreateFallbackZone(Z14SupplyDropRuleComponent comp)
    {
        var stations = _station.GetStationsSet()
            .Where(s => TryComp<StationDataComponent>(s, out _))
            .Where(s => !comp.SkipSafeMaps || !HasComp<StalkerSafeZoneComponent>(Transform(s).MapUid))
            .ToList();

        if (stations.Count == 0)
            return null;

        var station = RobustRandom.Pick(stations);
        var stationData = Comp<StationDataComponent>(station);

        if (!TryFindRandomTileOnStation((station, stationData), out _, out _, out var targetCoords))
            return null;

        var mapCoords = _transform.ToMapCoordinates(targetCoords);
        var zone = Spawn("Z14SupplyDropZone", mapCoords);
        if (TryComp<Z14SupplyDropZoneComponent>(zone, out var zoneComp))
            zoneComp.DeleteAfterSpawn = true;

        return zone;
    }

    private MapCoordinates? TryGetValidPosition(MapCoordinates center, float minRadius, float maxRadius, MapId mapId, int retries)
    {
        for (var i = 0; i < retries; i++)
        {
            var angle = RobustRandom.NextDouble() * Math.Tau;
            var distance = minRadius + RobustRandom.NextDouble() * (maxRadius - minRadius);
            var offset = new Vector2((float) (Math.Cos(angle) * distance), (float) (Math.Sin(angle) * distance));
            var pos = center.Offset(offset);

            if (!_mapManager.TryFindGridAt(mapId, pos.Position, out var gridUid, out var grid))
                continue;

            if (grid == null)
                continue;

            var mapUid = _mapManager.GetMapEntityId(mapId);
            var tile = _mapSystem.CoordinatesToTile(gridUid, grid, pos);

            if (_atmosphere.IsTileSpace(gridUid, mapUid, tile)
                || _atmosphere.IsTileAirBlocked(gridUid, tile, mapGridComp: grid))
            {
                continue;
            }

            return pos;
        }

        return null;
    }

    private string GetMapName(MapId mapId)
    {
        var mapUid = _mapManager.GetMapEntityId(mapId);
        if (TryComp<STMapKeyComponent>(mapUid, out var mapKey))
            return mapKey.Value;

        return MetaData(mapUid).EntityName;
    }
}
