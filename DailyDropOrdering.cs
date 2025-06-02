using System;
using System.Collections.Generic;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Levelling;
using PaxDrops.Configs;
using PaxDrops.MrStacks;

namespace PaxDrops
{
    /// <summary>
    /// Handles daily drop ordering system based on player rank.
    /// Players can order drops once per day (or more based on their tier).
    /// All drops are tier-based according to player's current rank.
    /// </summary>
    public static class DailyDropOrdering
    {
        private static bool _initialized = false;

        /// <summary>
        /// Initialize the daily drop ordering system
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Logger.Msg("[DailyDropOrdering] 🎯 Daily drop ordering system initialized");
        }

        /// <summary>
        /// Order a drop for today (standard)
        /// </summary>
        public static bool OrderStandardDrop()
        {
            return OrderDrop("standard");
        }

        /// <summary>
        /// Order a premium drop for today
        /// </summary>
        public static bool OrderPremiumDrop()
        {
            return OrderDrop("premium");
        }

        /// <summary>
        /// Order a random drop for today
        /// </summary>
        public static bool OrderRandomDrop()
        {
            return OrderDrop("random");
        }

        /// <summary>
        /// Main drop ordering logic
        /// </summary>
        private static bool OrderDrop(string dropType)
        {
            try
            {
                if (!_initialized) Init();

                var currentDay = DropConfig.GetCurrentGameDay();
                var playerRank = DropConfig.GetCurrentPlayerRank();
                var playerTier = DropConfig.GetCurrentPlayerTier();

                // Check if player can order today
                if (!DropConfig.CanPlayerOrderToday(currentDay))
                {
                    var remaining = DropConfig.GetRemainingOrdersToday(currentDay);
                    SendDailyLimitMessage(playerTier, remaining);
                    return false;
                }

                // Generate the appropriate drop package
                TierDropSystem.DropPackage package = dropType.ToLower() switch
                {
                    "premium" => TierDropSystem.GeneratePremiumDropPackage(playerTier),
                    "random" => TierDropSystem.GenerateRandomDropPackage(),
                    _ => TierDropSystem.GenerateDropPackage(playerTier)
                };

                // Process the order
                var dropHour = DropConfig.GetRandomDropDelay();
                var tierInfo = $"{TierConfig.GetTierName(playerTier)} tier";
                var orgName = TierConfig.GetOrganizationName(TierConfig.GetOrganization(playerTier));

                OrderProcessor.ProcessOrder(
                    organization: "Mrs. Stacks",
                    orderType: dropType,
                    customDay: currentDay,
                    customItems: package.ToFlatList(),
                    sendMessages: true,
                    tier: playerTier
                );

                Logger.Msg($"[DailyDropOrdering] ✅ {dropType} drop ordered for Day {currentDay} from {tierInfo}");
                
                // Send confirmation message
                SendOrderConfirmationMessage(dropType, tierInfo, orgName, package.CashAmount, package.Items.Count, dropHour);
                
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Order failed: {ex.Message}");
                SendErrorMessage($"Order failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if player can order any drops today
        /// </summary>
        public static bool CanOrderToday()
        {
            var currentDay = DropConfig.GetCurrentGameDay();
            return DropConfig.CanPlayerOrderToday(currentDay);
        }

        /// <summary>
        /// Get player's ordering status for today
        /// </summary>
        public static (int ordersUsed, int ordersLimit, int ordersRemaining) GetTodaysOrderStatus()
        {
            var currentDay = DropConfig.GetCurrentGameDay();
            var playerTier = DropConfig.GetCurrentPlayerTier();
            var limit = DropConfig.GetDailyOrderLimit(playerTier);
            var used = JsonDataStore.GetMrsStacksOrdersToday(currentDay);
            var remaining = Math.Max(0, limit - used);

            return (used, limit, remaining);
        }

        /// <summary>
        /// Get player's current tier information
        /// </summary>
        public static (TierConfig.Tier tier, string tierName, string orgName, ERank playerRank) GetPlayerTierInfo()
        {
            var playerRank = DropConfig.GetCurrentPlayerRank();
            var playerTier = DropConfig.GetCurrentPlayerTier();
            var tierName = TierConfig.GetTierName(playerTier);
            var orgName = TierConfig.GetOrganizationName(TierConfig.GetOrganization(playerTier));

            return (playerTier, tierName, orgName, playerRank);
        }

        /// <summary>
        /// Get next tier progression info
        /// </summary>
        public static string GetProgressionInfo()
        {
            return TierDropSystem.GetNextTierRequirements();
        }

        /// <summary>
        /// Send order confirmation message
        /// </summary>
        private static void SendOrderConfirmationMessage(string orderType, string tierInfo, string orgName, int cashAmount, int itemCount, int hours)
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                string typeText = orderType.ToLower() switch
                {
                    "premium" => "premium package",
                    "random" => "surprise package",
                    _ => "standard package"
                };

                var message = $"Order confirmed! Preparing {typeText} from {tierInfo} ({orgName}). " +
                             $"Package contains {itemCount} items plus ${cashAmount} cash. " +
                             $"Delivery in {hours} hours - I'll text the location when ready.";

                MrsStacksMessaging.SendMessage(npc, message);
                Logger.Msg("[DailyDropOrdering] 📱 Confirmation sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Confirmation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send daily limit message
        /// </summary>
        private static void SendDailyLimitMessage(TierConfig.Tier playerTier, int remaining)
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var tierName = TierConfig.GetTierName(playerTier);
                var nextTierInfo = TierDropSystem.GetNextTierRequirements();
                
                string message;
                if (remaining <= 0)
                {
                    message = $"You've reached your daily order limit for {tierName} tier. " +
                             $"Come back tomorrow for fresh inventory. " +
                             $"Tip: {nextTierInfo}";
                }
                else
                {
                    message = $"You have {remaining} order(s) remaining today for {tierName} tier.";
                }

                MrsStacksMessaging.SendMessage(npc, message);
                Logger.Msg("[DailyDropOrdering] 📱 Daily limit message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Daily limit message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send error message
        /// </summary>
        private static void SendErrorMessage(string errorText)
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                MrsStacksMessaging.SendMessage(npc, $"Sorry, there was an issue with your order: {errorText}");
                Logger.Msg("[DailyDropOrdering] 📱 Error message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Error message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send daily availability message (called on new day)
        /// </summary>
        public static void SendDailyAvailabilityMessage()
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var (tier, tierName, orgName, playerRank) = GetPlayerTierInfo();
                var (used, limit, remaining) = GetTodaysOrderStatus();

                if (remaining > 0)
                {
                    var messages = new[]
                    {
                        $"Good morning! Fresh {tierName} tier inventory available. {remaining} order(s) left for today.",
                        $"Morning briefing: {orgName} operations are active. Premium {tierName} packages ready.",
                        $"Day's open for business! {tierName} tier drops available - {remaining} order(s) remaining.",
                        $"Fresh stock from {orgName}! Your {tierName} tier access gives you {remaining} order(s) today."
                    };

                    var random = new System.Random();
                    var dailyMessage = messages[random.Next(messages.Length)];
                    
                    MrsStacksMessaging.SendMessage(npc, dailyMessage);
                    Logger.Msg("[DailyDropOrdering] 📱 Daily availability message sent");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Daily availability failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show current ordering status (for debugging/console)
        /// </summary>
        public static void ShowOrderingStatus()
        {
            try
            {
                var (tier, tierName, orgName, playerRank) = GetPlayerTierInfo();
                var (used, limit, remaining) = GetTodaysOrderStatus();
                var currentDay = DropConfig.GetCurrentGameDay();

                Logger.Msg($"[DailyDropOrdering] 📊 Ordering Status:");
                Logger.Msg($"  Player Rank: {playerRank}");
                Logger.Msg($"  Current Tier: {tierName} ({orgName})");
                Logger.Msg($"  Game Day: {currentDay}");
                Logger.Msg($"  Orders Today: {used}/{limit} (Remaining: {remaining})");
                Logger.Msg($"  Can Order: {(remaining > 0 ? "Yes" : "No")}");
                Logger.Msg($"  Progression: {GetProgressionInfo()}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DailyDropOrdering] ❌ Status display failed: {ex.Message}");
            }
        }
    }
} 