using UnityEngine;
using MelonLoader;
using PaxDrops.MrStacks;
using PaxDrops.Patches;
using PaxDrops.Runtime;
using System.Collections;

[assembly: MelonInfo(typeof(PaxDrops.InitMain), "PaxDrops", "1.0.0", "Pax")]
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
                // Initialize runtime detection first
                RuntimeEnvironment.Initialize();
                MelonLogger.Msg($"🔍 Runtime detected: {RuntimeEnvironment.RuntimeType}");
                
                // Initialize only basic logging system
                Logger.Init();
                
                // Initialize the unified API provider
                GameAPIProvider.Instance.Initialize();
                
                Logger.Info("⚙️ Application initialization complete", "InitMain");
                Logger.Info($"📋 Runtime: {RuntimeEnvironment.RuntimeType} - Waiting for main scene...", "InitMain");
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
                Logger.Info($"🎬 Main scene loaded (Scene: {sceneName}, Index: {buildIndex})", "InitMain");
                _isInMainScene = true;
                OnEnterMainScene();
            }
            else if (_isInMainScene && !isMainScene)
            {
                // Exiting main scene (going back to menu)
                Logger.Info($"🚪 Exiting main scene to {sceneName} (Index: {buildIndex})", "InitMain");
                OnExitMainScene();
                _isInMainScene = false;
            }
            else if (isMainScene && _isInMainScene)
            {
                // Re-entering main scene (loading different save)
                Logger.Info($"🔄 Re-entering main scene (Scene: {sceneName}, Index: {buildIndex})", "InitMain");
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
                Logger.Debug("🏗️ Bootstrapping PaxDrops for main scene...", "InitMain");
                
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
                
                Logger.Info("🎮 Main scene initialization complete! Waiting for player detection...", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Main scene entry failed: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
            }
        }

        /// <summary>
        /// Handle re-entering main scene (loading different save)
        /// </summary>
        private static void OnReEnterMainScene()
        {
            try
            {
                Logger.Debug("🔄 Re-entering main scene - refreshing save data", "InitMain");
                
                // Shutdown player-dependent systems for clean slate
                if (_playerDependentSystemsInitialized)
                {
                    ShutdownPlayerDependentSystems();
                }
                
                // Reload save file data
                TryLoadSaveFileData();
                
                // Restart player detection
                StartPlayerDetection();
                
                Logger.Info("✅ Main scene re-entry complete", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Main scene re-entry failed: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
            }
        }

        /// <summary>
        /// Handle exiting main scene (going back to menu)
        /// </summary>
        private static void OnExitMainScene()
        {
            try
            {
                Logger.Debug("📤 Exiting main scene - performing cleanup", "InitMain");
                
                // PHASE 4: Cleanup - shutdown player-dependent systems
                ShutdownPlayerDependentSystems();
                
                // Unload save data
                SaveFileJsonDataStore.UnloadCurrentSave();
                
                // Stop player detection
                StopPlayerDetection();
                
                Logger.Info("✅ Main scene exit complete", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Error during main scene exit: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
            }
        }

        #endregion

        #region System Initialization

        /// <summary>
        /// PHASE 2A: Initialize core systems that don't depend on player detection
        /// </summary>
        private static void InitCoreSystems()
        {
            Logger.Info("🔧 Initializing core PaxDrops systems...", "InitMain");
            
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
                
                Logger.Info("✅ Core systems initialized successfully!", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Core system initialization failed: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
            }
        }

        /// <summary>
        /// PHASE 3: Initialize player-dependent systems (NPCs, messaging, etc.)
        /// </summary>
        private static void InitPlayerDependentSystems(string playerName, string rank)
        {
            Logger.Info($"🚀 Initializing player-dependent systems for {playerName} (Rank: {rank})...", "InitMain");

            try
            {
                // Player-specific systems that require rank and save data
                DailyDropOrdering.Init();  // 📅 Daily drop ordering system (rank-based)
                
                // NPC and messaging systems (require player context)
                MrsStacksNPC.Init();       // 👤 Mrs. Stacks NPC integration (includes messaging)

                _playerDependentSystemsInitialized = true;

                Logger.Debug("✅ Player-dependent systems initialized!", "InitMain");
                Logger.Debug("🎯 Rank-based tier system active (11 tiers mapped 1:1 with ERank)", "InitMain");
                Logger.Debug("📅 Daily ordering system enabled - tier rewards based on player rank", "InitMain");
                Logger.Debug($"🎮 PaxDrops fully initialized for {playerName} (Rank: {rank})!", "InitMain");

                // Log final status
                LogSystemStatus();
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Player-dependent system initialization failed: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
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
                // TODO: Implement new PlayerDetection using runtime abstraction
                // PlayerDetection.StartDetection();

                // Subscribe to player detection events
                // PlayerDetection.OnPlayerLoaded += OnPlayerDetected;
                // PlayerDetection.OnPlayerRankLoaded += OnPlayerRankDetected;
                
                // For now, simulate player detection for testing
                MelonCoroutines.Start(SimulatePlayerDetection());
                
                Logger.Info("👀 Player detection started (simulated)", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Player detection start failed: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
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
                // PlayerDetection.OnPlayerLoaded -= OnPlayerDetected;
                // PlayerDetection.OnPlayerRankLoaded -= OnPlayerRankDetected;
                
                // IMPORTANT: Reset the PlayerDetection state so it can be restarted
                // PlayerDetection.Reset();
                
                Logger.Info("👀 Player detection stopped and reset", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Player detection stop failed: {ex.Message}", "InitMain");
            }
        }

        /// <summary>
        /// Called when the player is first detected
        /// </summary>
        private static void OnPlayerDetected(string playerName)
        {
            Logger.Info($"👤 Player detected: {playerName}", "InitMain");
            Logger.Info("Waiting for rank data...", "InitMain");
        }

        /// <summary>
        /// PHASE 3 TRIGGER: Called when player rank data becomes available
        /// </summary>
        private static void OnPlayerRankDetected(string playerName, string rank)
        {
            if (_playerDependentSystemsInitialized)
            {
                Logger.Info($"♻️ Player rank re-detected: {rank} (systems already initialized)", "InitMain");
                return; // Prevent double initialization
            }

            Logger.Info($"🎯 Player rank detected: {rank}", "InitMain");
            
            // PHASE 3: Initialize player-dependent systems
            InitPlayerDependentSystems(playerName, rank);
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
                Logger.Info("🔌 Shutting down player-dependent systems...", "InitMain");
                
                // Shutdown NPC and messaging systems
                MrsStacksNPC.Shutdown();
                
                // Note: DailyDropOrdering doesn't need explicit shutdown (stateless)
                
                _playerDependentSystemsInitialized = false;
                
                Logger.Info("✅ Player-dependent systems shutdown complete", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Player-dependent shutdown error: {ex.Message}", "InitMain");
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
                Logger.Info("🔌 Shutting down core systems...", "InitMain");
                
                // Shutdown core systems
                TimeMonitor.Shutdown();
                DeadDrop.Shutdown();
                SaveSystemPatch.Shutdown();
                SaveFileJsonDataStore.Shutdown();
                // CommandHandler.Shutdown();  // DISABLED FOR NOW
                
                _coreSystemsInitialized = false;
                
                Logger.Info("✅ Core systems shutdown complete", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Core shutdown error: {ex.Message}", "InitMain");
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
                var saveProvider = GameAPIProvider.Instance.SaveSystem;
                if (saveProvider != null)
                {
                    string? savePath = saveProvider.GetPlayersSavePath();
                    string? saveName = saveProvider.GetSaveName();
                    
                    if (!string.IsNullOrEmpty(savePath) && !string.IsNullOrEmpty(saveName))
                    {
                        Logger.Info($"📂 Loading data for save: {saveName} at {savePath}", "InitMain");
                        SaveFileJsonDataStore.LoadForSaveFile(savePath, saveName);
                    }
                    else
                    {
                        Logger.Warn("⚠️ Could not identify current save file - using default", "InitMain");
                        SaveFileJsonDataStore.LoadForSaveFile("", "default");
                    }
                }
                else
                {
                    Logger.Warn("⚠️ SaveManager not available - will retry later", "InitMain");
                    // We'll rely on the save system patch to catch saves later
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Failed to load save file data: {ex.Message}", "InitMain");
                Logger.Exception(ex, "InitMain");
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
                Logger.Debug($"Player Status: {PaxDrops.Configs.DropConfig.GetPlayerDetectionStatus()}", "InitMain");
                
                // Log save file status
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                if (isLoaded)
                {
                    Logger.Debug($"💾 Save File: {saveName} (ID: {saveId}, Steam: {steamId})", "InitMain");
                }
                else
                {
                    Logger.Warn("⚠️ No save file data loaded", "InitMain");
                }
                
                // Log system status
                Logger.Debug($"🔧 Core Systems: {(_coreSystemsInitialized ? "✅ Active" : "❌ Inactive")}", "InitMain");
                Logger.Debug($"👤 Player Systems: {(_playerDependentSystemsInitialized ? "✅ Active" : "❌ Inactive")}", "InitMain");
                Logger.Debug($"🎬 Main Scene: {(_isInMainScene ? "✅ Active" : "❌ Inactive")}", "InitMain");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"❌ Status logging failed: {ex.Message}", "InitMain");
            }
        }

        /// <summary>
        /// Temporary simulation of player detection for testing
        /// </summary>
        private static IEnumerator SimulatePlayerDetection()
        {
            yield return new WaitForSeconds(3.0f);
            
            Logger.Info("🧪 Simulating player detection for testing...", "InitMain");
            
            string playerName = "TestPlayer";
            string rank = "Street_Rat";
            
            // Simulate detecting a player using the runtime abstraction
            try
            {
                var playerProvider = GameAPIProvider.Instance.Player;
                playerName = playerProvider.GetPlayerName();
                rank = playerProvider.GetPlayerRank();
            }
            catch (System.Exception ex)
            {
                Logger.Warn($"🧪 Player simulation failed: {ex.Message}", "InitMain");
                // Keep fallback values set above
            }
            
            OnPlayerDetected(playerName);
            yield return new WaitForSeconds(1.0f);
            OnPlayerRankDetected(playerName, rank);
        }

        #endregion
    }
} 