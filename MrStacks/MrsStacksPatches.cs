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
                    Logger.Msg("[MrsStacksPatches] ⚙️ DeaddropRequested patch applied");
                }

                // Patch conversation creation
                var createConversationMethod = supplierType.GetMethod("CreateMessageConversation");
                if (createConversationMethod != null)
                {
                    var conversationPatchMethod = typeof(MrsStacksPatches).GetMethod(nameof(CreateMessageConversationPostfix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(createConversationMethod, postfix: new HarmonyMethod(conversationPatchMethod));
                    Logger.Msg("[MrsStacksPatches] ⚙️ CreateMessageConversation patch applied");
                }

                Logger.Msg("[MrsStacksPatches] ✅ Essential patches initialized (save/load patches removed)");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksPatches] ❌ Harmony patch setup failed: {ex.Message}");
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
                    Logger.Msg("[MrsStacksPatches] 🛑 Intercepted Mrs. Stacks dead drop request - bypassing shop interface");
                    
                    // Get current day for order processing
                    var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
                    int currentDay = timeManager?.ElapsedDays ?? 0;

                    // Use the unified order processor
                    MelonCoroutines.Start(ProcessMrsStacksOrder(currentDay));
                    
                    return false; // Skip original method
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksPatches] ❌ DeaddropRequested patch error: {ex.Message}");
            }
            
            return true; // Continue with original method for other suppliers
        }

        /// <summary>
        /// Process Mrs. Stacks order using the OrderProcessor system
        /// </summary>
        private static System.Collections.IEnumerator ProcessMrsStacksOrder(int currentDay)
        {
            yield return new UnityEngine.WaitForSeconds(0.1f);
            
            try
            {
                Logger.Msg("[MrsStacksPatches] 📦 Processing Mrs. Stacks dead drop order...");
                
                // Use the unified order processor with Mrs. Stacks organization
                OrderProcessor.ProcessOrder("Mrs. Stacks", "deadrop_interaction", null, null, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksPatches] ❌ Mrs. Stacks order processing failed: {ex.Message}");
            }
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
                    Logger.Msg("[MrsStacksPatches] 🎛️ Customizing Mrs. Stacks conversation options");
                    MelonCoroutines.Start(MrsStacksMessaging.CustomizeConversationAfterDelay(__instance));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksPatches] ❌ CreateMessageConversation patch error: {ex.Message}");
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
            
            Logger.Msg("[MrsStacksPatches] 🔌 Mrs. Stacks patches shutdown");
        }
    }
} 