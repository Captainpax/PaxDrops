using UnityEngine;
using MelonLoader;

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
        private static bool _initialized = false;

        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("🚀 PaxDrops loading...");
            Logger.Init();
        }

        public override void OnLateInitializeMelon()
        {
            MelonLogger.Msg("✅ PaxDrops loaded and persistent.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Main scene is usually index 1 or has "Main" in name
            if ((buildIndex == 1 || sceneName.Contains("Main")) && !_initialized)
            {
                MelonLogger.Msg("🎬 Main scene loaded. Bootstrapping PaxDrops...");
                
                // Initialize database first
                JsonDataStore.Init();
                
                // Initialize all other systems
                InitSystems();

                _initialized = true;
                MelonLogger.Msg("🎮 PaxDrops fully initialized!");
            }
        }

        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("🧼 PaxDrops shutting down...");
            try
            {
                TimeMonitor.Shutdown();
                // CommandHandler.Shutdown();
                JsonDataStore.Shutdown();
                Logger.Shutdown();
                MelonLogger.Msg("✅ Shutdown complete.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"❌ Shutdown error: {ex.Message}");
            }
        }

        /// <summary>
        /// Initializes all modular systems in startup order.
        /// </summary>
        private static void InitSystems()
        {
            Logger.Msg("[InitMain] 🔧 Initializing PaxDrops systems...");
            
            TierLevel.Init();        // 📦 Tiered loot system  
            DeadDrop.Init();         // 📬 Drop spawner
            TimeMonitor.Init();      // ⏰ Time monitoring system
            MrStacks.Init();         // 📱 Mrs. Stacks NPC handler
            // CommandHandler.Init();   // ⌨️ Console command registration
            
            Logger.Msg("[InitMain] ✅ All systems initialized successfully.");
        }
    }
} 