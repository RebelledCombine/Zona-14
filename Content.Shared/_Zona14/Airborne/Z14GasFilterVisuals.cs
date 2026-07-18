using Robust.Shared.Serialization;

namespace Content.Shared._Zona14.Airborne;

/// <summary>
///     Zona14: appearance key set on a gas mask reflecting its installed filter's remaining charge, so an
///     on-icon deterioration overlay can be wired via a GenericVisualizer once the sprites exist.
///     <see cref="ChargeLevel"/> is an int tier: 0 = healthy, 1 = worn, 2 = low, 3 = spent.
/// </summary>
[Serializable, NetSerializable]
public enum Z14GasFilterVisuals : byte
{
    ChargeLevel,
}
