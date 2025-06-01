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
                            ShowStatus();
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
            Logger.Msg($"📦 DevCommand drop scheduled - Day {day}, Type: {type}");
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
        /// Show current status of the drop system
        /// </summary>
        private static void ShowStatus()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                int currentDay = timeManager?.ElapsedDays ?? 0;
                int currentTime = timeManager?.CurrentTime ?? 0;
                
                Il2CppScheduleOne.Console.Log("=== PaxDrop Status ===");
                Il2CppScheduleOne.Console.Log($"Day: {currentDay}, Time: {currentTime}");
                Il2CppScheduleOne.Console.Log($"Pending drops: {JsonDataStore.PendingDrops.Count}");
                
                bool mrsStacksReady = JsonDataStore.HasMrsStacksOrderToday(currentDay);
                string stacksStatus = mrsStacksReady ? "Already ordered today" : "Ready to order";
                Il2CppScheduleOne.Console.Log($"Mrs. Stacks: {stacksStatus}");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[PaxDrop] Status error: {ex.Message}");
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
            Il2CppScheduleOne.Console.Log("  paxdrop help         - Show this help");
            Il2CppScheduleOne.Console.Log($"  Example: {Example}");
            Il2CppScheduleOne.Console.Log("  Note: Uses unified order system - no duplicates!");
        }
    }
} 