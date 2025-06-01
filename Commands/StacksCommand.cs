using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.GameTime;
using PaxDrops.MrStacks;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Handles the 'stacks' and 'mrsstacks' console commands for Mrs. Stacks functionality.
    /// Usage: stacks [order|status|reset|help|history|save|clear]
    /// </summary>
    public static class StacksCommand
    {
        public const string CommandName = "stacks";
        public const string AltName = "mrsstacks";
        public const string Description = "Interact with Mrs. Stacks dealer system";
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
        /// Place an order with Mrs. Stacks using OrderProcessor (once per day limit)
        /// </summary>
        private static void PlaceOrder(int currentDay)
        {
            try
            {
                // Check if already ordered today
                if (JsonDataStore.HasMrsStacksOrderToday(currentDay))
                {
                    Il2CppScheduleOne.Console.LogError("[Stacks] ❌ You already placed an order today. Mrs. Stacks only does one drop per customer per day.");
                    return;
                }

                Il2CppScheduleOne.Console.Log("[Stacks] 📦 Placing order with Mrs. Stacks...");
                
                // Use the unified order processor with messaging enabled
                OrderProcessor.ProcessOrder("Mrs. Stacks", "console_order", null, null, true);
                
                Il2CppScheduleOne.Console.Log("[Stacks] ✅ Order placed! Check your messages for confirmation.");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] Order failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show order status for Mrs. Stacks
        /// </summary>
        private static void ShowStatus(int currentDay)
        {
            try
            {
                bool orderedToday = JsonDataStore.HasMrsStacksOrderToday(currentDay);
                string status = orderedToday ? "✅ Ordered today" : "⏳ Available to order";
                
                Il2CppScheduleOne.Console.Log("=== Mrs. Stacks Status ===");
                Il2CppScheduleOne.Console.Log($"Today (Day {currentDay}): {status}");
                Il2CppScheduleOne.Console.Log("Orders: Once per day limit");
                Il2CppScheduleOne.Console.Log("Quality: Premium surprise packages");
                Il2CppScheduleOne.Console.Log("Contact: Via messaging system");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] Status check failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset the daily order limit for Mrs. Stacks
        /// </summary>
        private static void ResetDaily(int currentDay)
        {
            try
            {
                JsonDataStore.MrsStacksOrdersToday.Remove(currentDay);
                Il2CppScheduleOne.Console.Log("[Stacks] 🔄 Daily order limit reset");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.LogError($"[Stacks] Reset failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show Mrs. Stacks conversation history (JSON-based)
        /// </summary>
        private static void ShowConversationHistory()
        {
            try
            {
                Il2CppScheduleOne.Console.Log("=== Mrs. Stacks Conversation History ===");
                MrsStacksMessaging.ShowConversationHistory();
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
                Il2CppScheduleOne.Console.Log("=== Testing Mrs. Stacks Conversation Save ===");
                MrsStacksMessaging.ForceSaveConversation();
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
                Il2CppScheduleOne.Console.Log("=== Clearing Mrs. Stacks Conversation History ===");
                MrsStacksMessaging.ClearConversationHistory();
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
            Il2CppScheduleOne.Console.Log("=== Mrs. Stacks Commands ===");
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