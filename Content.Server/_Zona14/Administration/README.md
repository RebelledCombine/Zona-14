# Zona-14 Administration & Mentor Tools

This folder contains the Zona-14 administration stack: a central dashboard, whole-codebase admin logging helpers, an anti-cheat/alert system, and the Mentor / MentorHelp system.

## Admin dashboard

`Z14AdminDashboardEui` + `Z14AdminDashboardWindow` replace the legacy single-panel dashboard.

- Opened with the `z14dashboard` command (`[AdminCommand(AdminFlags.Admin)]`).
- Tabs: Overview, Players, Maps, Z14 Controls, Stalker, Live Events, Alerts, AHelp, Admin Tools, Mentorhelp.
- The server builds an `AllowedCommands` list filtered by the user's current admin flags.
- Player actions check per-feature flags (`Admin`, `Ban`, `Logs`, etc.) before sending messages.
- Recent events refresh asynchronously and include `Kill`, `AdminAlert`, `Z14Door`, `Z14MutantLair`, `Z14AnomalyMigration`, `Z14SupplyDrop`, `Z14PersonalCache`, `Z14MapRadiation`, `MentorHelp`, `Z14Inventory`, and other high-signal log types.

## Admin logging

`IAdminLogManager` is used by many Zona-14 systems to record important events:

| LogType | Raised by | Impact |
| --- | --- | --- |
| `Z14MapRadiation` | `Z14MapRadiationCommand` | Extreme |
| `Z14SupplyDrop` | `Z14SupplyDropRuleSystem` | High |
| `Z14AnomalyMigration` | `Z14AnomalyMigrationRuleSystem` / `Z14AnomalyMigrateCommand` | Extreme/High/Medium |
| `Z14PersonalCache` | `Z14PersonalCacheSystem` | Medium |
| `Z14MutantLair` | `Z14SpawnLairCommand` / `Z14MutantLairRuleSystem` | Extreme |
| `Z14Door` | `DoorLogSystem` | Medium |
| `Z14Inventory` | `Z14InventoryLogSystem` | Low |
| `Kill` | `MobStateSystem` / admin kill verbs | High |
| `AdminAlert` | `Z14CheatDetectionSystem` | Extreme |
| `MentorHelp` | `MentorHelpSystem` | Low |

## Anti-cheat / alerting

`Z14CheatDetectionSystem` monitors:

- Rapid kills by the same actor.
- Rapid door/window destruction.
- Rapid entity spawns tied to a player.
- Impossible player movement (teleport/speed).

Admins are excluded from movement checks. Alerts are written as `AdminAlert` logs.

## Mentor / MentorHelp

- New `AdminFlags.Mentor` can be assigned through the admin permissions panel.
- `msay` (`[AdminCommand(AdminFlags.Admin)]` + `[AdminCommand(AdminFlags.Mentor)]`) sends a mentor-only chat.
- `openmentorhelp` (Shift+F1) opens the mentor help window.
- Players see `UserMentorHelpWindow`; mentors/admins see `MentorHelpWindow`.
- Mentors can reply to questions and use mentor chat, but they have **no admin powers** and cannot run admin commands.
- `MentorHelp` messages are rate-limited and logged with `LogType.MentorHelp`.

## Commands

| Command | Flag | Description |
| --- | --- | --- |
| `z14dashboard` | `Admin` | Open the Zona-14 admin dashboard. |
| `z14admininfo` | `Admin` | Print online players, admins, round, and stations. |
| `playerlogs` | `Logs` | Open admin logs pre-filtered to a player. |
| `doorlogs` | `Logs` | Open admin logs filtered to `Z14Door`. |
| `adminactivity` | `Logs` | Show a summary of recent admin activity. |
| `ahelptranscript` | `Adminhelp` | Print the admin help transcript for a player. |
| `banlistall` | `Ban` | List all active bans (incl. lifted). |
| `whitelistlogs` | `Logs` | Show whitelist-related logs. |
| `whitelistsearch` | `Admin` | Search whitelists. |
| `whitelistview` | `Admin` | View a player's whitelist status. |
| `whitelistslots` | `Admin` | Show per-station job slot counts. |
| `bring` | `Admin` | Teleport a player/entity to your location. |
| `ghostfollow` | `Admin` | Make your admin ghost follow an entity. |
| `msay` | `Admin` or `Mentor` | Mentor-only chat. |
| `openmentorhelp` | any | Open the mentor help window. |
| `focusmentorchat` | any | Focus the mentor chat input. |

## Map preloading CVar

`NewMapTeleportSystem` uses `zona14.newmap_teleport_preload` to decide whether to preload all `MapLoaderPrototype` targets at startup:

- Default: `false`.
- `Resources/ConfigPresets/StalkerBuild/sttools.toml` and `strelease.toml` enable it for production.
- `Content.IntegrationTests/PoolManager.Cvars.cs` forces it to `false` so tests do not preload every map.
