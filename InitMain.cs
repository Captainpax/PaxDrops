using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using S1API.GameTime;
using S1API.Money;

namespace PaxDrops
{
    /// <summary>
    /// Main entry point for the PaxDrops mod.
    /// Boots all systems on the "Main" scene and wires time-based triggers.
    /// </summary>
    public class InitMain : MelonMod
    {
        /// <summary>
        /// Called once by MelonLoader when the mod is first loaded.
        /// Waits for the scene "Main" before initializing logic.
        /// </summary>
        public override void OnInitializeMelon()
        {
            Logger.Msg(">> PaxDrops initialized. Awaiting scene 'Main'...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Triggered on any scene load. PaxDrops only activates when "Main" is loaded.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main")
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Logger.Msg(">> Scene 'Main' detected. Starting PaxDrops systems...");

            // Initialize all PaxDrops systems
            TierLevel.Init();
            DataBase.Init();
            MrStacks.Init();
            DeadDrop.Init();

            // Hook daily drop check into in-game clock
            TimeManager.OnDayPass += DeadDrop.HandleDayPass;
        }

        /// <summary>
        /// Runs every frame. Used for developer hotkeys.
        /// </summary>
        public override void OnUpdate()
        {
            // PageDown = Give $5,000 to the player (debug)
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                Money.ChangeCashBalance(5000f, true, true);
                Logger.Msg("💵 $5,000 added to Global Bank (PageDown).");
            }

            // PageUp = Force-schedule drop and trigger MrStacks message (debug)
            if (Input.GetKeyDown(KeyCode.PageUp))
            {
                int today = TimeManager.ElapsedDays;

                // Force Tier 1 packet (test only)
                var packet = TierLevel.GetDropPacket(1);
                DataBase.SaveDrop(today, packet);

                Logger.Msg($"🧪 [Debug] Triggered test drop for Day {today} (Tier 1 packet).");

                // Trigger the message as if it were morning
                MrStacks.DebugTrigger();

                // Force drop spawn
                DeadDrop.HandleDayPass();
            }
        }
    }
}
