using S1API.GameTime;
using S1API.Messaging;
using S1API.Entities;
using System;

namespace PaxDrops
{
    /// <summary>
    /// Handles the logic related to Mr. Stacks — your NPC contact for ordering drops.
    /// Registers message options and schedules drop packets using phone interactions.
    /// </summary>
    public static class MrStacks
    {
        private static NPC _npc;
        private static bool _hasSentIntro;
        private static int _lastIntroDay;

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
            _npc = NPC.All.Find(n => n.ID == "MrStacks");

            if (_npc == null)
            {
                _npc = new MrStacksContact("MrStacks", "Mr.", "Stacks");

                // Manually register if not already in the list
                if (!NPC.All.Contains(_npc))
                    NPC.All.Add(_npc);

                Logger.Msg("[MrStacks] 🧍 Created fallback NPC contact.");
            }
        }

        /// <summary>
        /// Triggers the message + drop request option during morning hours.
        /// Only runs once per day during valid hours.
        /// </summary>
        private static void TrySendIntroMessage()
        {
            if (_npc == null)
            {
                Logger.Warn("[MrStacks] ❌ Cannot send message — contact missing.");
                return;
            }

            int hour = TimeManager.CurrentTime;
            int today = TimeManager.ElapsedDays;

            if (_hasSentIntro && _lastIntroDay == today)
                return;

            if (hour < 700 || hour >= 1900)
            {
                Logger.Msg($"[MrStacks] ⏰ Skipped intro message — hour {hour} is outside 7AM–7PM.");
                return;
            }

            _hasSentIntro = true;
            _lastIntroDay = today;

            try
            {
                var orderDrop = new Response
                {
                    Label = "ORDER_DROP",
                    Text = "Can I get a drop?",
                    OnTriggered = delegate
                    {
                        int tomorrow = TimeManager.ElapsedDays + 1;
                        int dropHour = new Random().Next(700, 1900); // Random hour between 7am–7pm
                        var packet = TierLevel.GetDropPacket(tomorrow);

                        DataBase.SaveDrop(tomorrow, packet, dropHour, "order");
                        _npc.SendTextMessage($"A drop is scheduled for tomorrow around {dropHour / 100}:00.");
                        Logger.Msg($"[MrStacks] ✅ Drop scheduled for Day {tomorrow} @ {dropHour}.");
                    }
                };

                _npc.SendTextMessage("Need something?", new[] { orderDrop });
                Logger.Msg("[MrStacks] 📩 Intro message sent.");
            }
            catch (Exception ex)
            {
                Logger.Exception(ex);
                Logger.Warn("[MrStacks] ⚠️ Failed to send message — phone app may be unstable.");
            }
        }

        /// <summary>
        /// Debug trigger: forcibly re-sends the intro message.
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
