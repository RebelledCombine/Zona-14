// SPDX-License-Identifier: MIT

namespace Content.Server._Zona14.AnomalyMigration;

public enum Z14AnomalyMigrationPhase
{
    Idle,
    Clear,
    Regenerate,
    Wait,
    Complete
}
