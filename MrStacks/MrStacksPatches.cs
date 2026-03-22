using System;
using HarmonyLib;
using MelonLoader;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Economy;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Harmony patches for Mr. Stacks functionality.
    /// Handles supplier interaction interception (no more invasive save/load patches).
    /// </summary>
    public static class MrStacksPatches
    {
        private static HarmonyLib.Harmony? _harmony;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize Harmony patches for Mr. Stacks
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                _harmony = new HarmonyLib.Harmony("PaxDrops.MrStacksPatches");

                // Patch supplier dead drop requests
                var supplierType = typeof(Supplier);
                var deadDropMethod = supplierType.GetMethod("DeaddropRequested");
                if (deadDropMethod != null)
                {
                    var prefix = typeof(MrStacksPatches).GetMethod(nameof(DeaddropRequestedPrefix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(deadDropMethod, prefix: new HarmonyMethod(prefix));
                    Logger.Info("⚙️ DeaddropRequested patch applied", "MrStacksPatches");
                }

                // Patch conversation creation
                var createConversationMethod = supplierType.GetMethod("CreateMessageConversation");
                if (createConversationMethod != null)
                {
                    var conversationPatchMethod = typeof(MrStacksPatches).GetMethod(nameof(CreateMessageConversationPostfix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(createConversationMethod, postfix: new HarmonyMethod(conversationPatchMethod));
                    Logger.Info("⚙️ CreateMessageConversation patch applied", "MrStacksPatches");
                }

                Logger.Info("✅ Essential patches initialized (save/load patches removed)", "MrStacksPatches");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Harmony patch setup failed: {ex.Message}", "MrStacksPatches");
            }
        }

        /// <summary>
        /// Harmony prefix patch - intercepts Mr. Stacks dead drop requests to bypass shop interface
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Supplier), "DeaddropRequested")]
        private static bool DeaddropRequestedPrefix(Supplier __instance)
        {
            try
            {
                if (IsMrStacks(__instance))
                {
                    Logger.Debug("🛑 Intercepted Mr. Stacks dead drop request - bypassing shop interface", "MrStacksPatches");
                    
                    // Open or refresh the native tier picker instead of auto-ordering.
                    MrStacksMessaging.ShowHomeMenu(__instance);
                    Logger.Debug("Mr. Stacks tier picker refreshed", "MrStacksPatches");

                    // Inform the player that bypass worked
                    Logger.Debug("✅ Mr. Stacks dead drop order bypassed shop interface", "MrStacksPatches");
                    
                    return false; // Skip original method
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ DeaddropRequested patch error: {ex.Message}", "MrStacksPatches");
            }
            
            return true; // Continue with original method for other suppliers
        }

        /// <summary>
        /// Harmony postfix patch - customizes conversation options after creation
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Supplier), "CreateMessageConversation")]
        private static void CreateMessageConversationPostfix(Supplier __instance)
        {
            try
            {
                // Only customize Mr. Stacks conversations
                if (IsMrStacks(__instance))
                {
                    Logger.Debug("🎛️ Customizing Mr. Stacks conversation options", "MrStacksPatches");
                    MelonCoroutines.Start(MrStacksMessaging.CustomizeConversationAfterDelay(__instance));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ CreateMessageConversation patch error: {ex.Message}", "MrStacksPatches");
            }
        }

        /// <summary>
        /// Check if a supplier is Mr. Stacks
        /// </summary>
        private static bool IsMrStacks(Supplier supplier)
        {
            return supplier.ID == "mr_stacks_001" || 
                   (supplier.FirstName == "Mr." && supplier.LastName == "Stacks");
        }

        /// <summary>
        /// Shutdown the Mr. Stacks patches
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            _harmony?.UnpatchSelf();
            _harmony = null;
            _initialized = false;
            
            Logger.Info("🔌 Mr. Stacks patches shutdown", "MrStacksPatches");
        }
    }
} 
