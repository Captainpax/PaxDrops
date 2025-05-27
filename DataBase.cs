using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace PaxDrops
{
    /// <summary>
    /// Handles persistent storage of scheduled dead drops.
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

        public static void Init()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
                using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
                {
                    conn.Open();
                    string table = "CREATE TABLE Drops (day INTEGER PRIMARY KEY, packet TEXT, hour INTEGER, type TEXT)";
                    using (var cmd = new SQLiteCommand(table, conn))
                        cmd.ExecuteNonQuery();
                }

                Logger.Msg($"[DataBase] 📁 Created new SQLite DB at {DbPath}");
            }
            else
            {
                Logger.Msg($"[DataBase] 🔗 Using existing SQLite DB at {DbPath}");
            }
        }

        public static void SaveDrop(int day, List<string> dropPacket, int hour = 700, string type = "random")
        {
            string joined = string.Join(",", dropPacket);
            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                string query = "INSERT OR REPLACE INTO Drops (day, packet, hour, type) VALUES (@day, @packet, @hour, @type)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@day", day);
                    cmd.Parameters.AddWithValue("@packet", joined);
                    cmd.Parameters.AddWithValue("@hour", hour);
                    cmd.Parameters.AddWithValue("@type", type);
                    cmd.ExecuteNonQuery();
                }
            }

            Logger.Msg($"[DataBase] 💾 Saved drop for Day {day} ({type} @ {hour}): {joined}");
        }

        public static bool GetDrop(int day, out List<string> packet, out int hour, out string type)
        {
            packet = new List<string>();
            hour = 0;
            type = "";

            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();
                string query = "SELECT packet, hour, type FROM Drops WHERE day = @day";
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
                        return true;
                    }
                }
            }
        }
    }
}
