using System.Numerics;
using Content.Server._Stalker.StationEvents.Components;
using Content.Shared._Stalker_EN.Emission;
using Content.Shared.Atmos.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Robust.Shared.Map;

namespace Content.Server._Zona14.Airborne;

/// <summary>
///     Applies airborne (inhaled) hazards — the STALKER "the air itself will hurt you" layer: radioactive
///     particulate, burning ash from fire anomalies, poison/acid gas from chemical anomalies, and the global
///     dose during an emission. It is a channel distinct from the penetrating radiation that armor gates:
///     airborne damage is reduced ONLY by an equipped gas-mask filter (<see cref="GasMaskFilterComponent"/>),
///     never by armor, and the filter drains while it protects.
///
///     Sources:
///     <list type="bullet">
///       <item>Emissions/blowouts — a global hazard while <c>EmissionStateChangedEvent.IsActive</c>.</item>
///       <item>Any entity with <see cref="AirborneHazardComponent"/> — a contaminated map (radius 0),
///             a mapper-placed contaminated pocket, or an anomaly's gas cloud (positive radius).</item>
///     </list>
///     Only <c>BlowoutTargetComponent</c> mobs are affected, and never inside a <c>StalkerSafeZoneComponent</c>.
///     All airborne damage hitting a mob in a tick is summed first, so its filter drains once regardless of
///     how many overlapping sources touch it.
/// </summary>
public sealed class AirborneHazardSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>Slot id of the gas-mask filter socket on <c>Z14GasMaskBase</c>.</summary>
    public const string FilterSlotId = "gas_filter";

    private const float TickInterval = 1f;

    /// <summary>Airborne radiation (rads/second) inflicted on unprotected mobs anywhere during an active emission.</summary>
    private const float EmissionRadsPerSecond = 12f;

    private float _accumulator;
    private bool _emissionActive;
    private DamageSpecifier _emissionDamage = new();

    // Reused each tick to sum airborne damage per mob before applying (so filters drain once per tick).
    private readonly Dictionary<EntityUid, DamageSpecifier> _perMob = new();

    // Per-map target index, rebuilt each tick (buckets reused) so sources only scan their own map's mobs.
    private readonly Dictionary<MapId, List<TargetInfo>> _targetsByMap = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmissionStateChangedEvent>(OnEmissionState);

        _emissionDamage = new DamageSpecifier();
        _emissionDamage.DamageDict["Radiation"] = FixedPoint2.New(EmissionRadsPerSecond);
    }

    private void OnEmissionState(ref EmissionStateChangedEvent args)
    {
        _emissionActive = args.IsActive;
    }

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < TickInterval)
            return;

        var dt = _accumulator;
        _accumulator = 0f;

        _perMob.Clear();

        // Collect eligible targets ONCE per tick, grouped by map, so each source only scans its own map's
        // mobs and we never re-enumerate the whole mob set per source. Early-out when nobody can be dosed —
        // on a server with hundreds of anomalies but empty maps this makes the whole system almost free.
        if (BuildTargetIndex() == 0)
            return;

        // Emission: a global airborne dose everywhere that isn't a safe zone.
        if (_emissionActive)
        {
            foreach (var list in _targetsByMap.Values)
                foreach (var target in list)
                    Accumulate(target.Uid, _emissionDamage);
        }

        // Source-based hazards: contaminated maps (radius 0) and area pockets / anomaly gas (positive radius).
        var sources = EntityQueryEnumerator<AirborneHazardComponent, TransformComponent>();
        while (sources.MoveNext(out _, out var hazard, out var srcXform))
        {
            if (!hazard.Enabled || hazard.Damage.Empty)
                continue;

            var srcMap = srcXform.MapID;
            if (srcMap == MapId.Nullspace || !_targetsByMap.TryGetValue(srcMap, out var list))
                continue; // no mobs on this source's map — skip entirely

            if (hazard.Radius <= 0f)
            {
                foreach (var target in list)
                    Accumulate(target.Uid, hazard.Damage);
            }
            else
            {
                var srcPos = _transform.GetWorldPosition(srcXform);
                var r2 = hazard.Radius * hazard.Radius;
                foreach (var target in list)
                {
                    if (Vector2.DistanceSquared(srcPos, target.Position) <= r2)
                        Accumulate(target.Uid, hazard.Damage);
                }
            }
        }

        // Apply the summed dose to each affected mob, draining its filter a single time.
        foreach (var (mob, perSecond) in _perMob)
            ApplyAirborne(mob, perSecond, dt);
    }

    /// <summary>Rebuilds the per-map target index; buckets are reused across ticks. Returns the total target count.</summary>
    private int BuildTargetIndex()
    {
        foreach (var list in _targetsByMap.Values)
            list.Clear();

        var total = 0;
        var targets = EntityQueryEnumerator<BlowoutTargetComponent, TransformComponent>();
        while (targets.MoveNext(out var uid, out _, out var xform))
        {
            var map = xform.MapID;
            if (map == MapId.Nullspace)
                continue;
            if (IsSafe(uid, xform))
                continue;

            if (!_targetsByMap.TryGetValue(map, out var list))
            {
                list = new List<TargetInfo>();
                _targetsByMap[map] = list;
            }

            list.Add(new TargetInfo(uid, _transform.GetWorldPosition(xform)));
            total++;
        }

        return total;
    }

    private readonly record struct TargetInfo(EntityUid Uid, Vector2 Position);

    private void Accumulate(EntityUid mob, DamageSpecifier perSecond)
    {
        // Take the MAX per damage type across all overlapping sources, NOT the sum — standing where 3-4
        // anomalies overlap should be as bad as the worst one, not 3-4x. Different damage types still coexist.
        if (!_perMob.TryGetValue(mob, out var existing))
        {
            _perMob[mob] = new DamageSpecifier(perSecond);
            return;
        }

        foreach (var (type, amount) in perSecond.DamageDict)
        {
            if (!existing.DamageDict.TryGetValue(type, out var current) || amount > current)
                existing.DamageDict[type] = amount;
        }
    }

    /// <summary>
    ///     Applies one tick of summed airborne damage to a mob, reduced by an equipped gas-mask filter with
    ///     charge (armor is intentionally ignored — the dose is inhaled). Drains the filter while it protects.
    /// </summary>
    private void ApplyAirborne(EntityUid mob, DamageSpecifier perSecond, float dt)
    {
        var protection = GetFilterProtectionAndDrain(mob, dt);

        var factor = dt * (1f - protection);
        if (factor <= 0f)
            return;

        var dose = perSecond * FixedPoint2.New(factor);
        if (dose.Empty)
            return;

        _damageable.TryChangeDamage(mob, dose, ignoreResistances: true, interruptsDoAfters: false);
    }

    /// <summary>
    ///     Returns the fraction (0-1) of airborne damage blocked by the mob's installed gas-mask filter, and
    ///     drains that filter for this tick. Zero if there is no mask, no filter installed, or the filter is spent.
    /// </summary>
    private float GetFilterProtectionAndDrain(EntityUid mob, float dt)
    {
        // Closed-cycle sealed suit (SEVA / scientist) = full airborne immunity, no gas-mask filter needed —
        // but ONLY when fully sealed: the suit worn AND its helmet deployed (a sealed hardsuit helmet with a
        // breath tool in the head slot). Helmet off = not closed = you breathe the air like anyone else.
        if (_inventory.TryGetSlotEntity(mob, "outerClothing", out var suit)
            && TryComp<Z14SealedSuitComponent>(suit, out var sealedSuit)
            && sealedSuit.Enabled
            && _inventory.TryGetSlotEntity(mob, "head", out var helmet)
            && HasComp<BreathToolComponent>(helmet))
            return 1f;

        if (!_inventory.TryGetSlotEntity(mob, "mask", out var mask))
            return 0f;

        if (!_itemSlots.TryGetSlot(mask.Value, FilterSlotId, out var slot))
            return 0f;

        if (slot.Item is not { } filterUid
            || !TryComp<GasMaskFilterComponent>(filterUid, out var filter)
            || filter.Charge <= 0f)
            return 0f;

        var protection = Math.Clamp(filter.Protection, 0f, 1f);
        filter.Charge = Math.Max(0f, filter.Charge - dt * filter.DrainPerSecond);
        return protection;
    }

    private bool IsSafe(EntityUid uid, TransformComponent xform)
    {
        if (HasComp<StalkerSafeZoneComponent>(uid))
            return true;

        var mapUid = xform.MapUid;
        return mapUid != null && HasComp<StalkerSafeZoneComponent>(mapUid.Value);
    }
}
