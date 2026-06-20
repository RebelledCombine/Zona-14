using System;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.Shared._Stalker.Armor;

public sealed class STArmorDurabilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STArmorDurabilityComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
    }

    public float GetDurabilityFraction(STArmorDurabilityComponent component)
    {
        if (component.MaxDurability <= 0f)
            return 0f;

        return Math.Clamp(component.CurrentDurability / component.MaxDurability, 0f, 1f);
    }

    public string GetDurabilityState(STArmorDurabilityComponent component)
    {
        var fraction = GetDurabilityFraction(component);

        // Zona14: localized
        if (fraction <= 0.2f)
            return Loc.GetString("st-durability-armor-heavily-worn");

        if (fraction <= 0.4f)
            return Loc.GetString("st-durability-armor-worn");

        if (fraction <= 0.7f)
            return Loc.GetString("st-durability-armor-slightly-worn");

        return Loc.GetString("st-durability-armor-serviceable");
        // End Zona14
    }

    public void RestoreDurability(EntityUid uid, STArmorDurabilityComponent component)
    {
        component.CurrentDurability = component.MaxDurability;
        Dirty(uid, component);
    }

    private void OnDamageModify(EntityUid uid, STArmorDurabilityComponent component, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        var damage = args.Args.Damage;

        var totalRelevantDamage = 0f;
        foreach (var entry in damage.DamageDict)
        {
            var damageType = entry.Key.ToString();
            if (!component.AffectedDamageTypes.Contains(damageType))
                continue;

            totalRelevantDamage += (float) entry.Value;
        }

        if (totalRelevantDamage <= 0f)
            return;

        var durabilityLoss = totalRelevantDamage * component.DurabilityLossPerDamage;
        if (durabilityLoss > 0f)
        {
            component.CurrentDurability = MathF.Max(0f, component.CurrentDurability - durabilityLoss);
            Dirty(uid, component);
        }

        var durabilityFraction = GetDurabilityFraction(component);

        var protectionFactor = component.MinProtectionFactor +
                               ((1f - component.MinProtectionFactor) * durabilityFraction);

        foreach (var key in damage.DamageDict.Keys)
        {
            var damageType = key.ToString();
            if (!component.AffectedDamageTypes.Contains(damageType))
                continue;

            var currentDamage = damage.DamageDict[key];

            var multiplier = 2f - protectionFactor;
            damage.DamageDict[key] = currentDamage * multiplier;
        }
    }
}