using Content.Shared._Zona14.Airborne;
using Content.Shared.Alert;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server._Zona14.Airborne;

/// <summary>
///     Zona14: surfaces a worn gas mask's filter charge to the player — a HUD alert that appears once the
///     filter runs low and escalates to spent, plus a <see cref="Z14GasFilterVisuals.ChargeLevel"/> appearance
///     tier on the mask so an on-icon deterioration overlay can be wired in later (needs sprites). Purely
///     informational; the filter is actually drained by <see cref="AirborneHazardSystem"/>.
/// </summary>
public sealed class Z14GasFilterIndicatorSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    private static readonly ProtoId<AlertPrototype> FilterAlert = "Z14GasFilter";

    private const float UpdateInterval = 1f;
    private float _accumulator;

    public override void Update(float frameTime)
    {
        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;
        _accumulator = 0f;

        var query = EntityQueryEnumerator<AlertsComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!TryGetFilter(uid, out var mask, out var filter) || filter.MaxCharge <= 0f)
            {
                _alerts.ClearAlert(uid, FilterAlert);
                continue;
            }

            var frac = Math.Clamp(filter.Charge / filter.MaxCharge, 0f, 1f);

            // Appearance tier for a future on-mask overlay: 0 healthy, 1 worn, 2 low, 3 spent.
            var level = frac <= 0f ? 3 : frac < 0.25f ? 2 : frac < 0.6f ? 1 : 0;
            _appearance.SetData(mask, Z14GasFilterVisuals.ChargeLevel, level);

            // Always show the gauge while a filter is installed, so its remaining charge is visible at a glance
            // (healthy -> low -> spent). It clears only when there's no mask/filter (handled above).
            var severity = frac > 0.6f ? (short) 0 : frac > 0.25f ? (short) 1 : (short) 2;
            _alerts.ShowAlert(uid, FilterAlert, severity);
        }
    }

    private bool TryGetFilter(EntityUid wearer, out EntityUid mask, out GasMaskFilterComponent filter)
    {
        mask = default;
        filter = default!;

        if (!_inventory.TryGetSlotEntity(wearer, "mask", out var maskUid))
            return false;
        if (!_itemSlots.TryGetSlot(maskUid.Value, AirborneHazardSystem.FilterSlotId, out var slot))
            return false;
        if (slot.Item is not { } filterUid || !TryComp(filterUid, out GasMaskFilterComponent? comp))
            return false;

        mask = maskUid.Value;
        filter = comp;
        return true;
    }
}
