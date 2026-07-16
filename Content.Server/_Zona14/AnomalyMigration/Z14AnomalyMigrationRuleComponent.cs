// SPDX-License-Identifier: MIT

using System.Threading.Tasks;
using Content.Server._Stalker.Anomaly.Generation.Jobs;
using Content.Shared._Stalker.Anomaly.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Zona14.AnomalyMigration;

[RegisterComponent]
public sealed partial class Z14AnomalyMigrationRuleComponent : Component
{
    /// <summary>
    /// Delay between clearing the old anomalies and starting the regeneration job(s).
    /// Gives the engine a few ticks to process queued deletions.
    /// </summary>
    [DataField]
    public TimeSpan RegenerateDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Skip maps marked with <see cref="StalkerSafeZoneComponent"/>.
    /// </summary>
    [DataField]
    public bool SkipSafeMaps = true;

    /// <summary>
    /// If no map has an <see cref="STAnomalyGeneratorTargetComponent"/>, allow picking any map
    /// and use <see cref="FallbackOptions"/> for regeneration.
    /// </summary>
    [DataField]
    public bool AllowFallback = false;

    /// <summary>
    /// Options prototype used when <see cref="AllowFallback"/> is true and a map has no target.
    /// </summary>
    [DataField]
    public ProtoId<STAnomalyGenerationOptionsPrototype>? FallbackOptions;

    /// <summary>
    /// Overrides how many anomalies are spawned. 0 means use the map's configured TotalCount.
    /// </summary>
    [DataField]
    public int MigrationCount = 0;

    /// <summary>
    /// Per-map anomaly count overrides keyed by STMapKey value.
    /// 0 values are ignored and fall back to <see cref="MigrationCount"/> or the prototype count.
    /// </summary>
    [DataField]
    public Dictionary<string, int> MapOverrides = new();

    /// <summary>
    /// If true, the event migrates all valid <see cref="STAnomalyGeneratorTargetComponent"/> maps.
    /// If false, a single random map is migrated.
    /// </summary>
    [DataField]
    public bool MigrateAll = false;

    // Runtime state

    public Z14AnomalyMigrationPhase Phase = Z14AnomalyMigrationPhase.Idle;

    public TimeSpan NextAction;

    public List<Z14AnomalyMigrationTarget> Targets = new();

    public List<Task<STAnomalyGenerationJobData>> MigrationTasks = new();

    /// <summary>
    /// Display name used for single-map announcements. For multi-map migrations the dedicated
    /// all-map locale keys are used instead.
    /// </summary>
    public string? TargetMapName;

    // Manual trigger overrides

    public MapId? ForcedMapId;

    public ProtoId<STAnomalyGenerationOptionsPrototype>? ForcedOptionsId;

    public int ForcedCount;
}
