using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Messaging;
using HarmonyLib;

namespace PaxDrops
{
    /// <summary>
    /// Mrs. Stacks - Premium dead drop supplier that bypasses shop interface
    /// </summary>
    public static class MrStacks
    {
        private static bool _initialized;
        private static Supplier? _mrsStacks;
        private static HarmonyLib.Harmony? _harmony;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Msg("[MrStacks] 🏗️ Initializing Mrs. Stacks...");
            
            DeadDrop.Init();
            SetupHarmonyPatches();
            MelonCoroutines.Start(FindAndCreateMrsStacks());
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            
            _harmony?.UnpatchSelf();
            DeadDrop.Shutdown();
            
            _initialized = false;
            _mrsStacks = null;
            _harmony = null;

            Logger.Msg("[MrStacks] 🔌 Mrs. Stacks shutdown");
        }

        /// <summary>
        /// Set up Harmony patches to intercept Mrs. Stacks calls
        /// </summary>
        private static void SetupHarmonyPatches()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony("PaxDrops.MrStacks");
                
                var supplierType = typeof(Supplier);
                
                // Patch DeaddropRequested to bypass shop interface for Mrs. Stacks
                var deaddropMethod = supplierType.GetMethod("DeaddropRequested");
                if (deaddropMethod != null)
                {
                    var patchMethod = typeof(MrStacks).GetMethod(nameof(DeaddropRequestedPatch), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(deaddropMethod, prefix: new HarmonyMethod(patchMethod));
                    Logger.Msg("[MrStacks] ⚙️ DeaddropRequested patch applied");
                }
                else
                {
                    Logger.Error("[MrStacks] ❌ Could not find DeaddropRequested method");
                }

                // Patch PayDebtRequested to disable debt functionality for Mrs. Stacks
                var payDebtMethod = supplierType.GetMethod("PayDebtRequested");
                if (payDebtMethod != null)
                {
                    var payDebtPatchMethod = typeof(MrStacks).GetMethod(nameof(PayDebtRequestedPatch), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(payDebtMethod, prefix: new HarmonyMethod(payDebtPatchMethod));
                    Logger.Msg("[MrStacks] ⚙️ PayDebtRequested patch applied");
                }
                else
                {
                    Logger.Error("[MrStacks] ❌ Could not find PayDebtRequested method");
                }

                // Patch CreateMessageConversation to customize response options for Mrs. Stacks
                var createConversationMethod = supplierType.GetMethod("CreateMessageConversation");
                if (createConversationMethod != null)
                {
                    var conversationPatchMethod = typeof(MrStacks).GetMethod(nameof(CreateMessageConversationPatch), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(createConversationMethod, postfix: new HarmonyMethod(conversationPatchMethod));
                    Logger.Msg("[MrStacks] ⚙️ CreateMessageConversation patch applied");
                }
                else
                {
                    Logger.Error("[MrStacks] ❌ Could not find CreateMessageConversation method");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Harmony patch setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Harmony patch - intercepts Mrs. Stacks dead drop requests to bypass shop interface
        /// </summary>
        private static bool DeaddropRequestedPatch(Supplier __instance)
        {
            try
            {
                // Only intercept Mrs. Stacks calls
                if (__instance.ID == "mrs_stacks_001" || 
                    (__instance.FirstName == "Mrs." && __instance.LastName == "Stacks"))
                {
                    Logger.Msg("[MrStacks] 🎯 Mrs. Stacks order intercepted - processing directly");
                    HandleDirectOrder();
                    return false; // Skip original method
                }
                
                return true; // Allow normal behavior for other suppliers
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Patch error: {ex.Message}");
                return true; // Continue with original on error
            }
        }

        /// <summary>
        /// Harmony patch - intercepts Mrs. Stacks pay debt requests to disable them
        /// </summary>
        private static bool PayDebtRequestedPatch(Supplier __instance)
        {
            try
            {
                // Only intercept Mrs. Stacks calls
                if (__instance.ID == "mrs_stacks_001" || 
                    (__instance.FirstName == "Mrs." && __instance.LastName == "Stacks"))
                {
                    Logger.Msg("[MrStacks] 🚫 Pay Debt blocked for Mrs. Stacks");
                    return false; // Skip original method - no debt functionality
                }
                
                return true; // Allow normal behavior for other suppliers
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ PayDebt patch error: {ex.Message}");
                return true; // Continue with original on error
            }
        }

        /// <summary>
        /// Harmony patch - customizes Mrs. Stacks conversation after creation
        /// </summary>
        private static void CreateMessageConversationPatch(Supplier __instance)
        {
            try
            {
                // Only customize Mrs. Stacks conversations
                if (__instance.ID == "mrs_stacks_001" || 
                    (__instance.FirstName == "Mrs." && __instance.LastName == "Stacks"))
                {
                    Logger.Msg("[MrStacks] 🎛️ Customizing Mrs. Stacks conversation options");
                    MelonCoroutines.Start(CustomizeConversationAfterDelay(__instance));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Conversation patch error: {ex.Message}");
            }
        }

        /// <summary>
        /// Customize conversation options after a brief delay to ensure it's fully created
        /// </summary>
        private static System.Collections.IEnumerator CustomizeConversationAfterDelay(Supplier supplier)
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            
            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(supplier);
                if (conversation != null)
                {
                    Logger.Msg("[MrStacks] 🔧 Applying conversation customizations...");
                    
                    // Try to disable debt-related functionality
                    if (supplier.Debt > 0.01f)
                    {
                        // Can't set Debt directly, but we can try using ChangeDebt
                        try
                        {
                            supplier.ChangeDebt(-supplier.Debt);
                            Logger.Msg("[MrStacks] 💰 Cleared Mrs. Stacks debt");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"[MrStacks] ⚠️ Could not clear debt: {ex.Message}");
                        }
                    }
                    
                    Logger.Msg("[MrStacks] ✅ Conversation customization complete");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Conversation customization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle Mrs. Stacks direct order (no shop interface)
        /// </summary>
        private static void HandleDirectOrder()
        {
            try
            {
                Logger.Msg("[MrStacks] 📦 Processing direct order...");

                var timeManager = TimeManager.Instance;
                if (timeManager == null)
                {
                    Logger.Error("[MrStacks] ❌ TimeManager unavailable");
                    SendErrorMessage("Service temporarily unavailable. Try again later.");
                    return;
                }

                int currentDay = timeManager.ElapsedDays;

                // Check daily limit
                if (JsonDataStore.HasMrsStacksOrderToday(currentDay))
                {
                    Logger.Msg("[MrStacks] 🚫 Daily order limit reached");
                    SendDailyLimitMessage();
                    return;
                }

                // Mark order for today
                JsonDataStore.MarkMrsStacksOrderToday(currentDay);

                // Generate surprise package (no JsonDataStore.SaveDrop - that causes duplicates!)
                var packet = TierLevel.GetRandomDropPacket(currentDay);
                var items = packet.ToFlatList();
                var dropHours = new System.Random().Next(1, 5);

                // Send confirmation
                SendConfirmationMessage(dropHours, items.Count, packet.CashAmount);

                // Schedule drop spawn (direct spawn only - no pending drops tracking)
                MelonCoroutines.Start(SpawnDropAfterDelay(dropHours, items, currentDay));

                Logger.Msg($"[MrStacks] ✅ Order processed - {items.Count} items, ${packet.CashAmount}, ready in {dropHours}h");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Order processing failed: {ex.Message}");
                SendErrorMessage("Order failed. Please try again.");
            }
        }

        /// <summary>
        /// Spawn drop after time delay
        /// </summary>
        private static System.Collections.IEnumerator SpawnDropAfterDelay(int hours, List<string> items, int day)
        {
            yield return new UnityEngine.WaitForSeconds(hours * 60.0f);
            
            // Execute spawn logic after yield
            ExecuteSpawnLogic(hours, items, day);
        }

        /// <summary>
        /// Execute the spawn logic (separated to avoid yield/try-catch conflict)
        /// </summary>
        private static void ExecuteSpawnLogic(int hours, List<string> items, int day)
        {
            try
            {
                Logger.Msg("[MrStacks] ⏰ Spawning dead drop...");

                var dropRecord = new JsonDataStore.DropRecord
                {
                    Day = day,
                    Items = items,
                    DropHour = hours,
                    DropTime = $"{hours:D2}:00",
                    Org = "Mrs. Stacks",
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = "mrs_stacks_order",
                    Location = ""
                };

                string? location = DeadDrop.SpawnImmediateDrop(dropRecord);
                
                // Brief delay then send ready message
                MelonCoroutines.Start(SendReadyMessageAfterDelay(location));

                Logger.Msg($"[MrStacks] ✅ Drop spawned at: {location ?? "unknown"}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Drop spawn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send ready message after brief delay
        /// </summary>
        private static System.Collections.IEnumerator SendReadyMessageAfterDelay(string? location)
        {
            yield return new UnityEngine.WaitForSeconds(2.0f);
            SendReadyMessage(location);
        }

        /// <summary>
        /// Send order confirmation message
        /// </summary>
        private static void SendConfirmationMessage(int hours, int itemCount, int cashAmount)
        {
            var npc = FindMrsStacksNPC();
            if (npc == null) return;

            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation == null) return;

                var message = new Message(
                    $"Copy that. Preparing surprise package with {itemCount} items and ${cashAmount} cash. " +
                    $"Give me {hours} hours. I'll message when ready with location.",
                    Message.ESenderType.Other, true);

                conversation.SendMessage(message, true, true);
                Logger.Msg("[MrStacks] 📱 Confirmation sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Confirmation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send ready message with location
        /// </summary>
        private static void SendReadyMessage(string? location)
        {
            var npc = FindMrsStacksNPC();
            if (npc == null) return;

            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation == null) return;

                string messageText = !string.IsNullOrEmpty(location) 
                    ? $"Package ready at {location}. Look for markers. Clean pickup, no traces."
                    : "Package ready. Check usual dead drop spots for markers.";

                var message = new Message(messageText, Message.ESenderType.Other, true);
                conversation.SendMessage(message, true, true);
                Logger.Msg($"[MrStacks] 📱 Ready message sent: {location ?? "generic"}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Ready message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send daily limit reached message
        /// </summary>
        private static void SendDailyLimitMessage()
        {
            var npc = FindMrsStacksNPC();
            if (npc == null) return;

            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation == null) return;

                var message = new Message(
                    "You already placed an order today. I only do one drop per customer per day. " +
                    "Quality over quantity. Come back tomorrow.",
                    Message.ESenderType.Other, true);

                conversation.SendMessage(message, true, true);
                Logger.Msg("[MrStacks] 📱 Daily limit message sent");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Daily limit message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Send error message
        /// </summary>
        private static void SendErrorMessage(string errorText)
        {
            var npc = FindMrsStacksNPC();
            if (npc == null) return;

            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation == null) return;

                var message = new Message(errorText, Message.ESenderType.Other, true);
                conversation.SendMessage(message, true, true);
                Logger.Msg($"[MrStacks] 📱 Error message sent: {errorText}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Error message failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Find Mrs. Stacks NPC efficiently
        /// </summary>
        private static NPC? FindMrsStacksNPC()
        {
            // Primary: Use NPCManager lookup
            var npc = NPCManager.GetNPC("mrs_stacks_001");
            if (npc != null) return npc;

            // Fallback: Use stored reference
            return _mrsStacks;
        }

        /// <summary>
        /// Find Albert and create Mrs. Stacks
        /// </summary>
        private static System.Collections.IEnumerator FindAndCreateMrsStacks()
        {
            yield return new UnityEngine.WaitForSeconds(3.0f);
            
            try
            {
                var albert = NPCManager.GetNPC("albert_hoover");
                
                if (albert != null)
                {
                    Logger.Msg($"[MrStacks] ✅ Found Albert: {albert.FirstName} {albert.LastName}");
                    CreateMrsStacks(albert);
                }
                else
                {
                    Logger.Error("[MrStacks] ❌ Albert not found");
                    TryFallbackCreation();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Mrs. Stacks creation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create Mrs. Stacks as separate NPC
        /// </summary>
        private static void CreateMrsStacks(NPC albertTemplate)
        {
            try
            {
                Logger.Msg("[MrStacks] 🏗️ Creating Mrs. Stacks...");

                var mrsStacksNPC = UnityEngine.Object.Instantiate(albertTemplate);
                mrsStacksNPC.FirstName = "Mrs.";
                mrsStacksNPC.LastName = "Stacks";
                mrsStacksNPC.ID = "mrs_stacks_001";
                
                SetupIcon(mrsStacksNPC);
                
                _mrsStacks = mrsStacksNPC as Supplier;
                if (_mrsStacks != null)
                {
                    _mrsStacks.DeliveriesEnabled = true;
                }

                // Register and create conversation
                NPCManager.NPCRegistry?.Add(mrsStacksNPC);
                mrsStacksNPC.CreateMessageConversation();
                
                SetupConversation(mrsStacksNPC);
                
                Logger.Msg("[MrStacks] ✅ Mrs. Stacks created successfully");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Creation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup conversation and send welcome
        /// </summary>
        private static void SetupConversation(NPC npc)
        {
            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation != null)
                {
                    conversation.contactName = "Mrs. Stacks";
                    
                    var player = Player.Local;
                    string playerName = player?.PlayerName ?? "friend";

                    var welcomeMessage = new Message(
                        $"Hey {playerName}! Mrs. Stacks here - premium dead drop supplier. " +
                        "No catalogs, just quality surprise packages. Order and I'll text the pickup location.",
                        Message.ESenderType.Other, true);

                    conversation.SendMessage(welcomeMessage, true, true);
                    Logger.Msg("[MrStacks] 📱 Welcome sent");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Conversation setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Set up custom icon for Mrs. Stacks
        /// </summary>
        private static void SetupIcon(NPC npc)
        {
            try
            {
                npc.AutoGenerateMugshot = false;
                
                var customSprite = CreateCustomSprite();
                if (customSprite != null)
                {
                    npc.MugshotSprite = customSprite;
                    Logger.Msg("[MrStacks] ⚙️ Custom icon set");
                }
                else
                {
                    npc.AutoGenerateMugshot = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Icon setup failed: {ex.Message}");
                npc.AutoGenerateMugshot = true;
            }
        }

        /// <summary>
        /// Create purple-themed sprite for Mrs. Stacks
        /// </summary>
        private static UnityEngine.Sprite? CreateCustomSprite()
        {
            try
            {
                var texture = new UnityEngine.Texture2D(64, 64);
                var purple = new UnityEngine.Color(0.6f, 0.2f, 0.8f, 1.0f);
                var lightPurple = new UnityEngine.Color(0.9f, 0.7f, 1.0f, 1.0f);
                
                for (int x = 0; x < 64; x++)
                {
                    for (int y = 0; y < 64; y++)
                    {
                        var distance = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                        
                        if (distance < 16 || (distance < 28 && distance > 24))
                            texture.SetPixel(x, y, lightPurple);
                        else if (distance < 32)
                            texture.SetPixel(x, y, purple);
                        else
                            texture.SetPixel(x, y, Color.clear);
                    }
                }
                
                texture.Apply();
                
                var sprite = UnityEngine.Sprite.Create(texture, 
                    new UnityEngine.Rect(0, 0, 64, 64), 
                    new UnityEngine.Vector2(0.5f, 0.5f), 100.0f);
                
                sprite.name = "MrsStacksIcon";
                return sprite;
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Sprite creation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Fallback creation if Albert not found
        /// </summary>
        private static void TryFallbackCreation()
        {
            try
            {
                var registry = NPCManager.NPCRegistry;
                if (registry == null) return;

                for (int i = 0; i < registry.Count; i++)
                {
                    if (registry[i] is Supplier supplier)
                    {
                        Logger.Msg($"[MrStacks] 🔄 Using fallback: {supplier.FirstName} {supplier.LastName}");
                        CreateMrsStacks(supplier);
                        return;
                    }
                }
                
                Logger.Error("[MrStacks] ❌ No suppliers found for fallback");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrStacks] ❌ Fallback failed: {ex.Message}");
            }
        }

        // Public API methods for external use
        public static void TriggerTestOrder() => HandleDirectOrder();
        public static void OnNewDay() => Logger.Msg("[MrStacks] 🌅 Ready for business");
        public static void OnDayChanged() => Logger.Msg("[MrStacks] 🔄 Day changed");
        
        /// <summary>
        /// Process order (public API for external commands)
        /// </summary>
        public static void ProcessOrder(string orderType = "standard")
        {
            Logger.Msg($"[MrStacks] 📦 External order request: {orderType}");
            HandleDirectOrder(); // All orders are surprise packages from Mrs. Stacks
        }
        
        public static void ShowSupplierInfo()
        {
            var status = _mrsStacks != null ? "Available" : "Not initialized";
            Logger.Msg($"[MrStacks] 📋 Mrs. Stacks: {status}");
        }
    }
} 