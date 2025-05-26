using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace PaxDrops
{
    /// <summary>
    /// Handles persistent storage of scheduled dead drops.
    /// Uses SQLite to read/write drop packets based on in-game days.
    /// </summary>
    public static class DataBase
    {
        // Absolute path to the PaxDrops SQLite database
        private static readonly string DbPath = Path.Combine("Mods", "PaxDrops", "drops.db");

        // Ensure directory exists before anything happens
        static DataBase()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath));
        }

        /// <summary>
        /// Initializes the SQLite database file if it doesn't exist.
        /// </summary>
        public static void Init()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
                using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
                {
                    conn.Open();

                    string table = "CREATE TABLE Drops (day INTEGER PRIMARY KEY, packet TEXT)";
                    using (var cmd = new SQLiteCommand(table, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                Logger.Msg($"[DataBase] 📁 Created new SQLite DB at {DbPath}");
            }
            else
            {
                Logger.Msg($"[DataBase] 🔗 Using existing SQLite DB at {DbPath}");
            }
        }

        /// <summary>
        /// Saves a drop packet (list of item IDs) for a specific in-game day.
        /// Overwrites any existing entry for the same day.
        /// </summary>
        /// <param name="day">The in-game day to store the drop for.</param>
        /// <param name="dropPacket">A list of item IDs.</param>
        public static void SaveDrop(int day, List<string> dropPacket)
        {
            string joined = string.Join(",", dropPacket);

            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();

                string query = "INSERT OR REPLACE INTO Drops (day, packet) VALUES (@day, @packet)";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@day", day);
                    cmd.Parameters.AddWithValue("@packet", joined);
                    cmd.ExecuteNonQuery();
                }
            }

            Logger.Msg($"[DataBase] 💾 Saved drop for Day {day}: {joined}");
        }

        /// <summary>
        /// Retrieves a drop packet for a specific in-game day.
        /// </summary>
        /// <param name="day">The in-game day to fetch the packet for.</param>
        /// <returns>List of item IDs if found; null otherwise.</returns>
        public static List<string> GetDrop(int day)
        {
            using (var conn = new SQLiteConnection($"Data Source={DbPath};Version=3;"))
            {
                conn.Open();

                string query = "SELECT packet FROM Drops WHERE day = @day";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@day", day);
                    object result = cmd.ExecuteScalar();

                    if (result == null)
                        return null;

                    return new List<string>(result.ToString().Split(','));
                }
            }
        }
    }
}
