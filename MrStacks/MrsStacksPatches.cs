using System;
using HarmonyLib;
using MelonLoader;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Economy;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Harmony patches for Mrs. Stacks functionality.
    /// Handles supplier interaction interception (no more invasive save/load patches).
    /// </summary>
    public static class MrsStacksPatches
    {
        private static HarmonyLib.Harmony? _harmony;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize Harmony patches for Mrs. Stacks
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                _harmony = new HarmonyLib.Harmony("PaxDrops.MrsStacksPatches");

                // Patch supplier dead drop requests
                var supplierType = typeof(Supplier);
                var deadDropMethod = supplierType.GetMethod("DeaddropRequested");
                if (deadDropMethod != null)
                {
                    var prefix = typeof(MrsStacksPatches).GetMethod(nameof(DeaddropRequestedPrefix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(deadDropMethod, prefix: new HarmonyMethod(prefix));
                    Logger.Info("⚙️ DeaddropRequested patch applied", "MrsStacksPatches");
                }

                // Patch conversation creation
                var createConversationMethod = supplierType.GetMethod("CreateMessageConversation");
                if (createConversationMethod != null)
                {
                    var conversationPatchMethod = typeof(MrsStacksPatches).GetMethod(nameof(CreateMessageConversationPostfix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(createConversationMethod, postfix: new HarmonyMethod(conversationPatchMethod));
                    Logger.Info("⚙️ CreateMessageConversation patch applied", "MrsStacksPatches");
                }

                Logger.Info("✅ Essential patches initialized (save/load patches removed)", "MrsStacksPatches");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Harmony patch setup failed: {ex.Message}", "MrsStacksPatches");
            }
        }

        /// <summary>
        /// Harmony prefix patch - intercepts Mrs. Stacks dead drop requests to bypass shop interface
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Supplier), "DeaddropRequested")]
        private static bool DeaddropRequestedPrefix(Supplier __instance)
        {
            try
            {
                if (IsMrsStacks(__instance))
                {
                    Logger.Debug("🛑 Intercepted Mrs. Stacks dead drop request - bypassing shop interface", "MrsStacksPatches");
                    
                    // Process order via DailyDropOrdering system (handles daily limits and tracking)
                    DailyDropOrdering.ProcessMrsStacksOrder("deadrop_interaction", null, true);

                    // Inform the player that bypass worked
                    Logger.Debug("✅ Mrs. Stacks dead drop order bypassed shop interface", "MrsStacksPatches");
                    
                    return false; // Skip original method
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ DeaddropRequested patch error: {ex.Message}", "MrsStacksPatches");
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
                // Only customize Mrs. Stacks conversations
                if (IsMrsStacks(__instance))
                {
                    Logger.Debug("🎛️ Customizing Mrs. Stacks conversation options", "MrsStacksPatches");
                    MelonCoroutines.Start(MrsStacksMessaging.CustomizeConversationAfterDelay(__instance));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ CreateMessageConversation patch error: {ex.Message}", "MrsStacksPatches");
            }
        }

        /// <summary>
        /// Check if a supplier is Mrs. Stacks
        /// </summary>
        private static bool IsMrsStacks(Supplier supplier)
        {
            return supplier.ID == "mrs_stacks_001" || 
                   (supplier.FirstName == "Mrs." && supplier.LastName == "Stacks");
        }

        /// <summary>
        /// Shutdown the Mrs. Stacks patches
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            _harmony?.UnpatchSelf();
            _harmony = null;
            _initialized = false;
            
            Logger.Info("🔌 Mrs. Stacks patches shutdown", "MrsStacksPatches");
        }
    }
} 