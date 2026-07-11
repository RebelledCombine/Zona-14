using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Characteristics.Modifiers.MovementSpeed;

// Zona14: moved to Content.Shared + networked so the Dexterity movement modifier is predicted
// on the client. Previously this lived in Content.Server, so the client recomputed
// RefreshMovementSpeedModifiersEvent without it and mispredicted movement speed (rubber-banding,
// most visible when crossing SpeedModifierContacts tiles like water).
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CharacteristicModifierMovementSpeedComponent : Component
{
    [DataField]
    public float MaxBonus = 2f;

    [DataField]
    public float MinBonus = 0.1f;

    [DataField]
    public float PositiveModifier = 0.02f;

    [DataField]
    public float NegativeModifier = -0.02f;

    /// <summary>
    /// Server-computed speed multiplier derived from the Dexterity characteristic.
    /// Networked so the client applies the exact same value during movement prediction.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float CurrentModifier = 1f;
}
