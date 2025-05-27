using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace PaxDrops
{
    /// <summary>
    /// Handles SQLite persistence of scheduled dead drops (day, items, time, and location).
    /// </summary>
    public static class DataBase
    {
        private static readonly string DbPath = Path.Combine("Mods", "PaxDrops", "drops.db");

        static DataBase()
        {
            string dir = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Initializes the database and creates schema if not already present.
        /// </summary>
        public static void Init()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
                using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
                {
                    conn.Open();
                    string table = @"CREATE TABLE Drops (
                        day INTEGER PRIMARY KEY, 
                        packet TEXT, 
                        hour INTEGER, 
                        type TEXT,
                        location TEXT
                    )";
                    using (var cmd = new SQLiteCommand(table, conn))
                        cmd.ExecuteNonQuery();
                }

                Logger.Msg($"[DataBase] 📁 Created SQLite DB: {DbPath}");
            }
            else
            {
                Logger.Msg($"[DataBase] 🔗 Using existing DB at: {DbPath}");
            }
        }

        /// <summary>
        /// Stores a scheduled drop entry.
        /// </summary>
        public static void SaveDrop(int day, List<string> dropPacket, int hour = 700, string type = "random", string location = "")
        {
            string joined = string.Join(",", dropPacket);

            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                string query = @"INSERT OR REPLACE INTO Drops 
                                (day, packet, hour, type, location) 
                                VALUES (@day, @packet, @hour, @type, @location)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@day", day);
                    cmd.Parameters.AddWithValue("@packet", joined);
                    cmd.Parameters.AddWithValue("@hour", hour);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.Parameters.AddWithValue("@location", location);
                    cmd.ExecuteNonQuery();
                }
            }

            Logger.Msg($"[DataBase] 💾 Saved drop ➤ Day {day} | {type} @ {hour} → {location} :: {joined}");
        }

        /// <summary>
        /// Retrieves a scheduled drop entry, if any.
        /// </summary>
        public static bool GetDrop(int day, out List<string> packet, out int hour, out string type, out string location)
        {
            packet = new List<string>();
            hour = 0;
            type = "";
            location = "";

            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT packet, hour, type, location FROM Drops WHERE day = @day";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@day", day);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return false;

                        string raw = reader.GetString(0);
                        packet = new List<string>(raw.Split(','));
                        hour = reader.GetInt32(1);
                        type = reader.GetString(2);
                        location = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        return true;
                    }
                }
            }
        }
    }
}
