using UnityEngine;
using MelonLoader;
using PaxDrops.MrStacks;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Levelling;

[assembly: MelonInfo(typeof(PaxDrops.InitMain), "PaxDrops", "1.0.0", "CaptainPax")]
[assembly: MelonGame("Cortez", "Schedule 1")]

namespace PaxDrops
{
    /// <summary>
    /// Entry point and lifecycle manager for the PaxDrops mod.
    /// Handles system initialization, persistence, and shutdown.
    /// Now uses event-driven initialization based on player detection.
    /// </summary>
    public class InitMain : MelonMod
    {
        private static bool _initialized = false;
        private static bool _playerDependentSystemsInitialized = false;

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
                
                // Initialize core systems first (player-independent)
                InitCoreSystems();

                // Start player detection
                PlayerDetection.StartDetection();

                // Subscribe to player detection events
                PlayerDetection.OnPlayerLoaded += OnPlayerDetected;
                PlayerDetection.OnPlayerRankLoaded += OnPlayerRankDetected;

                _initialized = true;
                MelonLogger.Msg("🎮 PaxDrops core systems initialized! Waiting for player detection...");
            }
        }

        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("🧼 PaxDrops shutting down...");
            try
            {
                // Unsubscribe from events
                PlayerDetection.OnPlayerLoaded -= OnPlayerDetected;
                PlayerDetection.OnPlayerRankLoaded -= OnPlayerRankDetected;

                MrsStacksNPC.Shutdown();
                TimeMonitor.Shutdown();
                // CommandHandler.Shutdown();  // DISABLED FOR NOW
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
        /// Initialize core systems that don't depend on player detection
        /// </summary>
        private static void InitCoreSystems()
        {
            Logger.Msg("[InitMain] 🔧 Initializing core PaxDrops systems...");
            
            try
            {
                // Initialize data storage and basic systems first
                JsonDataStore.Init();      // 💾 JSON persistence layer
                DeadDrop.Init();           // ⚰️ Dead drop spawning system
                TierDropSystem.Init();     // 🎯 Tier-based drop system (basic init)
                // CommandHandler.Init();     // 🎮 Console command system (DISABLED FOR NOW)
                TimeMonitor.Init();        // ⏰ Time monitoring for drops

                Logger.Msg("✅ [InitMain] Core systems initialized successfully!");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Core system initialization failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Called when the player is first detected
        /// </summary>
        private static void OnPlayerDetected(Player player)
        {
            Logger.Msg($"[InitMain] 👤 Player detected: {player.PlayerName}");
            Logger.Msg("[InitMain] Waiting for rank data...");
        }

        /// <summary>
        /// Called when player rank data becomes available
        /// </summary>
        private static void OnPlayerRankDetected(Player player, ERank rank)
        {
            if (_playerDependentSystemsInitialized) return; // Prevent double initialization

            Logger.Msg($"[InitMain] 🎯 Player rank detected: {rank}");
            Logger.Msg("[InitMain] 🚀 Initializing player-dependent systems...");

            try
            {
                // Initialize systems that depend on player data
                DailyDropOrdering.Init();  // 📅 Daily drop ordering system (rank-based)
                MrsStacksNPC.Init();       // 👤 Mrs. Stacks NPC integration

                _playerDependentSystemsInitialized = true;

                Logger.Msg("✅ [InitMain] Player-dependent systems initialized!");
                Logger.Msg("🎯 [InitMain] New rank-based tier system active (11 tiers mapped 1:1 with ERank)");
                Logger.Msg("📅 [InitMain] Daily ordering system enabled - tier rewards based on player rank");
                Logger.Msg($"🎮 [InitMain] PaxDrops fully initialized for {player.PlayerName} (Rank: {rank})!");

                // Log player detection status
                Logger.Msg($"[InitMain] Player Status: {PaxDrops.Configs.DropConfig.GetPlayerDetectionStatus()}");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Player-dependent system initialization failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }
    }
} 