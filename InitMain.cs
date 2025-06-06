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
    /// Handles proper initialization phases:
    /// 1. Application Init - Basic resources and logging
    /// 2. Scene Init - Core systems and save detection
    /// 3. Player Init - Player-dependent systems (NPCs, messaging)
    /// 4. Proper cleanup when exiting scenes
    /// </summary>
    public class InitMain : MelonMod
    {
        private static bool _coreSystemsInitialized = false;
        private static bool _playerDependentSystemsInitialized = false;
        private static bool _isInMainScene = false;

        #region Application Lifecycle

        public override void OnInitializeMelon()
        {
            // PHASE 1: Application start - minimal setup only
            MelonLogger.Msg("🚀 PaxDrops loading...");
            
            try
            {
                // Initialize only basic logging system
                Logger.Init();
                
                Logger.Msg("[InitMain] ⚙️ Application initialization complete");
                Logger.Msg("[InitMain] 📋 Waiting for main scene...");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"❌ Application initialization failed: {ex.Message}");
            }
        }

        public override void OnLateInitializeMelon()
        {
            // Application fully loaded - still waiting for scene
            MelonLogger.Msg("[PaxDrops] [InitMain] ✅ PaxDrops loaded and ready for scene detection.");
        }

        public override void OnApplicationQuit()
        {
            MelonLogger.Msg("[PaxDrops] [InitMain] 🧼 PaxDrops shutting down...");
            try
            {
                // Clean shutdown of all systems
                ShutdownPlayerDependentSystems();
                ShutdownCoreSystemsOnExit();
                Logger.Shutdown();
                MelonLogger.Msg("[PaxDrops] [InitMain] ✅ Complete shutdown finished.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[PaxDrops] [InitMain] ❌ Shutdown error: {ex.Message}");
            }
        }

        #endregion

        #region Scene Lifecycle

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Main scene detection (usually index 1 or contains "Main")
            bool isMainScene = (buildIndex == 1 || sceneName.Contains("Main"));
            
            if (isMainScene && !_isInMainScene)
            {
                // PHASE 2: Entering main scene
                Logger.Msg($"[InitMain] 🎬 Main scene loaded (Scene: {sceneName}, Index: {buildIndex})");
                _isInMainScene = true;
                OnEnterMainScene();
            }
            else if (_isInMainScene && !isMainScene)
            {
                // Exiting main scene (going back to menu)
                Logger.Msg($"[InitMain] 🚪 Exiting main scene to {sceneName} (Index: {buildIndex})");
                OnExitMainScene();
                _isInMainScene = false;
            }
            else if (isMainScene && _isInMainScene)
            {
                // Re-entering main scene (loading different save)
                Logger.Msg($"[InitMain] 🔄 Re-entering main scene (Scene: {sceneName}, Index: {buildIndex})");
                OnReEnterMainScene();
            }
        }

        /// <summary>
        /// Handle entering main scene for the first time
        /// </summary>
        private static void OnEnterMainScene()
        {
            try
            {
                Logger.Msg("[InitMain] 🏗️ Bootstrapping PaxDrops for main scene...");
                
                // PHASE 2A: Initialize core systems (scene-dependent but player-independent)
                if (!_coreSystemsInitialized)
                {
                    InitCoreSystems();
                    _coreSystemsInitialized = true;
                }
                
                // PHASE 2B: Try to detect save file early
                TryLoadSaveFileData();
                
                // PHASE 2C: Start player detection
                StartPlayerDetection();
                
                Logger.Msg("🎮 [InitMain] Main scene initialization complete! Waiting for player detection...");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Main scene entry failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Handle re-entering main scene (loading different save)
        /// </summary>
        private static void OnReEnterMainScene()
        {
            try
            {
                Logger.Msg("[InitMain] 🔄 Re-entering main scene - refreshing save data");
                
                // Shutdown player-dependent systems for clean slate
                if (_playerDependentSystemsInitialized)
                {
                    ShutdownPlayerDependentSystems();
                }
                
                // Reload save file data
                TryLoadSaveFileData();
                
                // Restart player detection
                StartPlayerDetection();
                
                Logger.Msg("[InitMain] ✅ Main scene re-entry complete");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Main scene re-entry failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Handle exiting main scene (going back to menu)
        /// </summary>
        private static void OnExitMainScene()
        {
            try
            {
                Logger.Msg("[InitMain] 📤 Exiting main scene - performing cleanup");
                
                // PHASE 4: Cleanup - shutdown player-dependent systems
                ShutdownPlayerDependentSystems();
                
                // Unload save data
                SaveFileJsonDataStore.UnloadCurrentSave();
                
                // Stop player detection
                StopPlayerDetection();
                
                Logger.Msg("[InitMain] ✅ Main scene exit complete");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Error during main scene exit: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        #endregion

        #region System Initialization

        /// <summary>
        /// PHASE 2A: Initialize core systems that don't depend on player detection
        /// </summary>
        private static void InitCoreSystems()
        {
            Logger.Msg("[InitMain] 🔧 Initializing core PaxDrops systems...");
            
            try
            {
                // Core data systems (no player dependency)
                SaveFileJsonDataStore.Init();  // 💾 Save-file-aware JSON persistence
                SaveSystemPatch.Init();        // 🎣 Save system integration
                
                // Core game systems (no player dependency)
                DeadDrop.Init();              // ⚰️ Dead drop spawning system
                TierDropSystem.Init();        // 🎯 Tier-based drop system (basic init)
                TimeMonitor.Init();           // ⏰ Time monitoring for drops
                
                // Console/debugging (disabled for now)
                // CommandHandler.Init();     // 🎮 Console command system
                
                Logger.Msg("✅ [InitMain] Core systems initialized successfully!");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Core system initialization failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// PHASE 3: Initialize player-dependent systems (NPCs, messaging, etc.)
        /// </summary>
        private static void InitPlayerDependentSystems(Player player, ERank rank)
        {
            Logger.Msg($"[InitMain] 🚀 Initializing player-dependent systems for {player.PlayerName} (Rank: {rank})...");

            try
            {
                // Player-specific systems that require rank and save data
                DailyDropOrdering.Init();  // 📅 Daily drop ordering system (rank-based)
                
                // NPC and messaging systems (require player context)
                MrsStacksNPC.Init();       // 👤 Mrs. Stacks NPC integration (includes messaging)

                _playerDependentSystemsInitialized = true;

                Logger.Msg("✅ [InitMain] Player-dependent systems initialized!");
                Logger.Msg("🎯 [InitMain] Rank-based tier system active (11 tiers mapped 1:1 with ERank)");
                Logger.Msg("📅 [InitMain] Daily ordering system enabled - tier rewards based on player rank");
                Logger.Msg($"🎮 [InitMain] PaxDrops fully initialized for {player.PlayerName} (Rank: {rank})!");

                // Log final status
                LogSystemStatus();
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Player-dependent system initialization failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        #endregion

        #region Player Detection

        /// <summary>
        /// Start player detection systems
        /// </summary>
        private static void StartPlayerDetection()
        {
            try
            {
                // Start player detection
                PlayerDetection.StartDetection();

                // Subscribe to player detection events
                PlayerDetection.OnPlayerLoaded += OnPlayerDetected;
                PlayerDetection.OnPlayerRankLoaded += OnPlayerRankDetected;
                
                Logger.Msg("[InitMain] 👀 Player detection started");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Player detection start failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop player detection systems
        /// </summary>
        private static void StopPlayerDetection()
        {
            try
            {
                // Unsubscribe from events
                PlayerDetection.OnPlayerLoaded -= OnPlayerDetected;
                PlayerDetection.OnPlayerRankLoaded -= OnPlayerRankDetected;
                
                // IMPORTANT: Reset the PlayerDetection state so it can be restarted
                PlayerDetection.Reset();
                
                Logger.Msg("[InitMain] 👀 Player detection stopped and reset");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Player detection stop failed: {ex.Message}");
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
        /// PHASE 3 TRIGGER: Called when player rank data becomes available
        /// </summary>
        private static void OnPlayerRankDetected(Player player, ERank rank)
        {
            if (_playerDependentSystemsInitialized)
            {
                Logger.Msg($"[InitMain] ♻️ Player rank re-detected: {rank} (systems already initialized)");
                return; // Prevent double initialization
            }

            Logger.Msg($"[InitMain] 🎯 Player rank detected: {rank}");
            
            // PHASE 3: Initialize player-dependent systems
            InitPlayerDependentSystems(player, rank);
        }

        #endregion

        #region Shutdown

        /// <summary>
        /// Shutdown player-dependent systems when exiting scene or changing saves
        /// </summary>
        private static void ShutdownPlayerDependentSystems()
        {
            if (!_playerDependentSystemsInitialized) return;

            try
            {
                Logger.Msg("[InitMain] 🔌 Shutting down player-dependent systems...");
                
                // Shutdown NPC and messaging systems
                MrsStacksNPC.Shutdown();
                
                // Note: DailyDropOrdering doesn't need explicit shutdown (stateless)
                
                _playerDependentSystemsInitialized = false;
                
                Logger.Msg("[InitMain] ✅ Player-dependent systems shutdown complete");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Player-dependent shutdown error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shutdown core systems on application exit
        /// </summary>
        private static void ShutdownCoreSystemsOnExit()
        {
            if (!_coreSystemsInitialized) return;

            try
            {
                Logger.Msg("[InitMain] 🔌 Shutting down core systems...");
                
                // Shutdown core systems
                TimeMonitor.Shutdown();
                DeadDrop.Shutdown();
                SaveSystemPatch.Shutdown();
                SaveFileJsonDataStore.Shutdown();
                // CommandHandler.Shutdown();  // DISABLED FOR NOW
                
                _coreSystemsInitialized = false;
                
                Logger.Msg("[InitMain] ✅ Core systems shutdown complete");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Core shutdown error: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

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
        /// Log comprehensive system status
        /// </summary>
        private static void LogSystemStatus()
        {
            try
            {
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
                
                // Log system status
                Logger.Msg($"[InitMain] 🔧 Core Systems: {(_coreSystemsInitialized ? "✅ Active" : "❌ Inactive")}");
                Logger.Msg($"[InitMain] 👤 Player Systems: {(_playerDependentSystemsInitialized ? "✅ Active" : "❌ Inactive")}");
                Logger.Msg($"[InitMain] 🎬 Main Scene: {(_isInMainScene ? "✅ Active" : "❌ Inactive")}");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[InitMain] ❌ Status logging failed: {ex.Message}");
            }
        }

        #endregion
    }
} 