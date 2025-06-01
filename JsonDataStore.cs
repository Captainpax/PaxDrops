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
        
        // Track Mrs. Stacks daily orders to prevent multiple orders per day
        public static readonly Dictionary<int, bool> MrsStacksOrdersToday = new Dictionary<int, bool>();

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
                if (items == null)
                {
                    Logger.Error("[JsonDataStore] ❌ Items list is null. Cannot save drop.");
                    return;
                }

                string now = DateTime.UtcNow.ToString("s");
                string dropTime = TimeSpan.FromMinutes(hour).ToString(@"hh\:mm");
                
                // Get organization name from the load manager
                string org = "Unknown";
                try
                {
                    var loadManager = Il2CppScheduleOne.Persistence.LoadManager.Instance;
                    if (loadManager?.ActiveSaveInfo != null)
                    {
                        org = loadManager.ActiveSaveInfo.OrganisationName ?? "Unknown";
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[JsonDataStore] ⚠️ Could not get organization name: {ex.Message}");
                }

                var record = new DropRecord
                {
                    Day = day,
                    Items = new List<string>(items),
                    DropHour = hour,
                    DropTime = dropTime,
                    Org = org,
                    CreatedTime = now,
                    Type = meta,
                    Location = ""
                };

                PendingDrops[day] = record;
                SaveToFile();

                Logger.Msg($"[JsonDataStore] 💾 Drop saved for day {day} @ {hour} with {items.Count} items ({meta})");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to save drop.");
                Logger.Exception(ex);
            }
        }

        private static void SaveToFile()
        {
            try
            {
                var json = JsonConvert.SerializeObject(PendingDrops, Formatting.Indented);
                File.WriteAllText(DropsFile, json);
                Logger.Msg($"[JsonDataStore] 📁 Saved {PendingDrops.Count} pending drops to file.");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to save to file.");
                Logger.Exception(ex);
            }
        }

        private static void LoadPendingDrops()
        {
            try
            {
                if (!File.Exists(DropsFile))
                {
                    Logger.Msg("[JsonDataStore] 📂 No existing drops file found. Starting fresh.");
                    return;
                }

                var json = File.ReadAllText(DropsFile);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Msg("[JsonDataStore] 📂 Empty drops file. Starting fresh.");
                    return;
                }

                var loadedDrops = JsonConvert.DeserializeObject<Dictionary<int, DropRecord>>(json);
                if (loadedDrops != null)
                {
                    PendingDrops.Clear();
                    foreach (var kvp in loadedDrops)
                    {
                        PendingDrops[kvp.Key] = kvp.Value;
                    }
                }

                Logger.Msg($"[JsonDataStore] 📦 Loaded {PendingDrops.Count} pending drops from file.");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to load pending drops.");
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
        /// Check if Mrs. Stacks has already received an order today
        /// </summary>
        public static bool HasMrsStacksOrderToday(int day)
        {
            return MrsStacksOrdersToday.ContainsKey(day);
        }

        /// <summary>
        /// Mark that Mrs. Stacks received an order today
        /// </summary>
        public static void MarkMrsStacksOrderToday(int day)
        {
            MrsStacksOrdersToday[day] = true;
        }
    }
} 