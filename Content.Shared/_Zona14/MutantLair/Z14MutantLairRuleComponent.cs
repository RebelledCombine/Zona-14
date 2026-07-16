// SPDX-License-Identifier: MIT

using Robust.Shared.GameObjects;

namespace Content.Shared._Zona14.MutantLair;

/// <summary>
/// Station event rule that spawns one or more mutant lairs.
/// </summary>
[RegisterComponent]
public sealed partial class Z14MutantLairRuleComponent : Component
{
    /// <summary>
    /// Maximum number of lairs this event may spawn at once.
    /// </summary>
    [DataField]
    public int MaxLairsPerEvent = 1;

    /// <summary>
    /// Maximum lairs allowed on a single map at any time.
    /// </summary>
    [DataField]
    public int MaxLairsPerMap = 2;
}
