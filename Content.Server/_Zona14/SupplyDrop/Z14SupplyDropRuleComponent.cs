// SPDX-License-Identifier: MIT

using Content.Shared._Zona14.SupplyDrop;
using Content.Shared.EntityTable;
using Robust.Shared.Prototypes;

namespace Content.Server._Zona14.SupplyDrop;

[RegisterComponent]
public sealed partial class Z14SupplyDropRuleComponent : Component
{
    [DataField]
    public EntProtoId CrateProto = "Z14SupplyDropCrate";

    [DataField]
    public Dictionary<Z14SupplyDropVariant, EntProtoId> VehiclePrototypes = new()
    {
        { Z14SupplyDropVariant.Helicopter, "helicopterStructure" },
        { Z14SupplyDropVariant.Truck, "ZIL" }
    };

    [DataField]
    public ProtoId<EntityTablePrototype> LootTable = "Z14SupplyDropLoot";

    [DataField]
    public ProtoId<EntityTablePrototype> GuardianTable = "Z14SupplyDropGuardians";

    [DataField]
    public bool SkipSafeMaps = true;

    [DataField]
    public bool AllowFallback;

    [DataField]
    public int MaxSpawnRetries = 10;

    [ViewVariables]
    public Z14SupplyDropVariant? ForcedVariant;

    [ViewVariables]
    public EntityUid? ForcedZone;

    [ViewVariables]
    public EntityUid? ForcedUser;
}
