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
                var allDrops = JsonDataStore.GetAllDrops();
                Logger.Msg($"[TimeMonitor] 🔍 Checking {allDrops.Count} total drops for delivery at day {currentDay}, hour {currentHour}");

                var deliveriesToProcess = new List<JsonDataStore.DropRecord>();

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

                foreach (var drop in deliveriesToProcess)
                {
                    Logger.Msg($"[TimeMonitor] 📦 Processing delivery: {drop.Org} scheduled for day {drop.Day} at {DropConfig.FormatGameTime(drop.DropHour)}");
                    
                    // Spawn the drop at a location
                    string? location = DeadDrop.SpawnImmediateDrop(drop);
                    
                    if (!string.IsNullOrEmpty(location))
                    {
                        // Send ready message if it's from Mrs. Stacks
                        if (drop.Org.Contains("Mrs. Stacks"))
                        {
                            SendReadyMessage(drop, location);
                        }
                        
                        Logger.Msg($"[TimeMonitor] ✅ Delivery completed: {drop.Org} at {location}");
                    }
                    else
                    {
                        Logger.Error($"[TimeMonitor] ❌ Failed to deliver: {drop.Org}");
                    }
                }

                if (deliveriesToProcess.Count > 0)
                {
                    Logger.Msg($"[TimeMonitor] 📬 Processed {deliveriesToProcess.Count} deliveries");
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
        /// Send ready message for delivered drop
        /// </summary>
        private static void SendReadyMessage(JsonDataStore.DropRecord drop, string location)
        {
            try
            {
                var npc = MrsStacksMessaging.FindMrsStacksNPC();
                if (npc == null) return;

                var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                string expiryText = expiryDay != -1 ? $" (Expires day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})" : "";

                MrsStacksMessaging.SendMessage(npc, 
                    $"Package ready! Your {drop.Org} delivery is waiting at {location}. " +
                    $"Retrieve when safe. Quality guaranteed as always.{expiryText}");

                Logger.Msg("[TimeMonitor] 📱 Ready message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Ready message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check collection status of active drops by monitoring storage
        /// </summary>
        private static void CheckCollectionStatus(int currentDay, int currentHour)
        {
            try
            {
                var pendingDrops = JsonDataStore.GetAllDrops();
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
                            JsonDataStore.MarkDropCollected(drop.Day);
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
                var allDrops = JsonDataStore.GetAllDrops();
                var expiredDrops = new List<JsonDataStore.DropRecord>();

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

                    // Remove from pending drops
                    JsonDataStore.RemoveDrop(drop.Day);
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
                var pendingDrops = JsonDataStore.GetAllDrops();
                var uncollectedDrops = new List<JsonDataStore.DropRecord>();

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
                    Logger.Msg($"[TimeMonitor] 📱 Sending evening reminders for {uncollectedDrops.Count} uncollected drops");

                    var npc = MrsStacksMessaging.FindMrsStacksNPC();
                    if (npc != null)
                    {
                        foreach (var drop in uncollectedDrops)
                        {
                            var (expiryDay, expiryHour) = DropConfig.ParseExpiryTime(drop.ExpiryTime);
                            string expiryText = expiryDay != -1 ? $" (expires day {expiryDay} at {DropConfig.FormatGameTime(expiryHour)})" : "";

                            MrsStacksMessaging.SendMessage(npc, 
                                $"Evening reminder: Your {drop.Org} package is still waiting at {drop.Location}{expiryText}. " +
                                $"Don't forget to collect it when safe!");
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
                var keysToRemove = new List<int>();

                foreach (var kvp in JsonDataStore.PendingDrops)
                {
                    // Remove drops that are more than 2 days old (extra safety buffer)
                    if (kvp.Key < currentDay - 2)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    JsonDataStore.PendingDrops.Remove(key);
                    removedCount++;
                }

                if (removedCount > 0)
                {
                    Logger.Msg($"[TimeMonitor] 🗑️ Cleaned up {removedCount} old drop records");
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