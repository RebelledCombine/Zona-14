using Content.Shared._Stalker.ZoneAnomaly;
using Content.Shared._Stalker.ZoneAnomaly.Components;
using Content.Shared._Stalker.ZoneAnomaly.Effects.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
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

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ZoneAnomalyEffectGravityWellComponent, ZoneAnomalyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var effect, out var anomaly, out _))
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
        foreach(var entity in targets)
        {
            if (effect.Comp.Whitelist is { } whitelist && !_whitelistSystem.IsWhitelistPass(whitelist, entity))
                continue;

            if (!_physicsQuery.TryGetComponent(entity, out var physics) || physics.BodyType == BodyType.Static)
                continue;

            var center = _transform.GetMapCoordinates(effect).Position;
            var delta = center - _transform.GetWorldPosition(entity, _transformQuery);

            var scaling = GetScaling(effect, delta.LengthSquared()) * physics.Mass;

            _physics.ApplyLinearImpulse(entity, delta.Normalized() * scaling * effect.Comp.Radial, body: physics);
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
