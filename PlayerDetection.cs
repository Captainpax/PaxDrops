using System;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Levelling;

namespace PaxDrops
{
    /// <summary>
    /// Centralized player detection system that uses event-driven detection instead of polling.
    /// Hooks into Unity lifecycle events to detect when the player becomes available.
    /// </summary>
    public static class PlayerDetection
    {
        /// <summary>
        /// Event triggered when the player is successfully detected and validated
        /// </summary>
        public static event Action<Player>? OnPlayerLoaded;
        
        /// <summary>
        /// Event triggered when player rank data becomes available
        /// </summary>
        public static event Action<Player, ERank>? OnPlayerRankLoaded;

        /// <summary>
        /// Current detected player instance (null if not found yet)
        /// </summary>
        public static Player? CurrentPlayer { get; private set; }

        /// <summary>
        /// Current player's rank (Street_Rat if not detected yet)
        /// </summary>
        public static ERank CurrentRank { get; private set; } = ERank.Street_Rat;

        /// <summary>
        /// Whether the player has been successfully detected
        /// </summary>
        public static bool IsPlayerDetected => CurrentPlayer != null;

        /// <summary>
        /// Whether player rank data is available
        /// </summary>
        public static bool IsRankDetected { get; private set; } = false;

        private static bool _detectionStarted = false;
        private static bool _detectionComplete = false;
        private static int _checkAttempts = 0;

        /// <summary>
        /// Start the event-driven player detection system
        /// </summary>
        public static void StartDetection()
        {
            if (_detectionStarted)
            {
                Logger.Warn("Detection already started!", "PlayerDetection");
                return;
            }

            _detectionStarted = true;
            Logger.Debug("🕵️ Starting event-driven player detection...", "PlayerDetection");
            
            // Start with an initial check and fallback timer
            MelonCoroutines.Start(InitialDetectionCheck());
        }

        /// <summary>
        /// Initial detection check with fallback - only runs a few times instead of continuous polling
        /// </summary>
        private static IEnumerator InitialDetectionCheck()
        {
            Logger.Debug("🔍 Starting initial detection checks", "PlayerDetection");
            
            // Wait for scene to settle
            yield return new WaitForSeconds(1f);

            // Try detection immediately
            if (TryDetectPlayerComplete())
                yield break;

            // If not found, try a few more times with increasing delays
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                yield return new WaitForSeconds(attempt * 2f); // 2s, 4s, 6s, 8s, 10s
                
                Logger.Debug($"Detection attempt {attempt}/5", "PlayerDetection");
                if (TryDetectPlayerComplete())
                    yield break;
            }

            // If still not found after initial attempts, set up periodic checks (much less frequent)
            Logger.Debug("📡 Setting up periodic detection checks", "PlayerDetection");
            MelonCoroutines.Start(PeriodicDetectionFallback());
        }

        /// <summary>
        /// Fallback periodic checks - only every 10 seconds instead of 0.5 seconds
        /// </summary>
        private static IEnumerator PeriodicDetectionFallback()
        {
            while (!_detectionComplete && _checkAttempts < 30) // Max 5 minutes
            {
                yield return new WaitForSeconds(10f); // Much less frequent polling
                _checkAttempts++;
                
                Logger.Debug($"Periodic check {_checkAttempts}/30", "PlayerDetection");
                if (TryDetectPlayerComplete())
                    yield break;
            }

            if (!_detectionComplete)
            {
                Logger.Warn("⚠️ Player detection failed after all attempts", "PlayerDetection");
            }
        }

        /// <summary>
        /// Try to detect player and rank in one go
        /// </summary>
        private static bool TryDetectPlayerComplete()
        {
            try
            {
                var player = TryDetectPlayer();
                if (player == null) return false;

                Logger.Debug($"✅ Player detected: {player.PlayerName}", "PlayerDetection");
                CurrentPlayer = player;
                OnPlayerLoaded?.Invoke(player);

                var rank = TryDetectPlayerRank(player);
                Logger.Debug($"✅ Player rank detected: {rank}", "PlayerDetection");
                CurrentRank = rank;
                IsRankDetected = true;
                OnPlayerRankLoaded?.Invoke(player, rank);

                _detectionComplete = true;
                Logger.Debug("🎯 Player detection complete!", "PlayerDetection");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Detection attempt failed: {ex.Message}", "PlayerDetection");
                return false;
            }
        }

