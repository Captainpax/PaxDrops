using System;
using Il2CppScheduleOne.GameTime;
using MelonLoader;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace PaxDrops
{
    /// <summary>
    /// Monitors game time events and triggers daily checks at 7:00 AM for drops and messaging.
    /// Replaces the manual time polling with proper TimeManager event hooks.
    /// </summary>
    public static class TimeMonitor
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;

            try
            {
                // Hook into TimeManager events
                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Warn("[TimeMonitor] ⚠️ TimeManager not available, will retry later.");
                    MelonCoroutines.Start(WaitForTimeManager());
                    return;
                }

                HookTimeManagerEvents(timeManager);
                _initialized = true;
                Logger.Msg("[TimeMonitor] ✅ Hooked into TimeManager events.");
            }
            catch (Exception ex)
            {
                Logger.Error("[TimeMonitor] ❌ Failed to initialize time monitoring.");
                Logger.Exception(ex);
            }
        }

        private static System.Collections.IEnumerator WaitForTimeManager()
        {
            yield return new UnityEngine.WaitForSeconds(2.0f);
            
            var timeManager = TimeManager.Instance;
            while (timeManager == null)
            {
                yield return new UnityEngine.WaitForSeconds(1.0f);
                timeManager = TimeManager.Instance;
            }

            HookTimeManagerEvents(timeManager);
            _initialized = true;
            Logger.Msg("[TimeMonitor] ✅ Hooked into TimeManager events (delayed).");
        }

        private static void HookTimeManagerEvents(TimeManager timeManager)
        {
            // Hook into hour changes to check for 7:00 AM events
            timeManager.onHourPass += new System.Action(OnHourPass);
            
            // Hook into day changes for daily reset
            timeManager.onDayPass += new System.Action(OnDayPass);
            
            Logger.Msg("[TimeMonitor] 🕐 Event hooks established.");
        }

        /// <summary>
        /// Handles hour changes - checks for scheduled drops.
        /// </summary>
        private static void OnHourPass()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentHour = timeManager.CurrentTime;
                Logger.Msg($"[TimeMonitor] ⏰ Hour changed to {currentHour}");

                // Notify DeadDrop system about time change
                DeadDrop.OnTimeChanged();

                // Special case: 7 AM is start of business day
                if (currentHour == 700) // 7:00 AM
                {
                    Logger.Msg("[TimeMonitor] 🌅 Morning business hours starting!");
                    MrStacks.OnNewDay();
                }

                // Check for scheduled drops
                CheckScheduledDrops(currentHour, timeManager.ElapsedDays);
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
                MrStacks.OnDayChanged();

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
                    // Remove drops that are more than 1 day old
                    if (kvp.Key < currentDay - 1)
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
                    Logger.Msg($"[TimeMonitor] 🗑️ Cleaned up {removedCount} expired drops");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[TimeMonitor] ❌ Error cleaning up old drops: {ex.Message}");
            }
        }

        private static void CheckScheduledDrops(int currentHour, int currentDay)
        {
            bool dropFound = false;

            foreach (var kvp in JsonDataStore.PendingDrops)
            {
                var drop = kvp.Value;
                if (drop.DropHour == currentHour)
                {
                    Logger.Msg($"[TimeMonitor] ⏰ Drop scheduled for {currentHour}:00 - triggering spawn");
                    DeadDrop.OnTimeChanged();
                    
                    // Remove processed drop
                    var key = kvp.Key;
                    MelonCoroutines.Start(RemoveDropAfterDelay(key));
                    
                    dropFound = true;
                    break; // Only process one drop per hour check
                }
            }
            
            if (!dropFound)
            {
                Logger.Msg($"[TimeMonitor] 📭 No drops scheduled for {currentHour}:00");
            }
        }

        private static IEnumerator RemoveDropAfterDelay(int key)
        {
            yield return new WaitForSeconds(1f);
            JsonDataStore.PendingDrops.Remove(key);
            Logger.Msg($"[TimeMonitor] 🗑️ Removed processed drop for day {key}");
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