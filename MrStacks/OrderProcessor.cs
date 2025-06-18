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
                Logger.Debug($"📦 Processing {organization} order: {orderType}", "OrderProcessor");

                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("❌ TimeManager unavailable", "OrderProcessor");
                    if (sendMessages) SendErrorMessage(organization, "Service temporarily unavailable. Try again later.");
                    return;
                }

                int currentDay = customDay ?? timeManager.ElapsedDays;
                var currentRank = DropConfig.GetCurrentPlayerRank();
                var maxTier = tier ?? DropConfig.GetCurrentMaxUnlockedTier();

                // Check tier access for Mrs. Stacks (DevCommand can bypass)
                if (organization == "Mrs. Stacks" && tier.HasValue && !TierDropSystem.CanPlayerAccessTier(tier.Value))
                {
                    Logger.Debug($"🚫 {organization} tier {TierConfig.GetTierName(tier.Value)} not unlocked", "OrderProcessor");
                    if (sendMessages) SendTierNotUnlockedMessage(organization, tier.Value);
                    return;
                }

                // Check daily limit for Mrs. Stacks (consider tier-based limits)
                if (organization == "Mrs. Stacks")
                {
                    int dailyLimit = DropConfig.GetDailyOrderLimit(maxTier);
                    int ordersToday = SaveFileJsonDataStore.GetMrsStacksOrdersToday(currentDay);
                    
                    if (ordersToday >= dailyLimit)
                    {
                        Logger.Debug($"🚫 {organization} daily order limit reached ({ordersToday}/{dailyLimit})", "OrderProcessor");
                        if (sendMessages) SendDailyLimitMessage(organization, dailyLimit);
                        return;
                    }

                    // Mark order for today (this tracks ORDER DAY, not delivery day)
                    SaveFileJsonDataStore.MarkMrsStacksOrderToday(currentDay);
                }

                // Calculate delivery day - always next game day
                int deliveryDay = currentDay + 1;
                int deliveryHour = 730; // 7:30 AM game time
                
                // Calculate expiry - day after delivery at same time
                int expiryDay = deliveryDay + 1;
                int expiryHour = deliveryHour;

                Logger.Debug($"📅 Order placed on game day {currentDay}, delivery on game day {deliveryDay} at {deliveryHour}", "OrderProcessor");

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

                // Send confirmation message if messaging enabled
                if (sendMessages)
                {
                    SendConfirmationMessage(organization, orderType, deliveryDay, deliveryHour, items.Count, cashAmount, tierInfo);
                }

                // Create drop record
                var dropRecord = new SaveFileJsonDataStore.DropRecord
                {
                    Day = deliveryDay,           // When drop becomes available (next game day)
                    Items = items,
                    DropHour = deliveryHour,
                    DropTime = DropConfig.FormatGameTime(deliveryHour),
                    Org = $"{organization} ({tierInfo})",
                    CreatedTime = DateTime.Now.ToString("s"), // Real time when order was created
                    Type = orderType,
                    Location = "",
                    ExpiryTime = $"{expiryDay}:{expiryHour}", // Store as game day:hour format
                    OrderDay = currentDay,       // When order was placed (current game day)
                    IsCollected = false,
                    InitialItemCount = items.Count + (cashAmount > 0 ? 1 : 0) // Include cash as item
                };

                // Schedule the drop for delivery
                SaveFileJsonDataStore.SaveDropRecord(dropRecord);

                // Send immediate preparation message with location assignment
                if (sendMessages && organization == "Mrs. Stacks")
                {
                    SendPreparationMessage(organization, deliveryDay, deliveryHour, tierInfo);
                }

                Logger.Debug($"✅ {organization} {orderType} order scheduled for game day {deliveryDay} at {DropConfig.FormatGameTime(deliveryHour)} - {items.Count} items, ${cashAmount}, {tierInfo}", "OrderProcessor");
                Logger.Debug($"📋 Order tracking: OrderDay={currentDay}, DeliveryDay={deliveryDay}, ExpiryDay={expiryDay}", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ {organization} order processing failed: {ex.Message}", "OrderProcessor");
                if (sendMessages) SendErrorMessage(organization, "Order failed. Please try again.");
            }
        }

        /// <summary>
        /// Send order confirmation message (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendConfirmationMessage(string organization, string orderType, int deliveryDay, int deliveryHour, int itemCount, int cashAmount, string tierInfo)
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
                    $"Delivery tomorrow at {DropConfig.FormatGameTime(deliveryHour)}. I'll message when ready with location.");

                Logger.Debug("📱 Confirmation sent", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Confirmation failed: {ex.Message}", "OrderProcessor");
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
                Logger.Debug("📱 Daily limit message sent", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Daily limit message failed: {ex.Message}", "OrderProcessor");
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
                Logger.Debug("📱 Tier locked message sent", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Tier locked message failed: {ex.Message}", "OrderProcessor");
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
                Logger.Debug("📱 Error message sent", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Error message failed: {ex.Message}", "OrderProcessor");
            }
        }

        /// <summary>
        /// Send preparation message with location assignment (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendPreparationMessage(string organization, int deliveryDay, int deliveryHour, string tierInfo)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                MrsStacksMessaging.SendMessage(npc, 
                    $"Package prep underway. Scheduled for day {deliveryDay} at {DropConfig.FormatGameTime(deliveryHour)}. " +
                    $"I'll message you with the actual dead drop location when it's ready. Stay tuned!");

                Logger.Debug("📱 Preparation message sent (no preliminary location to avoid confusion)", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Preparation message failed: {ex.Message}", "OrderProcessor");
            }
        }
    }
} 