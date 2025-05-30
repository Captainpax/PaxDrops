using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using ScheduleOne.Persistence;

namespace PaxDrops
{
    /// <summary>
    /// Manages persistent storage of scheduled drops using a single SQLite database.
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

        public static readonly Dictionary<int, DropRecord> PendingDrops = new Dictionary<int, DropRecord>();

        public static void Init()
        {
            Directory.CreateDirectory(DbDir);
            bool fresh = !File.Exists(DbPath);

            _conn = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            _conn.Open();

            if (fresh) CreateSchema();
            LoadPendingDrops();
            Logger.Msg("[DataBase] ✅ Initialized and loaded.");
        }

        private static void CreateSchema()
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

            using (var cmd = new SQLiteCommand(sql, _conn))
                cmd.ExecuteNonQuery();

            Logger.Msg("[DataBase] 📁 Schema created.");
        }

        public static void SaveDrop(int day, List<string> items, int hour, string meta = "manual")
        {
            try
            {
                string now = DateTime.UtcNow.ToString("s");
                string dropTime = TimeSpan.FromMinutes(hour).ToString(@"hh\:mm");
                string org = LoadManager.Instance?.ActiveSaveInfo?.OrganisationName ?? "Unknown";
                string itemList = string.Join(",", items);

                string sql = "INSERT INTO drops (org, createdTime, dropTime, hour, items, meta) VALUES (@org, @created, @drop, @hour, @items, @meta);";
                using (var cmd = new SQLiteCommand(sql, _conn))
                {
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
                int today = S1API.GameTime.TimeManager.ElapsedDays;

                string sql = "SELECT * FROM drops;";
                using (var cmd = new SQLiteCommand(sql, _conn))
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
            _conn?.Close();
            Logger.Msg("[DataBase] 🔒 DB connection closed.");
        }
    }
}
