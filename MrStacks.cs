using S1API.GameTime;
using S1API.Messaging;
using S1API.Entities;
using System;
using System.Collections.Generic;
using static PaxDrops.TierLevel;

namespace PaxDrops
{
    /// <summary>
    /// Manages the phone interaction with Mr. Stacks, allowing the player to request custom drop tiers.
    /// </summary>
    public static class MrStacks
    {
        private static NPC _npc;
        private static bool _hasSentToday;
        private static int _lastDay;

        /// <summary>
        /// Initializes Mr. Stacks and ensures his contact exists.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrStacks] 🧠 Booting up.");
            SetupContact();
            TimeManager.OnDayPass += TrySendIntroMessage;
        }

        /// <summary>
        /// Ensures Mr. Stacks exists as a contact in the phone system.
        /// </summary>
        private static void SetupContact()
        {
            _npc = NPC.All.Find(n => n.ID == "MrStacks");

            if (_npc == null)
            {
                _npc = new MrStacksContact("MrStacks", "Mr.", "Stacks");

                if (!NPC.All.Contains(_npc))
                    NPC.All.Add(_npc);

                Logger.Msg("[MrStacks] 📇 Created fallback contact.");
            }
        }

        /// <summary>
        /// Sends the initial "Need somethin'?" message if during valid hours and not yet sent today.
        /// </summary>
        private static void TrySendIntroMessage()
        {
            if (_npc == null)
                return;

            int hour = TimeManager.CurrentTime;
            int today = TimeManager.ElapsedDays;

            if (_hasSentToday && _lastDay == today)
                return;

            if (hour < 700 || hour >= 1900)
            {
                Logger.Msg($"[MrStacks] 💤 Skipped message — hour {hour} outside 7AM–7PM.");
                return;
            }

            _hasSentToday = true;
            _lastDay = today;

            // Tier group options (split by mafia theme)
            var groupOptions = new List<Response>
            {
                new Response
                {
                    Label = "GROUP_STREET",
                    Text = "Gimme a Street Earner pack",
                    OnTriggered = () => AskForTier("Street Earner", new[]
                    {
                        Tier.StreetEarner1, Tier.StreetEarner2, Tier.StreetEarner3
                    })
                },
                new Response
                {
                    Label = "GROUP_CAPO",
                    Text = "Callin' in Capo favors",
                    OnTriggered = () => AskForTier("Capo", new[]
                    {
                        Tier.Capo1, Tier.Capo2, Tier.Capo3
                    })
                },
                new Response
                {
                    Label = "GROUP_DON",
                    Text = "Summon the Don’s respect",
                    OnTriggered = () => AskForTier("Don", new[]
                    {
                        Tier.Don1, Tier.Don2, Tier.Don3
                    })
                }
            };

            _npc.SendTextMessage("Lookin' for somethin' special?", groupOptions.ToArray());
            Logger.Msg("[MrStacks] 📲 Intro message sent.");
        }

        /// <summary>
        /// Asks the player to choose a tier from the selected group.
        /// </summary>
        private static void AskForTier(string groupName, Tier[] groupTiers)
        {
            int today = TimeManager.ElapsedDays;
            var options = new List<Response>();

            foreach (var tier in groupTiers)
            {
                if (!IsTierUnlocked(tier))
                    continue;

                options.Add(new Response
                {
                    Label = $"TIER_{(int)tier}",
                    Text = $"Tier {(int)tier}",
                    OnTriggered = () =>
                    {
                        int dropHour = new Random().Next(700, 1900);
                        var packet = GetDropPacket(today + 1);

                        DataBase.SaveDrop(today + 1, packet.ToFlatList(), dropHour, $"order:{tier}");
                        _npc.SendTextMessage($"You got it. Tier {(int)tier} comin' your way around {dropHour / 100}:00.");
                        Logger.Msg($"[MrStacks] 📦 Scheduled Tier {(int)tier} drop for Day {today + 1} @ {dropHour}.");
                    }
                });
            }

            if (options.Count == 0)
            {
                _npc.SendTextMessage($"You're not ready for any {groupName} drops yet.");
                return;
            }

            _npc.SendTextMessage($"Which {groupName} drop you want?", options.ToArray());
        }

        /// <summary>
        /// Forces message resend for testing.
        /// </summary>
        public static void DebugTrigger()
        {
            _hasSentToday = false;
            TrySendIntroMessage();
        }

        /// <summary>
        /// Fallback NPC contact if none exists in game.
        /// </summary>
        private class MrStacksContact : NPC
        {
            public MrStacksContact(string id, string first, string last)
                : base(id, first, last) { }
        }
    }
}
