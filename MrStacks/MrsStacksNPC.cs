using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Handles Mrs. Stacks NPC creation, setup, and management.
    /// Manages character creation, icon setup, and NPC registration.
    /// </summary>
    public static class MrsStacksNPC
    {
        private static Supplier? _mrsStacks;

        /// <summary>
        /// Initialize Mrs. Stacks NPC creation
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[MrsStacksNPC] 🏗️ Initializing Mrs. Stacks NPC...");
            MrsStacksPatches.Init();
            MelonCoroutines.Start(FindAndCreateMrsStacks());
        }

        /// <summary>
        /// Get the Mrs. Stacks supplier instance
        /// </summary>
        public static Supplier? GetMrsStacksSupplier() => _mrsStacks;

        /// <summary>
        /// Called at 7:00 AM each day - sends intro/availability messages
        /// </summary>
        public static void OnNewDay()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                int currentDay = timeManager?.ElapsedDays ?? 0;
                
                Logger.Msg($"[MrsStacksNPC] 🌅 New day {currentDay} - checking Mrs. Stacks availability");

                // Check if Mrs. Stacks is available and hasn't ordered today
                if (_mrsStacks != null && !JsonDataStore.HasMrsStacksOrderToday(currentDay))
                {
                    // Send daily availability message
                    var npc = MrsStacksMessaging.FindMrsStacksNPC();
                    if (npc != null)
                    {
                        var messages = new[]
                        {
                            "Good morning! Mrs. Stacks here. Fresh inventory available today. Send 'order' when ready.",
                            "Morning! Got some premium packages ready. Quality guaranteed as always.",
                            "Hey there! Business is open. Today's selection is particularly good.",
                            "Morning briefing: All systems operational. Premium drops available on request."
                        };

                        var random = new System.Random();
                        var dailyMessage = messages[random.Next(messages.Length)];
                        
                        MrsStacksMessaging.SendMessage(npc, dailyMessage);
                        Logger.Msg("[MrsStacksNPC] 📱 Daily availability message sent");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksNPC] ❌ OnNewDay error: {ex.Message}");
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
                // Note: JsonDataStore tracks daily orders, no reset needed here
                
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
                var ordersToday = JsonDataStore.MrsStacksOrdersToday;
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
        /// Shutdown Mrs. Stacks NPC
        /// </summary>
        public static void Shutdown()
        {
            if (_mrsStacks == null) return;
            Logger.Msg("[MrsStacksNPC] 🧼 Shutting down Mrs. Stacks NPC...");
            _mrsStacks.DeliveriesEnabled = false;
            _mrsStacks.ChangeDebt(-_mrsStacks.Debt);
            MrsStacksPatches.Shutdown();
        }
    }
} 