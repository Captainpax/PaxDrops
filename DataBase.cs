using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.IO;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops
{
    /// <summary>
    /// Manages persistent storage of scheduled drops using a single Sqlite database.
    /// IL2CPP port using System.Data.Sqlite for CrossOver compatibility.
    /// </summary>
    public static class DataBase
    {
        private const string DbDir = "Mods/PaxDrops/Data";
        private const string DbPath = "Mods/PaxDrops/Data/drops.db";
        private static SqliteConnection? _conn;

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
            }
        }

        public static readonly Dictionary<int, DropRecord> PendingDrops = new Dictionary<int, DropRecord>();

        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(DbDir);
                bool fresh = !File.Exists(DbPath);

                _conn = new SqliteConnection($"Data Source={DbPath}");
                _conn.Open();
                
                Logger.Msg($"[DataBase] 🔗 Database connection opened. Fresh: {fresh}");

                if (fresh) CreateSchema();
                LoadPendingDrops();
                Logger.Msg("[DataBase] ✅ Initialized and loaded.");
            }
            catch (Exception ex)
            {
                Logger.Error("[DataBase] ❌ Failed to initialize database.");
                Logger.Exception(ex);
            }
        }

        private static void CreateSchema()
        {
            try
            {
                string sql = @"
                    CREATE TABLE IF NOT EXISTS drops (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        org TEXT,
                        createdTime TEXT,
                        dropTime TEXT,
                        hour INTEGER,
                        items TEXT,
                        meta TEXT
                    );";

                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }

                Logger.Msg("[DataBase] 📁 Schema created.");
            }
            catch (Exception ex)
            {
                Logger.Error("[DataBase] ❌ Failed to create schema.");
                Logger.Exception(ex);
            }
        }

        public static void SaveDrop(int day, List<string> items, int hour, string meta = "manual")
        {
            try
            {
                if (_conn == null)
                {
                    Logger.Error("[DataBase] ❌ Database connection is null. Cannot save drop.");
                    return;
                }

                if (items == null)
                {
                    Logger.Error("[DataBase] ❌ Items list is null. Cannot save drop.");
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
                    Logger.Warn($"[DataBase] ⚠️ Could not get organization name: {ex.Message}");
                }

                string itemList = string.Join(",", items);

                string sql = "INSERT INTO drops (org, createdTime, dropTime, hour, items, meta) VALUES (@org, @created, @drop, @hour, @items, @meta);";
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.Parameters.AddWithValue("@org", org);
                    cmd.Parameters.AddWithValue("@created", now);
                    cmd.Parameters.AddWithValue("@drop", dropTime);
                    cmd.Parameters.AddWithValue("@hour", hour);
                    cmd.Parameters.AddWithValue("@items", itemList);
                    cmd.Parameters.AddWithValue("@meta", meta);
                    cmd.ExecuteNonQuery();
                }

                Logger.Msg($"[DataBase] 💾 Drop saved for day {day} @ {hour} with {items.Count} items ({meta})");

                var record = new DropRecord
                {
                    Day = day,
                    Items = items,
                    DropHour = hour,
                    DropTime = dropTime,
                    Org = org,
                    CreatedTime = now,
                    Type = meta,
                    Location = ""
                };

                PendingDrops[day] = record;
            }
            catch (Exception ex)
            {
                Logger.Error("[DataBase] ❌ Failed to save drop.");
                Logger.Exception(ex);
            }
        }

        private static void LoadPendingDrops()
        {
            try
            {
                // Get current day for context
                int today = 1;
                try
                {
                    var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
                    if (timeManager != null)
                    {
                        today = timeManager.ElapsedDays;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[DataBase] ⚠️ Could not get current day: {ex.Message}");
                }

                string sql = "SELECT * FROM drops;";
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int day = today; // fallback
                            int.TryParse(reader["hour"]?.ToString(), out int hour);
                            string dropTime = reader["dropTime"]?.ToString();
                            string itemsRaw = reader["items"]?.ToString();
                            string org = reader["org"]?.ToString();
                            string created = reader["createdTime"]?.ToString();
                            string meta = reader["meta"]?.ToString();

                            if (string.IsNullOrWhiteSpace(itemsRaw)) continue;

                            var record = new DropRecord
                            {
                                Day = today,
                                DropHour = hour,
                                DropTime = dropTime,
                                Items = new List<string>(itemsRaw.Split(',')),
                                Org = org,
                                CreatedTime = created,
                                Type = meta,
                                Location = ""
                            };

                            PendingDrops[day] = record;
                        }
                    }
                }

                Logger.Msg($"[DataBase] 📦 Loaded {PendingDrops.Count} pending drops.");
            }
            catch (Exception ex)
            {
                Logger.Error("[DataBase] ❌ Failed to load pending drops.");
                Logger.Exception(ex);
            }
        }

        public static void Shutdown()
        {
            try
            {
                _conn?.Close();
                Logger.Msg("[DataBase] 🔒 DB connection closed.");
            }
            catch (Exception ex)
            {
                Logger.Error("[DataBase] ❌ Error during shutdown.");
                Logger.Exception(ex);
            }
        }
    }
} 