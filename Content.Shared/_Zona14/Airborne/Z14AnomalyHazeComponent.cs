using Content.Shared._Starfall.Particles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Zona14.Airborne;

/// <summary>
///     Zona14: a constant client-side particle haze marking an anomaly's airborne-hazard radius. Rendered by
///     the client Z14AnomalyHazeSystem, which spawns the emitter on component start (works for server-spawned
///     anomalies, unlike the _Starfall ParticleEmitter's map-init trigger). Visual only — the damage comes
///     from AirborneHazardComponent (server).
/// </summary>
[RegisterComponent]
public sealed partial class Z14AnomalyHazeComponent : Component
{
    /// <summary>The continuous particle effect to emit (e.g. Z14ParticleAshFire / Z14ParticleGasToxic).</summary>
    [DataField(required: true)]
    public ProtoId<ParticleEffectPrototype> Effect;
}
