using System;
using System.Collections.Generic;
using Content.Shared._Stalker.WarZone;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server._Stalker.WarZone;

[RegisterComponent]
public sealed partial class WarZoneComponent : Component
{
    [DataField]
    public string PortalName = string.Empty;

    [DataField("proto")]
    public ProtoId<STWarZonePrototype> ZoneProto;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? DefendingBandId = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? DefendingFactionId = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? CurrentAttackerBandId = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int? CurrentAttackerFactionId = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? CooldownEndTime = null;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool InitialLoadComplete = false;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<int> PresentBandIds = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<int> PresentFactionIds = new();
}
