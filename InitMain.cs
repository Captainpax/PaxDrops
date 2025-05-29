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
    /// Handles system initialization, persistence, and shutdown.
    /// </summary>
    public class InitMain : MelonMod
    {
        private static GameObject _persistentRoot;

        public override void OnInitializeMelon()
        {
            Logger.Msg("[InitMain] 🚀 PaxDrops loading...");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main") return;

            Logger.Msg("[InitMain] 🎬 Main scene loaded. Bootstrapping PaxDrops...");

            if (_persistentRoot == null)
            {
                _persistentRoot = new GameObject("PaxDrops.Persistent");
                Object.DontDestroyOnLoad(_persistentRoot);
            }

            InitSystems();
            MelonCoroutines.Start(WaitForSaveLoad());
        }

        private static void InitSystems()
        {
            Logger.Init();           // 🔧 Logging system
            DataBase.Init();         // 💾 SQLite drop persistence
            TierLevel.Init();        // 📦 Tier/loot scaling
            DeadDrop.Init();         // 📬 Drop spawning
            MrStacks.Init();         // 📱 Messaging system
            CommandHandler.Init();   // ⌨️ Console commands
        }

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

            MrStacks.TriggerIntroIfReady(); // ✅ Force intro message after save is loaded
        }

        public override void OnApplicationQuit()
        {
            Logger.Msg("[InitMain] 🧼 PaxDrops shutting down...");
            try
            {
                DeadDrop.Shutdown();
                DataBase.Shutdown();
                Logger.Msg("[InitMain] ✅ Shutdown complete.");
            }
            catch (System.Exception ex)
            {
                Logger.Exception(ex);
            }
        }

        public override void OnLateInitializeMelon()
        {
            Logger.Msg("[InitMain] ✅ PaxDrops loaded and persistent.");
        }
    }
}
