# 📦 PaxDrops Mod Overview

## 🧾 What It Does (Current)
PaxDrops adds an immersive dead drop system to *Schedule I*, delivering cash and loot based on in-game time, progress, and player rank.

- 📦 Scheduled drops appear at physical locations.
- 💵 Each drop contains random cash (up to $1,000) and mafia-sourced loot.
- 📈 Loot tiers scale with player day and rank.
- 📅 Drops are persistent using SQLite — not memory-based.
- 📱 NPC-based request system via phone contact: "Mrs. Stacks".
- 🛠 Console commands available for dev/debug purposes.

## 🎯 Intended Design Goals
- Narrative-driven underground economy simulation.
- Day/rank-scaled rewards using mafia faction tiers.
- Modular, extensible, and persistent logic.
- Full integration with S1API's NPC, messaging, and time systems.

## 🚀 Future Plans
- More faction-based NPCs and voice lines.
- Alternate drop types (e.g. time-sensitive packages, deliveries).
- Full UI drop-tracker in-game.
- Player customization of drop preferences.
- Multiplayer-safe drop logic.

---

# 📂 Code Structure Overview

## 🔹 Core Systems

### `InitMain.cs`
- **Purpose**: Main controller for PaxDrops lifecycle.
- **Responsibilities**:
  - Initializes all subsystems on load.
  - Ensures mod persists between scenes.
  - Hooks into scene loading.
- **Notes**: Should only control boot logic and not specific systems.

### `Logger.cs`
- **Purpose**: Custom logger for PaxDrops.
- **Responsibilities**:
  - Provides color-coded, tag-based logging using MelonLoader/S1API.
  - Used via `Logger.Msg()`, `Logger.Warn()`, etc.

### `CommandHandler.cs`
- **Purpose**: Handles developer/debug console commands.
- **Responsibilities**:
  - Registers commands into `Console.commands` and `Console.Commands`.
  - Example commands: `pax.spawn`, `pax.money`, `pax.settime`.

---

## 🔹 Drop System

### `TierLevel.cs`
- **Purpose**: Handles mafia tier and loot generation logic.
- **Responsibilities**:
  - Defines 9 mafia tiers (3 orgs × 3 ranks).
  - Determines available tiers based on in-game day and player rank.
  - Generates `DropPacket` (contains cash + loot).

### `DropPacket.cs` *(implied class or struct)*
- **Fields**:
  - `int cashAmount`
  - `List<(string itemId, int amount)> lootItems`
- Used to describe contents of a drop throughout the system.

### `DeadDrop.cs`
- **Purpose**: Manages in-world spawning of loot stacks.
- **Responsibilities**:
  - Picks spawn location.
  - Places stack objects with generated contents.
  - Runs daily 7:00 AM check for scheduled drops.
  - Marks drops as completed in database.

### `DataBase.cs`
- **Purpose**: Manages persistence of drop data.
- **Responsibilities**:
  - Uses `System.Data.SQLite`.
  - Stores: org, drop time, cash amount, item list.
  - Loads: pending drops on mod load.
  - Marks drops as complete after spawning.

---

## 🔹 NPC & Messaging System

### `MrStacks.cs`
- **Purpose**: Coordinates NPC message sending and interaction logic.
- **Responsibilities**:
  - Triggers daily SMS from “Mrs. Stacks” at 7:30 AM.
  - Manages player dialogue flow for requesting drops.
  - Controls which tiers are unlocked based on progress.
  - Interfaces with `MrStacksMsg`, `MrStacksNpc`.

### `MrStacksMsg.cs`
- **Purpose**: Manages the conversation tree in SMS.
- **Responsibilities**:
  - Dialogue step handling.
  - Sends player choices (org → tier → confirm).
  - Finalizes request and sends it to `TierLevel` and `DataBase`.

### `MrStacksNpc.cs`
- **Purpose**: Defines appearance and behavior for Mrs. Stacks NPC.
- **Responsibilities**:
  - Contains model, voice, and dialogue metadata.
  - Can be reused to define other NPCs.

### `NPCBuilder.cs`
- **Purpose**: Spawns or retrieves the NPC in the scene.
- **Responsibilities**:
  - Searches for "MrStacks" by ID.
  - If missing, spawns one using S1API methods.
  - Modular and reusable for future NPCs.

---

# 🧭 Code Flow Summary

### ▶️ On Load
1. `InitMain.cs` starts up and initializes all systems.
2. `DataBase.cs` loads any scheduled drops from SQLite.
3. `MrStacks.cs` hooks into the time system to send 7:30 AM messages.

### ⏰ At 7:00 AM
- `DeadDrop.cs` checks if a drop is scheduled for today.
- If yes, spawns the drop and marks it as complete in DB.

### 📱 At 7:30 AM
- `MrStacks.cs` sends SMS message from Mrs. Stacks to player.

### 💬 When Player Opens Message
1. `MrStacksMsg.cs` walks through org → tier → confirm steps.
2. `TierLevel.cs` generates a new `DropPacket`.
3. `DataBase.cs` schedules the drop in SQLite.

### ⏰ Next 7:00 AM
- `DeadDrop.cs` spawns the requested drop using `DropPacket`.