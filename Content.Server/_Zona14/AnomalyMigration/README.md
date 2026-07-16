# Zona-14 Dynamic Anomaly Migration

This feature re-generates anomalies during an emission. It reuses the existing
`STAnomalyGeneratorSystem` / `STAnomalyGenerationJob` so all spawn blockers and
safe-zone logic are preserved. Only anomalies placed by `STAnomalyGeneratorSystem`
are cleared; pre-mapped anomalies are never touched.

## Files

- `Z14AnomalyMigrationRuleComponent.cs` - runtime state for the event.
- `Z14AnomalyMigrationRuleSystem.cs` - `StationEvent` logic.
- `Z14AnomalyMigrationEmissionComponent.cs` - emission-trigger configuration.
- `Z14AnomalyMigrationEmissionSystem.cs` - triggers `Z14AnomalyMigrationRule` from emissions.
- `Z14AnomalyMigrateCommand.cs` - admin `z14anomigrate` command.
- `Resources/Prototypes/_Zona14/AnomalyMigration/events.yml` - `Z14AnomalyMigrationRule` prototype.
- `Resources/Prototypes/_Zona14/GameRules/emission_events.yml` - `Z14STEmissionEvent` with `Z14AnomalyMigrationEmission`.
- `Resources/Prototypes/_Stalker/game_presets.yml` - `STGamePreset` uses `Z14STEmissionEventScheduler` (`# Zona14:` marker).
- `Resources/Locale/{en-US,ru-RU}/_Zona14/AnomalyMigration/anomaly_migration.ftl` - event messages.

## How it works

1. `STGamePreset` uses `Z14STEmissionEventScheduler` which schedules `Z14STEmissionEvent`.
2. `Z14STEmissionEvent` has a `Z14AnomalyMigrationEmission` component and the built-in
   `EmissionAnomalyRegen` component disabled.
3. When the emission reaches Stage 2 (`EmissionStateChangedEvent.IsActive == true`),
   `Z14AnomalyMigrationEmissionSystem` triggers `Z14AnomalyMigrationRule` with `MigrateAll` enabled.
4. `Z14AnomalyMigrationRule` is a `StationEvent` with no `GameRule` delay.
   - `Added` plays the warning audio and sends the generic `StartAnnouncement`.
   - `Started` selects all valid anomaly-generator target maps (or one if `MigrateAll` is false).
   - `ActiveTick` runs the migration in phases:
     - **Clear** - calls `STAnomalyGeneratorSystem.ClearGeneration()` for every target map.
     - **Regenerate** - waits `RegenerateDelay`, then starts `STAnomalyGenerationJob` for each map.
     - **Wait** - polls all async jobs and ends the `GameRule` when they complete.
   - `Ended` sends the completion announcement and plays the end sound.
5. Safe maps (`StalkerSafeZoneComponent`) are skipped by default.
6. `STAnomalyGeneratorSpawnBlocker` regions are automatically respected because the
   regeneration job already uses them.
7. Only entities in `STAnomalyGeneratorComponent.MapGeneratedAnomalies` are deleted.
   Pre-mapped anomalies are not tracked and are therefore never touched.

## Configuration

`Z14AnomalyMigrationRuleComponent` fields:

| Field | Description |
|-------|-------------|
| `regenerateDelay` | Seconds between clearing and re-spawning (default 2). |
| `skipSafeMaps` | Skip maps with `StalkerSafeZoneComponent` (default true). |
| `allowFallback` | Allow migrating on maps without `STAnomalyGeneratorTarget` (default false). |
| `fallbackOptions` | `STAnomalyGenerationOptionsPrototype` ID for fallback maps. |
| `migrationCount` | Override anomaly count; 0 uses the options prototype's count. |
| `mapOverrides` | Map-keyed count overrides. |
| `migrateAll` | Migrate all valid maps (default false). The emission prototype sets this to true. |

`Z14AnomalyMigrationEmissionComponent` fields:

| Field | Description |
|-------|-------------|
| `triggerAtStart` | Trigger migration when `EmissionStateChangedEvent.IsActive == true` (default true). |
| `triggerAtEnd` | Trigger migration when `EmissionStateChangedEvent.IsActive == false` (default false). |
| `migrateAll` | Migrate all valid maps (default true). |
| `regenerateDelay` | Override the spawned rule's regeneration delay (0 means use the rule's default). |

## Admin command

- `z14anomigrate` - trigger a random single-map migration.
- `z14anomigrate all` - trigger migration on all valid anomaly-generator maps.
- `z14anomigrate <mapKey>` - trigger on a specific map by `STMapKey` (e.g. `Kordon`).
- `z14anomigrate <mapId>` - trigger on a specific map by numeric `MapId`.
- `z14anomigrate <mapKey|mapId> <count>` - override the number of anomalies to spawn.

## No map file edits

The system does not modify `Resources/Maps/_Zona14/World/*.yml`. It only operates at
runtime through the existing anomaly generator.
