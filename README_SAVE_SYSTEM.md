<!--
@file README_SAVE_SYSTEM.md
@description Save persistence guide for PaxDrops covering the SQLite-backed per-save storage layout, schema, and lifecycle.
@editCount 1
-->

# PaxDrops Save Persistence

## Quick Navigation

- [Root README](README.md)
- [Repository agent guide](AGENTS.md)
- [Docs index](Docs/README.md)
- [Architecture note](Docs/Doc.md)
- [Project file](PaxDrops.IL2CPP.csproj)

## Overview

PaxDrops now stores gameplay persistence in one SQLite database per game save:

`Mods/PaxDrops/SaveFiles/<steamId>/<saveId>/paxdrops.db`

Runtime behavior stays the same:

- Data loads when a save is entered.
- State stays in memory while you play.
- A full snapshot is written only when the game saves.
- Data unloads when leaving the save.

Legacy JSON files are not imported, read, modified, or deleted.

## Storage Model

The public facade is still `SaveFileJsonDataStore`, so existing gameplay code keeps the same API. Internally it now delegates to `SaveFileSqliteBackend`, which owns schema setup, snapshot reads, snapshot writes, and save inspection.

`MrStacksMessaging` no longer owns `conversation.json`. Conversation history is loaded from and saved back to the same per-save SQLite database.

## Directory Layout

```text
Mods/PaxDrops/SaveFiles/
|-- <steamId>/
|   |-- <saveId>/
|   |   |-- paxdrops.db
|   |-- <saveId>/
|   |   |-- paxdrops.db
|-- <steamId>/
|   |-- <saveId>/
|   |   |-- paxdrops.db
```

## SQLite Schema

`paxdrops.db` contains:

- `save_metadata`
- `drops`
- `drop_items`
- `mr_stacks_daily_orders`
- `conversation_messages`

Schema versioning uses `PRAGMA user_version`.

## Save Lifecycle

### On Save Load

1. `SaveFileJsonDataStore.LoadForSaveFile(...)` resolves Steam/save identity.
2. The per-save database is created if needed.
3. The current snapshot is loaded into memory.

### On Game Save

1. `SaveFileJsonDataStore.SaveForCurrentSaveFile()` collects the in-memory state.
2. Metadata, drops, order counts, and conversation history are replaced inside one SQLite transaction.

### On Save Exit

1. `SaveFileJsonDataStore.UnloadCurrentSave()` clears cached state.
2. `MrStacksMessaging.UnloadConversationForCurrentSave()` clears loaded conversation data.

## Debug and Cleanup Commands

The save inspection commands now read SQLite metadata and row counts:

- `paxdrop debug_saves`
- `paxdrop analyze_saves`
- `paxdrop cleanup_saves`
- `paxdrop metadata`
- `stacks history`
- `stacks save`
- `stacks clear`

Duplicate detection and cleanup only consider save directories that already contain `paxdrops.db`. Legacy JSON-only directories are ignored so they are not removed by accident.

## Deployment

The build now ships:

- `PaxDrops.dll`
- `PaxDrops.deps.json`
- `Microsoft.Data.Sqlite.dll`
- `SQLitePCLRaw.*.dll`
- `runtimes/**` native SQLite assets

Both the MSBuild deploy target and the helper copy scripts mirror those files into the game's `Mods` directory.
