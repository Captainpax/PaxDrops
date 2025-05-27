using S1API.GameTime;
using S1API.DeadDrops;
using S1API.Items;
using S1API.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PaxDrops
{
    /// <summary>
    /// Handles logic for spawning scheduled dead drops into the world.
    /// Reacts to day progression and supports debug spawn injection.
    /// </summary>
    public static class DeadDrop
    {
        /// <summary>
        /// Initializes the DeadDrop system and recovers pending drops from saved state.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[DeadDrop] ✅ System initialized. Listening for OnDayPass...");

            // Try recover drop on game load
            TrySpawnPendingDrop();

            // Listen for daily ticks
            TimeManager.OnDayPass += HandleDayPass;
        }

        /// <summary>
        /// Handles startup recovery in case a drop was scheduled but not yet spawned due to mid-day save/load.
        /// </summary>
        private static void TrySpawnPendingDrop()
        {
            int day = TimeManager.ElapsedDays;
            int hour = TimeManager.CurrentTime;

            // Skip if outside delivery window
            if (hour < 700 || hour > 1900)
                return;

            if (!DataBase.GetDrop(day, out var packet, out int dropHour, out string type, out string _))
                return;

            if (dropHour <= hour)
            {
                Logger.Msg($"[DeadDrop] 🔄 Recovering saved drop (Day {day} @ {dropHour})");
                SpawnDrop(day, packet, dropHour, type);
            }
        }

        /// <summary>
        /// Called each in-game day to evaluate and spawn the day's drop if timing matches.
        /// </summary>
        public static void HandleDayPass()
        {
            int day = TimeManager.ElapsedDays;
            int hour = TimeManager.CurrentTime;

            if (hour < 700 || hour > 1900)
            {
                Logger.Msg($"[DeadDrop] ⏰ Skipped drop — current hour {hour} is outside 7AM–7PM window.");
                return;
            }

            if (!DataBase.GetDrop(day, out var packet, out int scheduledHour, out string type, out string _))
            {
                Logger.Msg($"[DeadDrop] 📭 No scheduled drop found for Day {day}.");
                return;
            }

            if (scheduledHour != hour)
            {
                Logger.Msg($"[DeadDrop] ⏳ Drop not ready — set for {scheduledHour}, now is {hour}.");
                return;
            }

            SpawnDrop(day, packet, hour, type);
        }

        /// <summary>
        /// Immediately forces a drop to spawn at the nearest dead drop.
        /// </summary>
        public static void ForceSpawnDrop(int day, List<string> packet, string type = "debug", int hour = -1)
        {
            if (hour == -1)
                hour = TimeManager.CurrentTime;

            SpawnDrop(day, packet, hour, type);
        }

        /// <summary>
        /// Core logic to convert a drop packet into real items and place them into storage.
        /// </summary>
        private static void SpawnDrop(int day, List<string> packet, int hour, string type)
        {
            DeadDropInstance target = GetNearestAvailableDrop();
            if (target == null)
            {
                Logger.Warn("[DeadDrop] ❌ No valid drop point found.");
                return;
            }

            Logger.Msg($"[DeadDrop] 📦 Spawning into dead drop at {target.Position} ➤ {target.Storage.GetType().Name}");
            Logger.Msg($"[DeadDrop] 📅 Day {day} @ {hour} ({type}) ➤ Contents: {string.Join(", ", packet)}");

            int success = 0, fail = 0;

            foreach (string id in packet)
            {
                var def = ItemManager.GetItemDefinition(id);
                if (def == null)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Invalid item ID: '{id}'");
                    fail++;
                    continue;
                }

                var item = def.CreateInstance();
                if (item == null)
                {
                    Logger.Warn($"[DeadDrop] ❌ Failed to create item: '{id}'");
                    fail++;
                    continue;
                }

                target.Storage.AddItem(item);
                success++;
            }

            Logger.Msg($"[DeadDrop] ✅ Drop spawned: {success} added, {fail} failed.");
        }

        /// <summary>
        /// Finds the closest dead drop to the player, or returns first available if player isn't found.
        /// </summary>
        private static DeadDropInstance GetNearestAvailableDrop()
        {
            var all = DeadDropManager.All;
            if (all == null || all.Length == 0)
                return null;

            var player = Player.Local;
            if (player == null)
                return all.FirstOrDefault();

            return all.OrderBy(d => Vector3.Distance(player.Position, d.Position)).FirstOrDefault();
        }
    }
}
