using System;
using System.Collections;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Levelling;

namespace PaxDrops
{
    /// <summary>
    /// Centralized player detection system that continuously monitors for player availability.
    /// Uses events to notify other systems when the player is ready.
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

        /// <summary>
        /// Start the player detection coroutine
        /// </summary>
        public static void StartDetection()
        {
            if (_detectionStarted)
            {
                Logger.Warn("[PlayerDetection] Detection already started!");
                return;
            }

            _detectionStarted = true;
            Logger.Msg("[PlayerDetection] 🕵️ Starting player detection...");
            
            // Start the detection coroutine
            MelonCoroutines.Start(DetectionCoroutine());
        }

        /// <summary>
        /// Coroutine that continuously tries to detect the player
        /// </summary>
        private static IEnumerator DetectionCoroutine()
        {
            Logger.Msg("[PlayerDetection] 🔍 Detection coroutine started");
            
            // Wait a bit for the scene to fully load
            yield return new WaitForSeconds(2f);

            int attempts = 0;
            const int maxAttempts = 100; // Try for ~50 seconds (500ms intervals)

            while (!_detectionComplete && attempts < maxAttempts)
            {
                attempts++;
                
                try
                {
                    Logger.LogDebug($"[PlayerDetection] Detection attempt {attempts}/{maxAttempts}");
                    
                    // Try to find the player
                    var player = TryDetectPlayer();
                    if (player != null)
                    {
                        Logger.Msg($"[PlayerDetection] ✅ Player detected: {player.PlayerName}");
                        CurrentPlayer = player;
                        
                        // Notify systems that player is loaded
                        OnPlayerLoaded?.Invoke(player);
                        
                        // Try to get rank data
                        var rank = TryDetectPlayerRank(player);
                        if (rank != ERank.Street_Rat)
                        {
                            Logger.Msg($"[PlayerDetection] ✅ Player rank detected: {rank}");
                            CurrentRank = rank;
                            IsRankDetected = true;
                            OnPlayerRankLoaded?.Invoke(player, rank);
                            
                            _detectionComplete = true;
                            Logger.Msg("[PlayerDetection] 🎯 Player detection complete!");
                            yield break;
                        }
                        else
                        {
                            Logger.LogDebug("[PlayerDetection] Player found but rank not available yet, continuing...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug($"[PlayerDetection] Detection attempt {attempts} failed: {ex.Message}");
                }

                // Wait before next attempt
                yield return new WaitForSeconds(0.5f);
            }

            if (!_detectionComplete)
            {
                Logger.Warn($"[PlayerDetection] ⚠️ Player detection failed after {maxAttempts} attempts");
                
                // Even if rank detection failed, if we have a player, use it
                if (CurrentPlayer != null)
                {
                    Logger.Msg("[PlayerDetection] 📝 Using detected player with fallback rank");
                    IsRankDetected = true; // Mark as detected even with fallback
                    OnPlayerRankLoaded?.Invoke(CurrentPlayer, CurrentRank);
                }
            }
        }

        /// <summary>
        /// Try to detect the player using multiple methods
        /// </summary>
        private static Player TryDetectPlayer()
        {
            // Method 1: Player.Local
            try
            {
                var localPlayer = Player.Local;
                if (IsPlayerValid(localPlayer))
                {
                    Logger.LogDebug($"[PlayerDetection] Found valid Player.Local: {localPlayer.PlayerName}");
                    return localPlayer;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[PlayerDetection] Player.Local failed: {ex.Message}");
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
                            Logger.LogDebug($"[PlayerDetection] Found valid local player via FindObjectsOfType: {player.PlayerName}");
                            return player;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[PlayerDetection] FindObjectsOfType failed: {ex.Message}");
            }

            return null!;
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
                    
                    Logger.LogDebug($"[PlayerDetection] Player LevelManager: Rank={rank}, TotalXP={totalXP}, Tier={tier}");
                    
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
                        Logger.LogDebug($"[PlayerDetection] GlobalLM GetFullRank: Rank={fullRank.Rank}, Tier={fullRank.Tier}");
                        
                        if (fullRank.Rank != ERank.Street_Rat || fullRank.Tier > 1)
                        {
                            return fullRank.Rank;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug($"[PlayerDetection] GetFullRank failed: {ex.Message}");
                    }

                    // Try direct properties
                    var rank = globalLevelManager.Rank;
                    var totalXP = globalLevelManager.TotalXP;
                    var tier = globalLevelManager.Tier;
                    
                    Logger.LogDebug($"[PlayerDetection] GlobalLM Direct: Rank={rank}, TotalXP={totalXP}, Tier={tier}");
                    
                    if (totalXP > 1000 || tier > 1)
                    {
                        // Try calculating from XP
                        try
                        {
                            var calculatedRank = globalLevelManager.GetFullRank(totalXP);
                            Logger.LogDebug($"[PlayerDetection] Calculated from XP: Rank={calculatedRank.Rank}");
                            return calculatedRank.Rank;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogDebug($"[PlayerDetection] XP calculation failed: {ex.Message}");
                        }
                    }
                    
                    return rank;
                }

                // Method 3: Try finding all LevelManagers
                var allLevelManagers = UnityEngine.Object.FindObjectsOfType<LevelManager>();
                Logger.LogDebug($"[PlayerDetection] Found {allLevelManagers?.Length ?? 0} LevelManager instances");
                
                if (allLevelManagers != null && allLevelManagers.Length > 0)
                {
                    foreach (var lm in allLevelManagers)
                    {
                        if (lm != null)
                        {
                            var rank = lm.Rank;
                            var xp = lm.TotalXP;
                            var tier = lm.Tier;
                            Logger.LogDebug($"[PlayerDetection] LM instance: Rank={rank}, TotalXP={xp}, Tier={tier}");
                            
                            if (rank != ERank.Street_Rat || xp > 0 || tier > 1)
                            {
                                Logger.LogDebug($"[PlayerDetection] Using LM instance with data: {rank}");
                                return rank;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[PlayerDetection] Rank detection failed: {ex.Message}");
            }

            return ERank.Street_Rat;
        }

        /// <summary>
        /// Validate that a player instance is properly initialized
        /// </summary>
        private static bool IsPlayerValid(Player player)
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
                Logger.LogDebug($"[PlayerDetection] Player validation failed: {ex.Message}");
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
                Logger.Msg("[PlayerDetection] 🔄 Refreshing player rank...");
                var newRank = TryDetectPlayerRank(CurrentPlayer);
                
                if (newRank != CurrentRank)
                {
                    Logger.Msg($"[PlayerDetection] 📈 Rank changed: {CurrentRank} → {newRank}");
                    CurrentRank = newRank;
                    OnPlayerRankLoaded?.Invoke(CurrentPlayer, newRank);
                }
                else
                {
                    Logger.Msg($"[PlayerDetection] Rank unchanged: {CurrentRank}");
                }
            }
            else
            {
                Logger.Warn("[PlayerDetection] ⚠️ Cannot refresh rank - no player detected");
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
            Logger.Msg("[PlayerDetection] 🔄 Resetting detection state");
            _detectionStarted = false;
            _detectionComplete = false;
            CurrentPlayer = null;
            CurrentRank = ERank.Street_Rat;
            IsRankDetected = false;
        }
    }
} 