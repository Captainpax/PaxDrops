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
    /// Handles NPC messaging from "MrStacks" and supports scheduling tiered drops via player response.
    /// </summary>
    public static class MrStacks
    {
        private static S1NPC _npc;
        private static bool _hasSentToday;
        private static int _lastDay;
        private static bool _ready;

        public static void Init()
        {
            Logger.Msg("[MrStacks] 🔧 Initializing...");
            MelonCoroutines.Start(WaitForNpc());
            TimeManager.OnDayPass += TrySendIntroMessage;
        }

        private static IEnumerator WaitForNpc()
        {
            yield return new WaitUntil(() => S1NPC.All.Any(n => n.ID == "MrStacks"));
            _npc = S1NPC.All.FirstOrDefault(n => n.ID == "MrStacks");

            if (_npc == null)
            {
                Logger.Error("[MrStacks] ❌ Could not find MrStacks NPC.");
                yield break;
            }

            _ready = true;
            Logger.Msg("[MrStacks] ✅ NPC loaded and ready.");
        }

        public static void TriggerIntroIfReady()
        {
            _hasSentToday = false; // Reset so message will show
            TrySendIntroMessage();
        }

        private static void TrySendIntroMessage()
        {
            if (!_ready || _npc == null)
            {
                Logger.Warn("[MrStacks] ⚠️ NPC not ready, cannot send message.");
                return;
            }

            int hour = TimeManager.CurrentTime;
            int today = TimeManager.ElapsedDays;

            if (_hasSentToday && _lastDay == today)
                return;

            if (hour < 700 || hour >= 1900)
            {
                Logger.Msg($"[MrStacks] 💤 Outside hours ({hour}). Skipping.");
                return;
            }

            _hasSentToday = true;
            _lastDay = today;

            _npc.SendTextMessage("Lookin' for somethin' special?", new[]
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
            });

            Logger.Msg("[MrStacks] 📲 Sent intro message.");
        }

        private static void AskForTier(string groupName, Tier[] groupTiers)
        {
            if (_npc == null)
            {
                Logger.Warn("[MrStacks] ❌ Cannot prompt tier — NPC missing.");
                return;
            }

            int today = TimeManager.ElapsedDays;
            var options = new List<Response>();

            foreach (var tier in groupTiers)
            {
                if (!IsTierUnlocked(tier)) continue;

                options.Add(new Response
                {
                    Label = $"TIER_{(int)tier}",
                    Text = $"Tier {(int)tier}",
                    OnTriggered = () =>
                    {
                        int dropHour = new System.Random().Next(700, 1900);
                        var packet = GetDropPacket(today + 1);

                        string org = ScheduleOne.Persistence.LoadManager.Instance?.ActiveSaveInfo?.OrganisationName ?? "Unknown";
                        string dropTime = $"{today + 1:D3} @ {dropHour}";
                        DataBase.SaveDrop(today + 1, packet.ToFlatList(), dropHour, $"order:{tier}", org);

                        _npc.SendTextMessage($"You got it. Tier {(int)tier} comin' your way around {dropHour / 100}:00.");
                        Logger.Msg($"[MrStacks] ✅ Scheduled Tier {(int)tier} drop ➤ Day {today + 1} @ {dropHour} | Org: {org}");
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

        public static void DebugTrigger()
        {
            if (!_ready || _npc == null)
            {
                Logger.Warn("[MrStacks] ⚠️ NPC not ready for debug.");
                return;
            }

            var today = TimeManager.ElapsedDays;
            int dropHour = TimeManager.CurrentTime;

            var packet = GetDropPacket(today);
            string org = ScheduleOne.Persistence.LoadManager.Instance?.ActiveSaveInfo?.OrganisationName ?? "Unknown";

            DataBase.SaveDrop(today, packet.ToFlatList(), dropHour, "debug", org);
            Logger.Msg($"[MrStacks] 🧪 Debug drop saved: Day {today} @ {dropHour} | Org: {org}");
        }
    }
}
