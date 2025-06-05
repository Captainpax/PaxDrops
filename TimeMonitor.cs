using System;
using Il2CppScheduleOne.GameTime;
using MelonLoader;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using PaxDrops.MrStacks;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.Economy;
using PaxDrops.Configs;

namespace PaxDrops
{
    /// <summary>
    /// Enhanced time monitoring system with collection detection, expiry cleanup, and reminders.
    /// Monitors game time changes and handles scheduled events.
    /// </summary>
    public static class TimeMonitor
    {
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager != null)
                {
                    HookTimeManagerEvents(timeManager);
                }
                else
                {
                    Logger.Warn("[TimeMonitor] ⚠️ TimeManager not found during init, will retry later.");
                    MelonCoroutines.Start(RetryTimeManagerHook());
                }

                Logger.Msg("[TimeMonitor] ⏰ Time monitoring initialized.");
            }
            catch (Exception ex)
            {
                Logger.Error("[TimeMonitor] ❌ Failed to initialize time monitoring.");
                Logger.Exception(ex);
            }
        }

        private static IEnumerator RetryTimeManagerHook()
        {
            int retries = 0;
            while (retries < 10 && TimeManager.Instance == null)
            {
                yield return new WaitForSeconds(5f);
                retries++;
            }

            var timeManager = TimeManager.Instance;
            if (timeManager != null)
            {
                HookTimeManagerEvents(timeManager);
                Logger.Msg("[TimeMonitor] ✅ TimeManager hook established after retry.");
            }
            else
            {
                Logger.Error("[TimeMonitor] ❌ Failed to hook TimeManager after retries.");
            }
        }

        private static void HookTimeManagerEvents(TimeManager timeManager)
        {
            // Hook into hour changes to check for 7:00 AM events and reminders
            timeManager.onHourPass += new System.Action(OnHourPass);
            
            // Hook into day changes for daily reset
            timeManager.onDayPass += new System.Action(OnDayPass);
            
            Logger.Msg("[TimeMonitor] 🕐 Event hooks established.");
        }

        /// <summary>
        /// Handles hour changes - checks for scheduled drops, collection detection, expiry cleanup, and reminders.
        /// </summary>
        private static void OnHourPass()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentHour = timeManager.CurrentTime;
                int currentDay = timeManager.ElapsedDays;
                
                Logger.Msg($"[TimeMonitor] ⏰ Hour changed to {currentHour} on day {currentDay}");

                // Check for scheduled drop deliveries at delivery time (7:30 AM = 730)
                if (currentHour == 730)
                {
                    Logger.Msg("[TimeMonitor] 🌅 Drop delivery time! Checking for scheduled deliveries...");
                    ProcessScheduledDeliveries(currentDay, currentHour);
                }
                // Also check for missed delivery windows (if player skipped 7:30 AM)
                else if (currentHour > 730 && currentHour < 1200) // Between 7:30 AM and noon
                {
                    Logger.Msg($"[TimeMonitor] 🔍 Checking for missed deliveries (current time {currentHour} is past 7:30 AM)...");
                    ProcessScheduledDeliveries(currentDay, currentHour);
                }

                // Collection detection and expiry cleanup (every hour)
                CheckCollectionStatus(currentDay, currentHour);
                CleanupExpiredDrops(currentDay, currentHour);

                // Morning business hours and inactivity reminders (7:00 AM = 700)
                if (currentHour == 700)
                {
                    Logger.Msg("[TimeMonitor] 🌅 Morning business hours starting!");
                    MrsStacksNPC.OnNewDay();
                }

                // Evening reminder check (8:00 PM = 2000)
                if (currentHour == 2000)
                {
                    Logger.Msg("[TimeMonitor] 🌙 Evening reminder time!");
                    SendEveningReminders(currentDay);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error handling hour change: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Handles day changes - resets daily flags and checks for new opportunities.
        /// </summary>
        private static void OnDayPass()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentDay = timeManager.ElapsedDays;
                Logger.Msg($"[TimeMonitor] 📅 Day changed to {currentDay}");

                // Notify MrStacks about day change
                MrsStacksNPC.OnDayChanged();

                // Clean up old drops from database
                CleanupOldDrops(currentDay);
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error handling day change: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Process scheduled deliveries for the current day and time
        /// </summary>
        private static void ProcessScheduledDeliveries(int currentDay, int currentHour)
        {
            try
            {
                var allDrops = SaveFileJsonDataStore.GetAllDrops();
                Logger.Msg($"[TimeMonitor] 🔍 Checking {allDrops.Count} total drops for delivery at day {currentDay}, hour {currentHour}");

                var deliveriesToProcess = new List<SaveFileJsonDataStore.DropRecord>();

                foreach (var drop in allDrops)
                {
                    Logger.Msg($"[TimeMonitor] 🔍 Examining drop: Day={drop.Day}, Hour={drop.DropHour}, Location='{drop.Location}', Available={DropConfig.IsDropAvailable(drop.Day, drop.DropHour, currentDay, currentHour)}");
                    
                    // Check if this drop should be delivered now
                    if (DropConfig.IsDropAvailable(drop.Day, drop.DropHour, currentDay, currentHour) && 
                        string.IsNullOrEmpty(drop.Location)) // Not yet spawned
                    {
                        deliveriesToProcess.Add(drop);
                        Logger.Msg($"[TimeMonitor] ✅ Drop scheduled for delivery: {drop.Org} (Day={drop.Day}, Hour={drop.DropHour})");
                    }
                    else
                    {
                        string reason = !DropConfig.IsDropAvailable(drop.Day, drop.DropHour, currentDay, currentHour) 
                            ? "not available yet" 
                            : "already spawned";
                        Logger.Msg($"[TimeMonitor] ⏭️ Skipping drop: {drop.Org} - {reason}");
                    }
                }

                Logger.Msg($"[TimeMonitor] 📦 Found {deliveriesToProcess.Count} drops ready for delivery");

                int successCount = 0;
                int failCount = 0;
                var readyDrops = new List<(SaveFileJsonDataStore.DropRecord drop, string location)>();

                foreach (var drop in deliveriesToProcess)
                {
                    Logger.Msg($"[TimeMonitor] 📦 Processing delivery #{successCount + failCount + 1}: {drop.Org} scheduled for day {drop.Day} at {DropConfig.FormatGameTime(drop.DropHour)}");
                    
                    // Spawn the drop at a location
                    string? location = DeadDrop.SpawnImmediateDrop(drop);
                    
                    if (!string.IsNullOrEmpty(location))
                    {
                        // Add to ready drops list for consolidated messaging
                        if (drop.Org.Contains("Mrs. Stacks"))
                        {
                            readyDrops.Add((drop, location));
                        }
                        
                        successCount++;
                        Logger.Msg($"[TimeMonitor] ✅ Delivery #{successCount} completed: {drop.Org} at {location}");
                    }
                    else
                    {
                        failCount++;
                        Logger.Error($"[TimeMonitor] ❌ Delivery #{failCount} failed: {drop.Org}");
                    }
                }

                // Send consolidated ready message for all Mrs. Stacks drops
                if (readyDrops.Count > 0)
                {
                    SendConsolidatedReadyMessage(readyDrops);
                }

                if (deliveriesToProcess.Count > 0)
                {
                    Logger.Msg($"[TimeMonitor] 📬 Processed {deliveriesToProcess.Count} deliveries: {successCount} success, {failCount} failed");
                }
                else
                {
                    Logger.Msg("[TimeMonitor] 📭 No deliveries processed at this time");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error processing deliveries: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Send consolidated ready message for multiple drops delivered at the same time
        /// </summary>
        private static void SendConsolidatedReadyMessage(List<(SaveFileJsonDataStore.DropRecord drop, string location)> readyDrops)
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                if (readyDrops.Count == 1)
                {
                    // Single drop - use original format
                    var drop = readyDrops[0].drop;
                    var location = readyDrops[0].location;
                    var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                    string expiryText = expiryDay != -1 ? $" (Expires day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})" : "";

                    MrsStacksMessaging.SendMessage(npc, 
                        $"Package ready! Your {drop.Org} delivery is waiting at {location}. " +
                        $"Retrieve when safe. Quality guaranteed as always.{expiryText}");
                }
                else
                {
                    // Multiple drops - consolidated format
                    var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(readyDrops[0].drop.ExpiryTime);
                    string expiryText = expiryDay != -1 ? $" (All expire day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})" : "";
                    
                    var message = $"Multiple packages ready! Your {readyDrops.Count} deliveries:\n";
                    for (int i = 0; i < readyDrops.Count; i++)
                    {
                        var (drop, location) = readyDrops[i];
                        message += $"• {drop.Org} at {location}\n";
                    }
                    message += $"Retrieve when safe. Quality guaranteed as always.{expiryText}";

                    MrsStacksMessaging.SendMessage(npc, message);
                }

                Logger.Msg($"[TimeMonitor] 📱 Consolidated ready message sent for {readyDrops.Count} drops");
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Consolidated ready message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check collection status of active drops by monitoring storage
        /// </summary>
        private static void CheckCollectionStatus(int currentDay, int currentHour)
        {
            try
            {
                var pendingDrops = SaveFileJsonDataStore.GetAllDrops();
                var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();

                foreach (var drop in pendingDrops)
                {
                    if (drop.IsCollected || string.IsNullOrEmpty(drop.Location)) continue;
                    
                    // Only check drops that are available for pickup
                    if (!DropConfig.IsDropAvailable(drop.Day, drop.DropHour, currentDay, currentHour)) continue;

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
                        // Check if storage is significantly emptier than initial
                        int currentItemCount = targetDeadDrop.Storage.ItemCount;
                        
                        // If storage has 50% or fewer items than initially placed, consider it collected
                        if (currentItemCount <= (drop.InitialItemCount * 0.5f))
                        {
                            Logger.Msg($"[TimeMonitor] ✅ Drop at {drop.Location} appears to have been collected ({currentItemCount}/{drop.InitialItemCount} items remaining)");
                            SaveFileJsonDataStore.MarkSpecificDropCollected(drop.Day, drop.Location);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error checking collection status: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Clean up expired drops from their storage locations
        /// </summary>
        private static void CleanupExpiredDrops(int currentDay, int currentHour)
        {
            try
            {
                var allDrops = SaveFileJsonDataStore.GetAllDrops();
                var expiredDrops = new List<SaveFileJsonDataStore.DropRecord>();

                // Find expired drops using game time
                foreach (var drop in allDrops)
                {
                    if (!string.IsNullOrEmpty(drop.ExpiryTime) && 
                        DropConfig.IsDropExpired(drop.ExpiryTime, currentDay, currentHour))
                    {
                        expiredDrops.Add(drop);
                    }
                }

                if (expiredDrops.Count == 0) return;

                var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();

                foreach (var drop in expiredDrops)
                {
                    var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                    Logger.Msg($"[TimeMonitor] 🗑️ Cleaning up expired drop at {drop.Location} (expired day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})");

                    // Find the dead drop and clear its contents
                    foreach (var deadDrop in deadDrops)
                    {
                        if (deadDrop.DeadDropName == drop.Location && deadDrop.Storage != null)
                        {
                            try
                            {
                                // Clear the storage contents
                                deadDrop.Storage.ClearContents();
                                Logger.Msg($"[TimeMonitor] ✅ Cleared expired drop contents from {drop.Location}");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"[TimeMonitor] ❌ Failed to clear contents from {drop.Location}: {ex.Message}");
                            }
                            break;
                        }
                    }

                    // Remove from pending drops - use specific location removal
                    SaveFileJsonDataStore.RemoveSpecificDrop(drop.Day, drop.Location);
                }

                if (expiredDrops.Count > 0)
                {
                    Logger.Msg($"[TimeMonitor] 🗑️ Cleaned up {expiredDrops.Count} expired drops");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error cleaning up expired drops: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Send evening reminders for uncollected drops
        /// </summary>
        private static void SendEveningReminders(int currentDay)
        {
            try
            {
                var pendingDrops = SaveFileJsonDataStore.GetAllDrops();
                var uncollectedDrops = new List<SaveFileJsonDataStore.DropRecord>();

                foreach (var drop in pendingDrops)
                {
                    // Check for drops that are available today but not collected
                    if (drop.Day == currentDay && !drop.IsCollected && !string.IsNullOrEmpty(drop.Location))
                    {
                        uncollectedDrops.Add(drop);
                    }
                }

                if (uncollectedDrops.Count > 0)
                {
                    Logger.Msg($"[TimeMonitor] 📱 Sending evening reminder for {uncollectedDrops.Count} uncollected drops");

                    var npc = MrsStacksMessaging.FindMrsStacksNPC();
                    if (npc != null)
                    {
                        if (uncollectedDrops.Count == 1)
                        {
                            // Single drop - use original format
                            var drop = uncollectedDrops[0];
                            var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                            string expiryText = expiryDay != -1 ? $" (expires day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})" : "";

                            MrsStacksMessaging.SendMessage(npc, 
                                $"Evening reminder: Your {drop.Org} package is still waiting at {drop.Location}{expiryText}. " +
                                $"Don't forget to collect it when safe!");
                        }
                        else
                        {
                            // Multiple drops - consolidated format
                            var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(uncollectedDrops[0].ExpiryTime);
                            string expiryText = expiryDay != -1 ? $" (all expire day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})" : "";

                            var message = $"Evening reminder: You have {uncollectedDrops.Count} uncollected packages:\n";
                            for (int i = 0; i < uncollectedDrops.Count; i++)
                            {
                                var drop = uncollectedDrops[i];
                                message += $"• {drop.Org} at {drop.Location}\n";
                            }
                            message += $"Don't forget to collect them when safe!{expiryText}";

                            MrsStacksMessaging.SendMessage(npc, message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error sending evening reminders: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Removes old drops that are past their expiration
        /// </summary>
        private static void CleanupOldDrops(int currentDay)
        {
            try
            {
                int removedCount = 0;
                var daysToRemove = new List<int>();

                foreach (var kvp in SaveFileJsonDataStore.PendingDrops)
                {
                    // Remove drops that are more than 2 days old (extra safety buffer)
                    if (kvp.Key < currentDay - 2)
                    {
                        removedCount += kvp.Value.Count;
                        daysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var day in daysToRemove)
                {
                    SaveFileJsonDataStore.PendingDrops.Remove(day);
                }

                if (removedCount > 0)
                {
                    Logger.Msg($"[TimeMonitor] 🗑️ Cleaned up {removedCount} old drop records from {daysToRemove.Count} days");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error cleaning up old drops: {ex.Message}");
            }
        }

        public static void Shutdown()
        {
            if (!_initialized) return;

            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager != null)
                {
                    // Unhook events
                    timeManager.onHourPass -= new System.Action(OnHourPass);
                    timeManager.onDayPass -= new System.Action(OnDayPass);
                }

                _initialized = false;
                Logger.Msg("[TimeMonitor] 🔌 Time monitoring shutdown.");
            }
            catch (Exception ex)
            {
                Logger.Error("[TimeMonitor] ❌ Error during shutdown.");
                Logger.Exception(ex);
            }
        }
    }
} 