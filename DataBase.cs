using System.Collections.Generic;

namespace PaxDrops
{
    /// <summary>
    /// Manages scheduled drop persistence.
    /// Currently, it uses in-memory storage; a future version will use SQLite or MySQL.
    /// </summary>
    public static class DataBase
    {
        /// <summary>
        /// Tracks scheduled drops keyed by in-game day.
        /// </summary>
        private static readonly Dictionary<int, List<string>> ScheduledDrops = new Dictionary<int, List<string>>();

        /// <summary>
        /// Initializes the database layer.
        /// This is currently a stub for in-memory persistence.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[DataBase] Stub initialized (SQLite backend planned).");
        }

        /// <summary>
        /// Stores a drop packet for a specific day.
        /// Overwrites any previous entry for the same day.
        /// </summary>
        /// <param name="day">In-game day to schedule the drop for</param>
        /// <param name="dropPacket">List of item IDs to spawn</param>
        public static void SaveDrop(int day, List<string> dropPacket)
        {
            ScheduledDrops[day] = dropPacket;
            Logger.Msg(string.Format("[DataBase] 📦 Drop scheduled for Day {0}: {1}", day, string.Join(", ", dropPacket)));
        }

        /// <summary>
        /// Retrieves the drop packet for a specific day if one exists.
        /// </summary>
        /// <param name="day">In-game day to check</param>
        /// <returns>The drop packet list, or null if not found</returns>
        public static List<string> GetDrop(int day)
        {
            List<string> packet;
            return ScheduledDrops.TryGetValue(day, out packet) ? packet : null;
        }
    }
}