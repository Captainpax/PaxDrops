using System;
using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.GameTime;
using PaxDrops.MrStacks;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Handles the 'paxdrop' console command for spawning debug drops.
    /// Usage: paxdrop [day] [type]
    /// Examples: paxdrop, paxdrop 12, paxdrop 12 order:4
    /// </summary>
    public static class PaxDropCommand
    {
        public const string CommandName = "paxdrop";
        public const string ShortName = "pax";
        public const string Description = "Spawns a debug PaxDrop dead drop. Optional args: [day] [type]";
        public const string Usage = "paxdrop [day] [type]";
        public const string Example = "paxdrop 12 order:4";

        /// <summary>
        /// Executes the paxdrop command with the given arguments
        /// </summary>
        public static void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                if (args.Count >= 2)
                {
                    string subCommand = args[1].ToLower();
                    
                    switch (subCommand)
                    {
                        case "trigger":
                        case "test":
                            TriggerTestDrop();
                            break;
                            
                        case "status":
                            HandleStatusCommand();
                            break;
                            
                        case "debug_saves":
                            HandleDebugSavesCommand();
                            break;
                            
                        case "test_saveid":
                            HandleTestSaveIdCommand(args);
                            break;
                            
                        case "cleanup_saves":
                            HandleCleanupSavesCommand();
                            break;
                            
                        case "analyze_saves":
                            HandleAnalyzeSavesCommand();
                            break;
                            
                        case "metadata":
                            HandleMetadataCommand();
                            break;
                            
                        case "help":
                            ShowHelp();
                            break;
                            
                        default:
                            // If it's not a subcommand, treat it as the old-style args: [day] [type]
                            ExecuteDropSpawn(args);
                            break;
                    }
                }
                else
                {
                    // No args or just "paxdrop" - spawn with current day
                    ExecuteDropSpawn(args);
                }
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[PaxDrop] Command error: {ex.Message}");
                Logger.Error($"[PaxDropCommand] Command execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute drop spawning using the unified OrderProcessor system
        /// </summary>
        private static void ExecuteDropSpawn(Il2CppSystem.Collections.Generic.List<string> args)
        {
            var timeManager = TimeManager.Instance;
            int day = timeManager?.ElapsedDays ?? 0;
            string type = "dev_command";

            // Parse day from args[1] if present
            if (args.Count >= 2 && int.TryParse(args[1], out int parsedDay))
                day = parsedDay;

            // Parse type from args[2] if present
            if (args.Count >= 3)
                type = args[2];

            // Use unified OrderProcessor for consistent behavior
            OrderProcessor.ProcessOrder(
                organization: "DevCommand", 
                orderType: type, 
                customDay: day, 
                customItems: null, // Let it generate random package
                sendMessages: false // No messaging for dev commands
            );

            Il2CppScheduleOne.Console.Log($"[PaxDrop] 📦 DevCommand drop scheduled for Day {day} | Type: {type}");
            Logger.Debug($"📦 DevCommand drop scheduled - Day {day}, Type: {type}", "PaxDropCommand");
        }

        /// <summary>
        /// Trigger a test drop using the OrderProcessor
        /// </summary>
        private static void TriggerTestDrop()
        {
            try
            {
                OrderProcessor.ProcessOrder(
                    organization: "DevCommand", 
                    orderType: "console_test", 
                    customDay: null, 
                    customItems: null, 
                    sendMessages: false
                );
                
                Il2CppScheduleOne.Console.Log("[PaxDrop] 🧪 Test drop triggered successfully");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[PaxDrop] Test drop failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle status command
        /// </summary>
        private static void HandleStatusCommand()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Il2CppScheduleOne.Console.Log("❌ Time system not available");
                    return;
                }

                int currentDay = timeManager.ElapsedDays;
                int currentTime = timeManager.CurrentTime;

                Il2CppScheduleOne.Console.Log($"📊 PaxDrops Status (Day {currentDay}, Time {currentTime}):");
                Il2CppScheduleOne.Console.Log($"Pending drops: {SaveFileJsonDataStore.PendingDrops.Count}");
                
                bool mrsStacksReady = SaveFileJsonDataStore.HasMrsStacksOrderToday(currentDay);
                Il2CppScheduleOne.Console.Log($"Mrs. Stacks ready: {mrsStacksReady}");
                
                // Show player detection status
                Il2CppScheduleOne.Console.Log($"Player Status: {PaxDrops.Configs.DropConfig.GetPlayerDetectionStatus()}");
                
                // Show save file info with enhanced metadata
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                if (isLoaded)
                {
                    Il2CppScheduleOne.Console.Log($"💾 Save File: {saveName} (ID: {saveId})");
                    Il2CppScheduleOne.Console.Log($"👤 Steam ID: {steamId}");
                    
                    // Show metadata if available
                    var metadata = SaveFileJsonDataStore.GetCurrentSaveMetadata();
                    if (metadata != null)
                    {
                        Il2CppScheduleOne.Console.Log($"🏢 Organization: {metadata.OrganizationName}");
                        Il2CppScheduleOne.Console.Log($"📅 Start Date: {metadata.StartDate}");
                        Il2CppScheduleOne.Console.Log($"🕐 Last Accessed: {metadata.LastAccessed}");
                        Il2CppScheduleOne.Console.Log($"📁 Directory: SaveFiles/{metadata.SteamId}/{metadata.SaveId}/");
                    }
                }
                else
                {
                    Il2CppScheduleOne.Console.Log("💾 Save File: No save loaded");
                }
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Status command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Show help for the paxdrop command
        /// </summary>
        private static void ShowHelp()
        {
            Il2CppScheduleOne.Console.Log("=== PaxDrop Commands ===");
            Il2CppScheduleOne.Console.Log("  paxdrop [day] [type] - Spawn drop for specific day/type");
            Il2CppScheduleOne.Console.Log("  paxdrop trigger      - Force spawn a test drop");
            Il2CppScheduleOne.Console.Log("  paxdrop status       - Show system status");
            Il2CppScheduleOne.Console.Log("  paxdrop debug_saves  - Show all save directories");
            Il2CppScheduleOne.Console.Log("  paxdrop test_saveid [path] [name] - Test save ID generation");
            Il2CppScheduleOne.Console.Log("  paxdrop test_saveid paths - Test with actual log paths");
            Il2CppScheduleOne.Console.Log("  paxdrop cleanup_saves - Clean up duplicate save directories");
            Il2CppScheduleOne.Console.Log("  paxdrop analyze_saves - Show detailed save analysis");
            Il2CppScheduleOne.Console.Log("  paxdrop metadata     - Show current cached metadata");
            Il2CppScheduleOne.Console.Log("  paxdrop help         - Show this help");
            Il2CppScheduleOne.Console.Log($"  Example: {Example}");
            Il2CppScheduleOne.Console.Log("  Note: Uses unified order system - no duplicates!");
        }

        /// <summary>
        /// Handle debug saves command - show all save directories
        /// </summary>
        private static void HandleDebugSavesCommand()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("[PaxDrop] 🔍 Debug: Showing all save directories");
                SaveFileJsonDataStore.DebugShowAllSaveDirectories();
                
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                Il2CppScheduleOne.Console.Log($"Current save: {saveName} (ID: {saveId}, Steam: {steamId}, Loaded: {isLoaded})");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Debug saves failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle test save ID command - test save ID generation
        /// </summary>
        private static void HandleTestSaveIdCommand(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                if (args.Count > 2 && args[2] == "paths")
                {
                    // Test the specific paths we see in the logs
                    string basePath = "C:/users/crossover/AppData/LocalLow/TVGS/Schedule I\\Saves";
                    string specificPath = "C:/users/crossover/AppData/LocalLow/TVGS/Schedule I\\Saves\\76561198832878173\\SaveGame_2";
                    string saveName = "DevSave";
                    
                    Il2CppScheduleOne.Console.Log($"[PaxDrop] 🧪 Testing actual log paths for save ID consistency:");
                    Il2CppScheduleOne.Console.Log($"[PaxDrop]   Base path: {basePath}");
                    Il2CppScheduleOne.Console.Log($"[PaxDrop]   Specific path: {specificPath}");
                    Il2CppScheduleOne.Console.Log($"[PaxDrop]   Save name: {saveName}");
                    
                    SaveFileJsonDataStore.DebugTestSaveIdGeneration(basePath, saveName);
                    SaveFileJsonDataStore.DebugTestSaveIdGeneration(specificPath, saveName);
                }
                else
                {
                    string testPath = args.Count > 2 ? args[2] : "/default/test/path";
                    string testName = args.Count > 3 ? args[3] : "TestSave";
                    
                    Il2CppScheduleOne.Console.Log($"[PaxDrop] 🧪 Testing save ID generation for: {testPath} | {testName}");
                    SaveFileJsonDataStore.DebugTestSaveIdGeneration(testPath, testName);
                }
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Test save ID failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle cleanup saves command - clean up duplicate save directories
        /// </summary>
        private static void HandleCleanupSavesCommand()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("[PaxDrop] 🧹 Cleaning up duplicate save directories");
                SaveFileJsonDataStore.CleanupDuplicateSaves();
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Cleanup saves failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle analyze saves command - show detailed save analysis
        /// </summary>
        private static void HandleAnalyzeSavesCommand()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("[PaxDrop] 🔍 Analyzing saves");
                SaveFileJsonDataStore.AnalyzeSaves();
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Analyze saves failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle metadata command - show current cached metadata
        /// </summary>
        private static void HandleMetadataCommand()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("[PaxDrop] 🔍 Showing current cached metadata");
                SaveFileJsonDataStore.DebugShowCurrentCachedMetadata();
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Metadata command failed: {ex.Message}");
            }
        }
    }
} 