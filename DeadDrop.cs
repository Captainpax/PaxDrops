using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Money;
using Il2CppInterop.Runtime;

namespace PaxDrops
{
    /// <summary>
    /// Handles logic for spawning scheduled dead drops into actual game dead drop locations.
    /// IL2CPP port using the real Schedule I dead drop system from Il2CppScheduleOne.Economy.DeadDrop.
    /// </summary>
    public static class DeadDrop
    {
        private static bool _initialized;

        /// <summary>
        /// Initialize the dead drop system.
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            Logger.Msg("[DeadDrop] ✅ System initialized");
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;
            Logger.Msg("[DeadDrop] 🔌 Shutdown complete");
        }

        /// <summary>
        /// Called by TimeMonitor when time changes to check for scheduled drops
        /// </summary>
        public static void OnTimeChanged()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                if (timeManager == null) return;

                int elapsedDays = timeManager.ElapsedDays;
                int currentTime = timeManager.CurrentTime;
                
                int currentDay = elapsedDays;
                int currentHour = currentTime;
                
                // Handle different time formats
                if (currentTime > 2400)
                {
                    currentDay = currentTime / 10000;
                    currentHour = (currentTime % 10000) / 100;
                }
                else if (currentTime > 100)
                {
                    currentHour = currentTime;
                }

                Logger.Msg($"[DeadDrop] ⏰ Time check - Day: {currentDay}, Hour: {currentHour}");

                if (!JsonDataStore.PendingDrops.TryGetValue(currentDay, out var drop))
                {
                    return; // No drop scheduled for today
                }

