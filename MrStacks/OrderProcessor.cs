/*
@file OrderProcessor.cs
@description Shared PaxDrops order scheduling logic for random drops and Mr. Stacks ordered tiers, including persistence-aware charge handling.
@editCount 2
*/

using System;
using System.Collections.Generic;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Money;
using PaxDrops.Configs;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Shared drop scheduling code for Mr. Stacks and debug orders.
    /// Ordered Mr. Stacks menu orders use the dedicated 3x3 typed flow below.
    /// </summary>
    public static class OrderProcessor
    {
        private const int DeliveryHour = 730;

        /// <summary>
        /// Legacy/shared order scheduling path for random/system/debug orders.
        /// </summary>
        public static void ProcessOrder(
            string organization,
            string orderType = "standard",
            int? customDay = null,
            List<string>? customItems = null,
            bool sendMessages = false,
            TierConfig.Tier? tier = null)
        {
            try
            {
                Logger.Debug($"Processing {organization} order: {orderType}", "OrderProcessor");

                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("TimeManager unavailable", "OrderProcessor");
                    if (sendMessages) SendErrorMessage(organization, "Service temporarily unavailable. Try again later.");
                    return;
                }

                int currentDay = customDay ?? timeManager.ElapsedDays;
                var maxTier = tier ?? DropConfig.GetCurrentMaxUnlockedTier();

                if (organization == "Mr. Stacks" && tier.HasValue && !TierDropSystem.CanPlayerAccessTier(tier.Value))
                {
                    Logger.Debug($"{organization} tier {TierConfig.GetTierName(tier.Value)} not unlocked", "OrderProcessor");
                    if (sendMessages) SendLegacyTierNotUnlockedMessage(tier.Value);
                    return;
                }

                if (organization == "Mr. Stacks")
                {
                    int dailyLimit = DropConfig.GetDailyOrderLimit(maxTier);
                    int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);

                    if (ordersToday >= dailyLimit)
                    {
                        Logger.Debug($"{organization} daily order limit reached ({ordersToday}/{dailyLimit})", "OrderProcessor");
                        if (sendMessages) SendLegacyDailyLimitMessage(dailyLimit);
                        return;
                    }
                }

                int deliveryDay = currentDay + 1;
                int expiryDay = deliveryDay + 1;

                List<string> items;
                int cashAmount;
                string tierInfo;

                if (customItems != null)
                {
                    items = new List<string>(customItems);
                    cashAmount = 0;
                    tierInfo = "Custom";

                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        if (!items[i].StartsWith("cash:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (int.TryParse(items[i].Substring(5), out int parsedCash))
                        {
                            cashAmount += parsedCash;
                        }

                        items.RemoveAt(i);
                    }

                    if (cashAmount > 0)
                    {
                        items.Insert(0, $"cash:{cashAmount}");
                    }
                }
                else
                {
                    TierDropSystem.DropPackage package = orderType.ToLowerInvariant() switch
                    {
                        "premium" => tier.HasValue ? TierDropSystem.GeneratePremiumDropPackage(tier.Value) : TierDropSystem.GeneratePremiumDropPackage(),
                        "random" => TierDropSystem.GenerateRandomDropPackage(),
                        _ => tier.HasValue ? TierDropSystem.GenerateDropPackage(tier.Value) : TierDropSystem.GenerateDropPackage()
                    };

                    items = package.ToFlatList();
                    cashAmount = package.CashAmount;
                    tierInfo = $"{TierConfig.GetTierName(package.Tier)} ({TierConfig.GetOrganizationName(TierConfig.GetOrganization(package.Tier))})";
                }

                int nonCashItemCount = CountNonCashItems(items);

                if (sendMessages)
                {
                    SendLegacyConfirmationMessage(orderType, deliveryDay, DeliveryHour, nonCashItemCount, cashAmount, tierInfo);
                }

                var dropRecord = new SaveFileJsonDataStore.DropRecord
                {
                    Day = deliveryDay,
                    Items = items,
                    DropHour = DeliveryHour,
                    DropTime = DropConfig.FormatGameTime(DeliveryHour),
                    Org = organization == "Mr. Stacks" ? $"{organization} ({tierInfo})" : organization,
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = orderType,
                    Location = "",
                    ExpiryTime = $"{expiryDay}:{DeliveryHour}",
                    OrderDay = currentDay,
                    IsCollected = false,
                    InitialItemCount = items.Count
                };

                bool saveSucceeded = SaveFileJsonDataStore.SaveDropRecord(dropRecord);
                if (!saveSucceeded)
                {
                    Logger.Error("Failed to queue drop record", "OrderProcessor");
                    if (sendMessages) SendErrorMessage(organization, "Order failed to save. Please try again.");
                    return;
                }

                if (organization == "Mr. Stacks")
                {
                    SaveFileJsonDataStore.MarkMrStacksOrderToday(currentDay);

                    if (sendMessages)
                    {
                        SendPreparationMessage(deliveryDay, DeliveryHour, tierInfo);
                    }
                }

                Logger.Debug($"{organization} {orderType} order scheduled for day {deliveryDay} at {DropConfig.FormatGameTime(DeliveryHour)}", "OrderProcessor");
            }
            catch (Exception ex)
            {
                Logger.Error($"{organization} order processing failed: {ex.Message}", "OrderProcessor");
                if (sendMessages) SendErrorMessage(organization, "Order failed. Please try again.");
            }
        }

        /// <summary>
        /// Typed ordered-flow entrypoint used by the Mr. Stacks conversation menu.
        /// </summary>
        public static bool ProcessMrStacksTierOrder(OrderedDropConfig.OrderedTier selectedTier, bool sendMessages = true)
        {
            MoneyManager? moneyManager = null;
            bool charged = false;
            int chargedAmount = 0;
            string chargedTierName = OrderedDropConfig.GetTierName(selectedTier);

            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("TimeManager unavailable", "OrderProcessor");
                    if (sendMessages) SendErrorMessage("Mr. Stacks", "Service temporarily unavailable. Try again later.");
                    return false;
                }

                var saveLoadResult = SaveFileJsonDataStore.EnsureCurrentSaveLoadedFromGame("Mr. Stacks order");
                if (!saveLoadResult.IsLoaded)
                {
                    Logger.Warn($"Blocking Mr. Stacks order because save data is unavailable: {saveLoadResult.Message}", "OrderProcessor");
                    if (sendMessages) SendSaveUnavailableMessage();
                    return false;
                }

                int currentDay = timeManager.ElapsedDays;
                int currentTime = timeManager.CurrentTime;
                ERank currentRank = DropConfig.GetCurrentPlayerRank();

                if (!OrderedDropConfig.IsUnlocked(selectedTier, currentRank, currentDay))
                {
                    Logger.Debug($"Ordered tier {chargedTierName} is locked", "OrderProcessor");
                    if (sendMessages) SendLockedTierMessage(selectedTier, currentRank, currentDay);
                    return false;
                }

                var highestUnlockedTier = OrderedDropConfig.GetHighestUnlockedTier(currentRank, currentDay);
                if (!highestUnlockedTier.HasValue)
                {
                    Logger.Debug("No ordered tiers are unlocked for the player", "OrderProcessor");
                    if (sendMessages) SendErrorMessage("Mr. Stacks", "You do not have any order tiers unlocked yet.");
                    return false;
                }

                int dailyLimit = OrderedDropConfig.GetDailyOrderLimit(highestUnlockedTier.Value);
                int ordersToday = SaveFileJsonDataStore.GetMrStacksOrdersToday(currentDay);
                if (ordersToday >= dailyLimit)
                {
                    Logger.Debug($"Mr. Stacks daily order limit reached ({ordersToday}/{dailyLimit})", "OrderProcessor");
                    if (sendMessages) SendDailyLimitMessage(dailyLimit, highestUnlockedTier.Value);
                    return false;
                }

                moneyManager = MoneyManager.Instance;
                if (moneyManager == null)
                {
                    Logger.Error("MoneyManager unavailable", "OrderProcessor");
                    if (sendMessages) SendErrorMessage("Mr. Stacks", "Banking services are unavailable right now. Try again in a bit.");
                    return false;
                }

                int price = OrderedDropConfig.GetPrice(selectedTier);
                float onlineBalance = moneyManager.onlineBalance;
                if (onlineBalance < price)
                {
                    Logger.Debug($"Insufficient online balance for {chargedTierName}: ${onlineBalance} < ${price}", "OrderProcessor");
                    if (sendMessages) SendInsufficientFundsMessage(selectedTier, price, onlineBalance);
                    return false;
                }

                int deliveryDay = currentDay + 1;
                int expiryDay = deliveryDay + 1;
                int hoursUntilDelivery = OrderedDropConfig.CalculateHoursUntilDelivery(currentDay, currentTime, deliveryDay, DeliveryHour);
                string groupName = OrderedDropConfig.GetGroupName(OrderedDropConfig.GetGroup(selectedTier));
                var package = OrderedDropSystem.GenerateDropPackage(selectedTier);
                var items = package.ToFlatList();

                moneyManager.CreateOnlineTransaction("Mr. Stacks Package", -price, 1f, $"{chargedTierName} package");
                charged = true;
                chargedAmount = price;

                var dropRecord = new SaveFileJsonDataStore.DropRecord
                {
                    Day = deliveryDay,
                    Items = items,
                    DropHour = DeliveryHour,
                    DropTime = DropConfig.FormatGameTime(DeliveryHour),
                    Org = $"Mr. Stacks ({chargedTierName})",
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = "ordered",
                    Location = "",
                    ExpiryTime = $"{expiryDay}:{DeliveryHour}",
                    OrderDay = currentDay,
                    IsCollected = false,
                    InitialItemCount = items.Count,
                    OrderedTierId = OrderedDropConfig.GetTierId(selectedTier),
                    OrderedTierName = chargedTierName,
                    PricePaid = price
                };

                bool saveSucceeded = SaveFileJsonDataStore.SaveDropRecord(dropRecord);
                if (!saveSucceeded)
                {
                    RefundOnlineOrder(moneyManager, chargedAmount, chargedTierName);
                    charged = false;

                    if (sendMessages)
                    {
                        SendErrorMessage("Mr. Stacks", "Order storage failed, so I sent your money back. Try again.");
                    }

                    return false;
                }

                SaveFileJsonDataStore.MarkMrStacksOrderToday(currentDay);

                if (sendMessages)
                {
                    SendOrderedConfirmationMessage(selectedTier, groupName, price, deliveryDay, DeliveryHour, hoursUntilDelivery, package.Items.Count, package.CashAmount);
                    SendPreparationMessage(deliveryDay, DeliveryHour, chargedTierName);
                }

                Logger.Debug($"Ordered Mr. Stacks tier {chargedTierName} for ${price}; delivery day {deliveryDay} at {DropConfig.FormatGameTime(DeliveryHour)}", "OrderProcessor");
                return true;
            }
            catch (Exception ex)
            {
                if (charged && moneyManager != null && chargedAmount > 0)
                {
                    RefundOnlineOrder(moneyManager, chargedAmount, chargedTierName);
                }

                Logger.Error($"Ordered Mr. Stacks tier processing failed: {ex.Message}", "OrderProcessor");
                if (sendMessages) SendErrorMessage("Mr. Stacks", "Order failed. If anything charged incorrectly, it has been refunded.");
                return false;
            }
        }

        private static int CountNonCashItems(List<string> items)
        {
            int count = 0;

            foreach (var item in items)
            {
                if (!item.StartsWith("cash:", StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        private static void RefundOnlineOrder(MoneyManager moneyManager, int amount, string tierName)
        {
            try
            {
                moneyManager.CreateOnlineTransaction("Mr. Stacks Refund", amount, 1f, $"{tierName} package refund");
                Logger.Warn($"Refunded ${amount} for failed {tierName} order", "OrderProcessor");
            }
            catch (Exception refundEx)
            {
                Logger.Error($"Failed to refund ${amount} for {tierName}: {refundEx.Message}", "OrderProcessor");
            }
        }

        private static void SendLegacyConfirmationMessage(string orderType, int deliveryDay, int deliveryHour, int itemCount, int cashAmount, string tierInfo)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                string typeText = orderType.ToLowerInvariant() switch
                {
                    "premium" => "premium package",
                    "random" => "surprise package",
                    _ => "standard package"
                };

                MrStacksMessaging.SendMessage(
                    npc,
                    $"Copy that. Preparing {typeText} from {tierInfo} with {itemCount} items and ${cashAmount} cash. Delivery tomorrow at {DropConfig.FormatGameTime(deliveryHour)}.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Legacy confirmation failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendOrderedConfirmationMessage(
            OrderedDropConfig.OrderedTier selectedTier,
            string groupName,
            int price,
            int deliveryDay,
            int deliveryHour,
            int hoursUntilDelivery,
            int itemCount,
            int cashAmount)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                string tierName = OrderedDropConfig.GetTierName(selectedTier);
                MrStacksMessaging.SendMessage(
                    npc,
                    $"All right. {tierName} from {groupName} is locked in for ${price}. Expect the drop tomorrow at {DropConfig.FormatGameTime(deliveryHour)} in about {hoursUntilDelivery} in-game hour(s). Package is tagged with {itemCount} item stack(s) and a cash bundle.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ordered confirmation failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendDailyLimitMessage(int dailyLimit, OrderedDropConfig.OrderedTier highestUnlockedTier)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                string tierName = OrderedDropConfig.GetTierName(highestUnlockedTier);
                MrStacksMessaging.SendMessage(
                    npc,
                    $"You've already used all {dailyLimit} order slot(s) for today. Your current access still tops out at {tierName}. Come back tomorrow.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Daily limit message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendLegacyDailyLimitMessage(int dailyLimit)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                MrStacksMessaging.SendMessage(
                    npc,
                    $"You've reached your daily order limit of {dailyLimit}. Come back tomorrow for more business.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Legacy daily limit message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendLockedTierMessage(OrderedDropConfig.OrderedTier selectedTier, ERank currentRank, int currentDay)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                MrStacksMessaging.SendMessage(npc, OrderedDropConfig.GetLockedReason(selectedTier, currentRank, currentDay));
            }
            catch (Exception ex)
            {
                Logger.Error($"Locked tier message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendSaveUnavailableMessage()
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                MrStacksMessaging.SendTransientMessage(
                    npc,
                    "I can't pin your save file right now. No charge was placed. Save once and try again.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Save unavailable message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendLegacyTierNotUnlockedMessage(TierConfig.Tier tier)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                string requirements = TierDropSystem.GetNextTierRequirements();
                string message = $"Sorry, {TierConfig.GetTierName(tier)} tier isn't available to you yet. {requirements}";
                MrStacksMessaging.SendMessage(npc, message);
            }
            catch (Exception ex)
            {
                Logger.Error($"Legacy tier locked message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendInsufficientFundsMessage(OrderedDropConfig.OrderedTier tier, int price, float onlineBalance)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                string tierName = OrderedDropConfig.GetTierName(tier);
                MrStacksMessaging.SendMessage(
                    npc,
                    $"{tierName} costs ${price}, but your online balance is only ${MathF.Floor(onlineBalance)}. Move more into the bank and try again.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Insufficient funds message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendErrorMessage(string organization, string errorText)
        {
            if (organization != "Mr. Stacks") return;

            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                MrStacksMessaging.SendMessage(npc, errorText);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error message failed: {ex.Message}", "OrderProcessor");
            }
        }

        private static void SendPreparationMessage(int deliveryDay, int deliveryHour, string tierInfo)
        {
            try
            {
                var npc = MrStacksMessaging.FindMrStacksNPC();
                if (npc == null) return;

                MrStacksMessaging.SendMessage(
                    npc,
                    $"Prep is underway for your {tierInfo} package. It's scheduled for day {deliveryDay} at {DropConfig.FormatGameTime(deliveryHour)}. I'll message the dead drop location when it's placed.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Preparation message failed: {ex.Message}", "OrderProcessor");
            }
        }
    }
}
