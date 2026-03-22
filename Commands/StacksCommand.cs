using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.GameTime;
using PaxDrops.Configs;
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
        /// Executes the stacks command with the given arguments.
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

                string subCommand = args[1].ToLowerInvariant();
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
        /// Place an order using the highest currently unlocked ordered tier.
        /// </summary>
        private static void PlaceOrder(int currentDay)
        {
            try
            {
                var highestTier = OrderedDropConfig.GetHighestUnlockedTierForCurrentPlayer();
                if (!highestTier.HasValue)
                {
                    Il2CppScheduleOne.Console.Log("[Stacks] No ordered Mr. Stacks tier is unlocked yet.");
                    return;
                }

                int dailyLimit = OrderedDropConfig.GetCurrentDailyOrderLimit();
                int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);
                if (ordersToday >= dailyLimit)
                {
                    Il2CppScheduleOne.Console.Log($"[Stacks] Daily limit reached ({ordersToday}/{dailyLimit}).");
                    return;
                }

                string tierName = OrderedDropConfig.GetTierName(highestTier.Value);
                Il2CppScheduleOne.Console.Log($"[Stacks] Placing {tierName} order with Mr. Stacks...");

                bool orderSucceeded = DailyDropOrdering.ProcessHighestUnlockedMrStacksOrder(true);
                if (orderSucceeded)
                {
                    Il2CppScheduleOne.Console.Log("[Stacks] Order placed. Check your messages for confirmation.");
                }
                else
                {
                    Il2CppScheduleOne.Console.Log("[Stacks] Order failed. Check your messages or logs for details.");
                }
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"[Stacks] Order command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Show order status for Mr. Stacks.
        /// </summary>
        private static void ShowStatus(int currentDay)
        {
            try
            {
                int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);
                int dailyLimit = OrderedDropConfig.GetCurrentDailyOrderLimit();
                int remaining = DailyDropOrdering.GetRemainingOrdersToday();
                var highestTier = OrderedDropConfig.GetHighestUnlockedTierForCurrentPlayer();
                string tierName = highestTier.HasValue ? OrderedDropConfig.GetTierName(highestTier.Value) : "None";
                string status = remaining > 0 ? "Available to order" : "Daily limit reached";

                Il2CppScheduleOne.Console.Log("=== Mr. Stacks Status ===");
                Il2CppScheduleOne.Console.Log($"Today (Day {currentDay}): {status}");
                Il2CppScheduleOne.Console.Log($"Orders: {ordersToday}/{dailyLimit} used ({remaining} remaining)");
                Il2CppScheduleOne.Console.Log($"Top ordered tier: {tierName}");
                Il2CppScheduleOne.Console.Log("Selection: 3 groups x 3 subtiers in the message menu");
                Il2CppScheduleOne.Console.Log("Contact: Via messaging system");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"[Stacks] Status command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Reset the daily order limit for Mr. Stacks.
        /// </summary>
        private static void ResetDaily(int currentDay)
        {
            try
            {
                SaveFileJsonDataStore.MrStacksOrdersToday.Remove(currentDay);
                Il2CppScheduleOne.Console.Log($"[Stacks] Reset Mr. Stacks orders for Day {currentDay}");
            }
            catch (Exception ex)
            {
                Il2CppScheduleOne.Console.Log($"[Stacks] Reset command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Show Mr. Stacks conversation history.
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
        /// Test conversation save functionality.
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
        /// Clear conversation history for testing.
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
        /// Show help for the stacks command.
        /// </summary>
        private static void ShowHelp()
        {
            Il2CppScheduleOne.Console.Log("=== Mr. Stacks Commands ===");
            Il2CppScheduleOne.Console.Log("  stacks order   - Order your highest unlocked Mr. Stacks tier");
            Il2CppScheduleOne.Console.Log("  stacks status  - Show order status");
            Il2CppScheduleOne.Console.Log("  stacks reset   - Reset daily limit");
            Il2CppScheduleOne.Console.Log("  stacks history - Show conversation history (SQLite)");
            Il2CppScheduleOne.Console.Log("  stacks save    - Test conversation persistence");
            Il2CppScheduleOne.Console.Log("  stacks clear   - Clear conversation history");
            Il2CppScheduleOne.Console.Log("  stacks help    - Show this help");
            Il2CppScheduleOne.Console.Log("  Note: The in-game message menu lets you pick a specific ordered tier.");
        }
    }
}
