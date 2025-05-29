using System;
using System.Collections.Generic;
using UnityEngine;
using S1API.GameTime;
using S1API.DeadDrops;
using S1API.Items;
using S1API.Entities;

namespace PaxDrops
{
    /// <summary>
    /// Handles logic for spawning scheduled dead drops into the world.
    /// </summary>
    public static class DeadDrop
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Msg("[DeadDrop] ✅ System initialized. Listening for OnDayPass...");
            TimeManager.OnDayPass += HandleDayPass;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

            TimeManager.OnDayPass -= HandleDayPass;
            Logger.Msg("[DeadDrop] 🔌 Shutdown complete.");
        }

        public static void HandleDayPass()
        {
            int day = TimeManager.ElapsedDays;
            int currentHour = TimeManager.CurrentTime;

            if (!DataBase.PendingDrops.TryGetValue(day, out var drop))
            {
                Logger.Msg($"[DeadDrop] 📭 No scheduled drop found for Day {day}.");
                return;
            }

            if (currentHour < drop.DropHour)
            {
                Logger.Msg($"[DeadDrop] ⏳ Drop scheduled later today @ {drop.DropTime}.");
                return;
            }

            Logger.Msg($"[DeadDrop] 🕐 Spawning drop for Day {day}...");
            SpawnDrop(drop);
        }

        public static void ForceSpawnDrop(int day, List<string> packet, string type = "debug", int hour = -1)
        {
            if (hour == -1)
                hour = TimeManager.CurrentTime;

            var record = new DataBase.DropRecord
            {
                Day = day,
                Items = packet,
                DropHour = hour,
                DropTime = TimeSpan.FromMinutes(hour).ToString(@"hh\:mm"),
                Org = "DevCommand",
                CreatedTime = DateTime.Now.ToString("s"),
                Type = type,
                Location = ""
            };

            SpawnDrop(record);
        }

        private static void SpawnDrop(DataBase.DropRecord drop)
        {
            var all = DeadDropManager.All;
            if (all == null || all.Length == 0)
            {
                Logger.Warn("[DeadDrop] ❌ No drop locations available.");
                return;
            }

            var player = Player.Local;
            DeadDropInstance target = player != null
                ? FindClosestDrop(player.Position, all)
                : all[0];

            Logger.Msg($"[DeadDrop] 📦 Dropping at {target.Position} | Storage: {target.Storage.GetType().Name}");
            Logger.Msg($"[DeadDrop] 🧾 From: {drop.Org} | {drop.Type} | {drop.DropTime} | Items: {drop.Items.Count}");

            int success = 0, fail = 0;

            foreach (string entry in drop.Items)
            {
                string[] parts = entry.Split(':');
                string id = parts[0];
                int amount = (parts.Length > 1 && int.TryParse(parts[1], out int parsed)) ? parsed : 1;

                var def = ItemManager.GetItemDefinition(id);
                if (def == null)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Invalid item ID: '{id}'");
                    fail++;
                    continue;
                }

                for (int i = 0; i < amount; i++)
                {
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
            }

            Logger.Msg($"[DeadDrop] ✅ Drop complete: {success} added, {fail} failed.");
        }

        private static DeadDropInstance FindClosestDrop(Vector3 origin, DeadDropInstance[] all)
        {
            DeadDropInstance best = all[0];
            float bestDist = Vector3.Distance(origin, best.Position);

            foreach (var drop in all)
            {
                float dist = Vector3.Distance(origin, drop.Position);
                if (dist < bestDist)
                {
                    best = drop;
                    bestDist = dist;
                }
            }

            return best;
        }
    }
}
