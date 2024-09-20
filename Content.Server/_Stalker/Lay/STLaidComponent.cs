namespace Content.Server._Stalker.Lay;

[RegisterComponent]
public sealed partial class STLaidComponent : Component
{
    [DataField]
    public bool Standing;

    [DataField]
    public float TileFrictionModifier = 0.4f;

    [DataField]
    public float MovementSpeedModifier = 0.3f;
}
