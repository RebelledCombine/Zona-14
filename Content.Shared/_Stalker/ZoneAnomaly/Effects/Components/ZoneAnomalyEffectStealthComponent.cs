namespace Content.Shared._Stalker.ZoneAnomaly.Effects.Components;

[RegisterComponent]
public sealed partial class ZoneAnomalyEffectStealthComponent : Component
{
    [DataField]
    public float Idle = -0.5f;

    [DataField]
    public float Activated = 0f;
}
