using System;
using HarmonyLib;
using PaxDrops.Commands;

namespace PaxDrops.Patches
{
    /// <summary>
    /// Harmony patches for intercepting console commands to add PaxDrops functionality.
    /// Uses prefix patches to intercept commands before they reach the original console system.
    /// </summary>
    [HarmonyPatch]
    public static class ConsolePatch
    {
        private static HarmonyLib.Harmony? _harmony;
        private static bool _initialized;

        /// <summary>
        /// Initialize the console patches
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;

            try
            {
                Logger.Info("🔧 Setting up console command interception...", "ConsolePatch");
                SetupHarmonyPatches();
                _initialized = true;
                Logger.Info("✅ Console patches ready", "ConsolePatch");
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Console patch initialization failed.", "ConsolePatch");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Set up Harmony patches to intercept console commands
        /// </summary>
        private static void SetupHarmonyPatches()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony("PaxDrops.ConsolePatch");
                
                var consoleType = typeof(Il2CppScheduleOne.Console);
                
                // Patch Console.SubmitCommand(List<string>) to add our own commands
                var submitCommandListMethod = consoleType.GetMethod("SubmitCommand", 
                    new[] { typeof(Il2CppSystem.Collections.Generic.List<string>) });
                if (submitCommandListMethod != null)
                {
                    var patchMethod = typeof(ConsolePatch).GetMethod(nameof(SubmitCommandListPrefix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(submitCommandListMethod, prefix: new HarmonyMethod(patchMethod));
                    Logger.Debug("⚙️ Console.SubmitCommand(List<string>) patch applied", "ConsolePatch");
                }
                else
                {
                    Logger.Error("❌ Could not find Console.SubmitCommand(List<string>) method", "ConsolePatch");
                }
                
                // Also patch Console.SubmitCommand(string) version
                var submitCommandStringMethod = consoleType.GetMethod("SubmitCommand", new[] { typeof(string) });
                if (submitCommandStringMethod != null)
                {
                    var patchStringMethod = typeof(ConsolePatch).GetMethod(nameof(SubmitCommandStringPrefix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(submitCommandStringMethod, prefix: new HarmonyMethod(patchStringMethod));
                    Logger.Debug("⚙️ Console.SubmitCommand(string) patch applied", "ConsolePatch");
                }
                else
                {
                    Logger.Error("❌ Could not find Console.SubmitCommand(string) method", "ConsolePatch");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Harmony patch setup failed: {ex.Message}", "ConsolePatch");
            }
        }

        /// <summary>
        /// Harmony prefix patch - intercepts console commands (List version) to add our own PaxDrop commands
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Il2CppScheduleOne.Console), "SubmitCommand", typeof(Il2CppSystem.Collections.Generic.List<string>))]
        private static bool SubmitCommandListPrefix(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                Logger.Debug($"🔍 List patch called with {args?.Count ?? 0} args", "ConsolePatch");
                if (args == null || args.Count == 0) return true;

                string command = args[0].ToLower();
                Logger.Debug($"🔍 Processing command: '{command}'", "ConsolePatch");
                
                // Handle our custom commands
                switch (command)
                {
                    case "paxdrop":
                    case "pax":
                        PaxDropCommand.Execute(args);
                        return false; // Skip original processing
                        
                    case "stacks":
                    case "mrstacks":
                        StacksCommand.Execute(args);
                        return false; // Skip original processing
                        
                    default:
                        return true; // Allow normal command processing
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Console command List patch error: {ex.Message}", "ConsolePatch");
                return true; // Continue with original on error
            }
        }

        /// <summary>
        /// Harmony prefix patch - intercepts console commands (string version) to add our own PaxDrop commands
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Il2CppScheduleOne.Console), "SubmitCommand", typeof(string))]
        private static bool SubmitCommandStringPrefix(string args)
        {
            try
            {
                Logger.Debug($"🔍 String patch called with: '{args ?? "null"}'", "ConsolePatch");
                if (string.IsNullOrEmpty(args)) return true;

                // Split the command string into parts
                var argList = new Il2CppSystem.Collections.Generic.List<string>();
                var parts = args.Split(' ');
                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part))
                        argList.Add(part.Trim());
                }

                string command = parts[0].ToLower();
                Logger.Debug($"🔍 Processing string command: '{command}'", "ConsolePatch");
                
                // Handle our custom commands
                switch (command)
                {
                    case "paxdrop":
                    case "pax":
                        PaxDropCommand.Execute(argList);
                        return false; // Skip original processing
                        
                    case "stacks":
                    case "mrstacks":
                        StacksCommand.Execute(argList);
                        return false; // Skip original processing
                        
                    default:
                        return true; // Allow normal command processing
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Console command string patch error: {ex.Message}", "ConsolePatch");
                return true; // Continue with original on error
            }
        }

        /// <summary>
        /// Shutdown the console patches
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            _harmony?.UnpatchSelf();
            _harmony = null;
            _initialized = false;
            
            Logger.Info("🔌 Console patches shutdown", "ConsolePatch");
        }
    }
} 
