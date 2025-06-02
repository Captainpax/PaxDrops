using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using Il2CppScheduleOne.GameTime;
using PaxDrops.Configs;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Shared order processing system for both Mrs. Stacks and DevCommand orders.
    /// Handles the complete flow: validation → confirmation → delay → spawn → notification
    /// Uses new TierDropSystem with ERank integration
    /// </summary>
    public static class OrderProcessor
    {
        /// <summary>
        /// Process an order with specified organization and optional parameters
        /// </summary>
        /// <param name="organization">Organization name (e.g., "Mrs. Stacks", "DevCommand")</param>
        /// <param name="orderType">Type of order (e.g., "console_order", "standard", "premium", "random")</param>
        /// <param name="customDay">Override day (null = use current day)</param>
        /// <param name="customItems">Custom items (null = generate based on order type)</param>
        /// <param name="sendMessages">Whether to send messaging notifications</param>
        /// <param name="tier">Specific tier to use (null = use player's max tier)</param>
        public static void ProcessOrder(string organization, string orderType = "standard", 
            int? customDay = null, List<string>? customItems = null, bool sendMessages = false, TierConfig.Tier? tier = null)
        {
            try
            {
                Logger.Msg($"[OrderProcessor] 📦 Processing {organization} order: {orderType}");

                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("[OrderProcessor] ❌ TimeManager unavailable");
                    if (sendMessages) SendErrorMessage(organization, "Service temporarily unavailable. Try again later.");
                    return;
                }

                int currentDay = customDay ?? timeManager.ElapsedDays;
                var currentRank = DropConfig.GetCurrentPlayerRank();
                var maxTier = tier ?? DropConfig.GetCurrentMaxUnlockedTier();

                // Check tier access for Mrs. Stacks (DevCommand can bypass)
                if (organization == "Mrs. Stacks" && tier.HasValue && !TierDropSystem.CanPlayerAccessTier(tier.Value))
                {
                    Logger.Msg($"[OrderProcessor] 🚫 {organization} tier {TierConfig.GetTierName(tier.Value)} not unlocked");
                    if (sendMessages) SendTierNotUnlockedMessage(organization, tier.Value);
                    return;
                }

                // Check daily limit for Mrs. Stacks (consider tier-based limits)
                if (organization == "Mrs. Stacks")
                {
                    int dailyLimit = DropConfig.GetDailyOrderLimit(maxTier);
                    int ordersToday = JsonDataStore.GetMrsStacksOrdersToday(currentDay);
                    
                    if (ordersToday >= dailyLimit)
                    {
                        Logger.Msg($"[OrderProcessor] 🚫 {organization} daily order limit reached ({ordersToday}/{dailyLimit})");
                        if (sendMessages) SendDailyLimitMessage(organization, dailyLimit);
                        return;
                    }

                    // Mark order for today
                    JsonDataStore.MarkMrsStacksOrderToday(currentDay);
                }

                // Generate package based on order type
                List<string> items;
                int cashAmount;
                string tierInfo;
                
                if (customItems != null)
                {
                    items = customItems;
                    cashAmount = 0;
                    tierInfo = "Custom";
                    
                    // Extract cash from custom items if specified
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        if (items[i].StartsWith("cash:"))
                        {
                            if (int.TryParse(items[i].Substring(5), out int cash))
                            {
                                cashAmount += cash;
                            }
                            items.RemoveAt(i);
                        }
                    }
                }
                else
                {
                    // Generate package using new TierDropSystem
                    TierDropSystem.DropPackage package;
                    
                    switch (orderType.ToLower())
                    {
                        case "premium":
                            package = tier.HasValue ? TierDropSystem.GeneratePremiumDropPackage(tier.Value) : TierDropSystem.GeneratePremiumDropPackage();
                            break;
                        case "random":
                            package = TierDropSystem.GenerateRandomDropPackage();
                            break;
                        default: // "standard" and others
                            package = tier.HasValue ? TierDropSystem.GenerateDropPackage(tier.Value) : TierDropSystem.GenerateDropPackage();
                            break;
                    }
                    
                    items = package.ToFlatList();
                    cashAmount = package.CashAmount;
                    tierInfo = $"{TierConfig.GetTierName(package.Tier)} ({TierConfig.GetOrganizationName(TierConfig.GetOrganization(package.Tier))})";
                }

                // Calculate drop delay based on tier
                int dropHours = DropConfig.GetRandomDropDelay();

                // Send confirmation message if messaging enabled
                if (sendMessages)
                {
                    SendConfirmationMessage(organization, orderType, dropHours, items.Count, cashAmount, tierInfo);
                }

                // Schedule drop spawn
                MelonCoroutines.Start(SpawnDropAfterDelay(organization, orderType, dropHours, items, cashAmount, currentDay, sendMessages, tierInfo));

                Logger.Msg($"[OrderProcessor] ✅ {organization} {orderType} order processed - {items.Count} items, ${cashAmount}, {tierInfo}, ready in {dropHours}h");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ {organization} order processing failed: {ex.Message}");
                if (sendMessages) SendErrorMessage(organization, "Order failed. Please try again.");
            }
        }

        /// <summary>
        /// Spawn drop after time delay
        /// </summary>
        private static System.Collections.IEnumerator SpawnDropAfterDelay(string organization, string orderType, 
            int hours, List<string> items, int cashAmount, int day, bool sendMessages, string tierInfo)
        {
            yield return new UnityEngine.WaitForSeconds(hours * 60.0f);
            
            // Execute spawn logic after yield
            ExecuteSpawnLogic(organization, orderType, hours, items, cashAmount, day, sendMessages, tierInfo);
        }

        /// <summary>
        /// Execute the spawn logic (separated to avoid yield/try-catch conflict)
        /// </summary>
        private static void ExecuteSpawnLogic(string organization, string orderType, int hours, 
            List<string> items, int cashAmount, int day, bool sendMessages, string tierInfo)
        {
            try
            {
                Logger.Msg($"[OrderProcessor] ⏰ Spawning {organization} drop ({tierInfo})...");

                var allItems = new List<string>(items);
                
                var dropRecord = new JsonDataStore.DropRecord
                {
                    Day = day,
                    Items = allItems,
                    DropHour = hours,
                    DropTime = $"{hours:D2}:00",
                    Org = $"{organization} ({tierInfo})",
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = orderType,
                    Location = ""
                };

                string? location = DeadDrop.SpawnImmediateDrop(dropRecord);
                
                // Send ready message if messaging enabled
                if (sendMessages)
                {
                    MelonCoroutines.Start(SendReadyMessageAfterDelay(organization, location, tierInfo));
                }

                Logger.Msg($"[OrderProcessor] ✅ {organization} drop spawned at: {location ?? "unknown"}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ {organization} drop spawn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send ready message after brief delay
        /// </summary>
        private static System.Collections.IEnumerator SendReadyMessageAfterDelay(string organization, string? location, string tierInfo)
        {
            yield return new UnityEngine.WaitForSeconds(2.0f);
            SendReadyMessage(organization, location, tierInfo);
        }

        /// <summary>
        /// Send order confirmation message (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendConfirmationMessage(string organization, string orderType, int hours, int itemCount, int cashAmount, string tierInfo)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
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

                MrsStacksMessaging.SendMessage(npc, 
                    $"Copy that. Preparing {typeText} from {tierInfo} with {itemCount} items and ${cashAmount} cash. " +
                    $"Give me {hours} hours. I'll message when ready with location.");

                Logger.Msg("[OrderProcessor] 📱 Confirmation sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Confirmation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send ready message with location (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendReadyMessage(string organization, string? location, string tierInfo)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                string locationText = !string.IsNullOrEmpty(location) ? location : "a secure location";
                
                MrsStacksMessaging.SendMessage(npc, 
                    $"Package ready! Your {tierInfo} delivery is waiting at {locationText}. " +
                    $"Retrieve when safe. Quality guaranteed as always.");

                Logger.Msg("[OrderProcessor] 📱 Ready message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Ready message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send daily limit message (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendDailyLimitMessage(string organization, int dailyLimit)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var nextTierInfo = TierDropSystem.GetNextTierRequirements();
                string message = $"You've reached your daily order limit ({dailyLimit}). Come back tomorrow. " +
                                $"Tip: {nextTierInfo}";

                MrsStacksMessaging.SendMessage(npc, message);
                Logger.Msg("[OrderProcessor] 📱 Daily limit message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Daily limit message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send tier not unlocked message
        /// </summary>
        private static void SendTierNotUnlockedMessage(string organization, TierConfig.Tier tier)
        {
            if (organization != "Mrs. Stacks") return;
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var requirements = TierDropSystem.GetNextTierRequirements();
                string message = $"Sorry, {TierConfig.GetTierName(tier)} tier isn't available to you yet. " +
                                $"{requirements}";

                MrsStacksMessaging.SendMessage(npc, message);
                Logger.Msg("[OrderProcessor] 📱 Tier locked message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Tier locked message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send error message (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendErrorMessage(string organization, string errorText)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                MrsStacksMessaging.SendMessage(npc, errorText);
                Logger.Msg("[OrderProcessor] 📱 Error message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Error message failed: {ex.Message}");
            }
        }
    }
} 