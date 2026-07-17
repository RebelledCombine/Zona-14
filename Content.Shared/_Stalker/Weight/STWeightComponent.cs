using Robust.Shared.GameStates;

namespace Content.Shared._Stalker.Weight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class STWeightComponent : Component
{
    /// <summary>
    /// The total weight of the entity, which is calculated
    /// by recursive passes over all children with this component
    /// </summary>
    // Zona14: pulled/dragged entities also count toward carry weight (anti drag-exploit), see PulledWeight below.
    [ViewVariables]
    public float Total => Self + InsideWeight + PulledWeight;

    [ViewVariables]
    public float TotalMaximum => Maximum * MaximumModifier;

    [DataField, ViewVariables]
    public float InsideWeight;

    // Zona14: weight contributed by an entity this one is currently pulling (dragging).
    // Set by STPulledWeightSystem so hauling loaded backpacks/crates by dragging is no longer weight-free.
    [DataField, ViewVariables, AutoNetworkedField]
    public float PulledWeight;

    // Zona14: fraction of a pulled ITEM/CONTAINER's weight added to the puller. 1.0 = dragging a loaded
    // backpack/crate costs its full weight (no discount) — a loot sled should not be cheaper than carrying.
    // (Its inside-weight reduction is also undone for dragging, see STPulledWeightSystem.)
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float PulledWeightFraction = 1.0f;

    // Zona14: fraction for a pulled CREATURE (mutant/animal corpse). Slightly easier than carrying, scaling
    // with the creature's weight — a heavy mutant is a real haul, a small one trivial.
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float PulledCreatureWeightFraction = 0.6f;

    // Zona14: lighter fraction for a pulled HUMANOID (a downed player), so dragging a teammate to safety
    // stays viable.
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float PulledHumanoidWeightFraction = 0.25f;

    [DataField, ViewVariables, AutoNetworkedField]
    public float WeightThrowModifier = 0.1f;

    /// <summary>
    /// This allows you to adjust the strength of
    /// the throw so that small objects are not thrown harder,
    /// but large objects are thrown weaker
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public float WeightThrowMinStrengthModifier = 1f;

    [DataField, ViewVariables, AutoNetworkedField]
    public float MovementSpeedModifier = 1f;

    [DataField, ViewVariables, AutoNetworkedField]
    public float MaximumModifier = 1f;

    /// <summary>
    /// <see cref="STWeightComponent.Total"/> weight at which the entity stops completely,
    /// yes this code has a linear deceleration schedule,
    /// possible improvements in the future
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Maximum = 200f;

    /// <summary>
    /// <see cref="STWeightComponent.Total"/> weight at which the entity begins to slow down.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Overload = 100f;

    [ViewVariables]
    public float TotalOverload => Overload * MaximumModifier;

    /// <summary>
    /// Entity's own weight
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Self = 0.05f;
}
