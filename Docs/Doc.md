<!--
@file Docs/Doc.md
@description Architecture and runtime flow note for PaxDrops, covering save-aware persistence, messaging, and the active storage path.
@editCount 1
-->

# PaxDrops Architecture

## Quick Navigation

- [Root README](../README.md)
- [Repository agent guide](../AGENTS.md)
- [Docs index](README.md)
- [Save persistence guide](../README_SAVE_SYSTEM.md)
- [Feature/design brief](Idea.md)
- [Ordered-tier drop table reference](droptableidea.md)

## What The Mod Does

PaxDrops adds scheduled dead drops and Mr. Stacks ordering to Schedule I.

- Drops are scheduled and restored per save.
- Mr. Stacks order counts persist per save.
- Mr. Stacks conversation history persists per save.
- Persistence is SQLite-backed, not JSON-backed.

## Core Persistence Pieces

### `SaveFileJsonDataStore.cs`

Public facade used by the rest of the mod.

- Tracks the currently loaded save.
- Keeps drops, order counts, and metadata in memory.
- Loads state on save entry.
- Writes one full snapshot when the game saves.
- Exposes debug and analysis helpers used by console commands.

### `SaveFileSqliteBackend.cs`

Internal SQLite backend.

- Creates `Mods/PaxDrops/SaveFiles/<steamId>/<saveId>/paxdrops.db`
- Applies schema and tracks version with `PRAGMA user_version`
- Loads and saves metadata, drops, drop items, order counts, and conversation messages
- Inspects save folders for debug and duplicate cleanup commands

### `SaveSystemPatch.cs`

Hooks into the game's save flow so PaxDrops writes only when the game itself saves.

## Messaging

### `MrStacksMessaging.cs`

- Keeps conversation history in memory for the active save
- Loads conversation rows from the current save database
- Queues new messages in memory during play
- Flushes conversation history through `SaveFileJsonDataStore` when requested during save operations

## Runtime Flow

### On Main Scene Entry

1. `InitMain` initializes core systems.
2. `SaveFileJsonDataStore` resolves the current save identity.
3. `SaveFileSqliteBackend` opens or creates `paxdrops.db`.
4. Cached drops, orders, metadata, and conversation history are loaded.

### During Play

1. Gameplay systems mutate in-memory state only.
2. No JSON gameplay files are written.
3. Mr. Stacks messages are queued in memory until the next save.

### On Game Save

1. `SaveSystemPatch` triggers PaxDrops persistence.
2. `SaveFileJsonDataStore` writes a full SQLite snapshot in one transaction.

### On Save Exit

1. Cached save state is unloaded.
2. Mr. Stacks conversation cache is cleared.

## Retired Path

`DataBase.cs` is not part of the live build. The active persistence path is `SaveFileJsonDataStore` plus `SaveFileSqliteBackend`.
