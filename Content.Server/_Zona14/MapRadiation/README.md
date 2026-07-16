# MapRadiation

`MapRadiation` is a server-side ambient radiation field tied to a map. It is intentionally separate from the point-source `RadiationSource` / `RadiationReceiver` ray-cast system: it is global for the map, affects every mob with `BlowoutTargetComponent`, and is gated by the same gear resistance pipeline used by `Emission` and anomaly damage.

## Concepts

- `MapRadiation` is a `MapRadiationComponent` placed on the **map entity root**.
- It applies a `DamageSpecifier` on a configurable interval to every `BlowoutTarget` / `Damageable` mob on that map.
- `MapRadiationBlocker` is a circular marker entity that protects all mobs within its radius.
- Existing `StalkerSafeZoneComponent` protects the whole map (on the map entity) or the individual mob (on the target).
- The system calls `DamageableSystem.TryChangeDamage`, which raises `DamageModifyEvent`. `Armor`, `DamageProtectionBuff`, and other modifiers are applied automatically, so the feature is a gear gate.

## Component fields

```csharp
[DataField] public bool Enabled = true;
[DataField] public float Interval = 1f; // seconds between damage ticks
[DataField] public DamageSpecifier Damage = new(); // total damage per tick
public TimeSpan NextDamageTime; // runtime, not serialized
```

`Damage` is the total damage per `Interval`, not per second. For example `Interval: 1` and `Radiation: 5` means 5 radiation damage per second. `Interval: 2` and `Radiation: 10` means the same 5 per second, but applied in 2-second chunks.

## MapRadiationBlocker

`MapRadiationBlockerComponent` defines a circular safe area:

```csharp
[DataField] public bool Enabled = true;
[DataField] public float Radius = 5f; // world-space metres
```

Prototypes in `Resources/Prototypes/_Zona14/MapRadiation/blockers.yml`:

| Prototype | Radius |
| --- | --- |
| `Z14MapRadiationBlocker` | abstract base |
| `Z14MapRadiationBlocker5` | 5 m |
| `Z14MapRadiationBlocker10` | 10 m |
| `Z14MapRadiationBlocker20` | 20 m |
| `Z14MapRadiationBlocker50` | 50 m |
| `Z14MapRadiationBlocker100` | 100 m |

All concrete blockers use `categories: [ Zona14 ]`, `suffix: Z14`, and the `anomaly_generator_blocker` sprite for mapping visibility.

## Protection rules

`MapRadiationSystem.ApplyRadiation` iterates all `BlowoutTarget` + `Damageable` + `Transform` mobs on the map and skips them if:

1. The map entity has `StalkerSafeZoneComponent`.
2. The target mob has `StalkerSafeZoneComponent`.
3. The target is within any enabled `MapRadiationBlocker` radius on that map.

If not skipped, the target receives `DamageableSystem.TryChangeDamage(target, mapRad.Damage, interruptsDoAfters: false)`. This is the same path used by `Emission` and reuses existing `DamageModifierSet` / `DamageModifyEvent` / `Armor` / `DamageProtectionBuff` handling.

## Geiger integration

`GeigerSystem` on the server calls `MapRadiationSystem.GetAmbientRadiation(user, damageType)` for each configured damage type and sums the result with the matching `RadiationReceiver` value. This lets `MapRadiation` emit any damage type (Radiation, Heat, Psy, Cold, Poison, etc.) and the dosimeter will display it. The per-second value is returned after safe-zone and blocker checks.

A `GeigerComponent` now also has a `CurrentDamage` dictionary of per-type readings and a `ShowAll` field. When `ShowAll` is true, both examine and the inventory item control show a per-damage-type readout.

## Manual map setup (apply by hand)

Per the project owner, `Resources/Maps/_Zona14/World/*.yml` are **not modified by this PR**. Add the following blocks manually.

### Map root component

Add to the map root entity (usually `uid: 1`, with `type: Map` component):

```yaml
- type: MapRadiation
  enabled: true
  interval: 1
  damage:
    types:
      Radiation: <value>
      # optional extra damage types for mixed ambient fields
      # Heat: 1
      # Psy: 0.5
      # Cold: 0.5
```

Suggested `Radiation` values:

