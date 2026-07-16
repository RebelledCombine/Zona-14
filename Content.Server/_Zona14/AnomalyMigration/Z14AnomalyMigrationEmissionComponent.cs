// SPDX-License-Identifier: MIT

namespace Content.Server._Zona14.AnomalyMigration;

[RegisterComponent]
public sealed partial class Z14AnomalyMigrationEmissionComponent : Component
{
    /// <summary>
    /// Trigger the migration at the start of the emission (Stage 2, when the event becomes active).
    /// </summary>
    [DataField]
    public bool TriggerAtStart = true;

    /// <summary>
    /// Trigger the migration at the end of the emission (Stage 3, when the event becomes inactive).
    /// </summary>
    [DataField]
    public bool TriggerAtEnd = false;

    /// <summary>
    /// If true, migrate all valid STAnomalyGeneratorTarget maps. If false, only one random map.
    /// </summary>
    [DataField]
    public bool MigrateAll = true;

    /// <summary>
    /// Optional override for the regeneration delay used by the spawned migration rule.
    /// A value of zero means the migration rule's default is used.
    /// </summary>
    [DataField]
    public TimeSpan RegenerateDelay = TimeSpan.Zero;
}
