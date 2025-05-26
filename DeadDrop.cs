using S1API.GameTime;
using S1API.DeadDrops;
using S1API.Items;
using System.Collections.Generic;
using System.Linq;

namespace PaxDrops
{
    /// <summary>
    /// Handles logic for spawning dead drops from scheduled drop packets.
    /// Triggered by the daily game time event at 7AM.
    /// </summary>
    public static class DeadDrop
    {
        /// <summary>
        /// Initializes the DeadDrop system and subscribes to in-game day changes.
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

            // Only proceed if the current in-game time is 7:00 AM
            if (currentTime != 700)
            {
                Logger.Msg($"[DeadDrop] ⏰ Skipped — current hour is {currentTime}, not 700.");
                return;
            }

            // Fetch the current day from the game clock
            int currentDay = TimeManager.ElapsedDays;

            // Retrieve a scheduled drop packet from the database (if any)
            List<string> packet = DataBase.GetDrop(currentDay);
            if (packet == null)
            {
                Logger.Msg($"[DeadDrop] 📭 No drop scheduled for Day {currentDay}.");
                return;
            }

            Logger.Msg($"[DeadDrop] 📦 Spawning drop for Day {currentDay}: {string.Join(", ", packet)}");

            // Find the first available drop location
            DeadDropInstance drop = DeadDropManager.All.FirstOrDefault();
            if (drop == null)
            {
                Logger.Warn("[DeadDrop] ❌ No valid dead drop found in the scene.");
                return;
            }

            // Iterate through each item ID in the drop packet
            foreach (string id in packet)
            {
                ItemDefinition def = ItemManager.GetItemDefinition(id);
                if (def == null)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Unknown item ID: '{id}'");
                    continue;
                }

                // Attempt to create an instance of the item
                var item = def.CreateInstance();
                if (item == null)
                {
                    Logger.Warn($"[DeadDrop] ❌ Failed to create item: '{id}'");
                    continue;
                }

                // Add the created item to the drop's storage
                drop.Storage.AddItem(item);
                Logger.Msg($"[DeadDrop] ➕ Added item: {id}");
            }

            Logger.Msg($"[DeadDrop] ✅ Drop placed successfully.");
        }
    }
}
