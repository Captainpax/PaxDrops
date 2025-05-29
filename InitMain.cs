using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader;
using ScheduleOne.Persistence;
using System.Collections;

[assembly: MelonInfo(typeof(PaxDrops.InitMain), "PaxDrops", "1.0.0", "CaptainPax")]
[assembly: MelonGame("Cortez", "Schedule 1")]

namespace PaxDrops
{
    /// <summary>
    /// Entry point and lifecycle manager for the PaxDrops mod.
    /// Handles system initialization, persistence, shutdown, and save info reporting.
    /// </summary>
    public class InitMain : MelonMod
    {
        // Keeps this GameObject alive across scene loads
        private static GameObject _persistentRoot;

        public override void OnInitializeMelon()
        {
            Logger.Msg("[InitMain] 🚀 PaxDrops loading...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// Handles scene transitions. We only care about the "Main" scene.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main")
                return;

            Logger.Msg("[InitMain] 🎬 Main scene loaded. Bootstrapping PaxDrops...");

            // Only create the persistent GameObject once
            if (_persistentRoot == null)
            {
                _persistentRoot = new GameObject("PaxDrops.Persistent");
                Object.DontDestroyOnLoad(_persistentRoot);
            }

            InitSystems();
            StartSaveInfoCoroutine(); // Wait and log active save once it's available
        }

        /// <summary>
        /// Boots all PaxDrops systems in load order.
        /// </summary>
        private static void InitSystems()
        {
            Logger.Init();           // 🔧 Logging system
            DataBase.Init();         // 💾 SQLite drop persistence
            TierLevel.Init();        // 📦 Tier/loot scaling
            DeadDrop.Init();         // 📬 Drop lifecycle + spawning
            MrStacks.Init();         // 📱 Messaging system
            CommandHandler.Init();   // ⌨️ Console commands
        }

        /// <summary>
        /// Begins a coroutine that waits for the game to finish loading a save.
        /// </summary>
        private static void StartSaveInfoCoroutine()
        {
            MelonCoroutines.Start(WaitForSaveLoad());
        }

        /// <summary>
        /// Waits for LoadManager to report a loaded save file, then logs its metadata.
        /// </summary>
        private static IEnumerator WaitForSaveLoad()
        {
            var lm = LoadManager.Instance;

            while (lm == null || !lm.IsGameLoaded)
            {
                yield return new WaitForSeconds(0.5f);
                lm = LoadManager.Instance;
            }

            string folder = lm.LoadedGameFolderPath ?? "(null)";
            string org = lm.ActiveSaveInfo?.OrganisationName ?? "Unknown Org";
            Logger.Msg($"📂 Save Loaded: {folder}");
            Logger.Msg($"🏢 Organization: {org}");
        }

        /// <summary>
        /// Called when the application is closing.
        /// Allows systems to clean up state or flush data.
        /// </summary>
        public override void OnApplicationQuit()
        {
            Logger.Msg("[InitMain] 🧼 PaxDrops shutting down. Cleaning up...");

            try
            {
                DeadDrop.Shutdown();  // Unsubscribe from game hooks
                DataBase.Shutdown();  // Finalize DB
                Logger.Msg("[InitMain] ✅ Shutdown complete.");
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Called once all mods are loaded. Good for final notices.
        /// </summary>
        public override void OnLateInitializeMelon()
        {
            Logger.Msg("[InitMain] ✅ PaxDrops loaded and persistent.");
        }
    }
}
