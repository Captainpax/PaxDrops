using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader; 

[assembly: MelonInfo(typeof(PaxDrops.InitMain), "PaxDrops", "1.0.0", "CaptainPax")]
[assembly: MelonGame("Cortez", "Schedule 1")]

namespace PaxDrops
{
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
            if (scene.name != "Main")
                return;

            Logger.Msg("[InitMain] 🎬 Main scene loaded. Bootstrapping PaxDrops...");

            if (_persistentRoot == null)
            {
                _persistentRoot = new GameObject("PaxDrops.Persistent");
                UnityEngine.Object.DontDestroyOnLoad(_persistentRoot);
            }

            InitSystems();
        }

        private static void InitSystems()
        {
            Logger.Init();           // 🔧 Logging system
            DataBase.Init();         // 💾 SQLite drop saves
            TierLevel.Init();        // 📦 Tier + loot scaling
            DeadDrop.Init();         // 📬 Dead drop spawner
            MrStacks.Init();         // 📱 NPC message interface
            CommandHandler.Init();   // 💻 Command registrar
        }

        public override void OnLateInitializeMelon()
        {
            Logger.Msg("[InitMain] ✅ PaxDrops loaded and persistent.");
        }
    }
}