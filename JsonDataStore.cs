using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;
using System.Linq;

namespace PaxDrops
{
    /// <summary>
    /// Manages persistent storage of scheduled drops using JSON files.
    /// IL2CPP port using simple JSON serialization for maximum compatibility.
    /// Enhanced with tier-based daily order tracking and collection detection.
    /// </summary>
    public static class JsonDataStore
    {
        private const string DataDir = "Mods/PaxDrops/Data";
        private const string DropsFile = "Mods/PaxDrops/Data/drops.json";
        private const string OrdersFile = "Mods/PaxDrops/Data/orders.json";

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
            public string ExpiryTime;
            public int OrderDay; // Day when order was placed
            public bool IsCollected; // Whether the drop has been collected
            public int InitialItemCount; // Initial item count for collection detection

            public DropRecord()
            {
                Items = new List<string>();
                DropTime = "";
                Org = "";
                CreatedTime = "";
                Type = "";
                Location = "";
                ExpiryTime = "";
                OrderDay = 0;
                IsCollected = false;
                InitialItemCount = 0;
            }
        }

        public static readonly Dictionary<int, DropRecord> PendingDrops = new Dictionary<int, DropRecord>();
        
        // Track Mrs. Stacks daily orders count per order day (not delivery day)
        public static readonly Dictionary<int, int> MrsStacksOrdersToday = new Dictionary<int, int>();
        
        // Track the last day when player ordered from Mrs. Stacks
        private static int _lastMrsStacksOrderDay = -1;

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                LoadPendingDrops();
                LoadDailyOrders();
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
                    Location = "",
                    ExpiryTime = "",
                    OrderDay = day, // For legacy records
                    IsCollected = false,
                    InitialItemCount = items.Count
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

        /// <summary>
        /// Save a complete drop record (used by OrderProcessor)
        /// </summary>
        public static void SaveDropRecord(DropRecord record)
        {
            try
            {
                PendingDrops[record.Day] = record;
                SaveToFile();
                Logger.Msg($"[JsonDataStore] 💾 Drop record saved for Day {record.Day}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to save drop record for Day {record.Day}");
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

        /// <summary>
        /// Mark a drop as collected
        /// </summary>
        public static void MarkDropCollected(int day)
        {
            try
            {
                if (PendingDrops.TryGetValue(day, out var drop))
                {
                    drop.IsCollected = true;
                    SaveToFile();
                    Logger.Msg($"[JsonDataStore] ✅ Drop for Day {day} marked as collected");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to mark drop collected for Day {day}");
                Logger.Exception(ex);
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
                    // Ensure new fields have default values for old records
                    if (drop.OrderDay == 0) drop.OrderDay = drop.Day;
                    if (drop.InitialItemCount == 0) drop.InitialItemCount = drop.Items?.Count ?? 0;
                    
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

        /// <summary>
        /// Load daily order counts from file
        /// </summary>
        private static void LoadDailyOrders()
        {
            try
            {
                if (!File.Exists(OrdersFile))
                {
                    Logger.Msg("[JsonDataStore] 📁 No existing orders file found, starting fresh.");
                    return;
                }

                string json = File.ReadAllText(OrdersFile);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Msg("[JsonDataStore] 📁 Empty orders file, starting fresh.");
                    return;
                }

                // Try to load as enhanced format first, fall back to simple format
                try
                {
                    var enhanced = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (enhanced != null && enhanced.ContainsKey("orders"))
                    {
                        var ordersJson = enhanced["orders"]?.ToString();
                        if (!string.IsNullOrEmpty(ordersJson))
                        {
                            var orders = JsonConvert.DeserializeObject<Dictionary<int, int>>(ordersJson) ?? new Dictionary<int, int>();
                            
                            foreach (var kvp in orders)
                            {
                                MrsStacksOrdersToday[kvp.Key] = kvp.Value;
                            }

                            // Load last order day if available
                            if (enhanced.ContainsKey("lastOrderDay") && int.TryParse(enhanced["lastOrderDay"].ToString(), out int lastDay))
                            {
                                _lastMrsStacksOrderDay = lastDay;
                            }

                            Logger.Msg($"[JsonDataStore] 📂 Loaded daily orders for {orders.Count} days (last order: day {_lastMrsStacksOrderDay})");
                            return;
                        }
                    }
                }
                catch 
                {
                    // Fall back to legacy format
                }

                // Legacy format - just the dictionary
                var legacyOrders = JsonConvert.DeserializeObject<Dictionary<int, int>>(json) ?? new Dictionary<int, int>();
                foreach (var kvp in legacyOrders)
                {
                    MrsStacksOrdersToday[kvp.Key] = kvp.Value;
                }

                // Set last order day to the highest day with orders (best guess)
                if (legacyOrders.Count > 0)
                {
                    _lastMrsStacksOrderDay = legacyOrders.Keys.Max();
                }

                Logger.Msg($"[JsonDataStore] 📂 Loaded legacy orders for {legacyOrders.Count} days");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to load daily orders");
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

        /// <summary>
        /// Save daily order counts to file
        /// </summary>
        private static void SaveDailyOrders()
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["orders"] = new Dictionary<int, int>(MrsStacksOrdersToday),
                    ["lastOrderDay"] = _lastMrsStacksOrderDay
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(OrdersFile, json);
                Logger.Msg($"[JsonDataStore] 💾 Saved daily orders ({MrsStacksOrdersToday.Count} days, last order: day {_lastMrsStacksOrderDay})");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Failed to save daily orders");
                Logger.Exception(ex);
            }
        }

        public static void Shutdown()
        {
            try
            {
                SaveToFile();
                SaveDailyOrders();
                Logger.Msg("[JsonDataStore] 🔒 Data saved and shutdown complete.");
            }
            catch (Exception ex)
            {
                Logger.Error("[JsonDataStore] ❌ Error during shutdown.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Check if Mrs. Stacks has already received an order on a specific day (legacy method)
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
            
            // Update last order day
            _lastMrsStacksOrderDay = day;
            
            SaveDailyOrders(); // Save immediately for persistence
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
                SaveDailyOrders();
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

        /// <summary>
        /// Get the last day when player ordered from Mrs. Stacks (-1 if never)
        /// </summary>
        public static int GetLastMrsStacksOrderDay()
        {
            return _lastMrsStacksOrderDay;
        }

        /// <summary>
        /// Get days since last Mrs. Stacks order (-1 if never ordered)
        /// </summary>
        public static int GetDaysSinceLastMrsStacksOrder(int currentDay)
        {
            if (_lastMrsStacksOrderDay == -1) return -1; // Never ordered
            return currentDay - _lastMrsStacksOrderDay;
        }
    }
} 