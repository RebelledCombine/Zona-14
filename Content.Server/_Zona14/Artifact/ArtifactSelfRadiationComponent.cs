namespace Content.Server._Zona14.Artifact;

/// <summary>
///     Makes an artifact irradiate ONLY its wearer while equipped in an ARTIFACT slot.
///     Unlike <c>RadiationSource</c> this never emits into the world, so it cannot be used to
///     grief bystanders in safe zones. Unlike <c>PersonalDamage</c> (which ignores resistances)
///     the radiation is routed through the normal radiation pipeline, so worn armor mitigates it.
///     <see cref="ArtifactSelfRadiationSystem"/> sums every equipped artifact's <see cref="Rads"/>
///     into a single radiation tick per wearer, so armor's flat reduction applies once and
///     stacking rad-emitting artifacts is self-limiting.
/// </summary>
[RegisterComponent]
public sealed partial class ArtifactSelfRadiationComponent : Component
{
    /// <summary>
    ///     Radiation, in rads per second, applied to the wearer while this artifact is equipped.
    /// </summary>
    [DataField]
    public float Rads;
}
