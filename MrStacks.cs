using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MelonLoader;
using S1API.GameTime;
using S1API.Messaging;
using static PaxDrops.TierLevel;
using S1NPC = S1API.Entities.NPC;

namespace PaxDrops
{
    /// <summary>
    /// Handles NPC MrStacks and player-triggered drop requests via phone messages.
    /// </summary>
    public static class MrStacks
    {
        private static S1NPC _npc;
        private static bool _hasSentToday;
        private static int _lastDay;
        private static bool _ready;

        /// <summary>
        /// Boot and wait for MrStacks NPC to load.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrStacks] 🔧 Initializing...");
            MelonCoroutines.Start(WaitForNpc());
            TimeManager.OnDayPass += TriggerIntroIfReady;
        }

        /// <summary>
        /// Coroutine that waits for MrStacks NPC to appear in the world.
        /// </summary>
        private static IEnumerator WaitForNpc()
        {
            yield return new WaitUntil(() => S1NPC.All.Any(npc => npc.ID == "MrStacks"));
            _npc = S1NPC.All.FirstOrDefault(npc => npc.ID == "MrStacks");

            if (_npc == null)
            {
                Logger.Error("[MrStacks] ❌ Failed to find MrStacks.");
                yield break;
            }

            Logger.Msg("[MrStacks] ✅ NPC ready.");
            _ready = true;

            // Trigger welcome after init (on first boot)
            TriggerIntroIfReady();
        }

        /// <summary>
        /// Sends the intro message if NPC and time are valid.
        /// </summary>
        public static void TriggerIntroIfReady()
        {
            if (!_ready || _npc == null)
            {
                Logger.Warn("[MrStacks] ⚠️ NPC not ready yet.");
                return;
            }

            MelonCoroutines.Start(SendIntroMessage());
        }

        /// <summary>
        /// Waits briefly and sends the "what do you want" message.
        /// </summary>
        private static IEnumerator SendIntroMessage()
        {
            yield return new WaitForSeconds(1.0f);

            int hour = TimeManager.CurrentTime;
            int today = TimeManager.ElapsedDays;

            if (_hasSentToday && _lastDay == today)
                yield break;

            if (hour < 700 || hour >= 1900)
            {
                Logger.Msg($"[MrStacks] 💤 Outside hours ({hour}). Skipping.");
                yield break;
            }

            _hasSentToday = true;
            _lastDay = today;

            Logger.Msg("[MrStacks] 📲 Sending welcome message...");

            _npc.SendTextMessage("Lookin' for somethin' special?", new[]
            {
                new Response
                {
                    Label = "GROUP_STREET",
                    Text = "Gimme a Street Earner pack",
                    OnTriggered = () => AskForTier("Street Earner", new[] {
                        Tier.StreetEarner1, Tier.StreetEarner2, Tier.StreetEarner3
                    })
                },
                new Response
                {
                    Label = "GROUP_CAPO",
                    Text = "Callin' in Capo favors",
                    OnTriggered = () => AskForTier("Capo", new[] {
                        Tier.Capo1, Tier.Capo2, Tier.Capo3
                    })
                },
                new Response
                {
                    Label = "GROUP_DON",
                    Text = "Summon the Don’s respect",
                    OnTriggered = () => AskForTier("Don", new[] {
                        Tier.Don1, Tier.Don2, Tier.Don3
                    })
                }
            });
        }

        /// <summary>
        /// Sends a follow-up message to choose a tier from the selected group.
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
                        int dropHour = new System.Random().Next(700, 1900);
                        var packet = GetDropPacket(today + 1);
                        DataBase.SaveDrop(today + 1, packet.ToFlatList(), dropHour, $"order:{tier}");
                        _npc.SendTextMessage($"You got it. Tier {(int)tier} comin' your way around {dropHour / 100}:00.");
                        Logger.Msg($"[MrStacks] ✅ Scheduled Tier {(int)tier} drop for Day {today + 1} @ {dropHour}.");
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
        /// Manual dev trigger.
        /// </summary>
        public static void DebugTrigger()
        {
            _hasSentToday = false;
            TriggerIntroIfReady();
        }
    }
}
