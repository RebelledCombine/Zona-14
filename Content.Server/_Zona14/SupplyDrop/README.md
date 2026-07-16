# Z14 Supply Drop

This is a crashed-vehicle supply drop event system for Zona-14.

## How it works

1. A `Z14SupplyDropScheduler` (`BasicStationEventScheduler`) periodically triggers a `Z14SupplyDropRule` game rule.
2. The rule selects a valid `Z14SupplyDropZone` marker on a non-safe map.
3. A localized warning is sent to every player within the zone's `WarningRadius`, and a warning sound is played.
4. After `Z14SupplyDropZone.WarningDelay` (default 20 seconds), an explosion is spawned at the marker and an impact sound is played.
5. A crashed vehicle (helicopter or truck), a locked reward crate, and 3-5 ranged hostile guards are spawned around the zone.
6. The reward crate (`Z14SupplyDropCrate`) has a 60-second forced-unlock do-after. Anyone can start it, but it is interrupted by movement or damage.

## Components

- `Z14SupplyDropZoneComponent` (server): marker data, danger zone, warning/impact sounds, explosion prototype, allowed variants.
- `Z14SupplyDropRuleComponent` (server): rule configuration such as crate, vehicle, loot, and guardian tables.
- `Z14SupplyDropCrateComponent` (shared): marker component for supply drop crates.
- `Z14SupplyDropCrateSystem` (shared): adds examine text for the timed lock.

## Admin command

`z14supplydrop [helicopter|truck|any] [here|<zoneNetEntity>]`

- `helicopter` / `truck` / `any`: force the vehicle variant.
- `here`: create a temporary zone at the admin's position.
- `<zoneNetEntity>`: use a specific `Z14SupplyDropZone` marker.

## Prototypes

- `Resources/Prototypes/_Zona14/SupplyDrop/events.yml`: scheduler and game rule.
- `Resources/Prototypes/_Zona14/SupplyDrop/zones.yml`: mappable marker.
- `Resources/Prototypes/_Zona14/SupplyDrop/crates.yml`: locked reward crate.
- `Resources/Prototypes/_Zona14/SupplyDrop/loot_tables.yml`: loot and guardian tables.
- `Resources/Prototypes/_Zona14/SupplyDrop/explosion.yml`: custom low-damage crash explosion.

## Danger zone

The `Z14SupplyDropZone` marker defines the danger zone. The event never places props outside the zone unless no valid tile is found inside the configured radius. The warning radius should be large enough for players to evacuate.

## Safe maps

The system skips `Z14SupplyDropZone` markers on maps whose root has a `StalkerSafeZoneComponent`. It also skips `StationDataComponent` stations on safe maps when fallback is enabled.

## Tuning rewards

The reward crate contents are driven by the `Z14SupplyDropLoot` entity table. The guard count and types are driven by the `Z14SupplyDropGuardians` entity table. Both use `rolls: !type:RangeNumberSelector` and `GroupSelector` children. Edit those tables to change rewards without touching C#.
