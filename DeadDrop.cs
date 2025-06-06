using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Il2CppScheduleOne;
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
            Logger.Info("✅ System initialized", "DeadDrop");
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;
            Logger.Info("🔌 Shutdown complete", "DeadDrop");
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

                Logger.Debug($"⏰ Time check - Day: {currentDay}, Hour: {currentHour}", "DeadDrop");

                if (!SaveFileJsonDataStore.PendingDrops.TryGetValue(currentDay, out var dropsForDay) || 
                    dropsForDay == null || dropsForDay.Count == 0)
                {
                    return; // No drops scheduled for today
                }

                var dropsToSpawn = new List<SaveFileJsonDataStore.DropRecord>();
                foreach (var drop in dropsForDay)
                {
                    if (currentHour >= drop.DropHour && string.IsNullOrEmpty(drop.Location))
                    {
                        dropsToSpawn.Add(drop);
                    }
                }

                if (dropsToSpawn.Count > 0)
                {
                    Logger.Debug($"🎯 Spawning {dropsToSpawn.Count} scheduled drops for Day {currentDay}", "DeadDrop");
                    
                    foreach (var drop in dropsToSpawn)
                    {
                        SpawnDrop(drop);
                    }
                    
                    // Remove spawned drops from pending list
                    dropsForDay.RemoveAll(drop => dropsToSpawn.Contains(drop));
                    
                    // If no more drops for this day, remove the day entry
                    if (dropsForDay.Count == 0)
                    {
                        SaveFileJsonDataStore.PendingDrops.Remove(currentDay);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Time change error: {ex.Message}", "DeadDrop");
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

                var record = new SaveFileJsonDataStore.DropRecord
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

                Logger.Debug($"🔧 Force spawning {type} drop with {packet.Count} items", "DeadDrop");
                SpawnDrop(record);
                return record.Location;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Force spawn failed: {ex.Message}", "DeadDrop");
                return null;
            }
        }

        /// <summary>
        /// Spawn a drop immediately from a drop record and return location
        /// </summary>
        public static string? SpawnImmediateDrop(SaveFileJsonDataStore.DropRecord drop)
        {
            try
            {
                Logger.Debug($"📦 Spawning drop: {drop.Items.Count} items from {drop.Org}", "DeadDrop");

                var targetDeadDrop = FindSuitableDeadDrop();
                if (targetDeadDrop?.Storage == null)
                {
                    Logger.Warn("❌ No suitable dead drop found", "DeadDrop");
                    return null;
                }

                Logger.Debug($"📍 Target: {targetDeadDrop.DeadDropName}", "DeadDrop");
                drop.Location = targetDeadDrop.DeadDropName;

                // Save the complete drop record with location
                SaveFileJsonDataStore.SaveDropRecord(drop);

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
                        Logger.Debug($"✅ Added ${cashAmount} cash", "DeadDrop");
                    }
                    else
                    {
                        fail++;
                        Logger.Warn($"❌ Failed to add ${cashAmount} cash", "DeadDrop");
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
                        Logger.Warn($"⚠️ Invalid item: {itemId}", "DeadDrop");
                        fail++;
                        continue;
                    }

                    try
                    {
                        var itemInstance = CreateItemInstance(itemDef, totalAmount);
                        if (itemInstance != null && AddItemToStorage(targetDeadDrop.Storage, itemInstance))
                        {
                            success++;
                            Logger.Debug($"✅ Added {itemId} x{totalAmount}", "DeadDrop");
                        }
                        else
                        {
                            fail++;
                            Logger.Warn($"❌ Failed to add {itemId} x{totalAmount}", "DeadDrop");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"❌ Error with {itemId}: {ex.Message}", "DeadDrop");
                        fail++;
                    }
                }

                Logger.Debug($"✅ Drop complete: {success} added, {fail} failed at {targetDeadDrop.DeadDropName}", "DeadDrop");
                return targetDeadDrop.DeadDropName;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Spawn failed: {ex.Message}", "DeadDrop");
                return null;
            }
        }

        /// <summary>
        /// Main method to spawn drops into game dead drop locations
        /// </summary>
        private static void SpawnDrop(SaveFileJsonDataStore.DropRecord drop)
        {
            try
            {
                Logger.Debug($"📦 Spawning drop: {drop.Items.Count} items from {drop.Org}", "DeadDrop");

                var targetDeadDrop = FindSuitableDeadDrop();
                if (targetDeadDrop?.Storage == null)
                {
                    Logger.Warn("❌ No suitable dead drop found", "DeadDrop");
                    return;
                }

                Logger.Debug($"📍 Target: {targetDeadDrop.DeadDropName}", "DeadDrop");
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
                        Logger.Debug($"✅ Added ${cashAmount} cash", "DeadDrop");
                    }
                    else
                    {
                        fail++;
                        Logger.Warn($"❌ Failed to add ${cashAmount} cash", "DeadDrop");
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
                        Logger.Warn($"⚠️ Invalid item: {itemId}", "DeadDrop");
                        fail++;
                        continue;
                    }

                    try
                    {
                        var itemInstance = CreateItemInstance(itemDef, totalAmount);
                        if (itemInstance != null && AddItemToStorage(targetDeadDrop.Storage, itemInstance))
                        {
                            success++;
                            Logger.Debug($"✅ Added {itemId} x{totalAmount}", "DeadDrop");
                        }
                        else
                        {
                            fail++;
                            Logger.Warn($"❌ Failed to add {itemId} x{totalAmount}", "DeadDrop");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"❌ Error with {itemId}: {ex.Message}", "DeadDrop");
                        fail++;
                    }
                }

                Logger.Debug($"✅ Drop complete: {success} added, {fail} failed at {targetDeadDrop.DeadDropName}", "DeadDrop");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Spawn failed: {ex.Message}", "DeadDrop");
            }
        }

        /// <summary>
        /// Find suitable dead drop location (avoid conflicts with other active drops)
        /// </summary>
        private static Il2CppScheduleOne.Economy.DeadDrop? FindSuitableDeadDrop()
        {
            try
            {
                var allDeadDrops = Il2CppScheduleOne.Economy.DeadDrop.DeadDrops;
                if (allDeadDrops == null || allDeadDrops.Count == 0)
                {
                    Logger.Warn("⚠️ No dead drops found in game", "DeadDrop");
                    return null;
                }

                Logger.Debug($"🗺️ Found {allDeadDrops.Count} total dead drops in game:", "DeadDrop");
                for (int i = 0; i < allDeadDrops.Count; i++)
                {
                    var deadDrop = allDeadDrops[i];
                    if (deadDrop != null)
                    {
                        Logger.Debug($"📍 {i + 1}. {deadDrop.DeadDropName} - Available: {IsDeadDropAvailable(deadDrop)}", "DeadDrop");
                    }
                }

                // Get currently assigned drop locations to avoid conflicts
                var assignedLocations = new HashSet<string>();
                var allDrops = SaveFileJsonDataStore.GetAllDrops();
                foreach (var drop in allDrops)
                {
                    if (!string.IsNullOrEmpty(drop.Location) && !drop.IsCollected)
                    {
                        assignedLocations.Add(drop.Location);
                    }
                }

                Logger.Debug($"🚫 Currently assigned locations to avoid: {string.Join(", ", assignedLocations)}", "DeadDrop");

                // Filter for available drops that aren't already assigned
                var availableDrops = new List<Il2CppScheduleOne.Economy.DeadDrop>();
                for (int i = 0; i < allDeadDrops.Count; i++)
                {
                    var deadDrop = allDeadDrops[i];
                    if (deadDrop?.Storage != null && 
                        IsDeadDropAvailable(deadDrop) && 
                        !assignedLocations.Contains(deadDrop.DeadDropName))
                    {
                        availableDrops.Add(deadDrop);
                    }
                }

                if (availableDrops.Count == 0)
                {
                    Logger.Warn("⚠️ No available dead drops (all may be assigned or full)", "DeadDrop");
                    // Fallback: allow reuse if no other options
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
                        Logger.Error("❌ No dead drops available at all", "DeadDrop");
                        return null;
                    }
                }

                Logger.Debug($"✅ {availableDrops.Count} dead drops available for assignment", "DeadDrop");

                // Prefer game's selection method if available
                var player = Player.Local;
                if (player?.transform != null && availableDrops.Count > 1)
                {
                    // Try to get a random drop from our available list near player
                    var nearbyDrops = availableDrops.Where(drop => 
                        Vector3.Distance(player.transform.position, drop.transform.position) < 1000f).ToList();
                    
                    if (nearbyDrops.Count > 0)
                    {
                        var selected = nearbyDrops[new System.Random().Next(nearbyDrops.Count)];
                        Logger.Debug($"🎯 Selected nearby: {selected.DeadDropName}", "DeadDrop");
                        return selected;
                    }
                }

                // Fallback: manual selection from available drops
                var finalSelected = availableDrops[new System.Random().Next(availableDrops.Count)];
                Logger.Debug($"🎲 Selected: {finalSelected.DeadDropName} (avoided {assignedLocations.Count} assigned locations)", "DeadDrop");
                return finalSelected;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Dead drop search failed: {ex.Message}", "DeadDrop");
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

                Logger.Error($"❌ Failed to create instance for: {definition.name}", "DeadDrop");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Instance creation error: {ex.Message}", "DeadDrop");
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
                Logger.Warn($"⚠️ Failed to set quantity: {ex.Message}", "DeadDrop");
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
                Logger.Error($"❌ Storage insertion error: {ex.Message}", "DeadDrop");
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
                        Logger.Debug($"💰 Using MoneyManager.GetCashInstance for ${amount}", "DeadDrop");
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
                            Logger.Debug($"💰 Using CashDefinition.GetDefaultInstance for ${amount}", "DeadDrop");
                            return AddItemToStorage(storage, cashInst);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ CashDefinition.GetDefaultInstance failed: {ex.Message}", "DeadDrop");
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
                            Logger.Debug($"💰 Using manual CashInstance for ${amount}", "DeadDrop");
                            return AddItemToStorage(storage, cashInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Manual CashInstance creation failed: {ex.Message}", "DeadDrop");
                    }
                }

                // Method 3: Last resort - add to player inventory directly (no double-adding!)
                if (moneyManager != null)
                {
                    float currentBalance = moneyManager.cashBalance;
                    moneyManager.ChangeCashBalance((float)amount, true, true);
                    
                    if (Math.Abs(moneyManager.cashBalance - (currentBalance + amount)) < 0.01f)
                    {
                        Logger.Debug($"💰 Added ${amount} to player inventory (storage unavailable)", "DeadDrop");
                        return true;
                    }
                }

                Logger.Error($"❌ All cash methods failed for ${amount}", "DeadDrop");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Cash handling error: {ex.Message}", "DeadDrop");
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