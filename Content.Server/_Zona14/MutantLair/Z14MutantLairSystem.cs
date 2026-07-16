// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Server._Stalker.StationEvents.Components;
using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Shared._Zona14.MutantLair;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Zona14.MutantLair;

/// <summary>
/// Drives <see cref="Z14MutantLairComponent"/>: periodic mutant spawns, tracking, cleanup, and reward drops.
/// </summary>
public sealed class Z14MutantLairSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Z14MutantLairComponent, DamageThresholdReached>(OnDamageThresholdReached);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Z14MutantLairComponent>();
        while (query.MoveNext(out var uid, out var lair))
        {
            if (lair.SpawnsDone >= lair.MaxSpawns)
                continue;

            CleanupDeadMutants(lair);

            if (lair.SpawnedMutants.Count >= lair.MaxMutants)
                continue;

            if (lair.NextSpawnTime == TimeSpan.Zero)
                lair.NextSpawnTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(1f, lair.SpawnInterval));

            if (_timing.CurTime < lair.NextSpawnTime)
                continue;

            lair.NextSpawnTime = _timing.CurTime + TimeSpan.FromSeconds(lair.SpawnInterval);

            if (!TrySpawnMutant(uid, lair))
                continue;

            lair.SpawnsDone++;
        }
    }

    private void OnDamageThresholdReached(EntityUid uid, Z14MutantLairComponent lair, DamageThresholdReached args)
    {
        if (lair.RewardDropped)
            return;

        foreach (var behavior in args.Threshold.Behaviors)
        {
            if (behavior is not DoActsBehavior actBehavior || !actBehavior.HasAct(ThresholdActs.Destruction))
                continue;

            lair.RewardDropped = true;
            _adminLog.Add(LogType.Z14MutantLair, LogImpact.High,
                $"Lair {uid:lair} destroyed at {Transform(uid).MapPosition:coords}; {lair.RewardCount} reward(s) dropped");
            DropReward(uid, lair);
            StopSpawning(lair);
            return;
        }
    }

    /// <summary>
    /// Spawns a mutant near the lair and registers it as a lair spawn.
    /// </summary>
    private bool TrySpawnMutant(EntityUid lairUid, Z14MutantLairComponent lair)
    {
        if (lair.MutantPrototypes.Count == 0)
            return false;

        if (!TryFindValidTile(lairUid, lair, out var spawnCoords))
            return false;

        var proto = _random.Pick(lair.MutantPrototypes);
        var mob = Spawn(proto, spawnCoords);

        // Lair mutants are blowout targets so emissions and map radiation affect them.
        EnsureComp<BlowoutTargetComponent>(mob);

        lair.SpawnedMutants.Add(mob);

        _adminLog.Add(LogType.Z14MutantLair, LogImpact.Low,
            $"Lair {lairUid:lair} spawned {proto} at {_xforms.ToMapCoordinates(spawnCoords).Position:coords}");

        return true;
    }

    /// <summary>
    /// Tries to find a valid, non-blocked tile within the lair's spawn radius.
    /// </summary>
    private bool TryFindValidTile(EntityUid lairUid, Z14MutantLairComponent lair, out EntityCoordinates coords)
    {
        coords = EntityCoordinates.Invalid;
        var xform = Transform(lairUid);
        var origin = xform.Coordinates;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var angle = _random.NextFloat(0f, MathF.Tau);
            var distance = _random.NextFloat(0.5f, lair.SpawnRadius);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            var candidate = origin.Offset(offset);

            var tile = _turf.GetTileRef(candidate);
            if (tile == null)
                continue;

            if (_turf.IsTileBlocked(tile.Value, CollisionGroup.MobMask))
                continue;

            // Safe-zone guard: do not spawn inside a safe zone.
            var gridUid = candidate.GetGridUid(EntityManager);
            if (gridUid is { } grid && HasComp<StalkerSafeZoneComponent>(grid))
                continue;

            var mapUid = _mapManager.GetMapEntityId(xform.MapID);
            if (HasComp<StalkerSafeZoneComponent>(mapUid))
                continue;

            coords = candidate;
            return true;
        }

        return false;
    }

    private void CleanupDeadMutants(Z14MutantLairComponent lair)
    {
        for (var i = lair.SpawnedMutants.Count - 1; i >= 0; i--)
        {
            var mob = lair.SpawnedMutants[i];
            if (!EntityManager.EntityExists(mob) || _mobState.IsDead(mob))
                lair.SpawnedMutants.RemoveAt(i);
        }
    }

    private void DropReward(EntityUid lairUid, Z14MutantLairComponent lair)
    {
        if (lair.RewardPrototypes.Count == 0)
            return;

        var xform = Transform(lairUid);
        for (var i = 0; i < lair.RewardCount; i++)
        {
            var proto = _random.Pick(lair.RewardPrototypes);
            var coords = xform.Coordinates.Offset(new Vector2(_random.NextFloat(-0.5f, 0.5f), _random.NextFloat(-0.5f, 0.5f)));
            Spawn(proto, coords);
        }
    }

    private void StopSpawning(Z14MutantLairComponent lair)
    {
        lair.SpawnsDone = lair.MaxSpawns;
        lair.SpawnedMutants.Clear();
    }
}
