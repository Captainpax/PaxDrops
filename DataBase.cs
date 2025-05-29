using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;

namespace PaxDrops
{
    /// <summary>
    /// Handles SQLite persistence of scheduled dead drops (day, items, time, location, org).
    /// </summary>
    public static class DataBase
    {
        private static readonly string DbPath = Path.Combine("Mods", "PaxDrops", "drops.db");
        private static readonly string ConnStr = $"Data Source={DbPath};Version=3;";

        public static readonly Dictionary<int, DropRecord> PendingDrops = new Dictionary<int, DropRecord>();

        public class DropRecord
        {
            public int Day;
            public List<string> Items;
            public int DropHour;
            public string DropTime;
            public string Org;
            public string Type;
            public string Location;
            public string CreatedTime;
        }

        static DataBase()
        {
            string dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Initializes the database and loads any pending drops.
        /// </summary>
        public static void Init()
        {
            bool newDb = !File.Exists(DbPath);
            if (newDb)
            {
                SQLiteConnection.CreateFile(DbPath);
                Logger.Msg($"[DataBase] 📁 Created SQLite DB: {DbPath}");
            }
            else
            {
                Logger.Msg($"[DataBase] 🔗 Using existing DB at: {DbPath}");
            }

            try
            {
                using (var conn = new SQLiteConnection(ConnStr))
                {
                    conn.Open();
                    string createTable = @"CREATE TABLE IF NOT EXISTS Drops (
                        day INTEGER PRIMARY KEY,
                        packet TEXT,
                        hour INTEGER,
                        dropTime TEXT,
                        createdTime TEXT,
                        type TEXT,
                        location TEXT,
                        org TEXT
                    );";
                    using (var cmd = new SQLiteCommand(createTable, conn))
                        cmd.ExecuteNonQuery();
                }

                LoadPendingDrops();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Loads all saved drops into memory.
        /// </summary>
        private static void LoadPendingDrops()
        {
            try
            {
                using (var conn = new SQLiteConnection(ConnStr))
                {
                    conn.Open();
                    string query = "SELECT * FROM Drops";
                    using (var cmd = new SQLiteCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int day = reader.GetInt32(0);
                            string raw = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            int hour = reader.IsDBNull(2) ? 700 : reader.GetInt32(2);
                            string dropTime = reader.IsDBNull(3) ? "07:00" : reader.GetString(3);
                            string createdTime = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            string type = reader.IsDBNull(5) ? "random" : reader.GetString(5);
                            string location = reader.IsDBNull(6) ? "" : reader.GetString(6);
                            string org = reader.IsDBNull(7) ? "" : reader.GetString(7);

                            var items = new List<string>();
                            if (!string.IsNullOrWhiteSpace(raw))
                            {
                                foreach (var entry in raw.Split(','))
                                {
                                    var trimmed = entry.Trim();
                                    if (!string.IsNullOrEmpty(trimmed))
                                        items.Add(trimmed);
                                }
                            }

                            PendingDrops[day] = new DropRecord
                            {
                                Day = day,
                                Items = items,
                                DropHour = hour,
                                DropTime = dropTime,
                                CreatedTime = createdTime,
                                Type = type,
                                Location = location,
                                Org = org
                            };
                        }
                    }
                }

                Logger.Msg($"[DataBase] 📦 Loaded {PendingDrops.Count} scheduled drop(s) from DB.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        public static void Shutdown()
        {
            Logger.Msg("[DataBase] 🔌 Shutdown complete.");
        }

        public static void SaveDrop(int day, List<string> dropPacket, int hour = 700, string type = "random", string location = "", string org = "unknown")
        {
            if (dropPacket == null || dropPacket.Count == 0)
            {
                Logger.Warn("[DataBase] ⚠️ Tried to save empty drop packet.");
                return;
            }

            try
            {
                string joined = string.Join(",", dropPacket);
                string createdTime = DateTime.Now.ToString("s", CultureInfo.InvariantCulture);
                string dropTime = TimeSpan.FromMinutes(hour).ToString(@"hh\:mm");

                using (var conn = new SQLiteConnection(ConnStr))
                {
                    conn.Open();
                    string query = @"INSERT OR REPLACE INTO Drops 
                        (day, packet, hour, dropTime, createdTime, type, location, org) 
                        VALUES (@day, @packet, @hour, @dropTime, @createdTime, @type, @location, @org)";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@day", day);
                        cmd.Parameters.AddWithValue("@packet", joined);
                        cmd.Parameters.AddWithValue("@hour", hour);
                        cmd.Parameters.AddWithValue("@dropTime", dropTime);
                        cmd.Parameters.AddWithValue("@createdTime", createdTime);
                        cmd.Parameters.AddWithValue("@type", type);
                        cmd.Parameters.AddWithValue("@location", location);
                        cmd.Parameters.AddWithValue("@org", org);
                        cmd.ExecuteNonQuery();
                    }
                }

                Logger.Msg($"[DataBase] 💾 Saved drop ➤ Day {day} | {type} @ {dropTime} for {org} → {location} :: {joined}");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        public static bool GetDrop(int day, out List<string> packet, out int hour, out string type, out string location)
        {
            packet = new List<string>();
            hour = 0;
            type = "";
            location = "";

            try
            {
                if (!PendingDrops.ContainsKey(day))
                    return false;

                var drop = PendingDrops[day];
                packet = new List<string>(drop.Items);
                hour = drop.DropHour;
                type = drop.Type;
                location = drop.Location;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                return false;
            }
        }
    }
}
