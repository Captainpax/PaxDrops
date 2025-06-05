using System;
using MelonLoader;
using Il2CppScheduleOne.GameTime;
using PaxDrops.Configs;
using PaxDrops.MrStacks;

namespace PaxDrops
{
    /// <summary>
    /// Enhanced daily drop ordering system - properly tracks order day vs delivery day
    /// Ensures once-per-day ordering based on when the order was placed, not when delivered
    /// </summary>
    public static class DailyDropOrdering
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Msg("[DailyDropOrdering] 🎯 Daily drop ordering system initialized");
        }

        /// <summary>
        /// Check if player can order drops today (based on order day, not delivery day)
        /// </summary>
        public static bool CanPlayerOrderToday()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("[DailyDropOrdering] ❌ TimeManager not available");
                    return false;
                }

                int currentDay = timeManager.ElapsedDays;
                return DropConfig.CanPlayerOrderToday(currentDay);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Error checking order availability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get remaining orders available today for the player
        /// </summary>
        public static int GetRemainingOrdersToday()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) return 0;

                int currentDay = timeManager.ElapsedDays;
                return DropConfig.GetRemainingOrdersToday(currentDay);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Error getting remaining orders: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get player's order status information
        /// </summary>
        public static string GetOrderStatusInfo()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) return "Time system unavailable";

                int currentDay = timeManager.ElapsedDays;
                var currentTier = DropConfig.GetCurrentPlayerTier();
                var dailyLimit = DropConfig.GetDailyOrderLimit(currentTier);
                var ordersToday = SaveFileJsonDataStore.GetMrsStacksOrdersToday(currentDay);
                int remaining = dailyLimit - ordersToday;

                return $"Day {currentDay}: {ordersToday}/{dailyLimit} orders used, {remaining} remaining";
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Error getting status info: {ex.Message}");
                return "Status unavailable";
            }
        }

        /// <summary>
        /// Process a Mrs. Stacks drop order - handles daily limit enforcement
        /// </summary>
        public static void ProcessMrsStacksOrder(string orderType = "standard", int? tier = null, bool sendMessages = true)
        {
            try
            {
                if (!CanPlayerOrderToday())
                {
                    var statusInfo = GetOrderStatusInfo();
                    Logger.Msg($"[DailyDropOrdering] 🚫 Daily order limit reached - {statusInfo}");
                    
                    if (sendMessages)
                    {
                        var timeManager = TimeManager.Instance;
                        int currentDay = timeManager?.ElapsedDays ?? 0;
                        var currentTier = DropConfig.GetCurrentPlayerTier();
                        var dailyLimit = DropConfig.GetDailyOrderLimit(currentTier);
                        
                        SendDailyLimitMessage(dailyLimit);
                    }
                    return;
                }

                // Process the order through OrderProcessor
                OrderProcessor.ProcessOrder("Mrs. Stacks", orderType, tier, sendMessages: sendMessages);
                
                Logger.Msg($"[DailyDropOrdering] ✅ Mrs. Stacks {orderType} order processed");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Error processing Mrs. Stacks order: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Send daily limit reached message
        /// </summary>
        private static void SendDailyLimitMessage(int dailyLimit)
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                string limitText = dailyLimit switch
                {
                    1 => "one order per day",
                    2 => "two orders per day", 
                    3 => "three orders per day",
                    4 => "four orders per day",
                    _ => $"{dailyLimit} orders per day"
                };

                MrsStacksMessaging.SendMessage(npc, 
                    $"You've reached your daily limit of {limitText}. " +
                    $"Come back tomorrow for more business opportunities.");

                Logger.Msg("[DailyDropOrdering] 📱 Daily limit message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Failed to send daily limit message: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            _initialized = false;
            Logger.Msg("[DailyDropOrdering] 🔌 Daily drop ordering shutdown");
        }

        /// <summary>
        /// Send reminder message if player hasn't ordered in 4+ days
        /// </summary>
        public static void SendInactivityReminderIfNeeded()
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentDay = timeManager.ElapsedDays;
                int daysSinceLastOrder = SaveFileJsonDataStore.GetDaysSinceLastMrsStacksOrder(currentDay);
                
                // Send reminder if 4+ days since last order (or never ordered and it's been 4+ days since start)
                bool shouldSendReminder = false;
                
                if (daysSinceLastOrder == -1) // Never ordered
                {
                    // Send reminder if it's been 4+ days since game start
                    if (currentDay >= 4)
                    {
                        shouldSendReminder = true;
                    }
                }
                else if (daysSinceLastOrder >= 4)
                {
                    shouldSendReminder = true;
                }

                if (!shouldSendReminder) return;

                var currentTier = DropConfig.GetCurrentPlayerTier();
                var currentRank = DropConfig.GetCurrentPlayerRank();
                var dailyLimit = DropConfig.GetDailyOrderLimit(currentTier);
                var tierName = TierConfig.GetTierName(currentTier);
                var org = TierConfig.GetOrganization(currentTier);
                var orgName = TierConfig.GetOrganizationName(org);

                var reminderMessages = daysSinceLastOrder == -1 ? new[]
                {
                    // First-time user messages
                    $"Hey there! I'm Mrs. Stacks, your connection to {orgName}. Your {currentRank} rank gives you access to {tierName} tier drops. Interested in some business?",
                    $"Word on the street is you might need some... special deliveries. I handle {tierName} tier packages for {orgName}. What do you say?",
                    $"New face around here? I'm Mrs. Stacks - I arrange discrete deliveries. Your {tierName} tier access gets you {dailyLimit} order(s) per day.",
                    $"Looking for reliable supply lines? {orgName} operations are my specialty. Your rank unlocks {tierName} tier packages."
                } : new[]
                {
                    // Returning customer messages
                    $"It's been {daysSinceLastOrder} days since our last business. Missing the quality {tierName} tier packages? {orgName} has fresh inventory waiting.",
                    $"Haven't heard from you in {daysSinceLastOrder} days! The {tierName} tier supply chain is running smooth. Ready for another order?",
                    $"Mrs. Stacks here - been {daysSinceLastOrder} days since your last drop. Your {currentRank} access to {tierName} tier is still active. Need anything?",
                    $"Long time no see! {daysSinceLastOrder} days without business. {orgName} has some premium {tierName} tier stock if you're interested.",
                    $"Day {currentDay} check-in: It's been {daysSinceLastOrder} days since our last deal. Your {tierName} tier privileges are still good - want to place an order?"
                };

                var random = new System.Random();
                var reminderMessage = reminderMessages[random.Next(reminderMessages.Length)];
                
                MrsStacksMessaging.SendMessage(npc, reminderMessage);
                
                string logType = daysSinceLastOrder == -1 ? "first-time" : $"{daysSinceLastOrder}-day inactivity";
                Logger.Msg($"[DailyDropOrdering] 📱 Sent {logType} reminder message");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Inactivity reminder failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Send welcome message when Mrs. Stacks is first discovered
        /// </summary>
        public static void SendWelcomeMessage()
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var currentTier = DropConfig.GetCurrentPlayerTier();
                var currentRank = DropConfig.GetCurrentPlayerRank();
                var dailyLimit = DropConfig.GetDailyOrderLimit(currentTier);
                var tierName = TierConfig.GetTierName(currentTier);
                var org = TierConfig.GetOrganization(currentTier);
                var orgName = TierConfig.GetOrganizationName(org);

                var welcomeMessage = $"Welcome to the network! I'm Mrs. Stacks, your connection to {orgName} operations. " +
                                   $"Your {currentRank} rank gives you access to {tierName} tier packages - up to {dailyLimit} order(s) per day. " +
                                   $"Deliveries arrive at 7:30 AM and expire after 24 hours. Ready to do business?";

                MrsStacksMessaging.SendMessage(npc, welcomeMessage);
                Logger.Msg("[DailyDropOrdering] 📱 Welcome message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Welcome message failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }
    }
} 