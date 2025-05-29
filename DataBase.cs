using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using ScheduleOne.Persistence;
using S1API.GameTime;

namespace PaxDrops
{
    /// <summary>
    /// Manages persistent drop storage using SQLite, including drop verification and live cache.
    /// </summary>
    public static class DataBase
    {
        private const string DbDir = "Mods/PaxDrops/Data";
        private const string DbPath = "Mods/PaxDrops/Data/drops.db";
        private static SQLiteConnection _conn;

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
        }

        public static Dictionary<int, DropRecord> PendingDrops = new Dictionary<int, DropRecord>();

        public static void Init()
        {
            Directory.CreateDirectory(DbDir);
            bool createSchema = !File.Exists(DbPath);

            _conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            _conn.Open();

            if (createSchema)
                CreateSchema();

            Logger.Msg("[DataBase] ✅ Initialized SQLite DB");
            LoadAllPending();
        }

        private static void CreateSchema()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS drops (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    day INTEGER,
                    org TEXT,
                    createdTime TEXT,
                    dropTime TEXT,
                    hour INTEGER,
                    items TEXT,
                    type TEXT,
                    location TEXT
                );
            ";

            using (var cmd = new SQLiteCommand(sql, _conn))
                cmd.ExecuteNonQuery();

            Logger.Msg("[DataBase] 📁 Created initial DB schema.");
        }

        /// <summary>
        /// Loads all future-dated drops into memory cache.
        /// </summary>
        private static void LoadAllPending()
        {
            PendingDrops.Clear();
            int today = TimeManager.ElapsedDays;

            string sql = "SELECT day, org, createdTime, dropTime, hour, items, type, location FROM drops WHERE day >= @today";
            using (var cmd = new SQLiteCommand(sql, _conn))
            {
                cmd.Parameters.AddWithValue("@today", today);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var record = new DropRecord
                        {
                            Day = reader.GetInt32(0),
                            Org = reader.GetString(1),
                            CreatedTime = reader.GetString(2),
                            DropTime = reader.GetString(3),
                            DropHour = reader.GetInt32(4),
                            Items = new List<string>(reader.GetString(5).Split(',')),
                            Type = reader.GetString(6),
                            Location = reader.IsDBNull(7) ? "" : reader.GetString(7)
                        };

                        if (!PendingDrops.ContainsKey(record.Day))
                            PendingDrops[record.Day] = record;
                    }
                }
            }

            Logger.Msg($"[DataBase] 📤 Loaded {PendingDrops.Count} future drop(s) into cache.");
        }

        /// <summary>
        /// Persists a drop to disk and verifies it was committed.
        /// </summary>
        public static void SaveDrop(int day, List<string> items, int hour, string type = "manual", string location = "")
        {
            try
            {
                string org = LoadManager.Instance?.ActiveSaveInfo?.OrganisationName ?? "Unknown";
                string createdTime = DateTime.Now.ToString("s");
                string dropTime = TimeSpan.FromMinutes(hour).ToString(@"hh\:mm");
                string joinedItems = string.Join(",", items);

                string sql = @"
                    INSERT INTO drops (day, org, createdTime, dropTime, hour, items, type, location)
                    VALUES (@day, @org, @created, @drop, @hour, @items, @type, @location);
                ";

                using (var cmd = new SQLiteCommand(sql, _conn))
                {
                    cmd.Parameters.AddWithValue("@day", day);
                    cmd.Parameters.AddWithValue("@org", org);
                    cmd.Parameters.AddWithValue("@created", createdTime);
                    cmd.Parameters.AddWithValue("@drop", dropTime);
                    cmd.Parameters.AddWithValue("@hour", hour);
                    cmd.Parameters.AddWithValue("@items", joinedItems);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@location", location);
                    cmd.ExecuteNonQuery();
                }

                long lastId = (long)new SQLiteCommand("SELECT last_insert_rowid();", _conn).ExecuteScalar();
                Logger.Msg($"[DataBase] 💾 Drop saved: Day {day}, {joinedItems} @ {hour} ({type}) → #{lastId}");

                // Add to cache immediately
                PendingDrops[day] = new DropRecord
                {
                    Day = day,
                    Org = org,
                    CreatedTime = createdTime,
                    DropTime = dropTime,
                    DropHour = hour,
                    Items = items,
                    Type = type,
                    Location = location
                };
            }
            catch (Exception ex)
            {
                Logger.Error("[DataBase] ❌ Failed to save drop.");
                Logger.Exception(ex);
            }
        }

        public static void Shutdown()
        {
            _conn?.Close();
            Logger.Msg("[DataBase] 🔒 Closed SQLite connection.");
        }
    }
}
