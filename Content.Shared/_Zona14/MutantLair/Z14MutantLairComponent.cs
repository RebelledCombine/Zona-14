// SPDX-License-Identifier: MIT

using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Zona14.MutantLair;

/// <summary>
/// A mutant lair/nest that periodically spawns mutants until exhausted or destroyed.
/// </summary>
[RegisterComponent]
public sealed partial class Z14MutantLairComponent : Component
{
    /// <summary>
    /// Maximum mutants alive at once.
    /// </summary>
    [DataField]
    public int MaxMutants = 3;

    /// <summary>
    /// Total mutants this lair can ever spawn.
    /// </summary>
    [DataField]
    public int MaxSpawns = 12;

    /// <summary>
    /// Seconds between spawn attempts.
    /// </summary>
    [DataField]
    public float SpawnInterval = 30f;

    /// <summary>
    /// Radius around the lair in which mutants can spawn.
    /// </summary>
    [DataField]
    public float SpawnRadius = 4f;

    /// <summary>
    /// Prototypes to spawn. One is picked at random each spawn.
    /// </summary>
    [DataField]
    public List<EntProtoId> MutantPrototypes = new();

    /// <summary>
    /// Reward dropped when the lair is destroyed.
    /// </summary>
    [DataField]
    public List<EntProtoId> RewardPrototypes = new();

    /// <summary>
    /// Items selected from <see cref="RewardPrototypes"/> when the lair is destroyed.
    /// </summary>
    [DataField]
    public int RewardCount = 2;

    /// <summary>
    /// Runtime: total spawns performed so far.
    /// </summary>
    [DataField]
    public int SpawnsDone;

    /// <summary>
    /// Runtime: next allowed spawn time.
    /// </summary>
    [DataField]
    public TimeSpan NextSpawnTime;

    /// <summary>
    /// Runtime: reward already dropped for this lair.
    /// </summary>
    [DataField]
    public bool RewardDropped;

    /// <summary>
    /// Runtime: UIDs of mutants currently considered alive from this lair.
    /// </summary>
    [ViewVariables]
    public List<EntityUid> SpawnedMutants = new();
}
