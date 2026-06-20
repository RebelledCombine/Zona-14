# Agent Guidelines — Zona-14

Quick-reference for AI coding agents working in this repo.
Full details in [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Fork structure

Zona-14 is an English-direction S.T.A.L.K.E.R.-themed fork of SS14. Fork chain:
`space-wizards/SS14` → `space-syndicate/SS14` → `stalker14-project/stalker14` → **Zona-14**.

## The `_Zona14/` rule

**All new Zona-14 code goes under `_Zona14/` folders.**

| Tree | `_Zona14/` path |
|---|---|
| Server C# | `Content.Server/_Zona14/` |
| Client C# | `Content.Client/_Zona14/` |
| Shared C# | `Content.Shared/_Zona14/` |
| Tests | `Content.IntegrationTests/Tests/_Zona14/` |
| Prototypes | `Resources/Prototypes/_Zona14/` |
| Locale (en) | `Resources/Locale/en-US/_Zona14/` |
| Locale (ru) | `Resources/Locale/ru-RU/_Zona14/` |
| Maps | `Resources/Maps/_Zona14/` |
| Textures | `Resources/Textures/_Zona14/` |
| Audio | `Resources/Audio/_Zona14/` |
| Server Info | `Resources/ServerInfo/_Zona14/` |
| Config Presets | `Resources/ConfigPresets/_Zona14/` |
| Tools | `Tools/_Zona14/` |

Do **not** place new code in other underscore-prefixed folders — those belong to upstream forks:

- `_Stalker/` — stalker14-project upstream
- `_Stalker_EN/` — English translation layer for Stalker entities
- `_DZ/`, `_ES/`, `_Goob/`, `_NC/`, `_RD/`, `_Starfall/` — other upstream forks

`Corvax/` (no underscore) holds Corvax interface definitions (Discord auth, sponsors, loadout, etc.) — also not a place for new Zona-14 code.

## Upstream edit markers

When editing files **outside** `_Zona14/`, mark every change:

- **C#/XAML**: `// Zona14: reason` · `// Zona14: OLD<NEW` · `// Zona14: reason` … `// End Zona14` block · `using ...; // Zona14`
- **YAML/FTL**: `# Zona14: reason` · `# Zona14: OLD<NEW` · `# Zona14: reason` … `# End Zona14` block
- **New file outside `_Zona14/`**: first-line header `// Zona14: added in this fork` (or `#` variant)

Tag PR title `[upstream-port]` for pure merges from `stalker14-project` (skips marker enforcement).

## C# conventions

- **Namespace**: `Content.<Project>._Zona14.<Feature>.<Sub>;` — must match folder path.
- **ECS**: Components = data only (`[RegisterComponent]`). Systems = all logic (`EntitySystem`). Events = struct `[ByRefEvent]` named `…Event`.
- **Prediction symmetry**: Never use global `SubscribeLocalEvent<ShotAttemptedEvent>`, `<AttackAttemptEvent>`, or `<BeforeThrowEvent>` in `Content.Server/` or `Content.Client/`. Use component-targeted `SubscribeLocalEvent<TComp, TEvent>` or move to `Content.Shared/`.
- **CVars**: `Content.Shared/_Zona14/CCVar/Zona14CVars.cs`, prefixed `zona14.`.
- **SPDX header**: `// SPDX-License-Identifier: MIT` on `_Zona14/` files. Ported code adds source attribution.

## Prototype conventions

- **ID prefix**: `Z14` (e.g., `Z14WeaponPistolStalkerPM`).
- **Categories**: `categories: [ Zona14 ]` on every `- type: entity` prototype under `_Zona14/`.
- **Suffix**: `suffix: Z14` on concrete (non-abstract) `- type: entity` prototypes.
- **`categories`/`suffix` are entity-only**: never add them to **data** prototypes (e.g. `stWarZone`, `stBandShopListings`, `persistentCraftRecipe`, `vendingMachineInventory`) — those types don't define the fields and the **YAML Linter CI fails** on them.
- **Parent**: Use `Z14` parent when the base was also cloned; keep upstream parent otherwise.
- **Filenames**: Feature-scoped (`anomalies.yml`), not type-scoped (`entities.yml`).

## Asset licensing

Every `meta.json` needs `license` (SPDX) and `copyright`. Allowed: `CC-BY-SA-3.0` (default), `CC-BY-SA-4.0`, `CC-BY-4.0`, `CC0-1.0`, `OFL-1.1`, `Apache-2.0`, `MIT`. Never remove existing license/copyright fields.

## Build & test

```bash
dotnet build --configuration Tools
dotnet test --configuration Tools Content.Tests/Content.Tests.csproj
dotnet test --configuration Tools Content.IntegrationTests/Content.IntegrationTests.csproj
```

## Validate conventions before pushing

```bash
bash Tools/_Zona14/check-conventions.sh origin/master HEAD
```

A pre-commit hook (`Tools/_Zona14/pre-commit`) runs this automatically on `git commit`.
Install: `git config core.hooksPath Tools/_Zona14/hooks`

## PR checklist

- Target: `master`. Fill in `.github/PULL_REQUEST_TEMPLATE.md`.
- New files under `_Zona14/` (or `[upstream-port]` tag).
- Namespaces match folder. Upstream edits have markers. Assets have `meta.json`.
- Attach media (or mark N/A). Add `:cl:` changelog block for gameplay-visible changes.
