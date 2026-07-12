# Z14 Personal Cache

Persistent per-account hidden world caches.

## Overview

Players can buy a `Z14PersonalCacheKit` from the barman and use it to place a
`Z14PersonalCache` in the world. Each cache is owner-only, can hold up to
**30 kg** of items, and can be buried/unburied with a shovel. Players can
insert a `Z14PersonalCacheCartridge` into their PDA to list their caches with
location, status, and weight.

## Limits

- **5 caches per account**.
- **30 kg per cache**.
- Cannot be placed in safe zones (maps or grids with `StalkerSafeZoneComponent`).
- Cannot be placed on empty/invalid tiles.
- A cache must be empty before it can be removed; removing it returns the kit.

## Hiding

- Only the owner can use a shovel on the cache.
- A 5-second do-after toggles buried state.
- Buried caches are fully invisible, have no context menu, and have no collision.
- Buried caches can still be opened/removed by the owner after unburying.

## Persistence

- Cache data is stored in the `StalkerPersonalCache` table.
- The DB is written on:
  - Cache creation.
  - Every item insert/remove (via `StalkerStorageSystem.SaveStorage`).
  - Bury/unbury.
  - Empty removal.
- On `PostGameMapLoad` caches respawn at the stored `MapKey` + local coordinates.
- If the grid/tile is invalid, the system searches for a valid tile in a 3-tile
  radius. If none is found, the cache stays dormant in the DB and **is not**
  automatically teleported to a repository.

## PDA Cartridge

- `Z14PersonalCacheCartridge` is a PDA program.
- It lists the owner's caches with map key, coordinates, hidden/visible status,
  and current weight.

## Admin Commands

| Command | Description |
| --- | --- |
| `z14listcaches [userId\|all]` | List caches for a user or all users. |
| `z14cacheinfo <cacheId>` | Show details for a cache. |
| `z14tp2cache <cacheId>` | Teleport to a cache. |
| `z14clearcache <cacheId>` | Delete one cache and its contents. |
| `z14clearcaches <userId>` | Delete all caches for a user. |
| `z14recovercache <cacheId>` | Drop the cache's contents at the admin's feet. |

All admin actions are logged.

## Anti-abuse

- Owner-only access verified by `NetUserId`.
- Non-owners cannot insert, remove, or open the cache.
- Hidden caches are invisible and unclickable to others.
- No automatic item teleport on grid loss; caches become dormant.
- Paid kit and strict per-account limit keep caches scarce.
