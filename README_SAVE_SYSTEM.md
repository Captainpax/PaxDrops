# PaxDrops Save-File-Aware System

## Overview

The PaxDrops mod now features a sophisticated save-file-aware persistence system that properly integrates with Schedule I's save system. This ensures that:

1. **Per-Save Isolation**: Each game save has its own separate PaxDrops data
2. **Save-Triggered Persistence**: Data is only saved when the game actually saves
3. **Automatic Load/Unload**: Data loads when entering a save and unloads when returning to menu
4. **Multiple Save Support**: Players can have different PaxDrops progress across multiple game saves

## Architecture

### Core Components

#### 1. SaveSystemPatch.cs
- **Purpose**: Harmony patches that hook into Schedule I's `SaveManager`
- **Functionality**: 
  - Intercepts `SaveManager.Save()` and `SaveManager.Save(string)` calls
  - Triggers PaxDrops data saving only when the game saves
  - Identifies which save file is being saved using save path and name

#### 2. SaveFileJsonDataStore.cs
- **Purpose**: Save-file-aware data storage manager
- **Functionality**:
  - Manages separate data for each save file using unique save IDs
  - Loads appropriate data when entering main scene
  - Unloads data when returning to menu
  - Provides same API as legacy JsonDataStore but with per-save isolation

#### 3. InitMain.cs (Updated)
- **Purpose**: Enhanced initialization with scene management
- **Functionality**:
  - Detects scene transitions (Main ↔ Menu)
  - Loads save data when entering main scene
  - Unloads save data when exiting to menu
  - Initializes save system patches

## Data Storage Structure

```
Mods/PaxDrops/SaveFiles/
├── [SteamID1]/
│   ├── [SaveID1]/
│   │   ├── drops.json         # Pending drops for this save
│   │   ├── orders.json        # Mrs. Stacks order history  
│   │   ├── conversation.json  # Mrs. Stacks conversation history
│   │   └── metadata.json      # Enhanced save metadata
│   └── [SaveID2]/
│       ├── drops.json
│       ├── orders.json
│       ├── conversation.json
│       └── metadata.json
├── [SteamID2]/
│   └── [SaveID3]/
│       ├── drops.json
│       ├── orders.json
│       ├── conversation.json
│       └── metadata.json
└── ...
```

### Enhanced Save ID Generation

The system now generates robust save IDs using multiple identification factors:

#### Primary Identifiers
- **Steam ID**: Extracted from save path (e.g., `<steam_id>`) or generated hash for non-Steam users
- **Organization Name**: Player's organization from `LoadManager.ActiveSaveInfo.OrganisationName`
- **Save Start Date**: Game creation date or current day reference
- **Save Name**: User-defined save name
- **Save Path**: Normalized file system path

#### Save ID Algorithm
```
Enhanced Save ID = SHA256({SteamID}|{OrgName}|{StartDate}|{SaveName}|{NormalizedPath})[0:12]
```

#### Folder Structure
```
SaveFiles/{SteamID}/{SaveID}/
```

This ensures:
- **Per-User Isolation**: Each Steam user has separate data
- **Per-Save Isolation**: Each game save has unique identification  
- **Content-Based Identity**: IDs incorporate actual game save content
- **Collision Resistance**: Multiple identification factors prevent conflicts

### Save Metadata

Each save includes a `metadata.json` file with enhanced information:

```json
{
  "SaveId": "AbC123XyZ456",
  "SteamId": "76561198123456789", 
  "OrganizationName": "Smith & Associates",
  "SaveName": "MyGameSave",
  "SavePath": "C:/Users/.../Saves",
  "StartDate": "2024-01-15",
  "CreationTimestamp": "2024-01-15 14:30:00",
  "LastAccessed": "2024-01-15 16:45:00"
}
```

## Save Triggers

The system saves PaxDrops data when any of these game events occur:

1. **Sleep/Day Progression**: Game auto-saves when player sleeps
2. **Manual Save**: Player presses save button in properties menu
3. **Console Save**: Player uses `save` command in game console
4. **Other Game Saves**: Any other game-triggered save operation

## Migration from Legacy System

### ✅ Migration Complete
The legacy `JsonDataStore` system has been fully replaced by `SaveFileJsonDataStore`. 

- **Old data location**: `Mods/PaxDrops/Data/` (no longer used)
- **New data location**: `Mods/PaxDrops/SaveFiles/{SaveID}/`
- **Legacy code**: Removed from the codebase

### API Compatibility
All existing code seamlessly uses `SaveFileJsonDataStore` with the same public API:
- `SaveDrop()`, `GetAllDrops()`, `MarkDropCollected()`, etc.
- `HasMrsStacksOrderToday()`, `MarkMrsStacksOrderToday()`, etc.

**Mrs. Stacks Conversation System** is now also save-aware:
- `MrsStacksMessaging.SendMessage()` automatically saves to current save
- `MrsStacksMessaging.LoadConversationForCurrentSave()` loads conversation for current save
- `MrsStacksMessaging.UnloadConversationForCurrentSave()` unloads when exiting save
- All conversation commands (`stacks history`, etc.) work with current save

## Scene Management

### Main Scene Entry
1. Detect scene load (buildIndex == 1 or sceneName contains "Main")
2. Get current save info from `SaveManager.Instance`
3. Load appropriate PaxDrops data for that save
4. Initialize player-dependent systems

### Menu Scene Return
1. Detect scene exit (leaving main scene)
2. Unload current save data from memory
3. Reset player-dependent system flags
4. Clear temporary data

