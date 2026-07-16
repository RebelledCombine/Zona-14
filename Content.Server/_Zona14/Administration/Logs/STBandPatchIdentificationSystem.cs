// SPDX-License-Identifier: MIT

using System.Text;
using Content.Server.Administration.Logs;
using Content.Shared._Stalker.Bands;
using Content.Shared._Zona14.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Zona14.Administration.Logs;

/// <summary>
/// Logs band-patch identifications with the armor both players were wearing at the time.
/// </summary>
public sealed class STBandPatchIdentificationSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private static readonly string[] ArmorSlots =
    {
        "outerClothing",
        "torso",
        "legs",
        "cloak",
        "head",
        "mask",
        "gloves",
        "shoes",
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<STBandPatchIdentifiedEvent>(OnIdentified);
    }

    private void OnIdentified(STBandPatchIdentifiedEvent ev, EntitySessionEventArgs args)
    {
        var identifier = GetEntity(ev.Identifier);
        var target = GetEntity(ev.Target);

        if (!Exists(identifier) || !Exists(target))
            return;

        // Verify the sender is the identifier.
        if (args.SenderSession.AttachedEntity != identifier)
            return;

        if (!TryComp<BandsComponent>(target, out var targetBand))
            return;

        // Sanity-check distance to avoid spoofed events.
        if ((_transform.GetWorldPosition(identifier) - _transform.GetWorldPosition(target)).Length() > 7f)
            return;

        var idArmor = GetArmorSummary(identifier);
        var targetArmor = GetArmorSummary(target);

        _adminLog.Add(
            LogType.STBandPatchIdentified,
            LogImpact.Low,
            $"{identifier:identifier} identified {target:target} band patch {targetBand.BandStatusIcon} at {_transform.GetMapCoordinates(target):coords}; identifierArmor={idArmor}, targetArmor={targetArmor}");
    }

    private string GetArmorSummary(EntityUid uid)
    {
        var sb = new StringBuilder();

        foreach (var slot in ArmorSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out var item) && item.HasValue)
                sb.Append($"{slot}:{ToPrettyString(item.Value)}; ");
            else
                sb.Append($"{slot}:none; ");
        }

        return sb.ToString();
    }
}
