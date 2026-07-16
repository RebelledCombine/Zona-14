using Content.Shared.Damage.Prototypes;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Radiation.Components;

/// <summary>
///     Geiger counter that shows current radiation level.
///     Can be added as a component to clothes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedGeigerSystem))]
public sealed partial class GeigerComponent : Component
{
    /// <summary>
    ///     If true it will be active only when player equipped it.
    /// </summary>
    [DataField]
    public bool AttachedToSuit;

    /// <summary>
    ///     Is geiger counter currently active?
    ///     If false attached entity will ignore any radiation rays.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsEnabled;

    /// <summary>
    ///     Should it shows examine message with current radiation level?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool ShowExamine;

    /// <summary>
    ///     Should it shows item control when equipped by player?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool ShowControl;

    /// <summary>
    ///     Map of sounds that should be play on loop for different radiation levels.
    /// </summary>
    [DataField]
    public Dictionary<GeigerDangerLevel, SoundSpecifier> Sounds = new()
    {
        {GeigerDangerLevel.Low, new SoundPathSpecifier("/Audio/Items/Geiger/low.ogg")},
        {GeigerDangerLevel.Med, new SoundPathSpecifier("/Audio/Items/Geiger/med.ogg")},
        {GeigerDangerLevel.High, new SoundPathSpecifier("/Audio/Items/Geiger/high.ogg")},
        {GeigerDangerLevel.Extreme, new SoundPathSpecifier("/Audio/Items/Geiger/ext.ogg")}
    };

    // Zona14: configurable damage types for advanced dosimeters (e.g. modified/universal).
    /// <summary>
    ///     Damage types this dosimeter sums. If empty, it falls back to Radiation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<GeigerDamageType> DamageTypes = new();

    // Zona14: custom unit label shown in UI and examine. Treated as a Fluent key; if empty, defaults to "rads".
    /// <summary>
    ///     Custom unit label (Fluent key, e.g. "geiger-prefix-ceu"). Empty string uses the default "rads" locale.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Prefix = string.Empty;

    // Zona14: if true, examine and item control show per-damage-type readouts.
    /// <summary>
    ///     If true, the examine and item control show a per-damage-type readout.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowAll;

    /// <summary>
    ///     Current radiation level in rad per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public float CurrentRadiation;

    // Zona14: per-damage-type current readings used by ShowAll.
    /// <summary>
    ///     Per-damage-type current readings in damage per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Dictionary<string, float> CurrentDamage = new();

    /// <summary>
    ///     Estimated radiation danger level.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public GeigerDangerLevel DangerLevel = GeigerDangerLevel.None;

    /// <summary>
    ///     Current player that equipped geiger counter.
    ///     Because sound is annoying, geiger counter clicks will play
    ///     only for player that equipped it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    ///     Marked true if control needs to update UI with latest component state.
    /// </summary>
    [Access(typeof(SharedGeigerSystem), Other = AccessPermissions.ReadWrite)]
    public bool UiUpdateNeeded;

    /// <summary>
    ///     Current stream of geiger counter audio.
    ///     Played only for current user.
    /// </summary>
    public EntityUid? Stream;

    /// <summary>
    ///     Mark true if the audio should be heard by everyone around the device
    /// </summary>
    [DataField]
    public bool BroadcastAudio = false;

    /// <summary>
    ///     The distance within which the broadcast tone can be heard.
    /// </summary>
    [DataField]
    public float BroadcastRange = 4f;

    /// <summary>
    ///     The volume of the warning tone.
    /// </summary>
    [DataField]
    public float Volume = -4f;
}

// Zona14: entry used to configure which damage types a dosimeter displays and how they are labeled.
[DataDefinition, Serializable, NetSerializable]
public sealed partial class GeigerDamageType
{
    [DataField]
    public ProtoId<DamageTypePrototype> Id = "Radiation";

    /// <summary>
    ///     Fluent key for the damage-type label (e.g. "geiger-damage-heat"). If the key is not found, the raw value is used.
    /// </summary>
    [DataField]
    public string Name = string.Empty;
}

[Serializable, NetSerializable]
public enum GeigerDangerLevel : byte
{
    None,
    Low,
    Med,
    High,
    Extreme
}

[Serializable, NetSerializable]
public enum GeigerLayers : byte
{
    Screen
}

[Serializable, NetSerializable]
public enum GeigerVisuals : byte
{
    DangerLevel,
    IsEnabled
}
