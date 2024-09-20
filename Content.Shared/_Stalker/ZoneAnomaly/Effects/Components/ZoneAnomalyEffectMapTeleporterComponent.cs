using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._Stalker.ZoneAnomaly.Effects.Components;

[RegisterComponent]
public sealed partial class ZoneAnomalyEffectMapTeleporterComponent : Component
{
    [DataField]
    public ResPath MapPath = new("/Maps/_StalkerMaps/Anomaly/bubble_small.yml");

    [DataField]
    public MapId? MapId;

    [DataField]
    public EntityUid? MapEntity;
}
