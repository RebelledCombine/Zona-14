namespace Content.Server._Zona14.Airborne;

/// <summary>
///     Zona14: marks a closed-cycle sealed suit (SEVA, scientist SEVA). While worn in the outer slot, the
///     wearer is immune to airborne (inhaled) hazards without any gas-mask filter — the suit supplies its own
///     air. Does not affect penetrating radiation (that still goes through armor). Read by
///     <see cref="AirborneHazardSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class Z14SealedSuitComponent : Component
{
    [DataField]
    public bool Enabled = true;
}
