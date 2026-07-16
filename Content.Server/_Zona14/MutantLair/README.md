# Z14MutantLair

A dynamic mutant lair/nest system for Zona-14.

## Overview

`Z14MutantLair` is a destructible structure that periodically spawns mutants within a radius. Rewards are only dropped when the nest itself is destroyed; mutants spawned by the lair can be butchered normally.

## Components

- `Content.Shared/_Zona14/MutantLair/Z14MutantLairComponent.cs` — Nest configuration and runtime spawn state.
- `Content.Shared/_Zona14/MutantLair/Z14MutantLairZoneComponent.cs` — Mapper marker that designates a valid spawn location.
- `Content.Shared/_Zona14/MutantLair/Z14MutantLairRuleComponent.cs` — Station event rule configuration.
- `Content.Server/_Zona14/MutantLair/Z14MutantLairSystem.cs` — Spawns mutants, tracks them, adds `BlowoutTargetComponent`, drops rewards on destruction, and stops spawning when exhausted.
- `Content.Server/_Zona14/MutantLair/Z14MutantLairRuleSystem.cs` — Picks random `Z14MutantLairZone` markers, skips safe zones, respects per-map lair limits, and spawns the configured lair prototype.
- `Content.Server/_Zona14/Administration/Commands/Z14SpawnLairCommand.cs` — Admin command `z14spawnlair [here|<zone entity>]`.

## Prototypes

- `Resources/Prototypes/_Zona14/MutantLair/entities.yml` — `Z14MutantLair`, `Z14MutantLairT2`, `Z14MutantLairT3`, `Z14MutantLairZone`, `Z14MutantLairZoneT2`, `Z14MutantLairZoneT3`.
- `Resources/Prototypes/_Zona14/MutantLair/rules.yml` — `Z14MutantLairScheduler` (BasicStationEventScheduler) and `Z14MutantLairRule` (StationEvent).
- `Resources/Prototypes/_Stalker/game_presets.yml` — `Z14MutantLairScheduler` is added to `STGamePreset` (`# Zona14:` marker).

## How it works

1. `Z14MutantLairScheduler` periodically triggers `Z14MutantLairRule`.
2. `Z14MutantLairRuleSystem.Started` finds all `Z14MutantLairZone` markers, filters out those in safe zones, and randomly selects up to `MaxLairsPerEvent`.
3. It spawns a `Z14MutantLair` (or tier variant) at the marker's coordinates.
4. `Z14MutantLairSystem` spawns a mutant every `SpawnInterval` seconds (with a random startup delay) until `MaxMutants` are alive or `MaxSpawns` have been spawned.
5. Each spawned mutant gets `BlowoutTargetComponent`.
6. When the lair's `Destructible` threshold is reached, `Z14MutantLairSystem` drops `RewardCount` items from `RewardPrototypes` and stops spawning.

## Anti-farming

Lair mutants are marked as `BlowoutTarget` and can be butchered for parts. The nest itself drops a fixed `RewardCount` of items when destroyed, with `MaxSpawns` and `MaxLairsPerMap` limiting the total economy impact.

## Admin commands

- `z14spawnlair here` — Spawns a `Z14MutantLair` at the admin's current position.
- `z14spawnlair <zone entity uid>` — Spawns the lair configured by the specified `Z14MutantLairZone` marker.

## Safe zones

`Z14MutantLairRuleSystem` and `Z14MutantLairSystem` both check for `StalkerSafeZoneComponent` on the map entity and grid entity before spawning, preventing lairs and mutants from appearing in faction bases or safe areas.
