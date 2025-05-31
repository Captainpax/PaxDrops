using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MelonLoader;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.UI.Phone.Messages;
using static PaxDrops.TierLevel;

namespace PaxDrops
{
    /// <summary>
    /// Handles Mrs. Stacks (Mr. Stacks) messaging and drop scheduling.
    /// IL2CPP port using DataBase instead of complex messaging integration.
    /// </summary>
    public static class MrStacks
    {
        private static bool _initialized;
        private static bool _dailyDropScheduled;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Msg("[MrStacks] 📱 Mrs. Stacks handler initialized");
            
            // Schedule a test drop to verify the system works
            ScheduleTestDrop();
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

            Logger.Msg("[MrStacks] 📱 Mrs. Stacks handler shutdown");
        }

        /// <summary>
        /// Called by TimeMonitor when a new day starts (7:00 AM)
        /// </summary>
        public static void OnNewDay()
        {
            try
            {
                if (_dailyDropScheduled) return;

                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentDay = timeManager.ElapsedDays;
                Logger.Msg($"[MrStacks] 🌅 New day started: Day {currentDay}");

                // Check if we should schedule a drop for today
                if (ShouldScheduleDropToday(currentDay))
                {
                    ScheduleDailyDrop(currentDay);
                    _dailyDropScheduled = true;
                }
                else
                {
                    Logger.Msg($"[MrStacks] 📭 No drop scheduled for Day {currentDay}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Error processing new day: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Resets the daily flag when day changes
        /// </summary>
        public static void OnDayChanged()
        {
            _dailyDropScheduled = false;
            Logger.Msg("[MrStacks] 🔄 Daily drop flag reset");
        }

        /// <summary>
        /// Determines if a drop should be scheduled for the given day
        /// </summary>
        private static bool ShouldScheduleDropToday(int day)
        {
            // Check if there's already a drop scheduled for today
            if (JsonDataStore.PendingDrops.ContainsKey(day))
            {
                Logger.Msg($"[MrStacks] 📋 Drop already scheduled for Day {day}");
                return false;
            }

            // Schedule drops every few days, with some randomness
            var random = new System.Random(day); // Seeded randomness for consistency
            
            // Base chance of 30%, increasing with player progression
            float baseChance = 0.3f;
            float progressionBonus = Math.Min(day * 0.01f, 0.2f); // Up to 20% bonus
            float totalChance = baseChance + progressionBonus;
            
            bool shouldSchedule = random.NextDouble() < totalChance;
            
            Logger.Msg($"[MrStacks] 🎲 Drop chance for Day {day}: {totalChance:P1} - {(shouldSchedule ? "Scheduling" : "Skipping")}");
            
            return shouldSchedule;
        }

        /// <summary>
        /// Schedules a daily drop using the tier system
        /// </summary>
        private static void ScheduleDailyDrop(int day)
        {
            try
            {
                // Generate a drop packet using the tier system
                var packet = TierLevel.GetDropPacket(day);
                Logger.Msg($"[MrStacks] 📦 Generated drop packet: {packet}");

                // Convert to the legacy string format for database storage
                var items = packet.ToFlatList();

                // Schedule for later in the day (between 10 AM and 6 PM)
                var random = new System.Random();
                int dropHour = random.Next(10, 19); // 10 AM to 6 PM

                // Save to database
                JsonDataStore.SaveDrop(day, items, dropHour, "mrs_stacks");

                Logger.Msg($"[MrStacks] ✅ Scheduled drop for Day {day} at {dropHour}:00");
                Logger.Msg($"[MrStacks] 📱 Mrs. Stacks would send: \"Package available today at {dropHour}:00. Check your usual spot.\"");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Failed to schedule daily drop: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Schedules a test drop to verify the system is working
        /// </summary>
        private static void ScheduleTestDrop()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) 
                {
                    Logger.Warn($"[MrStacks] ⚠️ TimeManager not available for test drop");
                    return;
                }

                // Get current game state - DEBUG THE DAY ISSUE
                int rawElapsedDays = timeManager.ElapsedDays;
                int currentHour = timeManager.CurrentTime;
                
                Logger.Msg($"[MrStacks] 🔍 DEBUG: TimeManager.ElapsedDays = {rawElapsedDays}");
                Logger.Msg($"[MrStacks] 🔍 DEBUG: TimeManager.CurrentTime = {currentHour}");
                
                // Use the actual game day, don't force it to be different
                int currentDay = rawElapsedDays;
                
                Logger.Msg($"[MrStacks] 🧪 Scheduling test drop for Day {currentDay} (current hour: {currentHour})");
                Logger.Msg($"[MrStacks] 🎯 This should match the day shown in TimeMonitor!");

                // Get player info for realistic drop context
                var player = Il2CppScheduleOne.PlayerScripts.Player.Local;
                string playerName = player?.PlayerName ?? "Unknown";
                
                // Try to get player rank if possible (for tier calculations)
                try
                {
                    // Attempt to get player's crime rank or level
                    var crimeData = player?.CrimeData;
                    if (crimeData != null)
                    {
                        // Check if we can get rank from crime data
                        Logger.Msg($"[MrStacks] 👤 Player: {playerName} (has crime data)");
                    }
                    else
                    {
                        Logger.Msg($"[MrStacks] 👤 Player: {playerName} (no crime data available)");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[MrStacks] ⚠️ Could not get player rank: {ex.Message}");
                }

                // Generate a test packet appropriate for the current day/progression
                var packet = TierLevel.GetDropPacket(currentDay);
                var items = packet.ToFlatList();

                Logger.Msg($"[MrStacks] 📦 Generated test packet: {packet.Loot.Count} items + ${packet.CashAmount}");
                Logger.Msg($"[MrStacks] 🎯 Items: {string.Join(", ", items.Take(5))}{(items.Count > 5 ? "..." : "")}");

                // Schedule for a reasonable time (1-2 hours from now, or immediate for testing)
                int dropHour = currentHour + 1; // Schedule for next hour
                
                // For immediate testing, use current hour
                int immediateHour = currentHour;

                // Save scheduled drop to database
                JsonDataStore.SaveDrop(currentDay, items, dropHour, "test_scheduled");
                Logger.Msg($"[MrStacks] 📅 Scheduled drop saved for Day {currentDay} at {dropHour}:00");

                // IMMEDIATE TEST: Force spawn a drop right now to verify the system works
                Logger.Msg($"[MrStacks] 🚀 TESTING: Force spawning test drop immediately!");
                Logger.Msg($"[MrStacks] 📍 This will help verify storage detection and item spawning");
                
                // Create immediate test record with proper context
                DeadDrop.ForceSpawnDrop(currentDay, items, "immediate_test", immediateHour);
                
                // Log drop expiry info
                Logger.Msg($"[MrStacks] ⏰ Drop Timing Info:");
                Logger.Msg($"[MrStacks]   - Current Game Day: {currentDay} (from TimeManager.ElapsedDays: {rawElapsedDays})");
                Logger.Msg($"[MrStacks]   - Scheduled: Day {currentDay} @ {dropHour}:00");
                Logger.Msg($"[MrStacks]   - Immediate: Day {currentDay} @ {immediateHour}:00");
                Logger.Msg($"[MrStacks]   - Expiry: Drops expire after 1 day (Day {currentDay + 1})");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Failed to schedule test drop: {ex.Message}");
                Logger.Exception(ex);
            }
        }
    }
} 