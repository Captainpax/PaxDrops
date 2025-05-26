using S1API.GameTime;      // Provides access to in-game time (day, hour, etc.)
using S1API.Messaging;     // Used for Response objects sent via the phone
using S1API.Entities;      // Gives access to NPCs and their messaging systems

namespace PaxDrops
{
    /// <summary>
    /// Handles the logic related to Mr. Stacks — your NPC contact for ordering drops.
    /// Registers message options and schedules drop packets using phone interactions.
    /// </summary>
    public static class MrStacks
    {
        // Cached reference to the NPC contact
        private static NPC _npc;

        // Tracks whether the initial drop offer message has been sent this session
        private static bool _hasSentIntro;

        /// <summary>
        /// Entry point called during PaxDrops init. Registers message callbacks and phone logic.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrStacks] Initialized.");
            SetupContact();

            // Hook into daily progression to auto-send messages
            TimeManager.OnDayPass += TrySendIntroMessage;
        }

        /// <summary>
        /// Looks for the NPC contact. If none exists, creates a fallback instance.
        /// </summary>
        private static void SetupContact()
        {
            // Try to find a contact with ID 'MrStacks' in all loaded NPCs
            _npc = NPC.All.Find(n => n.ID == "MrStacks");

            // If not found, spawn a runtime fallback contact
            if (_npc == null)
            {
                _npc = new MrStacksContact("MrStacks", "Mr.", "Stacks");
                Logger.Msg("[MrStacks] Created fallback NPC contact.");
            }
        }

        /// <summary>
        /// Triggers the message + drop request option during morning hours.
        /// Only runs once per scene load.
        /// </summary>
        private static void TrySendIntroMessage()
        {
            // Only allow once
            if (_hasSentIntro || _npc == null)
                return;

            // Current in-game time (24h)
            int hour = TimeManager.CurrentTime;

            // Limit a message to a daytime window (7AM–7PM)
            if (hour < 700 || hour >= 1900)
            {
                Logger.Msg($"[MrStacks] ⏰ Skipped intro message — hour {hour} is outside 7AM–7PM window.");
                return;
            }

            _hasSentIntro = true;

            // Define a phone response option
            var orderDrop = new Response
            {
                Label = "ORDER_DROP",
                Text = "Can I get a drop?",
                OnTriggered = delegate
                {
                    int tomorrow = TimeManager.ElapsedDays + 1;
                    var packet = TierLevel.GetDropPacket(tomorrow);
                    DataBase.SaveDrop(tomorrow, packet);

                    _npc.SendTextMessage("I'll send you a drop tomorrow morning.");
                    Logger.Msg($"[MrStacks] ✅ Drop scheduled for Day {tomorrow}.");
                }
            };

            // Send the initial "Need something?" message + drop option
            _npc.SendTextMessage("Need something?", new[] { orderDrop });
        }

        /// <summary>
        /// Debug trigger: forcibly re-sends the intro message.
        /// Useful for testing phone behavior (called from PageUp, etc.)
        /// </summary>
        public static void DebugTrigger()
        {
            _hasSentIntro = false;
            TrySendIntroMessage();
        }

        /// <summary>
        /// A fallback NPC definition used if no base game contact with ID 'MrStacks' exists.
        /// </summary>
        private class MrStacksContact : NPC
        {
            public MrStacksContact(string id, string first, string last)
                : base(id, first, last) { }
        }
    }
}
