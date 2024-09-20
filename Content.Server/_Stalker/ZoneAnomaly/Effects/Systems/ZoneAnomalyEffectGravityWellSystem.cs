using System.Numerics;
using Content.Shared._Stalker.ZoneAnomaly;
using Content.Shared._Stalker.ZoneAnomaly.Components;
using Content.Shared._Stalker.ZoneAnomaly.Effects.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Server._Stalker.ZoneAnomaly.Effects.Systems;

public sealed class ZoneAnomalyEffectGravityWellSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ZoneAnomalyEffectGravityWellComponent, ZoneAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var effect, out var anomaly, out var transform))
        {
            if (anomaly.State != ZoneAnomalyState.Activated)
                continue;

            if (effect.PeriodTime > _timing.CurTime)
                continue;

            GravPulse((uid, effect));
            effect.PeriodTime = _timing.CurTime + effect.Period;
        }
    }

    private void GravPulse(Entity<ZoneAnomalyEffectGravityWellComponent> effect)
    {
        var targets = _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(effect), effect.Comp.Distance);
        var radial = effect.Comp.Radial;
        var tangential = effect.Comp.Tangential;

        var baseMatrixDeltaV = new Matrix3x2(radial, tangential, 0.0f, -tangential, radial, 0.0f);
        var epicenter = _transform.GetMapCoordinates(effect).Position;
        var bodyQuery = GetEntityQuery<PhysicsComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        foreach(var entity in targets)
        {
            if (effect.Comp.Whitelist is { } whitelist && !_whitelistSystem.IsWhitelistPass(whitelist, entity))
                continue;

            if (!bodyQuery.TryGetComponent(entity, out var physics) || physics.BodyType == BodyType.Static)
                continue;

            var displacement = epicenter - _transform.GetWorldPosition(entity, xformQuery);
            var scaling = GetScaling(effect, displacement.LengthSquared()) * physics.Mass;

            //_physics.ApplyLinearImpulse(entity, displacement.Normalized() * baseMatrixDeltaV * scaling, body: physics); // ST-TODO: needs to check math
        }
    }

    private float GetScaling(Entity<ZoneAnomalyEffectGravityWellComponent> effect, float distance)
    {
        return effect.Comp.Gradient switch
        {
            ZoneAnomalyEffectGravityWellGradient.Default => 1 / distance,
            ZoneAnomalyEffectGravityWellGradient.Liner => distance / effect.Comp.Distance,
            ZoneAnomalyEffectGravityWellGradient.ReversedLiner => 1f / (distance / effect.Comp.Distance),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
