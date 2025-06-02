using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;
using MelonLoader;

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

        /// <summary>
        /// Dictionary to track pending drops by day (supports multiple drops per day)
        /// Key: Day number, Value: List of drop records for that day
        /// </summary>
        public static readonly Dictionary<int, List<DropRecord>> PendingDrops = new Dictionary<int, List<DropRecord>>();
        
        // Track Mrs. Stacks daily orders count per order day (not delivery day)
        public static readonly Dictionary<int, int> MrsStacksOrdersToday = new Dictionary<int, int>();
        
        // Track the last day when player ordered from Mrs. Stacks
        private static int _lastMrsStacksOrderDay = -1;

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                LoadFromFile();
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

                if (!PendingDrops.ContainsKey(day))
                {
                    PendingDrops[day] = new List<DropRecord>();
                }
                PendingDrops[day].Add(record);
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
                if (!PendingDrops.ContainsKey(record.Day))
                {
                    PendingDrops[record.Day] = new List<DropRecord>();
                }
                PendingDrops[record.Day].Add(record);
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
                if (PendingDrops.ContainsKey(day))
                {
                    PendingDrops[day].Clear();
                    SaveToFile();
                    Logger.Msg($"[JsonDataStore] 🗑️ Removed all drops for Day {day}");
                }
                else
                {
                    Logger.Warn($"[JsonDataStore] ⚠️ No drops found for Day {day}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to remove drops for Day {day}");
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
                    drops.AddRange(kvp.Value);
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
                if (PendingDrops.ContainsKey(day))
                {
                    foreach (var drop in PendingDrops[day])
                    {
                        drop.IsCollected = true;
                    }
                    SaveToFile();
                    Logger.Msg($"[JsonDataStore] ✅ All drops for Day {day} marked as collected");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to mark drops collected for Day {day}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Load pending drops from JSON file
        /// </summary>
        private static void LoadFromFile()
        {
            try
            {
                if (!File.Exists(DropsFile))
                {
                    Logger.Msg("[JsonDataStore] 📁 No existing file found, starting fresh.");
                    return;
                }

                string json = File.ReadAllText(DropsFile);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Logger.Msg("[JsonDataStore] 📁 Empty file, starting fresh.");
                    return;
                }

                // Load the standard format: Dictionary<int, List<DropRecord>>
                var loadedData = JsonConvert.DeserializeObject<Dictionary<int, List<DropRecord>>>(json);
                if (loadedData != null)
                {
                    PendingDrops.Clear();
                    foreach (var kvp in loadedData)
                    {
                        PendingDrops[kvp.Key] = kvp.Value ?? new List<DropRecord>();
                        
                        // Ensure all fields are properly initialized
                        foreach (var drop in PendingDrops[kvp.Key])
                        {
                            if (drop.InitialItemCount == 0) 
                                drop.InitialItemCount = drop.Items?.Count ?? 0;
                            if (drop.OrderDay == 0) 
                                drop.OrderDay = drop.Day; // Fallback for legacy records
                        }
                    }
                    
                    int totalDrops = PendingDrops.Values.Sum(list => list.Count);
                    Logger.Msg($"[JsonDataStore] 📂 Loaded {totalDrops} pending drops from {PendingDrops.Count} days");
                    return;
                }

                // If loading failed, the file is in an incompatible format
                Logger.Warn("[JsonDataStore] ⚠️ File format incompatible with current version");
                BackupIncompatibleFile();
                Logger.Msg("[JsonDataStore] 📁 Starting fresh after backing up incompatible file");
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to load data: {ex.Message}");
                BackupIncompatibleFile();
                Logger.Msg("[JsonDataStore] 📁 Starting fresh after backup");
            }
        }

        /// <summary>
        /// Save pending drops to JSON file in standard format
        /// </summary>
        private static void SaveToFile()
        {
            try
            {
                // Always save in the standard format: Dictionary<int, List<DropRecord>>
                string json = JsonConvert.SerializeObject(PendingDrops, Formatting.Indented);
                File.WriteAllText(DropsFile, json);

                int totalDrops = PendingDrops.Values.Sum(list => list.Count);
                Logger.Msg($"[JsonDataStore] 💾 Saved {totalDrops} drops to file ({PendingDrops.Count} days)");
            }
            catch (Exception ex)
            {
                Logger.Error($"[JsonDataStore] ❌ Failed to save to file: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Backup incompatible file before starting fresh
        /// </summary>
        private static void BackupIncompatibleFile()
        {
            try
            {
                if (File.Exists(DropsFile))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string backupPath = $"{DropsFile}.backup_{timestamp}";
                    File.Copy(DropsFile, backupPath);
                    Logger.Msg($"[JsonDataStore] 📋 Backed up incompatible file to: {backupPath}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[JsonDataStore] ⚠️ Failed to backup file: {ex.Message}");
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