## Error Handling

### Save System Failures
- Graceful fallback if SaveManager unavailable
- Default save ID used if save identification fails
- Comprehensive logging for debugging

### Data Corruption
- JSON parsing errors handled gracefully
- Clean state ensured on load failures
- Backup/recovery mechanisms in place

## Benefits

### For Players
- **Separate Progress**: Each save file has independent PaxDrops progress
- **No Data Loss**: Switching between saves won't affect other saves
- **Reliable Persistence**: Data only saves when game saves (no data loss)
- **Clean Separation**: Menu/game transitions properly managed

### For Developers
- **Proper Integration**: Follows game's save patterns
- **Maintainable**: Clear separation of concerns
- **Extensible**: Easy to add new per-save data types
- **Debuggable**: Comprehensive logging and error handling

## Usage Examples

### Checking Current Save Status
```csharp
var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
if (isLoaded)
{
    Logger.Msg($"Current save: {saveName} (ID: {saveId}, Steam: {steamId})");
    
    // Get enhanced metadata
    var metadata = SaveFileJsonDataStore.GetCurrentSaveMetadata();
    if (metadata != null)
    {
        Logger.Msg($"Organization: {metadata.OrganizationName}");
        Logger.Msg($"Start Date: {metadata.StartDate}");
        Logger.Msg($"Directory: SaveFiles/{metadata.SteamId}/{metadata.SaveId}/");
    }
}
```

### Working with Save-Aware Data
```csharp
// This automatically works with the current save
SaveFileJsonDataStore.SaveDrop(day, items, hour);
var drops = SaveFileJsonDataStore.GetAllDrops();
bool hasOrdered = SaveFileJsonDataStore.HasMrsStacksOrderToday(currentDay);
```

## Console Commands

Enhanced console commands now work with the current save and Steam ID:
- `pax status` - Shows current save info including Steam ID and metadata
- `paxdrop debug_saves` - Shows all save directories organized by Steam ID
- `paxdrop analyze_saves` - Analyze saves organized by Steam user
- `paxdrop cleanup_saves` - Clean up duplicates within each Steam user's saves
- `paxdrop test_saveid` - Test enhanced save ID generation

## Logging

Enhanced logging provides visibility into save operations:
- `[SaveSystemPatch]` - Save system hook events
- `[SaveFileJsonDataStore]` - Enhanced data load/save operations with Steam ID
- `[InitMain]` - Scene transitions and save loading
- `[MrsStacksMessaging]` - Conversation persistence per save

## Enhanced Benefits

### For Players
- **User Isolation**: Each Steam user has completely separate PaxDrops data
- **Robust Save Identity**: Save IDs incorporate actual game content, not just file paths
- **Better Organization**: Clear folder structure organized by Steam ID
- **Rich Metadata**: Each save includes organization name, start date, and access history
- **Migration Safe**: Enhanced identification prevents save conflicts during file moves

### For Developers  
- **Content-Aware**: Save IDs incorporate game state, not just file system info
- **Steam Integration**: Natural integration with Steam user identification
- **Rich Debugging**: Comprehensive metadata for troubleshooting
- **Future-Proof**: Enhanced identification supports game updates and file system changes

## Troubleshooting

### 🔍 **Issue: Duplicate Save Directories** 

**Problem**: You see multiple save directories for the same game save.

**Solution**: The enhanced system now prevents duplicates through:
1. **Enhanced ID Generation**: Content-based IDs are more consistent
2. **Steam ID Organization**: Users are naturally separated  
3. **Analysis Tools**: Use `paxdrop analyze_saves` to identify issues
4. **Cleanup Tools**: Use `paxdrop cleanup_saves` to merge duplicates per user

**Commands**:
```
paxdrop analyze_saves   # Show all saves organized by Steam ID
paxdrop cleanup_saves   # Clean up duplicates within each user's saves
paxdrop debug_saves     # Show current save directory structure
paxdrop test_saveid paths # Test save ID generation with actual paths
```

### 📊 **Enhanced Debugging**

#### New Debug Information
- Steam ID extraction and validation
- Organization name retrieval from game
- Save start date detection
- Enhanced metadata persistence
- Per-user save organization

#### Debug Commands
```
pax status              # Enhanced save status with metadata
paxdrop debug_saves     # Steam ID organized save structure  
paxdrop test_saveid     # Test enhanced save ID generation
```

### Common Issues

#### "No save loaded" warnings
- Normal during initialization - data will load once in game
- Check that Steam ID extraction is working from save paths

#### Save metadata missing
- Occurs for saves created before the enhancement
- Metadata will be generated on next save operation

#### Steam ID extraction issues
- System falls back to path-based hash if Steam ID not found
- Check save path formats in logs for debugging

## Technical Notes

### Enhanced Architecture
- **Steam ID Extraction**: Multiple methods to identify Steam users from save paths
- **Content-Based Identity**: Save IDs incorporate actual game save content
- **Metadata Persistence**: Rich save information stored alongside game data
- **Backward Compatibility**: System handles both enhanced and legacy save identification

### Performance
- **Efficient ID Generation**: Enhanced hashing with multiple factors
- **Lazy Metadata Loading**: Metadata loaded only when needed
- **Steam ID Caching**: User identification cached per session
- **Optimized File Structure**: Organized by Steam ID for faster access

### Memory Management  
- **Per-User Organization**: Natural memory boundaries by Steam ID
- **Enhanced Cleanup**: Metadata-aware cleanup and validation
- **Efficient Lookups**: Steam ID organization improves performance