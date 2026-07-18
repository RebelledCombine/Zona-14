using Content.Shared.Damage;

namespace Content.Server._Zona14.Airborne;

/// <summary>
///     Marks an entity — a map, a mapper-placed zone marker, or an anomaly — that emits an airborne
///     ("inhaled") hazard into the air around it: radioactive particulate, burning ash, poison/acid gas, etc.
///     <c>BlowoutTargetComponent</c> mobs within <see cref="Radius"/> take <see cref="Damage"/> per second,
///     mitigated ONLY by an equipped gas-mask filter (never by armor — the dose is breathed in), and never
///     inside a <c>StalkerSafeZoneComponent</c>. Handled by <see cref="AirborneHazardSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class AirborneHazardComponent : Component
{
    /// <summary>Airborne damage inflicted per second on an unprotected breather in range.</summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    /// <summary>
    ///     Radius in metres. 0 or less means the whole map the source sits on (ambient / contaminated map);
    ///     a positive value is a circular pocket centered on this entity (a contaminated area, or an anomaly's
    ///     gas cloud).
    /// </summary>
    [DataField]
    public float Radius;

    [DataField]
    public bool Enabled = true;
}
