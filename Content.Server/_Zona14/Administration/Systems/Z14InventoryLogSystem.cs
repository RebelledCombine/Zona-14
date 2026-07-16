// SPDX-License-Identifier: MIT

using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Server._Zona14.Administration.Systems;

/// <summary>
/// Logs inventory equip and unequip events for player-owned entities to admin logs.
/// </summary>
public sealed class Z14InventoryLogSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<InventoryComponent, DidUnequipEvent>(OnDidUnequip);
    }

    private void OnDidEquip(EntityUid uid, InventoryComponent component, DidEquipEvent args)
    {
        var equipee = ToPrettyString(uid);
        if (equipee.Session is null)
            return;

        _adminLog.Add(
            LogType.Z14Inventory,
            LogImpact.Low,
            $"{equipee:player} equipped {args.Equipment:item} to {args.Slot}");
    }

    private void OnDidUnequip(EntityUid uid, InventoryComponent component, DidUnequipEvent args)
    {
        var equipee = ToPrettyString(uid);
        if (equipee.Session is null)
            return;

        _adminLog.Add(
            LogType.Z14Inventory,
            LogImpact.Low,
            $"{equipee:player} unequipped {args.Equipment:item} from {args.Slot}");
    }
}
