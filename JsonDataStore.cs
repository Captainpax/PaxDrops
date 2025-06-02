using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops
{
    /// <summary>
    /// Manages persistent storage of scheduled drops using JSON files.
    /// IL2CPP port using simple JSON serialization for maximum compatibility.
    /// Enhanced with tier-based daily order tracking.
    /// </summary>
    public static class JsonDataStore
    {
        private const string DataDir = "Mods/PaxDrops/Data";
        private const string DropsFile = "Mods/PaxDrops/Data/drops.json";

        public class DropRecord
        {
            public int Day;
            public List<string> Items;
            public int DropHour;
            public string DropTime;
            public string Org;
            public string CreatedTime;
            public string Type;
            public string Location;

            public DropRecord()
            {
                Items = new List<string>();
                DropTime = "";
                Org = "";
                CreatedTime = "";
                Type = "";
                Location = "";
            }
        }

        public static readonly Dictionary<int, DropRecord> PendingDrops = new Dictionary<int, DropRecord>();
        
        // Track Mrs. Stacks daily orders count (supports tier-based daily limits)
        public static readonly Dictionary<int, int> MrsStacksOrdersToday = new Dictionary<int, int>();

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                LoadPendingDrops();
                Logger.Msg("[JsonDataStore] ✅ Initialized and loaded.");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to initialize data store.");
                Logger.Exception(ex);
            }
        }

        public static void SaveDrop(int day, List<string> items, int hour, string meta = "manual")
        {
            try
            {
                if (PendingDrops.ContainsKey(day))
                {
                    Logger.Warn($"[JsonDataStore] ⚠️ Drop already scheduled for Day {day}, overwriting...");
                }

                var record = new DropRecord
                {
                    Day = day,
                    Items = items,
                    DropHour = hour,
                    DropTime = $"{hour:D2}:00",
                    Org = "PaxDrops",
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = meta,
                    Location = ""
                };

                PendingDrops[day] = record;
                SaveToFile();

                Logger.Msg($"[JsonDataStore] 💾 Drop saved for Day {day} with {items.Count} items");
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to save drop for Day {day}");
                Logger.Exception(ex);
            }
        }

        public static void RemoveDrop(int day)
        {
            try
            {
                if (PendingDrops.Remove(day))
                {
                    SaveToFile();
                    Logger.Msg($"[JsonDataStore] 🗑️ Removed drop for Day {day}");
                }
                else
                {
                    Logger.Warn($"[JsonDataStore] ⚠️ No drop found for Day {day}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to remove drop for Day {day}");
                Logger.Exception(ex);
            }
        }

        public static List<DropRecord> GetAllDrops()
        {
            try
            {
                var drops = new List<DropRecord>();
                foreach (var kvp in PendingDrops)
                {
                    drops.Add(kvp.Value);
                }
                return drops;
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to get all drops");
                Logger.Exception(ex);
                return new List<DropRecord>();
            }
        }

        private static void LoadPendingDrops()
        {
            try
            {
                if (!File.Exists(DropsFile))
                {
                    Logger.Msg("[JsonDataStore] 📁 No existing drops file found, starting fresh.");
                    return;
                }

                string json = File.ReadAllText(DropsFile);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Msg("[JsonDataStore] 📁 Empty drops file, starting fresh.");
                    return;
                }

                var drops = JsonConvert.DeserializeObject<List<DropRecord>>(json) ?? new List<DropRecord>();

                foreach (var drop in drops)
                {
                    PendingDrops[drop.Day] = drop;
                }

                Logger.Msg($"[JsonDataStore] 📂 Loaded {drops.Count} pending drops");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to load pending drops");
                Logger.Exception(ex);
            }
        }

        private static void SaveToFile()
        {
            try
            {
                var dropsList = new List<DropRecord>();
                foreach (var kvp in PendingDrops)
                {
                    dropsList.Add(kvp.Value);
                }

                string json = JsonConvert.SerializeObject(dropsList, Formatting.Indented);
                File.WriteAllText(DropsFile, json);

                Logger.Msg($"[JsonDataStore] 💾 Saved {dropsList.Count} drops to file");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to save to file");
                Logger.Exception(ex);
            }
        }

        public static void Shutdown()
        {
            try
            {
                SaveToFile();
                Logger.Msg("[JsonDataStore] 🔒 Data saved and shutdown complete.");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Error during shutdown.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Check if Mrs. Stacks has already received an order today (legacy method)
        /// </summary>
        public static bool HasMrsStacksOrderToday(int day)
        {
            return MrsStacksOrdersToday.ContainsKey(day) && MrsStacksOrdersToday[day] > 0;
        }

        /// <summary>
        /// Get the number of Mrs. Stacks orders for a specific day
        /// </summary>
        public static int GetMrsStacksOrdersToday(int day)
        {
            return MrsStacksOrdersToday.TryGetValue(day, out var count) ? count : 0;
        }

        /// <summary>
        /// Mark that Mrs. Stacks received an order today (increments counter)
        /// </summary>
        public static void MarkMrsStacksOrderToday(int day)
        {
            if (MrsStacksOrdersToday.ContainsKey(day))
            {
                MrsStacksOrdersToday[day]++;
            }
            else
            {
                MrsStacksOrdersToday[day] = 1;
            }
            Logger.Msg($"[JsonDataStore] 📝 Mrs. Stacks orders for day {day}: {MrsStacksOrdersToday[day]}");
        }

        /// <summary>
        /// Reset Mrs. Stacks orders for a specific day (for testing/debugging)
        /// </summary>
        public static void ResetMrsStacksOrdersToday(int day)
        {
            if (MrsStacksOrdersToday.ContainsKey(day))
            {
                MrsStacksOrdersToday.Remove(day);
                Logger.Msg($"[JsonDataStore] 🔄 Reset Mrs. Stacks orders for day {day}");
            }
        }

        /// <summary>
        /// Get summary of Mrs. Stacks order activity
        /// </summary>
        public static Dictionary<int, int> GetMrsStacksOrderSummary()
        {
            return new Dictionary<int, int>(MrsStacksOrdersToday);
        }
    }
} 