using Content.Server.Administration.Logs;
using Content.Server.Destructible;
using Content.Server.Effects;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Armor;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Projectiles;

public sealed class ProjectileSystem : SharedProjectileSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;
    [Dependency] private readonly GunSystem _guns = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _sharedCameraRecoil = default!;
    [Dependency] private readonly InventorySystem _inventory = default!; // Stalker-Changes
    [Dependency] private readonly IPrototypeManager _prototype = default!; // Stalker-Changes
    // Zona14: runtime-tunable penetration CVars
    [Dependency] private readonly IConfigurationManager _config = default!;
    // End Zona14

    // Zona14: cached CVar values for tier penetration + damage floor
    private float _penTierBelow;
    private float _penTierMatch;
    private float _penTierAboveOne;
    private float _penTierAboveTwo;
    private float _minDamageFloor;
    // End Zona14

    // Zona14: OnStartCollide subscription and the OnStartCollide guards now live in
    //         SharedProjectileSystem so the client-side predicted twin routes through the same
    //         entry point. ProjectileCollide is an override of the shared method.

    // Zona14: subscribe to penetration + floor CVars
    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_config, CCVars.PlaytestPenTierBelow, value => _penTierBelow = value, true);
        Subs.CVar(_config, CCVars.PlaytestPenTierMatch, value => _penTierMatch = value, true);
        Subs.CVar(_config, CCVars.PlaytestPenTierAboveOne, value => _penTierAboveOne = value, true);
        Subs.CVar(_config, CCVars.PlaytestPenTierAboveTwo, value => _penTierAboveTwo = value, true);
        Subs.CVar(_config, CCVars.PlaytestMinProjectileDamageFloor, value => _minDamageFloor = value, true);
    }
    // End Zona14

    // Zona14: server override of SharedProjectileSystem.ProjectileCollide. Preserves the
    //         stalker-fork armor / ignoreResistors / penetration logic byte-for-byte. When
    //         `predicted` is true, the firer's client has already drawn the hit effect / shake
    //         / impact broadcast, so we skip those to avoid double-rendering.
    public override void ProjectileCollide(Entity<ProjectileComponent, PhysicsComponent> projectile,
        EntityUid target, bool predicted = false)
    {
        var (uid, component, ourBody) = projectile;

        // Zona14: 5-tier armor penetration + damage floor
        var ignore = BuildPenetrationDict(target, component.ProjectileClass);

        var ignoreResitance = false;
        if (TryComp<DamageableComponent>(target, out var damageable) && damageable.DamageModifierSetId != null)
            if (_prototype.TryIndex(damageable.DamageModifierSetId, out var damageModifierSetPrototype))
                ignoreResitance = component.ProjectileClass >= damageModifierSetPrototype.Class;
        // stalker-changes-end

        // it's here so this check is only done once before possible hit
        var attemptEv = new ProjectileReflectAttemptEvent(uid, component, false);
        RaiseLocalEvent(target, ref attemptEv);
        if (attemptEv.Cancelled)
        {
            SetShooter(uid, component, target);
            return;
        }

        var ev = new ProjectileHitEvent(component.Damage * _damageableSystem.UniversalProjectileDamageModifier, target, component.Shooter);
        RaiseLocalEvent(uid, ref ev);

        var otherName = ToPrettyString(target);
        var damageRequired = _destructibleSystem.DestroyedAt(target);
        if (TryComp<DamageableComponent>(target, out var damageableComponent))
        {
            damageRequired -= damageableComponent.TotalDamage;
            damageRequired = FixedPoint2.Max(damageRequired, FixedPoint2.Zero);
        }
        var deleted = Deleted(target);

        var damageApplied = _damageableSystem.TryChangeDamage((target, damageableComponent), ev.Damage, out var damage, component.IgnoreResistances || ignoreResitance, origin: component.Shooter, ignoreResistors: ignore); // Stalker-Changes-IgnoreResistors
        if (damageApplied && Exists(component.Shooter))
        {
            if (!deleted && !predicted)
            {
                _color.RaiseEffect(Color.Red, new List<EntityUid> { target }, Filter.Pvs(target, entityManager: EntityManager));
            }

            _adminLogger.Add(LogType.BulletHit,
                LogImpact.Medium,
                $"Projectile {ToPrettyString(uid):projectile} shot by {ToPrettyString(component.Shooter!.Value):user} hit {otherName:target} and dealt {damage:damage} damage");

            EnforceMinimumDamageFloor(target, damageableComponent, ev.Damage, damage, component.Shooter); // Zona14

            TryPhysicsPenetrate(component, damage, damageRequired);
        }
        else
        {
            // Zona14: damage didn't apply (no DamageableComponent on target, damage spec rejected,
            // or shooter de-spawned mid-flight). The bullet still physically struck a hard fixture
            // — without this, ProjectileSpent stays false and the DeleteOnCollide check below
            // never QueueDels the projectile, so it sits stuck against the target. Penetration
            // tracking depends on `damage` info we don't have in this branch, so just mark spent.
            component.ProjectileSpent = true;
        }

        if (!deleted)
        {
            _guns.PlayImpactSound(target, damage, component.SoundHit, component.ForceSound);

            if (!predicted && !ourBody.LinearVelocity.IsLengthZero())
                _sharedCameraRecoil.KickCamera(target, ourBody.LinearVelocity.Normalized());
        }

        if (component.DeleteOnCollide && component.ProjectileSpent)
            QueueDel(uid);

        if (!predicted && component.ImpactEffect != null && TryComp(uid, out TransformComponent? xform))
        {
            var filter = Filter.Pvs(xform.Coordinates, entityMan: EntityManager);
            // Zona14: exclude the shooter — their client twin already raised a local
            // ImpactEffectEvent via SharedProjectileSystem.ProjectileCollide's IsClientSide
            // branch. Without this, the shooter sees two impact effects: one immediate from
            // the predicted twin, one a frame or two later from the server broadcast.
            if (component.Shooter is { } shooter && TryComp(shooter, out ActorComponent? actor))
                filter = filter.RemovePlayer(actor.PlayerSession);
            RaiseNetworkEvent(new ImpactEffectEvent(component.ImpactEffect, GetNetCoordinates(xform.Coordinates)), filter);
        }
    }

    // Zona14: extracted penetration, damage floor, and tier lookup methods
    private void TryPhysicsPenetrate(ProjectileComponent component, DamageSpecifier? dealtDamage, FixedPoint2 damageRequired)
    {
        if (dealtDamage == null)
        {
            component.ProjectileSpent = true;
            return;
        }

        if (component.PenetrationThreshold != 0)
        {
            if (component.PenetrationDamageTypeRequirement != null)
            {
                var stopPenetration = false;
                foreach (var requiredDamageType in component.PenetrationDamageTypeRequirement)
                {
                    if (!dealtDamage.DamageDict.Keys.Contains(requiredDamageType))
                    {
                        stopPenetration = true;
                        break;
                    }
                }
                if (stopPenetration)
                    component.ProjectileSpent = true;
            }

            if (dealtDamage.GetTotal() < damageRequired)
            {
                component.ProjectileSpent = true;
            }

            if (!component.ProjectileSpent)
            {
                component.PenetrationAmount += damageRequired;
                if (component.PenetrationAmount >= component.PenetrationThreshold)
                {
                    component.ProjectileSpent = true;
                }
            }
        }
        else
        {
            component.ProjectileSpent = true;
        }
    }

    private void EnforceMinimumDamageFloor(
        EntityUid target,
        DamageableComponent? damageableComponent,
        DamageSpecifier originalDamage,
        DamageSpecifier? dealtDamage,
        EntityUid? shooter)
    {
        if (damageableComponent == null || dealtDamage == null)
            return;

        var originalEffective = 0f;
        foreach (var (type, value) in originalDamage.DamageDict)
        {
            if (damageableComponent.Damage.DamageDict.ContainsKey(type))
                originalEffective += (float) value;
        }

        var dealtTotal = (float) dealtDamage.GetTotal();
        var minimumTotal = originalEffective * _minDamageFloor;

        if (originalEffective > 0f && dealtTotal < minimumTotal)
        {
            var supplement = new DamageSpecifier();
            supplement.DamageDict["Blunt"] = (FixedPoint2) Math.Max(0f, minimumTotal - dealtTotal);
            _damageableSystem.TryChangeDamage((target, damageableComponent), supplement, true, false, origin: shooter);
        }
    }

    private Dictionary<EntityUid, float> BuildPenetrationDict(EntityUid target, int? projectileClass)
    {
        var ignore = new Dictionary<EntityUid, float>();
        string[] slots =
        {
            "outerClothing",
            "head",
            "cloak",
            "eyes",
            "ears",
            "mask",
            "jumpsuit",
            "neck",
            "back",
            "belt",
            "gloves",
            "shoes",
            "id",
            "legs",
            "torso"
        };

        foreach (var slot in slots)
        {
            if (_inventory.TryGetSlotEntity(target, slot, out var entity)
                && TryComp<ArmorComponent>(entity, out var armorComp)
                && armorComp.ArmorClass.HasValue)
            {
                var classDiff = (projectileClass ?? 0) - armorComp.ArmorClass.Value;
                var penetration = classDiff switch
                {
                    <= -1 => _penTierBelow,
                    0 => _penTierMatch,
                    1 => _penTierAboveOne,
                    _ => _penTierAboveTwo
                };
                if (penetration > 0f)
                    ignore[entity.Value] = penetration;
            }
        }

        return ignore;
    }
    // End Zona14
}
