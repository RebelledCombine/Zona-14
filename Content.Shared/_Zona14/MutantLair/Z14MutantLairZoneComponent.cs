// SPDX-License-Identifier: MIT

using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Zona14.MutantLair;

/// <summary>
/// Mapper-placed marker that designates a valid location for a mutant lair.
/// </summary>
[RegisterComponent]
public sealed partial class Z14MutantLairZoneComponent : Component
{
    /// <summary>
    /// Lair entity prototype to spawn at this zone.
    /// </summary>
    [DataField]
    public EntProtoId LairPrototype = "Z14MutantLair";

    /// <summary>
    /// Optional tier label (T1, T2, T3, T4) used for logging/admin info.
    /// </summary>
    [DataField]
    public string Tier = "T1";
}
