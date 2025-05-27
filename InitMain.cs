using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using S1API.GameTime;
using S1API.Money;
using S1API.Entities;
using S1API.DeadDrops;

namespace PaxDrops
{
    /// <summary>
    /// Main entry point for the PaxDrops mod.
    /// Initializes systems when the main scene loads and supports debug keybinds.
    /// </summary>
    public class InitMain : MelonMod
    {
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
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                Money.ChangeCashBalance(5000f, true, true);
                Logger.Msg("💵 $5,000 added to Global Bank (PageDown).");
            }

            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                int today = TimeManager.ElapsedDays;
                var drop = TierLevel.GetDropPacket(today);
                DataBase.SaveDrop(today, drop.ToFlatList(), TimeManager.CurrentTime, "debug");

                Logger.Msg($"🧪 [Debug] Triggered test drop for Day {today} ➤ Loot: {string.Join(", ", drop.Loot)} | 💵 Cash: ${drop.CashAmount}");

                MrStacks.DebugTrigger();
                DeadDrop.ForceSpawnDrop(today, drop.ToFlatList(), "debug");
            }

            if (Input.GetKeyDown(KeyCode.Home))
            {
                TimeManager.SetTime(2000); // 8:00 PM
                Logger.Msg("🕗 [Dev] Time set to 8:00 PM (Home key).");
            }

            if (Input.GetKeyDown(KeyCode.End))
            {
                NPC mrStacks = NPC.All.Find(n => n.ID == "MrStacks");
                if (mrStacks != null)
                {
                    Vector3 origin = mrStacks.Position;
                    DeadDropInstance closest = null;
                    float closestDist = float.MaxValue;

                    foreach (var drop in DeadDropManager.All)
                    {
                        float dist = Vector3.Distance(origin, drop.Position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = drop;
                        }
                    }

                    if (closest != null)
                    {
                        mrStacks.Position = closest.Position;
                        Logger.Msg($"🧭 [Dev] Mr. Stacks teleported to closest dead drop at {closest.Position} (End key).");
                    }
                    else
                    {
                        Logger.Warn("[Dev] No valid dead drops found to teleport to.");
                    }
                }
                else
                {
                    Logger.Warn("[Dev] Mr. Stacks not found — teleport skipped.");
                }
            }
        }
    }
}
