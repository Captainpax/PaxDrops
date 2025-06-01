using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Shared order processing system for both Mrs. Stacks and DevCommand orders.
    /// Handles the complete flow: validation → confirmation → delay → spawn → notification
    /// </summary>
    public static class OrderProcessor
    {
        /// <summary>
        /// Process an order with specified organization and optional parameters
        /// </summary>
        /// <param name="organization">Organization name (e.g., "Mrs. Stacks", "DevCommand")</param>
        /// <param name="orderType">Type of order (e.g., "console_order", "standard")</param>
        /// <param name="customDay">Override day (null = use current day)</param>
        /// <param name="customItems">Custom items (null = generate random package)</param>
        /// <param name="sendMessages">Whether to send messaging notifications</param>
        public static void ProcessOrder(string organization, string orderType = "standard", 
            int? customDay = null, List<string>? customItems = null, bool sendMessages = false)
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

                // Check daily limit only for Mrs. Stacks (DevCommand has no limits)
                if (organization == "Mrs. Stacks" && JsonDataStore.HasMrsStacksOrderToday(currentDay))
                {
                    Logger.Msg($"[OrderProcessor] 🚫 {organization} daily order limit reached");
                    if (sendMessages) SendDailyLimitMessage(organization);
                    return;
                }

                // Mark order for today (only for Mrs. Stacks)
                if (organization == "Mrs. Stacks")
                {
                    JsonDataStore.MarkMrsStacksOrderToday(currentDay);
                }

                // Generate package
                List<string> items;
                int cashAmount;
                
                if (customItems != null)
                {
                    items = customItems;
                    cashAmount = 0; // Custom items don't include cash
                    
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
                    var packet = TierLevel.GetRandomDropPacket(currentDay);
                    items = packet.ToFlatList();
                    cashAmount = packet.CashAmount;
                }

                var dropHours = new System.Random().Next(1, 5);

                // Send confirmation message if messaging enabled
                if (sendMessages)
                {
                    SendConfirmationMessage(organization, dropHours, items.Count, cashAmount);
                }

                // Schedule drop spawn
                MelonCoroutines.Start(SpawnDropAfterDelay(organization, orderType, dropHours, items, cashAmount, currentDay, sendMessages));

                Logger.Msg($"[OrderProcessor] ✅ {organization} order processed - {items.Count} items, ${cashAmount}, ready in {dropHours}h");
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
            int hours, List<string> items, int cashAmount, int day, bool sendMessages)
        {
            yield return new UnityEngine.WaitForSeconds(hours * 60.0f);
            
            // Execute spawn logic after yield
            ExecuteSpawnLogic(organization, orderType, hours, items, cashAmount, day, sendMessages);
        }

        /// <summary>
        /// Execute the spawn logic (separated to avoid yield/try-catch conflict)
        /// </summary>
        private static void ExecuteSpawnLogic(string organization, string orderType, int hours, 
            List<string> items, int cashAmount, int day, bool sendMessages)
        {
            try
            {
                Logger.Msg($"[OrderProcessor] ⏰ Spawning {organization} drop...");

                // NOTE: items from TierLevel.ToFlatList() already includes cash!
                // Don't add cash again to avoid doubling
                var allItems = new List<string>(items);
                
                var dropRecord = new JsonDataStore.DropRecord
                {
                    Day = day,
                    Items = allItems,
                    DropHour = hours,
                    DropTime = $"{hours:D2}:00",
                    Org = organization,
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = orderType,
                    Location = ""
                };

                string? location = DeadDrop.SpawnImmediateDrop(dropRecord);
                
                // Send ready message if messaging enabled
                if (sendMessages)
                {
                    MelonCoroutines.Start(SendReadyMessageAfterDelay(organization, location));
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
        private static System.Collections.IEnumerator SendReadyMessageAfterDelay(string organization, string? location)
        {
            yield return new UnityEngine.WaitForSeconds(2.0f);
            SendReadyMessage(organization, location);
        }

        /// <summary>
        /// Send order confirmation message (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendConfirmationMessage(string organization, int hours, int itemCount, int cashAmount)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                MrsStacksMessaging.SendMessage(npc, 
                    $"Copy that. Preparing surprise package with {itemCount} items and ${cashAmount} cash. " +
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
        private static void SendReadyMessage(string organization, string? location)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                string messageText = !string.IsNullOrEmpty(location) 
                    ? $"Package ready at {location}. Look for markers. Clean pickup, no traces."
                    : "Package ready. Check usual dead drop spots for markers.";

                MrsStacksMessaging.SendMessage(npc, messageText);
                Logger.Msg($"[OrderProcessor] 📱 Ready message sent: {location ?? "generic"}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Ready message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send daily limit reached message (only for Mrs. Stacks messaging)
        /// </summary>
        private static void SendDailyLimitMessage(string organization)
        {
            if (organization != "Mrs. Stacks") return; // Only Mrs. Stacks sends messages
            
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                MrsStacksMessaging.SendMessage(npc,
                    "You already placed an order today. I only do one drop per customer per day. " +
                    "Quality over quantity. Come back tomorrow.");

                Logger.Msg("[OrderProcessor] 📱 Daily limit message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Daily limit message failed: {ex.Message}");
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
                Logger.Msg($"[OrderProcessor] 📱 Error message sent: {errorText}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[OrderProcessor] ❌ Error message failed: {ex.Message}");
            }
        }
    }
} 