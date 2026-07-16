# Contributing to Zona-14

Follow this guide, your PR sails through review. The `Zona14 convention check` CI workflow enforces most of it.

## 1. Project lineage

Zona-14 is an English-direction fork of SS14. The chain:

- [space-wizards/space-station-14](https://github.com/space-wizards/space-station-14) - upstream SS14.
- [space-syndicate/space-station-14](https://github.com/space-syndicate/space-station-14) - Russian mainline SS14 fork.
- [stalker14-project/stalker14](https://github.com/stalker14-project/stalker14) - S.T.A.L.K.E.R.-themed derivative (direct parent; Russian).
- **Zona-14** - this repo. English-direction.

We merge from `stalker14-project` regularly. PRs contain both upstream ports and Zona-14-specific work; conventions below make them easy to tell apart.

## 2. The `_Zona14/` rule

**New Zona-14 code lives under a `_Zona14/` folder.** Applies to every project tree where one exists:

- `Content.Server/_Zona14/`
- `Content.Client/_Zona14/`
- `Content.Shared/_Zona14/`
- `Content.IntegrationTests/Tests/_Zona14/`
- `Resources/Prototypes/_Zona14/`
- `Resources/Maps/_Zona14/`
- `Resources/Locale/en-US/_Zona14/`
- `Resources/Locale/ru-RU/_Zona14/`
- `Resources/Textures/_Zona14/`
- `Resources/Audio/_Zona14/`
- `Resources/ServerInfo/_Zona14/`
- `Resources/ConfigPresets/_Zona14/`

Inside `_Zona14/`, mirror upstream feature-driven layout (`_Zona14/Atmos/Components/...`, `_Zona14/Cargo/Systems/...`) rather than grouping by type.

### Namespace (C#)

File at `Content.<project>/_Zona14/<Feature>/<Sub>/File.cs` declares:

```csharp
namespace Content.<project>._Zona14.<Feature>.<Sub>;
```

**Example.** `Content.Shared/_Zona14/Anomalies/Components/StalkerAnomalyComponent.cs`:

```csharp
using Robust.Shared.GameStates;

namespace Content.Shared._Zona14.Anomalies.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StalkerAnomalyComponent : Component
{
    [DataField]
    public float FlickerRate = 0.5f;
}
```

## 3. Upstream edits: the `// Zona14:` marker

When you edit **or add** a file **outside** `_Zona14/` (anywhere in upstream SS14 / stalker14 / `_Stalker` / `_Stalker_EN` trees), mark Zona-14 provenance inline:

- **Edits to existing upstream files** - mark every logical change inline (see forms below).
- **New files added outside `_Zona14/`** - put `// Zona14: added in this fork` (or `# Zona14: added in this fork` for YAML / FTL / shell) header on first line. Prefer `_Zona14/`; only use this when extending an upstream tree is genuinely the right home (e.g. filling translation gaps in `Resources/Locale/en-US/_Stalker_EN/`).

Both forms make Zona-14 modifications easy to spot during upstream merges.

Forms:

- **Single line** - `// Zona14: short reason`:
  ```csharp
  public bool Inverted; // Zona14: if true, Species list is a blacklist
  ```
- **Value swap** - `// Zona14: OLD<NEW`:
  ```csharp
  public const int MaxPlayers = 100; // Zona14: 50<100
  ```
- **Multi-line block** - `// Zona14: reason` opens, `// End Zona14` closes:
  ```csharp
  // Zona14: custom stalker loadout validation
  if (profile.Species == "Stalker" && !StalkerLoadoutCheck(profile))
      return false;
  // End Zona14
  ```
- **Added `using`** - trailing `// Zona14`:
  ```csharp
  using Content.Shared._Zona14.Anomalies.Components; // Zona14
  ```

### YAML and Fluent (`.ftl`) edits

Same rule with `#` comments: `# Zona14:` / `# End Zona14`.

```yaml
- type: entity
  id: SomeUpstreamEntity
  components:
  - type: HealthAnalyzer
    scanDelay: 0.8 # Zona14: 1.2<0.8
```

### Upstream-port escape hatch

If PR is a pure merge or port from `stalker14-project` (no new Zona-14 logic), include `[upstream-port]` in PR title. Validator skips marker check. Only use for genuine upstream syncs.

## 4. YAML / prototype convention

- **`Z14` ID prefix** - all prototype IDs under `_Zona14/` use `Z14` prefix (e.g., `WeaponPistolPM` -> `Z14WeaponPistolPM`). Makes fork entities instantly identifiable in logs, spawn menus, and errors.
- **`categories: [ Zona14 ]`** - every entity prototype under `_Zona14/` includes `Zona14` in its categories list (alongside existing categories like `HideSpawnMenu`).
- **Directory mirroring** - cloned prototypes mirror upstream directory structure under `_Zona14/` (e.g., `_Stalker/.../Pistols/PM.yml` -> `_Zona14/.../Pistols/PM.yml`).
- **Parent references** - `parent:` points to `Z14` version when base was also cloned. Unmodified upstream bases (e.g., `STBaseWeaponGun`) keep original reference.
- Legacy `ZONA`-suffixed IDs predate this convention. Rename to `Z14` prefix planned.
- File names: feature-scoped, not type-scoped (`anomalies.yml`, not `entities.yml`).

## 5. Licensing (code)

Layered licensing. Nothing conflicts, it stacks:

- **Upstream code** (Space Wizards, Corvax) is **MIT**. Preserved verbatim.
- **Stalker-team contributions** (`stalker14-project` authors listed at top of `LICENSE.TXT`) are **All rights reserved**. Contact Stalker14 team to reuse.
- **Zona-14 team contributions** are **MIT** (c) 2024-2026 Zona-14 Team. Two channels count:
  - Everything inside a `_Zona14/` folder (at any depth).
  - Individual hunks inside upstream files annotated with `// Zona14:` / `# Zona14:` markers from section 3.

  By opening a PR that adds code in either channel, you agree your contribution is licensed under Zona-14 MIT terms in `LICENSE.TXT`.
- **Per-file license override inside `_Zona14/`.** If a file under `_Zona14/` needs a different license (e.g. port from fork under CC-BY-SA, or vendored code), put SPDX header (`// SPDX-License-Identifier: <id>`) or full license notice at top of that file. Header wins over folder rule for that file only. Use sparingly; flag in PR description.

Broader legal review of Stalker-team "All rights reserved" clause is **pending**. Flag questions to team; don't resolve them in code.

## 6. Licensing (assets: sprites, audio, maps)

**Every sprite `.rsi` directory, and every standalone asset with a `meta.json`, requires non-empty `license` and `copyright` fields.** CI validator fails any PR that adds or modifies a `meta.json` without them.

Allowed `license` values (SPDX identifiers):

- `CC-BY-SA-3.0` - SS14 default; use unless you have specific reason otherwise.
- `CC-BY-SA-4.0`
- `CC-BY-4.0`
- `CC0-1.0`
- `OFL-1.1`
- `Apache-2.0`
- `MIT`

Anything else requires `[custom-license]` in PR title plus justification in PR body.

**Template** - `Resources/Textures/_Zona14/Anomalies/flicker.rsi/meta.json`:

```json
{
  "version": 1,
  "license": "CC-BY-SA-3.0",
  "copyright": "Made by <contributor handle> for Zona-14, 2026",
  "size": { "x": 32, "y": 32 },
  "states": [{ "name": "icon" }]
}
```

### Reusing a sprite from another fork

Copy `license` and `copyright` values **verbatim**. Note source in PR description (e.g., "Ported from `space-wizards/space-station-14@<sha>` - `Resources/Textures/.../crowbar.rsi`.").

### Editing an existing sprite

**Never remove `license` or `copyright` fields.** Augment attribution:

```json
"copyright": "Made by Alice for SS14, 2022. Modified by Bob for Zona-14, 2026"
```

Validator fails PR if `license` or `copyright` field was present on `base` and is removed or emptied on `head`.

### Audio

`.ogg` files with `meta.json` follow same rule. `.ogg` files without `meta.json` need license declared in PR description and recorded in adjacent `README.md` or attribution file.

## 7. Branch / PR conventions

- **Target branch**: `master`.
- **PR title**: short imperative, one line. Include `[upstream-port]` for pure merges from `stalker14-project`. Include `[custom-license]` if any asset uses license outside allowlist.
- **PR body**: fill in `.github/PULL_REQUEST_TEMPLATE.md` sections. Include media (screenshots / GIFs / video) for anything visible in-game; upload larger videos to [Zona-14 Discord](https://discord.gg/57S48NzbZ9) and link them.
- **PR behavior** follows [SS14 pull-request guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html): separate PRs for features / bug fixes / refactors; test in-game before opening; no GitHub web editor; no force-push after reviewer comments.

## 8. Commit style

- English preferred for new Zona-14 work.
- Russian fine for merges / direct ports from `stalker14-project`.
- No Conventional-Commits requirement in v1. Write descriptive messages.

## 9. Code style and upstream SS14 standards

Zona-14 follows upstream Space Wizards' Den coding standards. Read and apply before any PR touching C# or YAML:

- [SS14 codebase info](https://docs.spacestation14.com/en/general-development/codebase-info.html) - landing page for full conventions tree.
- [SS14 conventions](https://docs.spacestation14.com/en/general-development/codebase-info/conventions.html) - naming, comments, ECS rules (components hold *only* data; systems hold logic; events are struct `[ByRefEvent]`s named `...Event` with `OnXEvent` handlers), XAML/UI, performance, `TimeSpan` / field-deltas, YAML conventions, localization, in-/out-of-simulation split. Primary document.
- [SS14 codebase organization](https://docs.spacestation14.com/en/general-development/codebase-info/codebase-organization.html) - project split (Client / Shared / Server), file layout, prototype organization (`base.yml` + per-type files; no `misc/` folders).
- [SS14 pull-request guidelines](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html) - PR hygiene (separate PRs for features / bug fixes / refactors, test in-game, no web edits, no force-push after reviews).
- [SS14 style guide](https://docs.spacestation14.com/en/general-development/codebase-info/style-guide.html) - C# formatting.

Local rules on top of upstream:

- `.editorconfig` enforces 4-space indent, 120-char line limit, trim trailing whitespace, no CRLF (matches upstream).
- Zona-14 adds no new stylistic rules in v1. Propose changes via Discord before adding rules.

**One exception to upstream.** SS14's `codebase-organization` says "game-code folders live directly under `Content.Client/Shared/Server`." Zona-14 overrides for **new fork code only**: new code goes under `_Zona14/` per section 2. Upstream files edited in place still follow upstream layout and carry `// Zona14:` markers per section 3.

## 10. CI checks

The `Zona14 convention check` workflow runs on every PR. It enforces:

1. **Namespace-folder alignment** - files under `Content.<project>/_Zona14/...` must declare matching namespace.
2. **Upstream-edit markers** - files edited outside `_Zona14/` must have `// Zona14` (or `# Zona14`) markers in added hunks; new files added outside `_Zona14/` must carry `// Zona14: added in this fork` header (skipped if PR tagged `[upstream-port]`).
3. **Misfiled namespace** - `.cs` files outside `_Zona14/` may not declare `_Zona14.*` namespace.
4. **Greenfield warning** - newly added `.cs` or `.yml` files outside `_Zona14/` produce warning (non-fatal); reviewers decide.
5. **Key-file delete guard** - protects `README.md`, `README.ru.md`, `LICENSE.TXT`, `CONTRIBUTING.md`, `.github/PULL_REQUEST_TEMPLATE.md`.
6. **Asset `meta.json` license/copyright** - every `meta.json` under `Resources/` (added or modified) must have populated `license` (SPDX on allowlist) and `copyright` fields; license removals on edits also fail.
7. **No global action-attempt subscribers in server/client** - `SubscribeLocalEvent<ShotAttemptedEvent>`, `SubscribeLocalEvent<AttackAttemptEvent>`, or `SubscribeLocalEvent<BeforeThrowEvent>` (without component constraint) anywhere under `Content.Server/` or `Content.Client/` will fail. These cancel action on one side only, producing prediction snap-backs ("ghost shots"). Move subscriber to `Content.Shared/`, or constrain to component (`SubscribeLocalEvent<TComp, TEvent>`).

### Running the check locally

```bash
bash Tools/_Zona14/check-conventions.sh origin/master HEAD
```

Requirements: `git`, `grep`, `awk`, `jq`. Install `jq` with `sudo apt install jq` (Ubuntu/Debian) or `brew install jq` (macOS).

## 11. Where to discuss

- **Bug reports, player feedback, feature requests**: this repo's [Issues tab](https://github.com/Zona-14/Zona-14/issues) or [Zona-14 Discord](https://discord.gg/57S48NzbZ9). Anyone can open an issue.
- **Community, news, updates, playtests, media uploads**: [Zona-14 Discord](https://discord.gg/57S48NzbZ9).
- **Code changes**: GitHub Pull Requests on this repo.

## 12. Changelog

Zona-14 ships its own in-game changelog tab (**Zona 14**) alongside inherited upstream, rules, maps, and admin tabs. Populated from `:cl:` blocks in PR bodies, merged into `Resources/Changelog/Zona14.yml` by maintainer after PR lands.

### PR body syntax

```
:cl: <optional author override>
- add: Added a new stalker artifact.
- fix: Fixed anomaly flicker at low light levels.
- tweak: Reduced bandage application time.
- remove: Removed the broken handheld scanner.
```

- Types: `add` / `remove` / `tweak` / `fix`. `bug` and `bugfix` are aliases for `fix`.
- Empty entries (`- add:` with no message) silently dropped by merger.
- Author defaults to GitHub username. Put display name after `:cl:` on same line to override.

### Category prefixes

By default entries land in **Zona 14** tab. Prefix later lines with category to route elsewhere:

```
:cl:
- add: Added a new stalker artifact.

ADMIN:
- add: Added an admin verb to force-ghost a player.

MAPS:
- tweak: On Delta, moved engineering locker closer to power.

RULES:
- tweak: Clarified rule 4 around IC/OOC boundaries.
```

Recognised categories: `ADMIN:` -> `Admin.yml`, `MAPS:` -> `Maps.yml`, `RULES:` -> `Rules.yml`. Unknown categories silently ignored (entries fall back to previous category), so typo just sends entries to Zona 14.

### When to skip the `:cl:` block

Omit (or leave all entries empty) for:
- Docs / comment changes.
- CI / tooling.
- Pure refactors with no gameplay impact.
- Upstream ports (`[upstream-port]` tag already tells validator this is a merge).

Gameplay-visible changes (new items, balance tweaks, bug fixes that affect play) should have `:cl:` entry. Optional but strongly encouraged.

### Writing effective entries

Follow SS14's [effective-changelog rules](https://docs.spacestation14.com/en/general-development/codebase-info/pull-request-guidelines.html#writing-an-effective-changelog):

1. Complete, grammatically correct sentences. Capital start, period end.
2. Log only changes with significant in-game impact.
3. Present, active voice.
4. Be concise. No IC flavor / RP jargon.
5. Set appropriate tone.

### Maintainer merge workflow

After merging PR, maintainer runs manual merger documented in [`Tools/_Zona14/changelog/README.md`](Tools/_Zona14/changelog/README.md). Automation (webhook bot) planned; manual flow covers gap until then.

## 13. Local test workflow

Before pushing, run the core local checks:

```bash
# Build
dotnet build --configuration Tools

# Zona-14 convention / marker check
bash Tools/_Zona14/check-conventions.sh origin/master HEAD

# YAML prototype linter
dotnet run --project Content.YAMLLinter/Content.YAMLLinter.csproj -- --configuration Tools

# Unit tests
dotnet test --configuration Tools Content.Tests/Content.Tests.csproj

# Integration tests (full run needs ~16 GB; on 8 GB machines use --filter by namespace/class)
dotnet test --configuration Tools Content.IntegrationTests/Content.IntegrationTests.csproj
```

The `zona14.newmap_teleport_preload` CVar controls whether the server preloads all `MapLoaderPrototype` targets at startup. It is enabled in `Resources/ConfigPresets/StalkerBuild/sttools.toml` and `strelease.toml` for tools/release builds, and disabled by `Content.IntegrationTests/PoolManager.Cvars.cs` so integration tests do not preload every map.
