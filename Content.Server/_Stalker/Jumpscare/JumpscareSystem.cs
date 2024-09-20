using Content.Server.Stunnable;
using Content.Shared._Stalker.Jumpscare;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stalker.Jumpscare;

public sealed class JumpscareSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<JumpscareComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            // this update is too large to call it every tick TwT
            if (comp.NextTimeUpdate > _timing.CurTime)
                continue;
            comp.NextTimeUpdate = _timing.CurTime + TimeSpan.FromSeconds(comp.UpdateCooldown);

            if (CheckHumanTarget(uid, comp) is not { } humanTarget)
                continue;

            if (_timing.CurTime > comp.EndTime)
            {
                comp.StartTime = _timing.CurTime;
                comp.EndTime = comp.StartTime + TimeSpan.FromSeconds(comp.ReloadTime + _random.NextFloat(-comp.RandomiseReloadTime, comp.RandomiseReloadTime)) ;

                if (TryComp<MobStateComponent>(uid, out var entityMobState) && _mobState.IsAlive(uid, entityMobState))
                {
                    _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(0.4f), false, 0f, 0f);

                    comp.PreparingStartTime = _timing.CurTime;
                    comp.PreparingEndTime = comp.PreparingStartTime + comp.PreparingReloadTime;
                    comp.OnCoolDown = false;
                }
            }

            if (_timing.CurTime > comp.PreparingEndTime && !comp.OnCoolDown)
            {
                comp.Jumping = true;

                // Set the jump start and end times here
                comp.JumpStartTime = _timing.CurTime;
                comp.JumpEndTime = comp.JumpStartTime + TimeSpan.FromSeconds(0.5f);

                var gomen = _xform.GetWorldPosition(humanTarget) - _xform.GetWorldPosition(uid);
                var length = gomen.Length();

                if (length > comp.AttackDistance)
                {
                    gomen *= comp.AttackDistance / length;
                }

                _throwing.TryThrow(uid, gomen, comp.JumpPower, user: uid, pushbackRatio: 0);
                _stunSystem.TrySlowdown(uid, TimeSpan.FromSeconds(2f), false, 0.5f, 0.5f);

                comp.OnCoolDown = true;
            }

            if (_timing.CurTime > comp.JumpEndTime)
            {
                comp.JumpStartTime = _timing.CurTime;
                comp.JumpEndTime = comp.JumpStartTime + TimeSpan.FromSeconds(0.5f);
                comp.Jumping = false;
            }

            if (!comp.Jumping)
                continue;

            foreach (var target in _lookup.GetEntitiesInRange(uid, comp.DamageRadius))
            {
                if (target == uid || !HasComp<HumanoidAppearanceComponent>(target) ||
                    HasComp<JumpscareResistantComponent>(target))
                    continue;

                EnsureComp<JumpscareResistantComponent>(target);
                _damage.TryChangeDamage(target, comp.ChargeDamage);
                if (comp.StaminaDamage > 0)
                    _stamina.TakeStaminaDamage(target, comp.StaminaDamage);
            }
        }
    }

    private EntityUid? CheckHumanTarget(EntityUid uid, JumpscareComponent component)
    {
        var closestDistance = float.MaxValue;
        EntityUid? target = null;

        // creating new hashset and filling it with entities to iterate through
        var entities = new HashSet<Entity<MobStateComponent>>();
        var xform = Transform(uid);
        var mapCoords = _xform.ToMapCoordinates(xform.Coordinates);
        // we'll iterate only through dynamic objects, cause we don't need to check non-alive objects
        _lookup.GetEntitiesInRange(mapCoords, component.AttackRadius, entities,
            LookupFlags.Dynamic);
        foreach (var entity in entities)
        {
            if (!HasComp<HumanoidAppearanceComponent>(entity))
                continue;

            if (!_mobState.IsAlive(entity, entity.Comp))
                continue;

            var dist = (_xform.GetWorldPosition(uid) - _xform.GetWorldPosition(entity)).Length();
            if (dist >= closestDistance)
                continue;

            target = entity;
            closestDistance = dist;
        }

        return target;
    }
}
