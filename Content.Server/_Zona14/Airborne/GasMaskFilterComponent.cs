namespace Content.Server._Zona14.Airborne;

/// <summary>
///     A consumable gas-mask filter item. Installed into a gas mask's <c>gas_filter</c> slot, it blocks
///     a fraction of incoming airborne hazards (radiation, ash, gas) while it holds charge — see
///     <see cref="AirborneHazardSystem"/>. Charge drains only while the filter is actively blocking, so it
///     lasts a long time in clean air and burns down while you loiter in contaminated pockets or gas clouds.
///     Once charge hits zero the filter is spent and offers no protection; eject it and install a fresh one.
///     Airborne hazards are inhaled, so armor never mitigates them — the filter is the only defense.
/// </summary>
[RegisterComponent]
public sealed partial class GasMaskFilterComponent : Component
{
    /// <summary>Fraction of airborne damage blocked while the filter has charge (0-1). 1 = a charged filter
    /// fully protects; the threat is entirely for the unfiltered/spent mask.</summary>
    [DataField]
    public float Protection = 1.0f;

    /// <summary>Seconds of active filtering remaining. Default ≈ 90 min of time actually spent in bad air.</summary>
    [DataField]
    public float Charge = 5400f;

    [DataField]
    public float MaxCharge = 5400f;

    /// <summary>Charge consumed per real second while the filter is actively blocking airborne hazards.</summary>
    [DataField]
    public float DrainPerSecond = 1f;
}