                if (currentHour >= drop.DropHour)
                {
                    Logger.Msg($"[DeadDrop] 🎯 Spawning scheduled drop for Day {currentDay}");
                    SpawnDrop(drop);
                    JsonDataStore.PendingDrops.Remove(currentDay);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Time change error: {ex.Message}");
            }
        }

        /// <summary>
        /// Force spawn drop for testing
        /// </summary>
        public static string? ForceSpawnDrop(int day, List<string> packet, string type = "debug", int hour = -1)
        {
            try
            {
                if (hour == -1)
                {
                    hour = TimeManager.Instance?.CurrentTime ?? 12;
                }

                var player = Player.Local;
                string playerName = player?.PlayerName ?? "Unknown";
                string orgName = type == "immediate_test" ? "Mrs. Stacks (Test)" : "DevCommand";

                var record = new JsonDataStore.DropRecord
                {
                    Day = day,
                    Items = packet,
                    DropHour = hour,
                    DropTime = $"{hour:D2}:00",
                    Org = orgName,
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = type,
                    Location = ""
                };

                Logger.Msg($"[DeadDrop] 🔧 Force spawning {type} drop with {packet.Count} items");
                SpawnDrop(record);
                return record.Location;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Force spawn failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Spawn immediate drop (for Mrs. Stacks orders)
        /// </summary>
        public static string? SpawnImmediateDrop(JsonDataStore.DropRecord dropRecord)
        {
            try
            {
                Logger.Msg($"[DeadDrop] 🚀 Spawning immediate drop from {dropRecord.Org}");
                SpawnDrop(dropRecord);
                return dropRecord.Location;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Immediate spawn failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Main method to spawn drops into game dead drop locations
        /// </summary>
        private static void SpawnDrop(JsonDataStore.DropRecord drop)
        {
            try
            {
                Logger.Msg($"[DeadDrop] 📦 Spawning drop: {drop.Items.Count} items from {drop.Org}");

                var targetDeadDrop = FindSuitableDeadDrop();
                if (targetDeadDrop?.Storage == null)
                {
                    Logger.Warn("[DeadDrop] ❌ No suitable dead drop found");
                    return;
                }

                Logger.Msg($"[DeadDrop] 📍 Target: {targetDeadDrop.DeadDropName}");
                drop.Location = targetDeadDrop.DeadDropName;

                // Consolidate items and cash
                var consolidatedItems = new Dictionary<string, int>();
                int cashAmount = 0;
                
                foreach (string entry in drop.Items)
                {
                    string[] parts = entry.Split(':');
                    string itemId = parts[0];
                    int amount = (parts.Length > 1 && int.TryParse(parts[1], out int parsed)) ? parsed : 1;

                    if (itemId == "cash")
                    {
                        cashAmount += amount;
                        continue;
                    }

                    string stackingKey = CreateItemStackingKey(itemId);
                    consolidatedItems[stackingKey] = consolidatedItems.GetValueOrDefault(stackingKey, 0) + amount;
                }

                int success = 0, fail = 0;

                // Add cash first
                if (cashAmount > 0)
                {
                    if (AddCashToStorage(targetDeadDrop.Storage, cashAmount))
                    {
                        success++;
                        Logger.Msg($"[DeadDrop] ✅ Added ${cashAmount} cash");
                    }
                    else
                    {
                        fail++;
                        Logger.Warn($"[DeadDrop] ❌ Failed to add ${cashAmount} cash");
                    }
                }

                // Add items
                foreach (var kvp in consolidatedItems)
                {
                    string itemId = ExtractItemIdFromStackingKey(kvp.Key);
                    int totalAmount = kvp.Value;
                    
                    var itemDef = Il2CppScheduleOne.Registry.GetItem(itemId);
                    if (itemDef == null)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ Invalid item: {itemId}");
                        fail++;
                        continue;
                    }

                    try
                    {
                        var itemInstance = CreateItemInstance(itemDef, totalAmount);
                        if (itemInstance != null && AddItemToStorage(targetDeadDrop.Storage, itemInstance))
                        {
                            success++;
                            Logger.Msg($"[DeadDrop] ✅ Added {itemId} x{totalAmount}");
                        }
                        else
                        {
                            fail++;
                            Logger.Warn($"[DeadDrop] ❌ Failed to add {itemId} x{totalAmount}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[DeadDrop] ❌ Error with {itemId}: {ex.Message}");
                        fail++;
                    }
                }

                Logger.Msg($"[DeadDrop] ✅ Drop complete: {success} added, {fail} failed at {targetDeadDrop.DeadDropName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Spawn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Find suitable dead drop location
        /// </summary>
        private static Il2CppScheduleOne.Economy.DeadDrop? FindSuitableDeadDrop()
        {
            try
            {
                var allDeadDrops = Il2CppScheduleOne.Economy.DeadDrop.DeadDrops;
                if (allDeadDrops == null || allDeadDrops.Count == 0)
                {
                    Logger.Warn("[DeadDrop] ⚠️ No dead drops found in game");
                    return null;
                }

                // Filter for available drops
                var availableDrops = new List<Il2CppScheduleOne.Economy.DeadDrop>();
                for (int i = 0; i < allDeadDrops.Count; i++)
                {
                    var deadDrop = allDeadDrops[i];
                    if (deadDrop?.Storage != null && IsDeadDropAvailable(deadDrop))
                    {
                        availableDrops.Add(deadDrop);
                    }
                }

                if (availableDrops.Count == 0)
                {
                    Logger.Warn("[DeadDrop] ⚠️ No available dead drops");
                    return null;
                }

                // Try game's method first
                var player = Player.Local;
                if (player?.transform != null)
                {
                    var randomDrop = Il2CppScheduleOne.Economy.DeadDrop.GetRandomEmptyDrop(player.transform.position);
                    if (randomDrop != null)
                    {
                        Logger.Msg($"[DeadDrop] 🎯 Selected: {randomDrop.DeadDropName}");
                        return randomDrop;
                    }
                }

                // Fallback: manual selection
                var selected = availableDrops[new System.Random().Next(availableDrops.Count)];
                Logger.Msg($"[DeadDrop] 🎲 Manual selection: {selected.DeadDropName}");
                return selected;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Dead drop search failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if dead drop has space
        /// </summary>
        private static bool IsDeadDropAvailable(Il2CppScheduleOne.Economy.DeadDrop deadDrop)
        {
            try
            {
                return deadDrop?.Storage != null && deadDrop.Storage.ItemCount < deadDrop.Storage.SlotCount;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create item instance from definition
        /// </summary>
        private static ItemInstance? CreateItemInstance(ItemDefinition definition, int amount = 1)
        {
            try
            {
                // Try StorableItemDefinition first
                if (definition is StorableItemDefinition storableDef)
                {
                    var instance = storableDef.GetDefaultInstance(amount);
                    if (instance != null) return instance;
                }

                // Try CashDefinition for cash items
                if (definition is CashDefinition cashDef)
                {
                    var cashInstance = cashDef.GetDefaultInstance(amount);
                    if (cashInstance != null) return cashInstance;
                }

                // Fallback: reflection-based approach
                var getDefaultMethod = definition.GetType().GetMethod("GetDefaultInstance");
                if (getDefaultMethod != null)
                {
                    var parameters = getDefaultMethod.GetParameters();
                    object? result;
                    
                    if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(int))
                    {
                        result = getDefaultMethod.Invoke(definition, new object[] { amount });
                    }
                    else
                    {
                        result = getDefaultMethod.Invoke(definition, new object[0]);
                        if (result is ItemInstance instance && amount != 1)
                        {
                            SetItemQuantity(instance, amount);
                        }
                    }
                    
                    if (result is ItemInstance itemInstance)
                    {
                        return itemInstance;
                    }
                }

                Logger.Error($"[DeadDrop] ❌ Failed to create instance for: {definition.name}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Instance creation error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Set item quantity using reflection
        /// </summary>
        private static void SetItemQuantity(ItemInstance item, int quantity)
        {
            try
            {
                var itemType = item.GetType();
                string[] quantityProps = { "Quantity", "Amount", "Count", "Stack", "StackSize" };
                
                foreach (string propName in quantityProps)
                {
                    var prop = itemType.GetProperty(propName, 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(item, quantity);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[DeadDrop] ⚠️ Failed to set quantity: {ex.Message}");
            }
        }

        /// <summary>
        /// Add item to storage
        /// </summary>
        private static bool AddItemToStorage(StorageEntity storage, ItemInstance item)
        {
            try
            {
                if (item == null || storage == null) return false;

                if (!storage.CanItemFit(item, 1)) return false;

                int beforeCount = storage.ItemCount;
                storage.InsertItem(item, true);
                return storage.ItemCount > beforeCount;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Storage insertion error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Add cash to storage using MoneyManager
        /// </summary>
        private static bool AddCashToStorage(StorageEntity storage, int amount)
        {
            try
            {
                // Method 1: Try MoneyManager.GetCashInstance first
                var moneyManager = MoneyManager.Instance;
                if (moneyManager != null)
                {
                    var cashInstance = moneyManager.GetCashInstance((float)amount);
                    if (cashInstance != null)
                    {
                        Logger.Msg($"[DeadDrop] 💰 Using MoneyManager.GetCashInstance for ${amount}");
                        return AddItemToStorage(storage, cashInstance);
                    }
                }

                // Method 2: Try creating cash instance via Registry
                var cashDef = Il2CppScheduleOne.Registry.GetItem<CashDefinition>("cash");
                if (cashDef != null)
                {
                    try
                    {
                        var cashInstance = cashDef.GetDefaultInstance(amount);
                        if (cashInstance is CashInstance cashInst)
                        {
                            // Ensure correct balance
                            if (Math.Abs(cashInst.Balance - amount) > 0.01f)
                            {
                                cashInst.SetBalance((float)amount, false);
                            }
                            Logger.Msg($"[DeadDrop] 💰 Using CashDefinition.GetDefaultInstance for ${amount}");
                            return AddItemToStorage(storage, cashInst);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ CashDefinition.GetDefaultInstance failed: {ex.Message}");
                    }
                    
                    // Try manual CashInstance creation
                    try
                    {
                        var cashInstance = new CashInstance(cashDef, amount);
                        if (cashInstance != null)
                        {
                            if (Math.Abs(cashInstance.Balance - amount) > 0.01f)
                            {
                                cashInstance.SetBalance((float)amount, false);
                            }
                            Logger.Msg($"[DeadDrop] 💰 Using manual CashInstance for ${amount}");
                            return AddItemToStorage(storage, cashInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ Manual CashInstance creation failed: {ex.Message}");
                    }
                }

                // Method 3: Last resort - add to player inventory directly (no double-adding!)
                if (moneyManager != null)
                {
                    float currentBalance = moneyManager.cashBalance;
                    moneyManager.ChangeCashBalance((float)amount, true, true);
                    
                    if (Math.Abs(moneyManager.cashBalance - (currentBalance + amount)) < 0.01f)
                    {
                        Logger.Msg($"[DeadDrop] 💰 Added ${amount} to player inventory (storage unavailable)");
                        return true;
                    }
                }

                Logger.Error($"[DeadDrop] ❌ All cash methods failed for ${amount}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Cash handling error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create stacking key for items (simple implementation)
        /// </summary>
        private static string CreateItemStackingKey(string itemId)
        {
            // Simple key for now - could be enhanced to include item properties
            return itemId;
        }

        /// <summary>
        /// Extract item ID from stacking key
        /// </summary>
        private static string ExtractItemIdFromStackingKey(string stackingKey)
        {
            return stackingKey.Split(':')[0];
        }
    }
} 