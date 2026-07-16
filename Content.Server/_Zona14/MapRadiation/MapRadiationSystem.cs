// SPDX-License-Identifier: MIT

using System.Numerics;
using Content.Server._Stalker.StationEvents.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server._Zona14.MapRadiation;

public sealed partial class MapRadiationSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<MapRadiationComponent, MapComponent>();
        while (query.MoveNext(out var mapUid, out var mapRad, out var mapComp))
        {
            if (!mapRad.Enabled || mapRad.Interval <= 0 || curTime < mapRad.NextDamageTime)
                continue;

            mapRad.NextDamageTime = curTime + TimeSpan.FromSeconds(mapRad.Interval);
            ApplyRadiation(mapUid, mapRad, mapComp.MapId);
        }
    }

    private void ApplyRadiation(EntityUid mapUid, MapRadiationComponent mapRad, MapId mapId)
    {
        if (mapRad.Damage.Empty)
            return;

        if (HasComp<StalkerSafeZoneComponent>(mapUid))
            return;

        var targets = EntityQueryEnumerator<BlowoutTargetComponent, DamageableComponent, TransformComponent>();
        while (targets.MoveNext(out var target, out _, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            if (HasComp<StalkerSafeZoneComponent>(target))
                continue;

            if (IsProtectedByBlocker(target, xform, mapId))
                continue;

            _damageable.TryChangeDamage(target, mapRad.Damage, interruptsDoAfters: false);
        }
    }

    private bool IsProtectedByBlocker(EntityUid target, TransformComponent xform, MapId mapId)
    {
        var targetPos = _transform.GetWorldPosition(xform);

        var blockers = EntityQueryEnumerator<MapRadiationBlockerComponent, TransformComponent>();
        while (blockers.MoveNext(out _, out var blocker, out var blockerXform))
        {
            if (!blocker.Enabled || blockerXform.MapID != mapId)
                continue;

            var blockerPos = _transform.GetWorldPosition(blockerXform);
            if (Vector2.Distance(targetPos, blockerPos) <= blocker.Radius)
                return true;
        }

        return false;
    }

    public float GetAmbientRadiation(EntityUid entity)
    {
        return GetAmbientRadiation(entity, "Radiation");
    }

    public float GetAmbientRadiation(EntityUid entity, string damageType)
    {
        if (!TryComp<TransformComponent>(entity, out var xform))
            return 0f;

        var mapId = xform.MapID;
        if (mapId == MapId.Nullspace)
            return 0f;

        var mapUid = _mapManager.GetMapEntityId(mapId);
        if (!TryComp<MapRadiationComponent>(mapUid, out var mapRad) || !mapRad.Enabled || mapRad.Interval <= 0)
            return 0f;

        if (HasComp<StalkerSafeZoneComponent>(mapUid) || HasComp<StalkerSafeZoneComponent>(entity))
            return 0f;

        if (IsProtectedByBlocker(entity, xform, mapId))
            return 0f;

        if (!mapRad.Damage.DamageDict.TryGetValue(damageType, out var damage))
            return 0f;

        return damage.Float() / mapRad.Interval;
    }
}
