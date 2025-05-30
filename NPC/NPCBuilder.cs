using S1API.GameTime;

namespace PaxDrops.NPC
{
    /// <summary>
    /// Handles timed message delivery from Mrs. Stacks each in-game morning.
    /// </summary>
    public static class MrStacksMSG
    {
        private static bool _hasSentToday = false;
        private static int _lastSentDay = -1;

        /// <summary>
        /// Checks and sends the message at 7:30 AM in-game time.
        /// </summary>
        public static void CheckAndSend(int hour, int minute)
        {
            int currentDay = TimeManager.ElapsedDays;

            if (_hasSentToday && _lastSentDay == currentDay)
                return;

            if (hour == 7 && minute == 30)
            {
                if (MrStacks.TrySendMorningMessage())
                {
                    _hasSentToday = true;
                    _lastSentDay = currentDay;
                    Logger.Msg("[MrStacksMSG] ✉️ Morning message sent.");
                }
                else
                {
                    Logger.Warn("[MrStacksMSG] ⚠️ Couldn't send message — NPC not ready.");
                }
            }
        }

        public static void ResetDailyFlag()
        {
            _hasSentToday = false;
            Logger.Msg("[MrStacksMSG] 🧼 Daily flag reset.");
        }
    }
}