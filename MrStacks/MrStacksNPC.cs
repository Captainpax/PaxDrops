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
    /// Handles Mr. Stacks NPC creation, setup, and management.
    /// Manages character creation, icon setup, and NPC registration.
    /// </summary>
    public static class MrStacksNPC
    {
        private static Supplier? _mrStacks;
        private static bool _initialized = false;

        /// <summary>
        /// Initialize Mr. Stacks NPC creation
        /// </summary>
        public static void Init()
        {
            Logger.Debug("[MrStacksNPC] 🏗️ Initializing Mr. Stacks NPC...", "MrStacksNPC");
            MrStacksPatches.Init();
            MelonCoroutines.Start(FindAndCreateMrStacks());
            _initialized = true;
        }

        /// <summary>
        /// Get the Mr. Stacks supplier instance
        /// </summary>
        public static Supplier? GetMrStacksSupplier() => _mrStacks;

        /// <summary>
        /// Handle new day events for Mr. Stacks
        /// </summary>
        public static void OnNewDay()
        {
            try
            {
                if (!_initialized) return;

                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int currentDay = timeManager.ElapsedDays;
                Logger.Debug($"🌅 New day {currentDay} - checking for business opportunities", "MrStacksNPC");

                // Check order history for inactivity reminders
                var ordersToday = SaveFileJsonDataStore.MrStacksOrdersToday;
                bool hasOrderedRecently = ordersToday.Values.Any(count => count > 0);

                if (!hasOrderedRecently)
                {
                    Logger.Debug("[MrStacksNPC] 📱 No recent orders - checking for inactivity reminder", "MrStacksNPC");
                    DailyDropOrdering.SendInactivityReminderIfNeeded();
                }
                else
                {
                    Logger.Debug($"✅ Player has recent order activity ({ordersToday.Count} days with orders)", "MrStacksNPC");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ New day processing error: {ex.Message}", "MrStacksNPC");
                Logger.Exception(ex, "MrStacksNPC");
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
                
                Logger.Debug($"📅 Day changed to {currentDay} - performing daily maintenance", "MrStacksNPC");

                // Reset Mr. Stacks availability for new day
                // Note: SaveFileJsonDataStore tracks daily orders, no reset needed here
                
                // Check for any pending Mr. Stacks orders that need attention
                if (_mrStacks != null)
                {
                    // Log supplier status
                    Logger.Debug($"📊 Mr. Stacks status - Debt: ${_mrStacks.Debt:F2}, Deliveries: {_mrStacks.DeliveriesEnabled}", "MrStacksNPC");
                    
                    // Ensure deliveries stay enabled
                    if (!_mrStacks.DeliveriesEnabled)
                    {
                        _mrStacks.DeliveriesEnabled = true;
                        Logger.Debug("✅ Re-enabled Mr. Stacks deliveries", "MrStacksNPC");
                    }
                    
                    // Clear any accumulated debt (Mr. Stacks operates debt-free)
                    if (_mrStacks.Debt > 0.01f)
                    {
                        try
                        {
                            _mrStacks.ChangeDebt(-_mrStacks.Debt);
                            Logger.Debug("💰 Cleared Mr. Stacks debt on day change", "MrStacksNPC");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"⚠️ Could not clear debt: {ex.Message}", "MrStacksNPC");
                        }
                    }
                }

                // Cleanup old Mr. Stacks orders (keep last 3 days)
                CleanupOldMrStacksOrders(currentDay);
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ OnDayChanged error: {ex.Message}", "MrStacksNPC");
            }
        }

        /// <summary>
        /// Clean up old Mr. Stacks order records
        /// </summary>
        private static void CleanupOldMrStacksOrders(int currentDay)
        {
            try
            {
                var ordersToday = SaveFileJsonDataStore.MrStacksOrdersToday;
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
                    Logger.Debug($"🗑️ Cleaned up {keysToRemove.Count} old Mr. Stacks order records", "MrStacksNPC");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Order cleanup error: {ex.Message}", "MrStacksNPC");
            }
        }

        /// <summary>
        /// Find Albert and create Mr. Stacks based on him
        /// </summary>
        private static System.Collections.IEnumerator FindAndCreateMrStacks()
        {
            yield return new UnityEngine.WaitForSeconds(2.0f);

            try
            {
                var albert = FindAlbertNPC();
                if (albert != null)
                {
                    Logger.Debug($"✅ Found Albert: {albert.FirstName} {albert.LastName}", "MrStacksNPC");
                    CreateMrStacks(albert);
                }
                else
                {
                    Logger.Error("❌ Albert not found", "MrStacksNPC");
                    TryFallbackCreation();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Mr. Stacks creation failed: {ex.Message}", "MrStacksNPC");
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
                    Logger.Debug($"✅ Found Albert via GetNPC: {albert.FirstName} {albert.LastName} (ID: {albert.ID})", "MrStacksNPC");
                    return albert;
                }
                Logger.Error("❌ Albert not found", "MrStacksNPC");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Albert search failed: {ex.Message}", "MrStacksNPC");
                return null;
            }
        }

        /// <summary>
        /// Create Mr. Stacks as separate NPC based on Albert template
        /// </summary>
        private static void CreateMrStacks(NPC albertTemplate)
        {
            try
            {
                Logger.Debug("🏗️ Creating Mr. Stacks...", "MrStacksNPC");

                var mrStacksNPC = UnityEngine.Object.Instantiate(albertTemplate);
                mrStacksNPC.FirstName = "Mr.";
                mrStacksNPC.LastName = "Stacks";
                mrStacksNPC.ID = "mr_stacks_001";
                
                SetupIcon(mrStacksNPC);
                
                _mrStacks = mrStacksNPC as Supplier;
                if (_mrStacks != null)
                {
                    _mrStacks.DeliveriesEnabled = true;
                }

                // Register and create conversation
                NPCManager.NPCRegistry?.Add(mrStacksNPC);
                mrStacksNPC.CreateMessageConversation();
                
                MrStacksMessaging.SetupConversation(mrStacksNPC);
                
                Logger.Debug("✅ Mr. Stacks created successfully", "MrStacksNPC");
                
                // Send welcome message after a short delay to ensure everything is set up
                MelonCoroutines.Start(SendDelayedWelcomeMessage());
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Creation failed: {ex.Message}", "MrStacksNPC");
            }
        }

        /// <summary>
        /// Set up custom icon for Mr. Stacks
        /// </summary>
        private static void SetupIcon(NPC npc)
        {
            try
            {
                SetAutoGenerateMugshot(npc, false);
                
                var customSprite = CreateCustomSprite();
                if (customSprite != null)
                {
                    npc.MugshotSprite = customSprite;
                    Logger.Debug("⚙️ Custom icon set", "MrStacksNPC");
                }
                else
                {
                    SetAutoGenerateMugshot(npc, true);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Icon setup failed: {ex.Message}", "MrStacksNPC");
                SetAutoGenerateMugshot(npc, true);
            }
        }

        private static void SetAutoGenerateMugshot(NPC npc, bool value)
        {
            try
            {
                var prop = npc.GetType().GetProperty("AutoGenerateMugshot");
                if (prop != null && prop.CanWrite)
                {
                    prop.SetValue(npc, value);
                }
            }
            catch
            {
                // Property is optional across game versions.
            }
        }

        /// <summary>
        /// Create purple-themed sprite for Mr. Stacks
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
                
                sprite.name = "MrStacksIcon";
                return sprite;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Sprite creation failed: {ex.Message}", "MrStacksNPC");
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
                        Logger.Debug($"🔄 Using fallback: {supplier.FirstName} {supplier.LastName}", "MrStacksNPC");
                        CreateMrStacks(supplier);
                        return;
                    }
                }
                
                Logger.Error("❌ No suppliers found for fallback", "MrStacksNPC");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Fallback failed: {ex.Message}", "MrStacksNPC");
            }
        }

        /// <summary>
        /// Show supplier info for debugging
        /// </summary>
        public static void ShowSupplierInfo()
        {
            var status = _mrStacks != null ? "Available" : "Not initialized";
            Logger.Debug($"📋 Mr. Stacks: {status}", "MrStacksNPC");
        }

        /// <summary>
        /// Shutdown Mr. Stacks NPC and clean up all resources
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                Logger.Debug("🧼 Shutting down Mr. Stacks NPC...", "MrStacksNPC");
                
                if (_mrStacks != null)
                {
                    // Disable deliveries and clear debt
                    _mrStacks.DeliveriesEnabled = false;
                    try
                    {
                        _mrStacks.ChangeDebt(-_mrStacks.Debt);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Could not clear debt on shutdown: {ex.Message}", "MrStacksNPC");
                    }
                    
                    // Try to remove from NPCManager registry
                    try
                    {
                        var registry = NPCManager.NPCRegistry;
                        if (registry != null && registry.Contains(_mrStacks))
                        {
                            registry.Remove(_mrStacks);
                            Logger.Debug("🗑️ Removed Mr. Stacks from NPC registry", "MrStacksNPC");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Could not remove from registry: {ex.Message}", "MrStacksNPC");
                    }
                    
                    // Destroy the GameObject if it exists
                    try
                    {
                        if (_mrStacks.gameObject != null)
                        {
                            UnityEngine.Object.Destroy(_mrStacks.gameObject);
                            Logger.Debug("🗑️ Destroyed Mr. Stacks GameObject", "MrStacksNPC");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Could not destroy GameObject: {ex.Message}", "MrStacksNPC");
                    }
                }
                
                // Clean up messaging system
                try
                {
                    MrStacksMessaging.Shutdown();
                    Logger.Debug("📤 Messaging system shutdown", "MrStacksNPC");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Messaging shutdown error: {ex.Message}", "MrStacksNPC");
                }
                
                // Shutdown patches
                try
                {
                    MrStacksPatches.Shutdown();
                    Logger.Debug("🔌 Patches shutdown", "MrStacksNPC");
                }
                catch (Exception ex)
                {
                    Logger.Warn($"⚠️ Patch shutdown error: {ex.Message}", "MrStacksNPC");
                }
                
                // Reset state
                _mrStacks = null;
                _initialized = false;
                
                Logger.Debug("✅ Mr. Stacks NPC shutdown complete", "MrStacksNPC");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Shutdown error: {ex.Message}", "MrStacksNPC");
                Logger.Exception(ex, "MrStacksNPC");
                
                // Force reset state even if shutdown failed
                _mrStacks = null;
                _initialized = false;
            }
        }

        /// <summary>
        /// Send welcome message after a delay to ensure Mr. Stacks is fully set up
        /// </summary>
        private static System.Collections.IEnumerator SendDelayedWelcomeMessage()
        {
            yield return new UnityEngine.WaitForSeconds(3.0f);
            
            try
            {
                Logger.Debug("🔍 Checking if welcome message should be sent...", "MrStacksNPC");
                
                // Check both order history AND conversation history to determine if user is truly new
                int lastOrderDay = SaveFileJsonDataStore.GetLastMrStacksOrderDay();
                
                // Also check conversation history - load it for current save to get accurate count
                MrStacksMessaging.LoadConversationForCurrentSave();
                
                // Get current conversation history info
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                
                Logger.Debug($"🔍 Welcome check - Last order day: {lastOrderDay}", "MrStacksNPC");
                Logger.Debug($"🔍 Save info - ID: {saveId}, Loaded: {isLoaded}", "MrStacksNPC");
                
                // Use a more specific method to check conversation history
                bool hasConversationHistory = MrStacksMessaging.HasExistingConversation();
                
                Logger.Debug($"🔍 Has conversation history: {hasConversationHistory}", "MrStacksNPC");
                
                // Only send welcome if BOTH conditions are true:
                // 1. Never ordered before (lastOrderDay == -1)
                // 2. No conversation history exists
                bool isReallyNewUser = (lastOrderDay == -1) && !hasConversationHistory;
                
                if (isReallyNewUser)
                {
                    Logger.Debug("🎉 Sending welcome message to truly new user", "MrStacksNPC");
                    DailyDropOrdering.SendWelcomeMessage();
                }
                else
                {
                    Logger.Debug($"♻️ Skipping welcome - existing user (Orders: {lastOrderDay != -1}, Conversation: {hasConversationHistory})", "MrStacksNPC");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Delayed welcome message failed: {ex.Message}", "MrStacksNPC");
                Logger.Exception(ex, "MrStacksNPC");
            }
        }
    }
} 

