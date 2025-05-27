using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using S1API.GameTime;
using S1API.Money;

namespace PaxDrops
{
    public class InitMain : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Logger.Msg(">> PaxDrops initialized. Awaiting scene 'Main'...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main") return;

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
                var packet = TierLevel.GetDropPacket(today);
                DataBase.SaveDrop(today, packet, TimeManager.CurrentTime, "debug");

                Logger.Msg($"🧪 [Debug] Triggered test drop for Day {today} (Tier 1 packet).");

                MrStacks.DebugTrigger();
                DeadDrop.ForceSpawnDrop(today, packet, "debug");
            }
        }
    }
}