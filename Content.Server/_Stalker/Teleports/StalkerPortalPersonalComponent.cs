namespace Content.Server._Stalker;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class StalkerPortalPersonalComponent : Component
{
    //Portal from which the player entered the stalker arena // Zona14: translated comment
    [ViewVariables]
    public string ReturnPortal = string.Empty;

    //Entity ID of the portal from which the player entered the stalker arena, needed for returning back // Zona14: translated comment
    [ViewVariables]
    public EntityUid ReturnPortalEntity;
}
