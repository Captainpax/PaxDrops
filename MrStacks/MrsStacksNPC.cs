using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using System.Linq;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Handles Mrs. Stacks NPC creation, setup, and management.
    /// Manages character creation, icon setup, and NPC registration.
    /// </summary>
    public static class MrsStacksNPC
    {
        private static Supplier? _mrsStacks;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize Mrs. Stacks NPC creation
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrsStacksNPC] 🏗️ Initializing Mrs. Stacks NPC...");
            MrsStacksPatches.Init();
            MelonCoroutines.Start(FindAndCreateMrsStacks());
            _initialized = true;
        }

        /// <summary>
        /// Get the Mrs. Stacks supplier instance
        /// </summary>
        public static Supplier? GetMrsStacksSupplier() => _mrsStacks;

        /// <summary>
        /// Handle new day events for Mrs. Stacks
        /// </summary>
        public static void OnNewDay()
        {
            try
            {
                if (!_initialized) return;

                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentDay = timeManager.ElapsedDays;
                Logger.Msg($"[MrsStacksNPC] 🌅 New day {currentDay} - checking for business opportunities");

                // Check order history for inactivity reminders
                var ordersToday = SaveFileJsonDataStore.MrsStacksOrdersToday;
                bool hasOrderedRecently = ordersToday.Values.Any(count => count > 0);

                if (!hasOrderedRecently)
                {
                    Logger.Msg("[MrsStacksNPC] 📱 No recent orders - checking for inactivity reminder");
                    DailyDropOrdering.SendInactivityReminderIfNeeded();
                }
                else
                {
                    Logger.Msg($"[MrsStacksNPC] ✅ Player has recent order activity ({ordersToday.Count} days with orders)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ New day processing error: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Called when day changes - perform daily maintenance
        /// </summary>
        public static void OnDayChanged()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                int currentDay = timeManager?.ElapsedDays ?? 0;
                
                Logger.Msg($"[MrsStacksNPC] 📅 Day changed to {currentDay} - performing daily maintenance");

                // Reset Mrs. Stacks availability for new day
                // Note: SaveFileJsonDataStore tracks daily orders, no reset needed here
                
                // Check for any pending Mrs. Stacks orders that need attention
                if (_mrsStacks != null)
                {
                    // Log supplier status
                    Logger.Msg($"[MrsStacksNPC] 📊 Mrs. Stacks status - Debt: ${_mrsStacks.Debt:F2}, Deliveries: {_mrsStacks.DeliveriesEnabled}");
                    
                    // Ensure deliveries stay enabled
                    if (!_mrsStacks.DeliveriesEnabled)
                    {
                        _mrsStacks.DeliveriesEnabled = true;
                        Logger.Msg("[MrsStacksNPC] ✅ Re-enabled Mrs. Stacks deliveries");
                    }
                    
                    // Clear any accumulated debt (Mrs. Stacks operates debt-free)
                    if (_mrsStacks.Debt > 0.01f)
                    {
                        try
                        {
                            _mrsStacks.ChangeDebt(-_mrsStacks.Debt);
                            Logger.Msg("[MrsStacksNPC] 💰 Cleared Mrs. Stacks debt on day change");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"[MrsStacksNPC] ⚠️ Could not clear debt: {ex.Message}");
                        }
                    }
                }

                // Cleanup old Mrs. Stacks orders (keep last 3 days)
                CleanupOldMrsStacksOrders(currentDay);
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ OnDayChanged error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up old Mrs. Stacks order records
        /// </summary>
        private static void CleanupOldMrsStacksOrders(int currentDay)
        {
            try
            {
                var ordersToday = SaveFileJsonDataStore.MrsStacksOrdersToday;
                var keysToRemove = new List<int>();

                foreach (var kvp in ordersToday)
                {
                    // Remove records older than 3 days
                    if (kvp.Key < currentDay - 3)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    ordersToday.Remove(key);
                }

                if (keysToRemove.Count > 0)
                {
                    Logger.Msg($"[MrsStacksNPC] 🗑️ Cleaned up {keysToRemove.Count} old Mrs. Stacks order records");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Order cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Find Albert and create Mrs. Stacks based on him
        /// </summary>
        private static System.Collections.IEnumerator FindAndCreateMrsStacks()
        {
            yield return new UnityEngine.WaitForSeconds(2.0f);

            try
            {
                var albert = FindAlbertNPC();
                if (albert != null)
                {
                    Logger.Msg($"[MrsStacksNPC] ✅ Found Albert: {albert.FirstName} {albert.LastName}");
                    CreateMrsStacks(albert);
                }
                else
                {
                    Logger.Error("[MrsStacksNPC] ❌ Albert not found");
                    TryFallbackCreation();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Mrs. Stacks creation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Find Albert NPC using the proper GetNPC method
        /// </summary>
        private static NPC? FindAlbertNPC()
        {
            try
            {
                // Finding Albert by ID
                var albert = NPCManager.GetNPC("albert_hoover");
                
                if (albert != null)
                {
                    Logger.Msg($"[MrsStacksNPC] ✅ Found Albert via GetNPC: {albert.FirstName} {albert.LastName} (ID: {albert.ID})");
                    return albert;
                }
                Logger.Error("[MrsStacksNPC] ❌ Albert not found");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Albert search failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create Mrs. Stacks as separate NPC based on Albert template
        /// </summary>
        private static void CreateMrsStacks(NPC albertTemplate)
        {
            try
            {
                Logger.Msg("[MrsStacksNPC] 🏗️ Creating Mrs. Stacks...");

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
                
                MrsStacksMessaging.SetupConversation(mrsStacksNPC);
                
                Logger.Msg("[MrsStacksNPC] ✅ Mrs. Stacks created successfully");
                
                // Send welcome message after a short delay to ensure everything is set up
                MelonCoroutines.Start(SendDelayedWelcomeMessage());
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Creation failed: {ex.Message}");
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
                    Logger.Msg("[MrsStacksNPC] ⚙️ Custom icon set");
                }
                else
                {
                    npc.AutoGenerateMugshot = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Icon setup failed: {ex.Message}");
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
                Logger.Error($"[MrsStacksNPC] ❌ Sprite creation failed: {ex.Message}");
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
                        Logger.Msg($"[MrsStacksNPC] 🔄 Using fallback: {supplier.FirstName} {supplier.LastName}");
                        CreateMrsStacks(supplier);
                        return;
                    }
                }
                
                Logger.Error("[MrsStacksNPC] ❌ No suppliers found for fallback");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Fallback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show supplier info for debugging
        /// </summary>
        public static void ShowSupplierInfo()
        {
            var status = _mrsStacks != null ? "Available" : "Not initialized";
            Logger.Msg($"[MrsStacksNPC] 📋 Mrs. Stacks: {status}");
        }

        /// <summary>
        /// Shutdown Mrs. Stacks NPC and clean up all resources
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                Logger.Msg("[MrsStacksNPC] 🧼 Shutting down Mrs. Stacks NPC...");
                
                if (_mrsStacks != null)
                {
                    // Disable deliveries and clear debt
                    _mrsStacks.DeliveriesEnabled = false;
                    try
                    {
                        _mrsStacks.ChangeDebt(-_mrsStacks.Debt);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[MrsStacksNPC] ⚠️ Could not clear debt on shutdown: {ex.Message}");
                    }
                    
                    // Try to remove from NPCManager registry
                    try
                    {
                        var registry = NPCManager.NPCRegistry;
                        if (registry != null && registry.Contains(_mrsStacks))
                        {
                            registry.Remove(_mrsStacks);
                            Logger.Msg("[MrsStacksNPC] 🗑️ Removed Mrs. Stacks from NPC registry");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[MrsStacksNPC] ⚠️ Could not remove from registry: {ex.Message}");
                    }
                    
                    // Destroy the GameObject if it exists
                    try
                    {
                        if (_mrsStacks.gameObject != null)
                        {
                            UnityEngine.Object.Destroy(_mrsStacks.gameObject);
                            Logger.Msg("[MrsStacksNPC] 🗑️ Destroyed Mrs. Stacks GameObject");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[MrsStacksNPC] ⚠️ Could not destroy GameObject: {ex.Message}");
                    }
                }
                
                // Clean up messaging system
                try
                {
                    MrsStacksMessaging.Shutdown();
                    Logger.Msg("[MrsStacksNPC] 📤 Messaging system shutdown");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[MrsStacksNPC] ⚠️ Messaging shutdown error: {ex.Message}");
                }
                
                // Shutdown patches
                try
                {
                    MrsStacksPatches.Shutdown();
                    Logger.Msg("[MrsStacksNPC] 🔌 Patches shutdown");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[MrsStacksNPC] ⚠️ Patch shutdown error: {ex.Message}");
                }
                
                // Reset state
                _mrsStacks = null;
                _initialized = false;
                
                Logger.Msg("[MrsStacksNPC] ✅ Mrs. Stacks NPC shutdown complete");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Shutdown error: {ex.Message}");
                Logger.Exception(ex);
                
                // Force reset state even if shutdown failed
                _mrsStacks = null;
                _initialized = false;
            }
        }

        /// <summary>
        /// Send welcome message after a delay to ensure Mrs. Stacks is fully set up
        /// </summary>
        private static System.Collections.IEnumerator SendDelayedWelcomeMessage()
        {
            yield return new UnityEngine.WaitForSeconds(3.0f);
            
            try
            {
                Logger.Msg($"[MrsStacksNPC] 🔍 Checking if welcome message should be sent...");
                
                // Check both order history AND conversation history to determine if user is truly new
                int lastOrderDay = SaveFileJsonDataStore.GetLastMrsStacksOrderDay();
                
                // Also check conversation history - load it for current save to get accurate count
                MrsStacksMessaging.LoadConversationForCurrentSave();
                
                // Get current conversation history info
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                
                Logger.Msg($"[MrsStacksNPC] 🔍 Welcome check - Last order day: {lastOrderDay}");
                Logger.Msg($"[MrsStacksNPC] 🔍 Save info - ID: {saveId}, Loaded: {isLoaded}");
                
                // Use a more specific method to check conversation history
                bool hasConversationHistory = MrsStacksMessaging.HasExistingConversation();
                
                Logger.Msg($"[MrsStacksNPC] 🔍 Has conversation history: {hasConversationHistory}");
                
                // Only send welcome if BOTH conditions are true:
                // 1. Never ordered before (lastOrderDay == -1)
                // 2. No conversation history exists
                bool isReallyNewUser = (lastOrderDay == -1) && !hasConversationHistory;
                
                if (isReallyNewUser)
                {
                    Logger.Msg($"[MrsStacksNPC] 🎉 Sending welcome message to truly new user");
                    DailyDropOrdering.SendWelcomeMessage();
                }
                else
                {
                    Logger.Msg($"[MrsStacksNPC] ♻️ Skipping welcome - existing user (Orders: {lastOrderDay != -1}, Conversation: {hasConversationHistory})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ Delayed welcome message failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }
    }
} 