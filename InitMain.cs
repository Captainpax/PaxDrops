using UnityEngine;
using MelonLoader;
using PaxDrops.MrStacks;
using PaxDrops.Patches;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.Persistence;

[assembly: MelonInfo(typeof(PaxDrops.InitMain), "PaxDrops", "1.0.0", "CaptainPax")]
[assembly: MelonGame("Cortez", "Schedule 1")]

namespace PaxDrops
{
    /// <summary>
    /// Entry point and lifecycle manager for the PaxDrops mod.
    /// Handles system initialization, persistence, and shutdown.
    /// Now uses event-driven initialization based on player detection and save-file-aware data management.
    /// </summary>
    public class InitMain : MelonMod
    {
        private static bool _initialized = false;
        private static bool _playerDependentSystemsInitialized = false;
        private static bool _isInMainScene = false;

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
                _isInMainScene = true;
                
                // Initialize core systems first (player-independent)
                InitCoreSystems();

                // Start player detection
                PlayerDetection.StartDetection();

                // Subscribe to player detection events
                PlayerDetection.OnPlayerLoaded += OnPlayerDetected;
                PlayerDetection.OnPlayerRankLoaded += OnPlayerRankDetected;

                _initialized = true;
                MelonLogger.Msg("🎮 PaxDrops core systems initialized! Waiting for player detection...");
                
                // Try to load save file data once we're in the main scene
                TryLoadSaveFileData();
            }
            else if (_isInMainScene && !(buildIndex == 1 || sceneName.Contains("Main")))
            {
                // We were in main scene and now we're not (going back to menu)
                MelonLogger.Msg($"🚪 Exiting main scene (Scene: {sceneName}, Index: {buildIndex})");
                OnExitMainScene();
                _isInMainScene = false;
            }
            else if (!_isInMainScene && (buildIndex == 1 || sceneName.Contains("Main")))
            {
                // We weren't in main scene and now we are (loading a save)
                MelonLogger.Msg($"🚪 Entering main scene (Scene: {sceneName}, Index: {buildIndex})");
                _isInMainScene = true;
                TryLoadSaveFileData();
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
                SaveSystemPatch.Shutdown();
                SaveFileJsonDataStore.Shutdown();
                // CommandHandler.Shutdown();  // DISABLED FOR NOW
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
                // Initialize save-aware data storage
                SaveFileJsonDataStore.Init();  // 💾 Save-file-aware JSON persistence layer
                
                // Initialize other core systems
                DeadDrop.Init();           // ⚰️ Dead drop spawning system
                TierDropSystem.Init();     // 🎯 Tier-based drop system (basic init)
                // CommandHandler.Init();     // 🎮 Console command system (DISABLED FOR NOW)
                TimeMonitor.Init();        // ⏰ Time monitoring for drops
                
                // Initialize save system hooks
                SaveSystemPatch.Init();    // 🎣 Save system integration
                
                Logger.Msg("✅ [InitMain] Core systems initialized successfully!");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Core system initialization failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Try to load save file data when entering main scene
        /// </summary>
        private static void TryLoadSaveFileData()
        {
            try
            {
                // Get the save manager to identify which save we're in
                var saveManager = SaveManager.Instance;
                if (saveManager != null)
                {
                    string? savePath = saveManager.PlayersSavePath;
                    string? saveName = saveManager.SaveName;
                    
                    if (!string.IsNullOrEmpty(savePath) && !string.IsNullOrEmpty(saveName))
                    {
                        Logger.Msg($"[InitMain] 📂 Loading data for save: {saveName} at {savePath}");
                        SaveFileJsonDataStore.LoadForSaveFile(savePath, saveName);
                    }
                    else
                    {
                        Logger.Warn("[InitMain] ⚠️ Could not identify current save file - using default");
                        SaveFileJsonDataStore.LoadForSaveFile("", "default");
                    }
                }
                else
                {
                    Logger.Warn("[InitMain] ⚠️ SaveManager not available - will retry later");
                    // We'll rely on the save system patch to catch saves later
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Failed to load save file data: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Handle exiting the main scene (going back to menu)
        /// </summary>
        private static void OnExitMainScene()
        {
            try
            {
                Logger.Msg("[InitMain] 📤 Exiting main scene - unloading save data");
                SaveFileJsonDataStore.UnloadCurrentSave();
                
                // Reset player-dependent systems
                _playerDependentSystemsInitialized = false;
                
                Logger.Msg("[InitMain] ✅ Main scene exit complete");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Error during main scene exit: {ex.Message}");
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
                
                // Log save file status
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                if (isLoaded)
                {
                    Logger.Msg($"[InitMain] 💾 Save File: {saveName} (ID: {saveId}, Steam: {steamId})");
                }
                else
                {
                    Logger.Warn("[InitMain] ⚠️ No save file data loaded");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Player-dependent system initialization failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }
    }
} 