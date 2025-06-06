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
                Logger.Info("🔧 CommandHandler temporarily disabled", "CommandHandler");
                
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
                Logger.Info("✅ Core systems ready (commands disabled)", "CommandHandler");
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Command handler initialization failed.", "CommandHandler");
                Logger.Error(ex.Message, "CommandHandler");
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
                Logger.Info("🔌 Command handler shutdown (was disabled)", "CommandHandler");
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Command shutdown failed.", "CommandHandler");
                Logger.Error(ex.Message, "CommandHandler");
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
            Logger.Debug("📊 PaxDrops System Information:", "CommandHandler");
            Logger.Debug("═══════════════════════════════════════════", "CommandHandler");
            
            // Player Detection Status
            Logger.Debug($"Player Detection: {DropConfig.GetPlayerDetectionStatus()}", "CommandHandler");
            
            // Current Game State
            var currentDay = DropConfig.GetCurrentGameDay();
            var currentRank = DropConfig.GetCurrentPlayerRank();
            var currentTier = DropConfig.GetCurrentPlayerTier();
            
            Logger.Debug($"Game Day: {currentDay}", "CommandHandler");
            Logger.Debug($"Player Rank: {currentRank}", "CommandHandler");
            Logger.Debug($"Player Tier: {TierConfig.GetTierName(currentTier)}", "CommandHandler");
            Logger.Debug($"Organization: {TierConfig.GetOrganizationName(TierConfig.GetOrganization(currentTier))}", "CommandHandler");
            
            // Daily Order Status
            var ordersToday = SaveFileJsonDataStore.GetMrsStacksOrdersToday(currentDay);
            var dailyLimit = DropConfig.GetDailyOrderLimit(currentTier);
            var remaining = DropConfig.GetRemainingOrdersToday(currentDay);
            
            Logger.Debug($"Orders Today: {ordersToday}/{dailyLimit} (Remaining: {remaining})", "CommandHandler");
            
            // Active Drops
            var pendingDrops = SaveFileJsonDataStore.GetAllDrops();
            var activeDrops = pendingDrops.FindAll(d => !d.IsCollected);
            var collectedDrops = pendingDrops.FindAll(d => d.IsCollected);
            
            Logger.Debug($"Active Drops: {activeDrops.Count}", "CommandHandler");
            Logger.Debug($"Collected Drops: {collectedDrops.Count}", "CommandHandler");
            
            // Show save file info
            var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
            if (isLoaded)
            {
                Logger.Debug($"Save File: {saveName} (ID: {saveId}, Steam: {steamId})", "CommandHandler");
            }
            else
            {
                Logger.Debug("Save File: No save loaded", "CommandHandler");
            }
            
            Logger.Debug("═══════════════════════════════════════════", "CommandHandler");
            
            if (activeDrops.Count > 0)
            {
                Logger.Debug("Active Drop Details:", "CommandHandler");
                foreach (var drop in activeDrops)
                {
                    var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                    string expiryText = expiryDay != -1 ? $"Day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)}" : "Unknown";
                    
                    Logger.Debug($"  Day {drop.Day}: {drop.Org} at {drop.Location} (expires {expiryText})", "CommandHandler");
                }
            }
        }

        /// <summary>
        /// Test collection detection manually
        /// </summary>
        private static void TestCollectionDetection()
        {
            Logger.Debug("🔍 Testing collection detection...", "CommandHandler");
            
            var pendingDrops = SaveFileJsonDataStore.GetAllDrops();
            var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();
            
            Logger.Debug($"Found {pendingDrops.Count} pending drops and {deadDrops.Length} dead drop locations", "CommandHandler");
            
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
                    
                    Logger.Debug($"Drop at {drop.Location}: {currentItemCount}/{drop.InitialItemCount} items ({collectionPercentage:F1}%)", "CommandHandler");
                    
                    if (currentItemCount <= (drop.InitialItemCount * 0.5f))
                    {
                        Logger.Debug($"  → Would mark as collected (≤50% items remaining)", "CommandHandler");
                    }
                }
                else
                {
                    Logger.Warn($"Drop location {drop.Location} not found or no storage", "CommandHandler");
                }
            }
        }

        /// <summary>
        /// Test expiry cleanup manually
        /// </summary>
        private static void TestExpiryCleanup()
        {
            Logger.Debug("🗑️ Testing expiry cleanup...", "CommandHandler");
            
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
            
            Logger.Debug($"Found {expiredDrops.Count} expired drops", "CommandHandler");
            
            foreach (var drop in expiredDrops)
            {
                var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                string expiryText = expiryDay != -1 ? $"Day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)}" : "Unknown";
                
                Logger.Debug($"Expired: {drop.Org} at {drop.Location} (expired {expiryText})", "CommandHandler");
            }
            
            if (expiredDrops.Count > 0)
            {
                Logger.Debug("Use 'pd_cleanup_expired' to actually clean them up", "CommandHandler");
            }
        }

        /// <summary>
        /// Reset daily orders for current day (testing)
        /// </summary>
        private static void ResetDailyOrders()
        {
            var currentDay = DropConfig.GetCurrentGameDay();
            SaveFileJsonDataStore.ResetMrsStacksOrdersToday(currentDay);
            Logger.Debug($"🔄 Reset daily orders for day {currentDay}", "CommandHandler");
        }

        /// <summary>
        /// Show order history
        /// </summary>
        private static void ShowOrderHistory()
        {
            Logger.Debug("📋 Order History:", "CommandHandler");
            
            var orderSummary = SaveFileJsonDataStore.GetMrsStacksOrderSummary();
            if (orderSummary.Count == 0)
            {
                Logger.Debug("No orders recorded yet.", "CommandHandler");
                return;
            }
            
            var sortedOrders = new List<KeyValuePair<int, int>>(orderSummary);
            sortedOrders.Sort((a, b) => a.Key.CompareTo(b.Key));
            
            foreach (var kvp in sortedOrders)
            {
                Logger.Debug($"Day {kvp.Key}: {kvp.Value} order(s)", "CommandHandler");
            }
        }

        /// <summary>
        /// Manually cleanup expired drops
        /// </summary>
        private static void CleanupExpiredDrops()
        {
            Logger.Debug("🗑️ Manually cleaning up expired drops...", "CommandHandler");
            
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
                Logger.Debug("No expired drops to clean up.", "CommandHandler");
                return;
            }

            var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();
            int cleanedCount = 0;

            foreach (var drop in expiredDrops)
            {
                Logger.Debug($"Cleaning up expired drop at {drop.Location}", "CommandHandler");

                // Find the dead drop and clear its contents
                foreach (var deadDrop in deadDrops)
                {
                    if (deadDrop.DeadDropName == drop.Location && deadDrop.Storage != null)
                    {
                        try
                        {
                            deadDrop.Storage.ClearContents();
                            Logger.Debug($"✅ Cleared contents from {drop.Location}", "CommandHandler");
                            cleanedCount++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Failed to clear contents from {drop.Location}: {ex.Message}", "CommandHandler");
                        }
                        break;
                    }
                }

                // Remove from pending drops
                SaveFileJsonDataStore.RemoveSpecificDrop(drop.Day, drop.Location);
            }

            Logger.Debug($"✅ Cleaned up {cleanedCount}/{expiredDrops.Count} expired drops", "CommandHandler");
        }
        */
    }
} 