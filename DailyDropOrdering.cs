using System;
using Il2CppScheduleOne.GameTime;
using PaxDrops.Configs;
using PaxDrops.MrStacks;

namespace PaxDrops
{
    /// <summary>
    /// Tracks Mr. Stacks ordered-drop availability and messaging against the dedicated 3x3 tier menu.
    /// </summary>
    public static class DailyDropOrdering
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Info("Daily drop ordering system initialized", "DailyDropOrdering");
        }

        /// <summary>
        /// Check whether the player still has any Mr. Stacks orders available today.
        /// </summary>
        public static bool CanPlayerOrderToday()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("TimeManager not available", "DailyDropOrdering");
                    return false;
                }

                var currentRank = DropConfig.GetCurrentPlayerRank();
                int currentDay = timeManager.ElapsedDays;
                var highestTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestTier.HasValue)
                {
                    return false;
                }

                int dailyLimit = OrderedDropConfig.GetDailyOrderLimit(highestTier.Value);
                int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);
                return ordersToday < dailyLimit;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking order availability: {ex.Message}", "DailyDropOrdering");
                return false;
            }
        }

        /// <summary>
        /// Get remaining Mr. Stacks orders for today.
        /// </summary>
        public static int GetRemainingOrdersToday()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    return 0;
                }

                var currentRank = DropConfig.GetCurrentPlayerRank();
                int currentDay = timeManager.ElapsedDays;
                var highestTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestTier.HasValue)
                {
                    return 0;
                }

                int dailyLimit = OrderedDropConfig.GetDailyOrderLimit(highestTier.Value);
                int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);
                return Math.Max(0, dailyLimit - ordersToday);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting remaining orders: {ex.Message}", "DailyDropOrdering");
                return 0;
            }
        }

        /// <summary>
        /// Get a short status string for the player's current Mr. Stacks access.
        /// </summary>
        public static string GetOrderStatusInfo()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    return "Time system unavailable";
                }

                var currentRank = DropConfig.GetCurrentPlayerRank();
                int currentDay = timeManager.ElapsedDays;
                var highestTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestTier.HasValue)
                {
                    return $"Day {currentDay}: no Mr. Stacks order tier unlocked yet";
                }

                int dailyLimit = OrderedDropConfig.GetDailyOrderLimit(highestTier.Value);
                int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);
                int remaining = Math.Max(0, dailyLimit - ordersToday);
                string tierName = OrderedDropConfig.GetTierName(highestTier.Value);

                return $"Day {currentDay}: {ordersToday}/{dailyLimit} orders used, {remaining} remaining | top ordered tier: {tierName}";
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting status info: {ex.Message}", "DailyDropOrdering");
                return "Status unavailable";
            }
        }

        /// <summary>
        /// Process an explicit Mr. Stacks tier selection.
        /// </summary>
        public static bool ProcessMrStacksOrder(OrderedDropConfig.OrderedTier selectedTier, bool sendMessages = true)
        {
            try
            {
                return OrderProcessor.ProcessMrStacksTierOrder(selectedTier, sendMessages);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing Mr. Stacks order: {ex.Message}", "DailyDropOrdering");
                return false;
            }
        }

        /// <summary>
        /// Convenience helper for debug/console flows that should order the best currently unlocked tier.
        /// </summary>
        public static bool ProcessHighestUnlockedMrStacksOrder(bool sendMessages = true)
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    return false;
                }

                int currentDay = timeManager.ElapsedDays;
                var currentRank = DropConfig.GetCurrentPlayerRank();
                var highestTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestTier.HasValue)
                {
                    if (sendMessages)
                    {
                        SendNoUnlockedTierMessage(currentDay, currentRank);
                    }

                    return false;
                }

                return ProcessMrStacksOrder(highestTier.Value, sendMessages);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error processing highest unlocked order: {ex.Message}", "DailyDropOrdering");
                return false;
            }
        }

        public static void Shutdown()
        {
            _initialized = false;
            Logger.Info("Daily drop ordering shutdown", "DailyDropOrdering");
        }

        /// <summary>
        /// Send reminder message if player has not ordered in 4+ days.
        /// </summary>
        public static void SendInactivityReminderIfNeeded()
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentDay = timeManager.ElapsedDays;
                int daysSinceLastOrder = SaveFileJsonDataStore.GetDaysSinceLastMrStacksOrder(currentDay);

                bool shouldSendReminder = false;
                if (daysSinceLastOrder == -1)
                {
                    shouldSendReminder = currentDay >= 4;
                }
                else if (daysSinceLastOrder >= 4)
                {
                    shouldSendReminder = true;
                }

                if (!shouldSendReminder)
                {
                    return;
                }

                var currentRank = DropConfig.GetCurrentPlayerRank();
                var highestTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestTier.HasValue)
                {
                    return;
                }

                string tierName = OrderedDropConfig.GetTierName(highestTier.Value);
                string groupName = OrderedDropConfig.GetGroupName(OrderedDropConfig.GetGroup(highestTier.Value));
                int dailyLimit = OrderedDropConfig.GetDailyOrderLimit(highestTier.Value);

                var reminderMessages = daysSinceLastOrder == -1
                    ? new[]
                    {
                        $"Mr. Stacks here. You've got access up through {tierName} in {groupName}, with {dailyLimit} order slot(s) a day. Hit the thread when you want to pick a package.",
                        $"Need a package lined up? Your current access tops out at {tierName} from {groupName}. Open the message thread and choose what tier you want.",
                        $"You've got the line to me now. {tierName} is available, more tiers open with rank and time, and deliveries still land at 07:30 the next day."
                    }
                    : new[]
                    {
                        $"Been {daysSinceLastOrder} days since our last deal. {tierName} packages from {groupName} are still available if you want back in.",
                        $"Quiet stretch. It's been {daysSinceLastOrder} days, and your {tierName} access is still live. Message me when you want another drop lined up.",
                        $"You still have {tierName} access and up to {dailyLimit} order slot(s) a day. If you want to move product again, open the thread and pick a tier."
                    };

                var random = new Random();
                MrStacksMessaging.SendMessage(npc, reminderMessages[random.Next(reminderMessages.Length)]);
                Logger.Debug($"Sent inactivity reminder after {daysSinceLastOrder} day(s)", "DailyDropOrdering");
            }
            catch (Exception ex)
            {
                Logger.Error($"Inactivity reminder failed: {ex.Message}", "DailyDropOrdering");
            }
        }

        /// <summary>
        /// Send welcome message when Mr. Stacks is first discovered.
        /// </summary>
        public static void SendWelcomeMessage()
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                var currentRank = DropConfig.GetCurrentPlayerRank();
                int currentDay = DropConfig.GetCurrentGameDay();
                var highestTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestTier.HasValue)
                {
                    MrStacksMessaging.SendMessage(
                        npc,
                        $"I'm Mr. Stacks. Your line opens once you hit day 1 and Street Rat access. Check back soon and I'll start taking orders.");
                    return;
                }

                string tierName = OrderedDropConfig.GetTierName(highestTier.Value);
                string groupName = OrderedDropConfig.GetGroupName(OrderedDropConfig.GetGroup(highestTier.Value));
                int dailyLimit = OrderedDropConfig.GetDailyOrderLimit(highestTier.Value);

                var welcomeMessage =
                    $"I'm Mr. Stacks, your connection for ordered dead drops. Right now you've got access through {tierName} in {groupName}, with {dailyLimit} order slot(s) per day. " +
                    $"Pick a group in the thread, choose the tier you want, and I'll have it moving for tomorrow's 07:30 drop.";

                MrStacksMessaging.SendMessage(npc, welcomeMessage);
                Logger.Info("Welcome message sent", "DailyDropOrdering");
            }
            catch (Exception ex)
            {
                Logger.Error($"Welcome message failed: {ex.Message}", "DailyDropOrdering");
            }
        }

        private static void SendNoUnlockedTierMessage(int currentDay, Il2CppScheduleOne.Levelling.ERank currentRank)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                MrStacksMessaging.SendMessage(
                    npc,
                    $"No package tiers are unlocked yet. Orders open on day 1 at rank Street Rat. You're on day {currentDay} at rank {OrderedDropConfig.FormatRankName(currentRank)}.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to send no-tier message: {ex.Message}", "DailyDropOrdering");
            }
        }
    }
}
