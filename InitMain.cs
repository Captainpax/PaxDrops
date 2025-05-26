using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using S1API.GameTime;
using S1API.Money;

namespace PaxDrops
{
    /// <summary>
    /// Main entry point for the PaxDrops mod.
    /// Waits for the Main scene, initializes systems, and hooks time-based events.
    /// </summary>
    public class InitMain : MelonMod
    {
        /// <summary>
        /// Called by MelonLoader when the mod is first loaded.
        /// Waits for the 'Main' scene to be ready before initializing logic.
        /// </summary>
        public override void OnInitializeMelon()
        {
            Logger.Msg(">> PaxDrops initialized. Awaiting scene 'Main'...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Triggered when any scene is loaded. Boots systems only on 'Main'.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main") return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            Logger.Msg(">> Scene 'Main' detected. Starting PaxDrops systems...");

            // Initialize core systems
            TierLevel.Init();
            DataBase.Init();
            MrStacks.Init();
            DeadDrop.Init();

            // Hook into day progression
            TimeManager.OnDayPass += DeadDrop.HandleDayPass;
        }

        /// <summary>
        /// Runs every frame. Used here for a developer shortcut (PageDown = add $5000).
        /// </summary>
        public override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.PageDown))
            {
                // Add money using S1API.Money interface
                Money.ChangeCashBalance(5000f, true, true);
                Logger.Msg("💵 $5,000 added to Global Bank (PageDown).");
            }
        }
    }
}