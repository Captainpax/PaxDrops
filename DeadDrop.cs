using S1API.GameTime;
using S1API.DeadDrops;
using S1API.Items;
using System.Collections.Generic;
using System.Linq;

namespace PaxDrops
{
    /// <summary>
    /// Handles logic for spawning dead drops from scheduled drop packets.
    /// </summary>
    public static class DeadDrop
    {
        /// <summary>
        /// Initializes the DeadDrop system.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[DeadDrop] ✅ System initialized. Listening for OnDayPass...");
            TimeManager.OnDayPass += HandleDayPass;
        }

        /// <summary>
        /// Automatically triggered each in-game day.
        /// Checks for scheduled drops between 7AM–7PM.
        /// </summary>
        public static void HandleDayPass()
        {
            int currentHour = TimeManager.CurrentTime;
            int currentDay = TimeManager.ElapsedDays;

            // Enforce allowed a drop window
            if (currentHour < 700 || currentHour > 1900)
            {
                Logger.Msg($"[DeadDrop] ⏰ Skipped — current time is {currentHour}, outside 7AM–7PM.");
                return;
            }

            // Load DB drop for today
            if (!DataBase.GetDrop(currentDay, out List<string> packet, out int scheduledHour, out string type))
            {
                Logger.Msg($"[DeadDrop] 📭 No drop scheduled for Day {currentDay}.");
                return;
            }

            // Don't spawn early or late
            if (scheduledHour != currentHour)
            {
                Logger.Msg($"[DeadDrop] ⏳ Scheduled drop for Day {currentDay} is set for {scheduledHour}, not {currentHour}.");
                return;
            }

            SpawnDrop(currentDay, packet, scheduledHour, type);
        }

        /// <summary>
        /// Force spawns a drop regardless of time.
        /// Used by debug/test triggers.
        /// </summary>
        public static void ForceSpawnDrop(int day, List<string> packet, string type = "debug", int hour = -1)
        {
            if (hour == -1)
                hour = TimeManager.CurrentTime;

            SpawnDrop(day, packet, hour, type);
        }

        /// <summary>
        /// Spawns a drop into the first available dead drop location.
        /// </summary>
        private static void SpawnDrop(int day, List<string> packet, int hour, string type)
        {
            DeadDropInstance drop = DeadDropManager.All.FirstOrDefault();
            if (drop == null)
            {
                Logger.Warn("[DeadDrop] ❌ No valid dead drop location found.");
                return;
            }

            Logger.Msg($"[DeadDrop] 📦 Spawning drop at {drop.Position} into {drop.Storage.GetType().Name} (GUID: {drop.GUID})");
            Logger.Msg($"[DeadDrop] 📅 Day {day} @ {hour} ({type}) ➤ Contents: {string.Join(", ", packet)}");

            int success = 0;
            int fail = 0;

            foreach (string id in packet)
            {
                var def = ItemManager.GetItemDefinition(id);
                if (def == null)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Unknown item ID: '{id}'");
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

                drop.Storage.AddItem(item);
                Logger.Msg($"[DeadDrop] ✅ Added item: {id}");
                success++;
            }

            Logger.Msg($"[DeadDrop] ✅ Drop complete for Day {day}: {success} added, {fail} failed.");
        }
    }
}