| Map | `Radiation` |
| --- | --- |
| `Cordon.yml` | `1` |
| `Darkscape.yml` | `2` |
| `Dytiatky.yml` | `0` |
| `Garbage.yml` | `3` |
| `RostokBarAndRVBunker.yml` | `0` (map root already has `StalkerSafeZone`) |
| `Swamps.yml` | `2` |

### Blocker entities

Add one blocker entity near the largest `StalkerExplSafezoneTriggerIn` cluster:

```yaml
- proto: Z14MapRadiationBlocker20
  entities:
  - uid: <next free uid>
    components:
    - type: Transform
      pos: <x>,<y>
      parent: 1
```

Suggested positions:

| Map | Position |
| --- | --- |
| `Cordon.yml` | `-139.5,-14.0` |
| `Darkscape.yml` | `143.03846153846155,39.73076923076923` |
| `Dytiatky.yml` | `-1.5,-55.5` |
| `Garbage.yml` | `65.5,316.1666666666667` |
| `RostokBarAndRVBunker.yml` | `25.5,76.5` |
| `Swamps.yml` | `2.0,7.5` |

After insertion, increment `entityCount` in the map header by the number of blocker entities added.

### Damage sources for all dosimeter types

`Resources/Prototypes/_Stalker/SpawnersNTriggers/radsources.yml` already has point `RadiationSource` prototypes for `Radiation`, `Heat`, `Psy`, and `Caustic`. This PR adds `AnomalyColdSource`, `AnomalyPoisonSource`, `AnomalyCellularSource`, `AnomalyCompressionSource`, `AnomalyStructuralSource`, `AnomalyShockSource`, `AnomalyAsphyxiationSource`, `AnomalyBloodlossSource`, `AnomalyBluntSource`, `AnomalyPiercingSource`, and `AnomalySlashSource` so that every damage type listed on the advanced dosimeters has a real source. Add more intensity variants by copying and adjusting `intensity`/`slope` as needed.

## Advanced Geiger counters (dosimeters)

Two upstream special dosimeters existed with commented-out `prefix`/`damageTypes` fields because the `GeigerComponent` did not support them. This PR adds those fields and finishes the items.

### New `Geiger` data fields

```yaml
- type: Geiger
  showControl: true
  showExamine: true
  showAll: true
  prefix: C.E.U.                  # custom unit label in UI/examine
  damageTypes:
    - id: Radiation
      name: Radiation
    - id: Psy
      name: Psi
```

- `Prefix` overrides the default "rads" unit label (e.g. `У.Е.В.`, `C.E.U.`).
- `DamageTypes` is the list of damage types the device sums. If empty, it falls back to `Radiation`.
- `ShowAll` toggles a per-damage-type readout in examine and the inventory item control.
- `CurrentDamage` is a networked runtime dictionary of per-type readings.

### Finished items

| ID | Type | Prefix | Note |
| --- | --- | --- | --- |
| `DosimeterModified` | upstream, RU | `У.Е.В.` | Sums all harmful types **except** `Radiation` |
| `DosimeterUniversal` | upstream, RU | `У.Е.В.` | Sums all harmful types **including** `Radiation` |
| `Z14DosimeterModified` | Zona-14 clone | `C.E.U.` | English clone of `DosimeterModified` |
| `Z14DosimeterUniversal` | Zona-14 clone | `C.E.U.` | English clone of `DosimeterUniversal` |

The Z14 clones are in `Resources/Prototypes/_Zona14/Objects/Tools/dosimeter.yml` and the RU variants are in `Resources/Prototypes/_Stalker/Entities/Objects/Tools/dosimeter.yml`. Corresponding `en-US` and `ru-RU` Fluent entries are provided.

## Testing checklist

1. `dotnet build --configuration Tools`
2. `bash Tools/_Zona14/check-conventions.sh origin/master HEAD` (against the PR branch)
3. `dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -- --configuration Tools`
4. `dotnet test --configuration Tools Content.Tests/Content.Tests.csproj`
5. `dotnet test --configuration Tools Content.IntegrationTests/Content.IntegrationTests.csproj`

Note: `Content.IntegrationTests` currently fails on a pre-existing `Darkscape.yml` `UserInterface` corruption (`uid: 298` has `actors: invalid`) and is unrelated to `MapRadiation`.
