using UnityEngine;
using MelonLoader;
using PaxDrops.MrStacks;

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
                MrsStacksNPC.Shutdown();
                TimeMonitor.Shutdown();
                CommandHandler.Shutdown();
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
            
            try
            {
                Logger.Msg("🚀 [InitMain] Starting PaxDrops IL2CPP initialization...");

                // Initialize core systems first
                DeadDrop.Init();        // ⚰️ Dead drop spawning system
                TierDropSystem.Init();   // 🎯 New tier-based drop system with ERank integration
                DailyDropOrdering.Init(); // 📅 Daily drop ordering system (rank-based)

                // Initialize data storage
                JsonDataStore.Init();   // 💾 JSON persistence layer
                
                // Initialize specific features
                CommandHandler.Init();  // 🎮 Console command system
                MrsStacksNPC.Init();    // 👤 Mrs. Stacks NPC integration
                TimeMonitor.Init();     // ⏰ Time monitoring for drops

                Logger.Msg("✅ [InitMain] PaxDrops IL2CPP initialization complete!");
                Logger.Msg("🎯 [InitMain] New rank-based tier system active (11 tiers mapped 1:1 with ERank)");
                Logger.Msg("📅 [InitMain] Daily ordering system enabled - tier rewards based on player rank");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ [InitMain] PaxDrops IL2CPP initialization error: {ex.Message}");
            }
            
            // Log tier system status
            var currentRank = PaxDrops.Configs.DropConfig.GetCurrentPlayerRank();
            var currentDay = PaxDrops.Configs.DropConfig.GetCurrentGameDay();
            var maxTier = PaxDrops.Configs.DropConfig.GetCurrentMaxUnlockedTier();
            var unlockedOrgs = TierDropSystem.GetPlayerUnlockedOrganizations();
            
            Logger.Msg($"[InitMain] 📊 Player Status: Day {currentDay}, Rank {currentRank}");
            Logger.Msg($"[InitMain] 🏆 Max Unlocked Tier: {PaxDrops.Configs.TierConfig.GetTierName(maxTier)}");
            Logger.Msg($"[InitMain] 🏢 Unlocked Organizations: {string.Join(", ", unlockedOrgs)}");
        }
    }
} 