        /// <summary>
        /// Try to detect the player using multiple methods
        /// </summary>
        private static Player? TryDetectPlayer()
        {
            // Method 1: Player.Local
            try
            {
                var localPlayer = Player.Local;
                if (IsPlayerValid(localPlayer))
                {
                    Logger.Debug($"Found valid Player.Local: {localPlayer.PlayerName}", "PlayerDetection");
                    return localPlayer;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Player.Local failed: {ex.Message}", "PlayerDetection");
            }

            // Method 2: FindObjectOfType
            try
            {
                var players = UnityEngine.Object.FindObjectsOfType<Player>();
                if (players != null && players.Length > 0)
                {
                    foreach (var player in players)
                    {
                        if (IsPlayerValid(player) && player.IsLocalPlayer)
                        {
                            Logger.Debug($"Found valid local player via FindObjectsOfType: {player.PlayerName}", "PlayerDetection");
                            return player;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"FindObjectsOfType failed: {ex.Message}", "PlayerDetection");
            }

            return null;
        }

        /// <summary>
        /// Try to detect the player's current rank
        /// </summary>
        private static ERank TryDetectPlayerRank(Player player)
        {
            if (player == null) return ERank.Street_Rat;

            try
            {
                // Method 1: Try LevelManager component on player
                var playerLevelManager = player.gameObject.GetComponent<LevelManager>();
                if (playerLevelManager != null)
                {
                    var rank = playerLevelManager.Rank;
                    var totalXP = playerLevelManager.TotalXP;
                    var tier = playerLevelManager.Tier;
                    
                    Logger.Debug($"Player LevelManager: Rank={rank}, TotalXP={totalXP}, Tier={tier}", "PlayerDetection");
                    
                    if (rank != ERank.Street_Rat || totalXP > 0 || tier > 1)
                    {
                        return rank;
                    }
                }

                // Method 2: Try LevelManager.Instance
                var globalLevelManager = LevelManager.Instance;
                if (globalLevelManager != null)
                {
                    // Try GetFullRank first
                    try
                    {
                        var fullRank = globalLevelManager.GetFullRank();
                        Logger.Debug($"GlobalLM GetFullRank: Rank={fullRank.Rank}, Tier={fullRank.Tier}", "PlayerDetection");
                        
                        if (fullRank.Rank != ERank.Street_Rat || fullRank.Tier > 1)
                        {
                            return fullRank.Rank;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Debug($"GetFullRank failed: {ex.Message}", "PlayerDetection");
                    }

                    // Try direct properties
                    var rank = globalLevelManager.Rank;
                    var totalXP = globalLevelManager.TotalXP;
                    var tier = globalLevelManager.Tier;
                    
                    Logger.Debug($"GlobalLM Direct: Rank={rank}, TotalXP={totalXP}, Tier={tier}", "PlayerDetection");
                    
                    if (totalXP > 1000 || tier > 1)
                    {
                        // Try calculating from XP
                        try
                        {
                            var calculatedRank = globalLevelManager.GetFullRank(totalXP);
                            Logger.Debug($"Calculated from XP: Rank={calculatedRank.Rank}", "PlayerDetection");
                            return calculatedRank.Rank;
                        }
                        catch (Exception ex)
                        {
                            Logger.Debug($"XP calculation failed: {ex.Message}", "PlayerDetection");
                        }
                    }
                    
                    return rank;
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Rank detection failed: {ex.Message}", "PlayerDetection");
            }

            return ERank.Street_Rat;
        }

        /// <summary>
        /// Manual check for player - can be called from external events
        /// </summary>
        public static void CheckForPlayer()
        {
            if (_detectionComplete) return;

            Logger.Debug("🔍 Manual player check triggered", "PlayerDetection");
            TryDetectPlayerComplete();
        }

        /// <summary>
        /// Validate that a player instance is properly initialized
        /// </summary>
        private static bool IsPlayerValid(Player? player)
        {
            try
            {
                return player != null && 
                       player.gameObject != null && 
                       player.gameObject.activeInHierarchy &&
                       !string.IsNullOrEmpty(player.PlayerName);
            }
            catch (Exception ex)
            {
                Logger.Debug($"Player validation failed: {ex.Message}", "PlayerDetection");
                return false;
            }
        }

        /// <summary>
        /// Force refresh player rank (useful for testing or manual refresh)
        /// </summary>
        public static void RefreshPlayerRank()
        {
            if (CurrentPlayer != null)
            {
                Logger.Debug("🔄 Refreshing player rank...", "PlayerDetection");
                var newRank = TryDetectPlayerRank(CurrentPlayer);
                
                if (newRank != CurrentRank)
                {
                    Logger.Debug($"📈 Rank changed: {CurrentRank} → {newRank}", "PlayerDetection");
                    CurrentRank = newRank;
                    OnPlayerRankLoaded?.Invoke(CurrentPlayer, newRank);
                }
                else
                {
                    Logger.Debug($"Rank unchanged: {CurrentRank}", "PlayerDetection");
                }
            }
            else
            {
                Logger.Warn("⚠️ Cannot refresh rank - no player detected", "PlayerDetection");
            }
        }

        /// <summary>
        /// Get player info summary for debugging
        /// </summary>
        public static string GetPlayerInfo()
        {
            if (!IsPlayerDetected)
            {
                return "No player detected";
            }

            try
            {
                return $"Player: {CurrentPlayer!.PlayerName} | Rank: {CurrentRank} | Valid: {IsPlayerValid(CurrentPlayer!)}";
            }
            catch (Exception ex)
            {
                return $"Player info error: {ex.Message}";
            }
        }

        /// <summary>
        /// Reset detection state (for testing)
        /// </summary>
        public static void Reset()
        {
            Logger.Debug("🔄 Resetting detection state", "PlayerDetection");
            _detectionStarted = false;
            _detectionComplete = false;
            _checkAttempts = 0;
            CurrentPlayer = null;
            CurrentRank = ERank.Street_Rat;
            IsRankDetected = false;
        }
    }
} 