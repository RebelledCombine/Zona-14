using Content.Server.Mind;
using Content.Shared._Stalker.Stagger;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Robust.Server.GameObjects;

namespace Content.Server._Stalker.Stagger;

// Zona14: server computes the stagger slowdown scalar (needs mind/lookup data) and writes it to the
// networked StaggerComponent. The modifier is applied in the shared SharedStaggerSystem so movement
// prediction stays in sync with the server.
public sealed class StaggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public const float UpdateDelay = 0.7f;
    private float _updateTime = 0;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateTime += frameTime;
        if (_updateTime < UpdateDelay)
            return;

        _updateTime -= UpdateDelay;


        var query = EntityQueryEnumerator<StaggerComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stagger, out var mobState, out var xform))
        {
            if (stagger.NetUserId is null)
            {
                if (!_mind.TryGetMind(uid, out _, out var mind))
                    continue;

                stagger.NetUserId = mind.UserId;
                continue;
            }

            if (mobState.CurrentState != MobState.Alive)
                continue;

            var closestDistance = float.MaxValue;
            var near = _entityLookup.GetEntitiesInRange<StaggerComponent>(xform.Coordinates, stagger.SlownessDistanceMax);
            var finded = false;
            foreach (var entity in near)
            {
                if (entity.Comp.NetUserId != stagger.NetUserId)
                    continue;

                if (!_mobState.IsDead(entity))
                    continue;

                var dist = (_transform.GetWorldPosition(xform) - _transform.GetWorldPosition(entity)).Length();
                if (dist >= closestDistance)
                    continue;

                closestDistance = dist;
                finded = true;
            }

            var newModifier = finded
                ? Math.Min(stagger.SlownessDistanceMax, closestDistance + stagger.SlownessDistanceMin) / stagger.SlownessDistanceMax
                : 1f;

            if (MathHelper.CloseTo(newModifier, stagger.MovementSpeedModifier))
                continue;

            stagger.MovementSpeedModifier = newModifier;
            Dirty(uid, stagger);
            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        }
    }
}
