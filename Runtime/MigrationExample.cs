using System;
using UnityEngine;
using MelonLoader;
using PaxDrops.Runtime;
using PaxDrops.Runtime.Abstractions;

namespace PaxDrops.Examples
{
    /// <summary>
    /// Example showing how to migrate existing IL2CPP-specific code to use the unified runtime abstraction.
    /// This demonstrates the pattern for updating your existing PaxDrops code.
    /// </summary>
    public static class MigrationExample
    {
        // OLD IL2CPP-specific code:
        /*
        public static void OldGetGameTime()
        {
            var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
            if (timeManager != null)
            {
                var currentTime = timeManager.CurrentTime;
                Logger.Info($"Current game time: Day {currentTime.Day}, {currentTime.Hour:00}:{currentTime.Minute:00}");
            }
        }
        */

        // NEW unified code that works with both IL2CPP and Mono:
        public static void NewGetGameTime()
        {
            var gameAPI = GameAPIProvider.Instance;
            var gameTime = gameAPI.GameTime;
            
            if (gameTime.IsAvailable())
            {
                var formattedTime = gameTime.GetFormattedTime();
                var currentDay = gameTime.GetCurrentDay();
                var currentHour = gameTime.GetCurrentHour();
                
                Logger.Info($"Runtime: {gameAPI.RuntimeType}");
                Logger.Info($"Current game time: {formattedTime}");
                Logger.Info($"Detailed: Day {currentDay}, Hour {currentHour}");
            }
            else
            {
                Logger.Info("Game time system not available", "MigrationExample");
            }
        }

        // OLD IL2CPP-specific player code:
        /*
        public static bool OldCheckPlayerRank()
        {
            var levelManager = Il2CppScheduleOne.Levelling.LevelManager.Instance;
            if (levelManager != null)
            {
                var rank = levelManager.Rank;
                return rank >= Il2CppScheduleOne.Levelling.ERank.Shot_Caller;
            }
            return false;
        }
        */

        // NEW unified player code:
        public static bool NewCheckPlayerRank()
        {
            var gameAPI = GameAPIProvider.Instance;
            var player = gameAPI.Player;
            
            if (player.IsPlayerAvailable())
            {
                var rank = player.GetPlayerRank();
                var position = player.GetPlayerPosition();
                
                Logger.Info($"Player available at position: {position}");
                Logger.Info($"Player rank: {rank}");
                
                // Check if player has a high rank (simple string comparison for now)
                return rank != null && !rank.Equals("Street_Rat", StringComparison.OrdinalIgnoreCase);
            }
            
            return false;
        }

        // OLD IL2CPP-specific dead drop code:
        /*
        public static void OldSpawnDeadDrop()
        {
            var deadDrops = UnityEngine.Object.FindObjectsOfType<Il2CppScheduleOne.Economy.DeadDrop>();
            foreach (var deadDrop in deadDrops)
            {
                var storage = deadDrop.StorageEntity;
                if (storage != null)
                {
                    var cashDef = Il2CppScheduleOne.Registry.GetItem<Il2CppScheduleOne.Money.CashDefinition>("cash");
                    if (cashDef != null)
                    {
                        var cashInstance = cashDef.GetDefaultInstance(1000);
                        storage.AddItem(cashInstance);
                    }
                }
            }
        }
        */

        // NEW unified dead drop code:
        public static void NewSpawnDeadDrop()
        {
            var gameAPI = GameAPIProvider.Instance;
            var deadDropProvider = gameAPI.DeadDrop;
            var storageProvider = gameAPI.Storage;
            
            if (deadDropProvider.IsAvailable() && storageProvider.IsAvailable())
            {
                var deadDrops = deadDropProvider.GetAllDeadDrops();
                Logger.Info($"Found {deadDrops.Length} dead drops ({gameAPI.RuntimeType})");
                
                foreach (var deadDrop in deadDrops)
                {
                    var storage = deadDropProvider.GetStorageEntity(deadDrop);
                    if (storage != null)
                    {
                        // Add cash using unified interface
                        var success = storageProvider.AddCashToStorage(storage, 1000);
                        if (success)
                        {
                            Logger.Info($"Added $1000 to dead drop storage");
                        }
                        else
                        {
                            Logger.Error("Failed to add cash to storage");
                        }
                    }
                }
            }
            else
            {
                Logger.Info("Dead drop or storage system not available", "MigrationExample");
            }
        }

        // OLD IL2CPP-specific console command:
        /*
        public static void OldExecuteConsoleCommand()
        {
            var console = Il2CppScheduleOne.Console.Instance;
            if (console != null)
            {
                var args = new Il2CppSystem.Collections.Generic.List<string>();
                args.Add("paxdrop");
                args.Add("spawn");
                console.SubmitCommand(args);
            }
        }
        */

        // NEW unified console command:
        public static void NewExecuteConsoleCommand()
        {
            var gameAPI = GameAPIProvider.Instance;
            var console = gameAPI.Console;
            
            if (console.IsAvailable())
            {
                var args = new System.Collections.Generic.List<string> { "paxdrop", "spawn" };
                console.ExecuteCommand(args);
                Logger.Info($"Executed console command via {gameAPI.RuntimeType} runtime");
            }
            else
            {
                Logger.Info("Console system not available", "MigrationExample");
            }
        }

        /// <summary>
        /// Example of runtime diagnostic information
        /// </summary>
        public static void ShowRuntimeDiagnostics()
        {
            var gameAPI = GameAPIProvider.Instance;
            
            Logger.Info("=== PaxDrops Runtime Diagnostics ===");
            Logger.Info(gameAPI.GetDiagnosticInfo());
            Logger.Info("====================================");
        }

        /// <summary>
        /// Example of conditional runtime-specific code when you absolutely need it
        /// </summary>
        public static void ConditionalRuntimeCode()
        {
            if (RuntimeEnvironment.IsIL2CPP)
            {
                Logger.Info("Running IL2CPP-specific optimizations...");
                // Use IL2CPP-specific optimizations here
#if !MONO_BUILD
                // IL2CPP-specific code that won't compile in Mono builds
#endif
            }
            else
            {
                Logger.Info("Running Mono-specific code paths...");
                // Use Mono-specific approaches here
            }
        }
    }
}
