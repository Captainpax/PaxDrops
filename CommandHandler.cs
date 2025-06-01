using System;
using PaxDrops.Patches;

namespace PaxDrops
{
    /// <summary>
    /// Main command handler that manages initialization and registration of all PaxDrops console commands.
    /// Uses Harmony patches to intercept the console system for reliable command registration.
    /// </summary>
    public static class CommandHandler
    {
        private static bool _initialized;

        /// <summary>
        /// Initialize all console commands for PaxDrops
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;

            try
            {
                Logger.Msg("[CommandHandler] 🔧 Initializing PaxDrops console commands...");
                
                // Initialize console patches to intercept commands
                ConsolePatch.Init();
                
                _initialized = true;
                Logger.Msg("[CommandHandler] ✅ Console commands ready: paxdrop/pax, stacks/mrsstacks");
                Logger.Msg("[CommandHandler] 💡 Use 'paxdrop help' or 'stacks help' for command usage");
            }
            catch (Exception ex)
            {
                Logger.Error("[CommandHandler] ❌ Command handler initialization failed.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Shutdown all console commands and patches
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            try
            {
                ConsolePatch.Shutdown();
                _initialized = false;
                Logger.Msg("[CommandHandler] 🔌 Console commands shutdown");
            }
            catch (Exception ex)
            {
                Logger.Error("[CommandHandler] ❌ Command shutdown failed.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Get initialization status
        /// </summary>
        public static bool IsInitialized => _initialized;
    }
} 