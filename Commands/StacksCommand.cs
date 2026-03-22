using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.GameTime;
using PaxDrops.MrStacks;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Handles the 'stacks' and 'mrstacks' console commands for Mr. Stacks functionality.
    /// Usage: stacks [order|status|reset|help|history|save|clear]
    /// </summary>
    public static class StacksCommand
    {
        public const string CommandName = "stacks";
        public const string AltName = "mrstacks";
        public const string Description = "Interact with Mr. Stacks dealer system";
        public const string Usage = "stacks [order|status|reset|help|history|save|clear]";

        /// <summary>
        /// Executes the stacks command with the given arguments
        /// </summary>
        public static void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                if (args.Count < 2)
                {
                    ShowHelp();
                    return;
                }

                string subCommand = args[1].ToLower();
                var timeManager = TimeManager.Instance;
                int currentDay = timeManager?.ElapsedDays ?? 0;
                
                switch (subCommand)
                {
                    case "order":
                        PlaceOrder(currentDay);
                        break;
                        
                    case "status":
                        ShowStatus(currentDay);
                        break;
                        
                    case "reset":
                        ResetDaily(currentDay);
                        break;
                        
                    case "history":
                        ShowConversationHistory();
                        break;
                        
                    case "save":
                        TestConversationSave();
                        break;
                        
                    case "clear":
                        ClearConversationHistory();
                        break;
                        
                    case "help":
                        ShowHelp();
                        break;
                        
                    default:
                        Il2CppScheduleOne.Console.LogError($"[Stacks] Unknown command: {subCommand}. Use 'stacks help' for commands.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] Command error: {ex.Message}");
                Logger.Error($"[StacksCommand] Command execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Place an order with Mr. Stacks using OrderProcessor (once per day limit)
        /// </summary>
        private static void PlaceOrder(int currentDay)
        {
            try
            {
                // Check if already ordered today
                if (SaveFileJsonDataStore.HasMrStacksOrderToday(currentDay))
                {
                    Il2CppScheduleOne.Console.Log($"⚠️ You've already ordered from Mr. Stacks today (Day {currentDay})");
                    return;
                }

                Il2CppScheduleOne.Console.Log("[Stacks] 📦 Placing order with Mr. Stacks...");
                
                // Use the unified order processor with messaging enabled
                OrderProcessor.ProcessOrder("Mr. Stacks", "console_order", null, null, true);
                
                Il2CppScheduleOne.Console.Log("[Stacks] ✅ Order placed! Check your messages for confirmation.");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Order command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Show order status for Mr. Stacks
        /// </summary>
        private static void ShowStatus(int currentDay)
        {
            try
            {
                bool orderedToday = SaveFileJsonDataStore.HasMrStacksOrderToday(currentDay);
                string status = orderedToday ? "✅ Ordered today" : "⏳ Available to order";
                
                Il2CppScheduleOne.Console.Log("=== Mr. Stacks Status ===");
                Il2CppScheduleOne.Console.Log($"Today (Day {currentDay}): {status}");
                Il2CppScheduleOne.Console.Log("Orders: Once per day limit");
                Il2CppScheduleOne.Console.Log("Quality: Premium surprise packages");
                Il2CppScheduleOne.Console.Log("Contact: Via messaging system");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Status command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Reset the daily order limit for Mr. Stacks
        /// </summary>
        private static void ResetDaily(int currentDay)
        {
            try
            {
                SaveFileJsonDataStore.MrStacksOrdersToday.Remove(currentDay);
                Il2CppScheduleOne.Console.Log($"🔄 Reset Mr. Stacks orders for Day {currentDay}");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"❌ Reset command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Show Mr. Stacks conversation history (JSON-based)
        /// </summary>
        private static void ShowConversationHistory()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("=== Mr. Stacks Conversation History ===");
                MrStacksMessaging.ShowConversationHistory();
                Il2CppScheduleOne.Console.Log("Check logs for detailed conversation stats");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] History check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Test conversation save functionality (JSON-based)
        /// </summary>
        private static void TestConversationSave()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("=== Testing Mr. Stacks Conversation Save ===");
                MrStacksMessaging.ForceSaveConversation();
                Il2CppScheduleOne.Console.Log("Save test completed - check logs for details");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] Save test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear conversation history (for testing)
        /// </summary>
        private static void ClearConversationHistory()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("=== Clearing Mr. Stacks Conversation History ===");
                MrStacksMessaging.ClearConversationHistory();
                Il2CppScheduleOne.Console.Log("Conversation history cleared");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] Clear failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show help for the stacks command
        /// </summary>
        private static void ShowHelp()
        {
            Il2CppScheduleOne.Console.Log("=== Mr. Stacks Commands ===");
            Il2CppScheduleOne.Console.Log("  stacks order   - Place an order (once per day)");
            Il2CppScheduleOne.Console.Log("  stacks status  - Show order status");
            Il2CppScheduleOne.Console.Log("  stacks reset   - Reset daily limit");
            Il2CppScheduleOne.Console.Log("  stacks history - Show conversation history (JSON)");
            Il2CppScheduleOne.Console.Log("  stacks save    - Test conversation persistence");
            Il2CppScheduleOne.Console.Log("  stacks clear   - Clear conversation history");
            Il2CppScheduleOne.Console.Log("  stacks help    - Show this help");
            Il2CppScheduleOne.Console.Log("  Note: Uses JSON-based conversation persistence!");
        }
    }
} 
