<!--
@file README.md
@description Player-focused repository overview for the PaxDrops Schedule I mod, including install/build entry points and linked documentation.
@editCount 2
-->

# PaxDrops

PaxDrops is a MelonLoader mod for Schedule I that adds scheduled dead drops, Mr. Stacks ordering, save-aware
persistence, and debug tooling for testing and support.

## Quick Navigation

- [Repository agent guide](AGENTS.md)
- [Docs index](Docs/README.md)
- [Save persistence guide](README_SAVE_SYSTEM.md)
- [Project file](PaxDrops.IL2CPP.csproj)
- [Windows build helper](build_and_copy_dll.bat)
- [macOS/Linux build helper](build_and_copy_dll.sh)

## What PaxDrops Adds

- Scheduled dead drops that can be queued for later delivery instead of appearing instantly during normal play.
- Mr. Stacks ordered drops with tiered progression, daily limits, and message-driven confirmations.
- Per-save persistence for drops, order counts, metadata, and Mr. Stacks conversation history.
- Debug and inspection commands for testing drops, reviewing save data, and troubleshooting persistence.

## Install And Use

PaxDrops targets the Schedule I MelonLoader mod flow.

Prerequisites:

- Schedule I installed locally
- MelonLoader installed for Schedule I
- A generated `MelonLoader/Il2CppAssemblies` folder created by launching the game once after MelonLoader install

Typical install flow:

1. Build the mod or use an already-built `PaxDrops.dll`.
2. Copy the generated runtime files into your Schedule I `Mods` folder.
3. Launch Schedule I and let MelonLoader load PaxDrops at startup.

The current build/deploy flow mirrors these files into the game `Mods` directory:

- `PaxDrops.dll`
- `PaxDrops.deps.json`
- `Microsoft.Data.Sqlite.dll`
- `SQLitePCLRaw.*.dll`
- `runtimes/**`

## Build From Source

The project uses `SCHEDULE_I_DIR` to find the game install and its MelonLoader-generated IL2CPP assemblies.

Windows PowerShell example:

```powershell
$env:SCHEDULE_I_DIR = "C:\Program Files (x86)\Steam\steamapps\common\Schedule I"
dotnet build .\PaxDrops.IL2CPP.csproj -c Debug
```

Windows helper script:

```powershell
.\build_and_copy_dll.bat --build Debug
```

Running `build_and_copy_dll.bat` without arguments now shows usage instead of starting a build immediately. When you
launch it from Explorer, the script keeps the window open so you can read errors and success output before it closes.

macOS/Linux helper script:

```bash
SCHEDULE_I_DIR="/path/to/Schedule I" ./build_and_copy_dll.sh --build Debug
```

Useful helper script modes:

- `--build <CONFIG>`: clean and build, then let MSBuild deploy runtime files to `Mods`
- `--dll <CONFIG>`: copy built runtime files from `bin/<CONFIG>/net6.0` to `Mods`
- `--clean`: remove `bin/` and `obj/`

If the build fails early, check that `SCHEDULE_I_DIR` points at a real Schedule I install with MelonLoader and valid
`Il2CppAssemblies`.

## Debug Commands

PaxDrops includes command surfaces for debugging and support workflows.

- `paxdrop status`
- `paxdrop debug_saves`
- `paxdrop analyze_saves`
- `paxdrop cleanup_saves`
- `paxdrop metadata`
- `stacks status`
- `stacks history`
- `stacks save`
- `stacks clear`

Command registration is still partially gated in the codebase, so treat these as developer/support tools rather than a
public gameplay UI.

## Repository Map

- [Commands/](Commands) - console command handlers for PaxDrops and Mr. Stacks flows
- [Configs/](Configs) - drop tiers, ordering rules, and static gameplay config
- [MrStacks/](MrStacks) - Mr. Stacks messaging, ordering, and NPC integration
- [Patches/](Patches) - game hook points such as console and save-system patches
- [Docs/](Docs) - architecture notes, design references, and item/drop-table source docs

## Deeper Docs

- Start at [Docs/README.md](Docs/README.md) for the internal docs index.
- Use [README_SAVE_SYSTEM.md](README_SAVE_SYSTEM.md) for the SQLite-backed persistence details.
- Use [Docs/Doc.md](Docs/Doc.md) for the runtime architecture overview.
