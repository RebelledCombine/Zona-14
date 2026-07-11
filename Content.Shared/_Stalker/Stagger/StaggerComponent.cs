using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._Stalker.Stagger;

// Zona14: moved to Content.Shared + networked MovementSpeedModifier so the stagger slowdown is
// predicted on the client. Previously server-only, which desynced RefreshMovementSpeedModifiersEvent
// and caused movement-prediction rubber-banding.
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaggerComponent : Component
{
    [DataField]
    public NetUserId? NetUserId;

    [DataField]
    public float SlownessDistanceMin = -2.5f;

    [DataField]
    public float SlownessDistanceMax = 3.5f;

    /// <summary>
    /// Server-computed slowdown multiplier (based on proximity to your own corpse).
    /// Networked so the client predicts the same movement speed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MovementSpeedModifier = 1f;
}
