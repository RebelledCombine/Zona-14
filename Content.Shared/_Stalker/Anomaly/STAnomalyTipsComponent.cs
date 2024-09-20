using Robust.Shared.Utility;

namespace Content.Shared._Stalker.Anomaly;

[RegisterComponent]
public sealed partial class STAnomalyTipsComponent : Component
{
    [DataField]
    public SpriteSpecifier Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_Stalker/Interface/Overlays/anomaly_tips.rsi"), "unknown");

    [DataField]
    public string State = "unknown";

    [DataField]
    public float Visibility = 1f;
}
