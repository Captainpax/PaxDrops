using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using S1API.GameTime;
using S1API.Money;
using S1API.Entities;
using S1API.DeadDrops;

namespace PaxDrops
{
    public class InitMain : MelonMod
    {
        private bool prevPageUp, prevPageDown, prevHome, prevEnd;

        public override void OnInitializeMelon()
        {
            Logger.Msg(">> PaxDrops initialized. Awaiting scene 'Main'...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main")
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Logger.Msg(">> Scene 'Main' detected. Starting PaxDrops systems...");

            TierLevel.Init();
            DataBase.Init();
            MrStacks.Init();
            DeadDrop.Init();

            TimeManager.OnDayPass += DeadDrop.HandleDayPass;
        }

        public override void OnUpdate()
        {
            if (!Application.isFocused) return;

            // Manual key state edge detection
            bool pageUp = Input.GetKey(KeyCode.PageUp);
            bool pageDown = Input.GetKey(KeyCode.PageDown);
            bool home = Input.GetKey(KeyCode.Home);
            bool end = Input.GetKey(KeyCode.End);

            if (pageDown && !prevPageDown)
            {
                Money.ChangeCashBalance(5000f, true, true);
                Logger.Msg("💵 $5,000 added to Global Bank (PageDown).");
            }

            if (pageUp && !prevPageUp)
            {
                int today = TimeManager.ElapsedDays;
                var drop = TierLevel.GetDropPacket(today);
                DataBase.SaveDrop(today, drop.ToFlatList(), TimeManager.CurrentTime, "debug");

                Logger.Msg($"🧪 [Debug] Triggered test drop for Day {today} ➤ Loot: {string.Join(", ", drop.Loot)} | 💵 Cash: ${drop.CashAmount}");

                MrStacks.DebugTrigger();
                DeadDrop.ForceSpawnDrop(today, drop.ToFlatList(), "debug");
            }

            if (home && !prevHome)
            {
                TimeManager.SetTime(2000);
                Logger.Msg("🕗 [Dev] Time set to 8:00 PM (Home key).");
            }

            if (end && !prevEnd)
            {
                var player = Player.Local;
                if (player == null)
                {
                    Logger.Warn("[Dev] Player not found — teleport skipped.");
                }
                else
                {
                    Vector3 origin = player.Position;
                    DeadDropInstance closest = null;
                    float closestDist = float.MaxValue;

                    foreach (var drop in DeadDropManager.All)
                    {
                        float dist = Vector3.Distance(origin, drop.Position);
                        if (dist < closestDist)
                        {
                            closest = drop;
                            closestDist = dist;
                        }
                    }

                    if (closest != null)
                    {
                        player.Position = closest.Position;
                        Logger.Msg($"🧭 [Dev] Player teleported to closest dead drop at {closest.Position} (End key).");
                    }
                    else
                    {
                        Logger.Warn("[Dev] No valid dead drops found to teleport to.");
                    }
                }
            }

            // Update previous key states
            prevPageUp = pageUp;
            prevPageDown = pageDown;
            prevHome = home;
            prevEnd = end;
        }
    }
}