using System;
using PaxDrops.Patches;
using System.Collections.Generic;
using Il2CppScheduleOne.Economy;
using PaxDrops.Configs;

namespace PaxDrops
{
    /// <summary>
    /// Main command handler that manages initialization and registration of all PaxDrops console commands.
    /// Uses Harmony patches to intercept the console system for reliable command registration.
    /// TEMPORARILY DISABLED - focusing on core functionality first
    /// </summary>
    public static class CommandHandler
    {
        private static bool _initialized;

        /// <summary>
        /// Initialize all console commands for PaxDrops
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;

            try
            {
                Logger.Msg("[CommandHandler] 🔧 CommandHandler temporarily disabled");
                
                // COMMENTED OUT - Focusing on core functionality first
                // Initialize console patches to intercept commands
                // ConsolePatch.Init();
                
                // Commands["pd_info"] = () => ShowSystemInfo();
                // Commands["pd_test_collection"] = () => TestCollectionDetection();
                // Commands["pd_test_expiry"] = () => TestExpiryCleanup();
                // Commands["pd_reset_orders"] = () => ResetDailyOrders();
                // Commands["pd_show_orders"] = () => ShowOrderHistory();
                // Commands["pd_cleanup_expired"] = () => CleanupExpiredDrops();
                
                _initialized = true;
                Logger.Msg("[CommandHandler] ✅ Core systems ready (commands disabled)");
            }
            catch (Exception ex)
            {
                Logger.Error("[CommandHandler] ❌ Command handler initialization failed.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Shutdown all console commands and patches
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            try
            {
                // ConsolePatch.Shutdown(); // COMMENTED OUT
                _initialized = false;
                Logger.Msg("[CommandHandler] 🔌 Command handler shutdown (was disabled)");
            }
            catch (Exception ex)
            {
                Logger.Error("[CommandHandler] ❌ Command shutdown failed.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Get initialization status
        /// </summary>
        public static bool IsInitialized => _initialized;

        /*
        // COMMENTED OUT - All command methods disabled for now

        /// <summary>
        /// Show comprehensive system information
        /// </summary>
        private static void ShowSystemInfo()
        {
            Logger.Msg("[CommandHandler] 📊 PaxDrops System Information:");
            Logger.Msg("═══════════════════════════════════════════");
            
            // Player Detection Status
            Logger.Msg($"Player Detection: {DropConfig.GetPlayerDetectionStatus()}");
            
            // Current Game State
            var currentDay = DropConfig.GetCurrentGameDay();
            var currentRank = DropConfig.GetCurrentPlayerRank();
            var currentTier = DropConfig.GetCurrentPlayerTier();
            
            Logger.Msg($"Game Day: {currentDay}");
            Logger.Msg($"Player Rank: {currentRank}");
            Logger.Msg($"Player Tier: {TierConfig.GetTierName(currentTier)}");
            Logger.Msg($"Organization: {TierConfig.GetOrganizationName(TierConfig.GetOrganization(currentTier))}");
            
            // Daily Order Status
            var ordersToday = SaveFileJsonDataStore.GetMrsStacksOrdersToday(currentDay);
            var dailyLimit = DropConfig.GetDailyOrderLimit(currentTier);
            var remaining = DropConfig.GetRemainingOrdersToday(currentDay);
            
            Logger.Msg($"Orders Today: {ordersToday}/{dailyLimit} (Remaining: {remaining})");
            
            // Active Drops
            var pendingDrops = SaveFileJsonDataStore.GetAllDrops();
            var activeDrops = pendingDrops.FindAll(d => !d.IsCollected);
            var collectedDrops = pendingDrops.FindAll(d => d.IsCollected);
            
            Logger.Msg($"Active Drops: {activeDrops.Count}");
            Logger.Msg($"Collected Drops: {collectedDrops.Count}");
            
            // Show save file info
            var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
            if (isLoaded)
            {
                Logger.Msg($"Save File: {saveName} (ID: {saveId}, Steam: {steamId})");
            }
            else
            {
                Logger.Msg("Save File: No save loaded");
            }
            
            Logger.Msg("═══════════════════════════════════════════");
            
            if (activeDrops.Count > 0)
            {
                Logger.Msg("Active Drop Details:");
                foreach (var drop in activeDrops)
                {
                    var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                    string expiryText = expiryDay != -1 ? $"Day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)}" : "Unknown";
                    
                    Logger.Msg($"  Day {drop.Day}: {drop.Org} at {drop.Location} (expires {expiryText})");
                }
            }
        }

        /// <summary>
        /// Test collection detection manually
        /// </summary>
        private static void TestCollectionDetection()
        {
            Logger.Msg("[CommandHandler] 🔍 Testing collection detection...");
            
            var pendingDrops = SaveFileJsonDataStore.GetAllDrops();
            var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();
            
            Logger.Msg($"Found {pendingDrops.Count} pending drops and {deadDrops.Length} dead drop locations");
            
            foreach (var drop in pendingDrops)
            {
                if (drop.IsCollected || string.IsNullOrEmpty(drop.Location)) continue;
                
                // Find the dead drop by name
                Il2CppScheduleOne.Economy.DeadDrop? targetDeadDrop = null;
                foreach (var deadDrop in deadDrops)
                {
                    if (deadDrop.DeadDropName == drop.Location)
                    {
                        targetDeadDrop = deadDrop;
                        break;
                    }
                }

                if (targetDeadDrop?.Storage != null)
                {
                    int currentItemCount = targetDeadDrop.Storage.ItemCount;
                    float collectionPercentage = (float)currentItemCount / drop.InitialItemCount * 100f;
                    
                    Logger.Msg($"Drop at {drop.Location}: {currentItemCount}/{drop.InitialItemCount} items ({collectionPercentage:F1}%)");
                    
                    if (currentItemCount <= (drop.InitialItemCount * 0.5f))
                    {
                        Logger.Msg($"  → Would mark as collected (≤50% items remaining)");
                    }
                }
                else
                {
                    Logger.Warn($"Drop location {drop.Location} not found or no storage");
                }
            }
        }

        /// <summary>
        /// Test expiry cleanup manually
        /// </summary>
        private static void TestExpiryCleanup()
        {
            Logger.Msg("[CommandHandler] 🗑️ Testing expiry cleanup...");
            
            var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
            int currentDay = timeManager?.ElapsedDays ?? 0;
            int currentHour = timeManager?.CurrentTime ?? 0;
            
            var allDrops = SaveFileJsonDataStore.GetAllDrops();
            var expiredDrops = new List<SaveFileJsonDataStore.DropRecord>();
            
            foreach (var drop in allDrops)
            {
                if (!string.IsNullOrEmpty(drop.ExpiryTime) && 
                    DropConfig.IsDropExpired(drop.ExpiryTime, currentDay, currentHour))
                {
                    expiredDrops.Add(drop);
                }
            }
            
            Logger.Msg($"Found {expiredDrops.Count} expired drops");
            
            foreach (var drop in expiredDrops)
            {
                var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                string expiryText = expiryDay != -1 ? $"Day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)}" : "Unknown";
                
                Logger.Msg($"Expired: {drop.Org} at {drop.Location} (expired {expiryText})");
            }
            
            if (expiredDrops.Count > 0)
            {
                Logger.Msg("Use 'pd_cleanup_expired' to actually clean them up");
            }
        }

        /// <summary>
        /// Reset daily orders for current day (testing)
        /// </summary>
        private static void ResetDailyOrders()
        {
            var currentDay = DropConfig.GetCurrentGameDay();
            SaveFileJsonDataStore.ResetMrsStacksOrdersToday(currentDay);
            Logger.Msg($"[CommandHandler] 🔄 Reset daily orders for day {currentDay}");
        }

        /// <summary>
        /// Show order history
        /// </summary>
        private static void ShowOrderHistory()
        {
            Logger.Msg("[CommandHandler] 📋 Order History:");
            
            var orderSummary = SaveFileJsonDataStore.GetMrsStacksOrderSummary();
            if (orderSummary.Count == 0)
            {
                Logger.Msg("No orders recorded yet.");
                return;
            }
            
            var sortedOrders = new List<KeyValuePair<int, int>>(orderSummary);
            sortedOrders.Sort((a, b) => a.Key.CompareTo(b.Key));
            
            foreach (var kvp in sortedOrders)
            {
                Logger.Msg($"Day {kvp.Key}: {kvp.Value} order(s)");
            }
        }

        /// <summary>
        /// Manually cleanup expired drops
        /// </summary>
        private static void CleanupExpiredDrops()
        {
            Logger.Msg("[CommandHandler] 🗑️ Manually cleaning up expired drops...");
            
            var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
            int currentDay = timeManager?.ElapsedDays ?? 0;
            int currentHour = timeManager?.CurrentTime ?? 0;
            
            var allDrops = SaveFileJsonDataStore.GetAllDrops();
            var expiredDrops = new List<SaveFileJsonDataStore.DropRecord>();
            
            foreach (var drop in allDrops)
            {
                if (!string.IsNullOrEmpty(drop.ExpiryTime) && 
                    DropConfig.IsDropExpired(drop.ExpiryTime, currentDay, currentHour))
                {
                    expiredDrops.Add(drop);
                }
            }
            
            if (expiredDrops.Count == 0)
            {
                Logger.Msg("No expired drops to clean up.");
                return;
            }

            var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();
            int cleanedCount = 0;

            foreach (var drop in expiredDrops)
            {
                Logger.Msg($"Cleaning up expired drop at {drop.Location}");

                // Find the dead drop and clear its contents
                foreach (var deadDrop in deadDrops)
                {
                    if (deadDrop.DeadDropName == drop.Location && deadDrop.Storage != null)
                    {
                        try
                        {
                            deadDrop.Storage.ClearContents();
                            Logger.Msg($"✅ Cleared contents from {drop.Location}");
                            cleanedCount++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to clear contents from {drop.Location}: {ex.Message}");
                        }
                        break;
                    }
                }

                // Remove from pending drops
                SaveFileJsonDataStore.RemoveSpecificDrop(drop.Day, drop.Location);
            }

            Logger.Msg($"[CommandHandler] ✅ Cleaned up {cleanedCount}/{expiredDrops.Count} expired drops");
        }
        */
    }
} 