using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using S1API.Entities;
using S1API.GameTime;
using S1API.Messaging;
using PaxDrops.Core;
using PaxDrops.Drops;
using static PaxDrops.TierLevel;

namespace PaxDrops.NPC
{
    /// <summary>
    /// Coordinates the Mrs. Stacks NPC, messaging logic, and drop selection.
    /// </summary>
    public static class MrStacks
    {
        private static NPC _npc;

        /// <summary>
        /// Called from InitMain — binds NPC message checks to time system.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrStacks] 🔄 Binding time hook...");
            TimeManager.OnMinutePass += CheckAndSend;
        }

        /// <summary>
        /// Handles every in-game minute — delegates to MrStacksMsg.
        /// </summary>
        private static void CheckAndSend(int hour, int minute)
        {
            MrStacksMsg.CheckAndSend(hour, minute);
        }

        /// <summary>
        /// Tries to send the morning message if NPC is present or created.
        /// </summary>
        public static bool TrySendMorningMessage()
        {
            if (!TryGetOrSpawnNpc())
                return false;

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

            return true;
        }

        /// <summary>
        /// Presents tier options for a given group.
        /// </summary>
        private static void AskForTier(string groupName, Tier[] groupTiers)
        {
            int today = TimeManager.ElapsedDays;
            var options = new List<Response>();

            foreach (var tier in groupTiers)
            {
                if (!IsTierUnlocked(tier)) continue;

                int tierNum = (int)tier;
                options.Add(new Response
                {
                    Label = $"TIER_{tierNum}",
                    Text = $"Tier {tierNum}",
                    OnTriggered = () =>
                    {
                        int dropHour = new System.Random().Next(700, 1900);
                        var packet = GetDropPacket(today + 1);
                        DataBase.SaveDrop(today + 1, packet.ToFlatList(), dropHour, $"order:{tier}");
                        _npc.SendTextMessage($"You got it. Tier {tierNum} comin' your way around {dropHour / 100}:00.");
                        Logger.Msg($"[MrStacks] ✅ Scheduled Tier {tierNum} for Day {today + 1} @ {dropHour}.");
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
        /// Gets or spawns the Mrs. Stacks NPC.
        /// </summary>
        private static bool TryGetOrSpawnNpc()
        {
            if (_npc != null)
                return true;

            _npc = NPCBuilder.SpawnStacks();
            return _npc != null;
        }

        /// <summary>
        /// Dev-only trigger to force morning message resend.
        /// </summary>
        public static void DebugTrigger()
        {
            MrStacksMsg.ResetDailyFlag();
            TrySendMorningMessage();
        }
    }
}
