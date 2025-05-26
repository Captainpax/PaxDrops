using S1API.GameTime;
using S1API.DeadDrops;
using S1API.Items;
using System.Collections.Generic;
using System.Linq;

namespace PaxDrops
{
    /// <summary>
    /// Handles logic for spawning dead drops from scheduled drop packets.
    /// Triggered by the daily game time event at 7:00 AM.
    /// </summary>
    public static class DeadDrop
    {
        /// <summary>
        /// Initializes the DeadDrop system and subscribes to day transitions.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[DeadDrop] ✅ System initialized. Listening for OnDayPass...");
            TimeManager.OnDayPass += HandleDayPass;
        }

        /// <summary>
        /// Triggered at the start of each new in-game day.
        /// Spawns a dead drop at exactly 7:00 AM if one is scheduled.
        /// </summary>
        public static void HandleDayPass()
        {
            int currentTime = TimeManager.CurrentTime;

            // Only spawn drops at 7:00 AM
            if (currentTime != 700)
            {
                Logger.Msg($"[DeadDrop] ⏰ Skipped — current time is {currentTime}, waiting for 700.");
                return;
            }

            int currentDay = TimeManager.ElapsedDays;
            List<string> packet = DataBase.GetDrop(currentDay);
            if (packet == null)
            {
                Logger.Msg($"[DeadDrop] 📭 No drop scheduled for Day {currentDay}.");
                return;
            }

            DeadDropInstance drop = DeadDropManager.All.FirstOrDefault();
            if (drop == null)
            {
                Logger.Warn("[DeadDrop] ❌ No valid dead drop location found in scene.");
                return;
            }

            Logger.Msg($"[DeadDrop] 📦 Spawning drop at {drop.Position} into {drop.Storage.GetType().Name} (GUID: {drop.GUID})");
            Logger.Msg($"[DeadDrop] ➤ Contents: {string.Join(", ", packet)}");

            int success = 0;
            int fail = 0;

            foreach (string id in packet)
            {
                ItemDefinition def = ItemManager.GetItemDefinition(id);
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

            Logger.Msg($"[DeadDrop] ✅ Drop complete for Day {currentDay}: {success} added, {fail} failed.");
        }
    }
}
