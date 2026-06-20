namespace Content.Server._Stalker;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StalkerPortalComponent : Component
{
    //Stalker teleport name, e.g. "Bandits", "Duty", etc. // Zona14: translated comment
    [DataField("PortalName")]
    public string PortalName = string.Empty;

    [DataField]
    public bool AllowAll;
}
