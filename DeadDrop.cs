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

            Logger.Msg("[DeadDrop] ✅ System initialized using Schedule I's dead drop system.");
        }

        public static void Shutdown()
        {
            if (!_initialized) return;
            _initialized = false;

            Logger.Msg("[DeadDrop] 🔌 Shutdown complete.");
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

                // Get multiple time values for debugging
                int elapsedDays = timeManager.ElapsedDays;
                int currentTime = timeManager.CurrentTime;
                
                // Try to get actual game day - CurrentTime might be the full day number
                int currentDay = elapsedDays;
                int currentHour = currentTime;
                
                // If CurrentTime is > 2400 (24 hours), it might include day information
                if (currentTime > 2400)
                {
                    // CurrentTime might be formatted as DDDHHMM or similar
                    currentDay = currentTime / 10000; // Extract day part
                    currentHour = (currentTime % 10000) / 100; // Extract hour part
                }
                else if (currentTime > 100)
                {
                    // Standard HHMM format, but day might be wrong
                    currentHour = currentTime;
                    // For now, let's see what happens - might need different approach
                }

                Logger.Msg($"[DeadDrop] ⏰ Time check - ElapsedDays: {elapsedDays}, CurrentTime: {currentTime}");
                Logger.Msg($"[DeadDrop] 🔍 Calculated - Day: {currentDay}, Hour: {currentHour}");

                // Check if we have a drop scheduled for today
                if (!JsonDataStore.PendingDrops.TryGetValue(currentDay, out var drop))
                {
                    Logger.Msg($"[DeadDrop] 📭 No scheduled drop found for Day {currentDay}");
                    return;
                }

                Logger.Msg($"[DeadDrop] 📦 Found scheduled drop for Day {currentDay}: {drop.Items.Count} items");

                // Check if it's time to spawn the drop
                if (currentHour >= drop.DropHour)
                {
                    Logger.Msg($"[DeadDrop] 🎯 TIME TO DROP! Spawning drop now!");
                    SpawnDrop(drop);
                    
                    // Remove from pending drops to prevent respawning
                    JsonDataStore.PendingDrops.Remove(currentDay);
                }
                else
                {
                    Logger.Msg($"[DeadDrop] ⏰ Drop scheduled for later today ({drop.DropTime})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Error processing time change: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Forces a drop spawn for testing purposes
        /// </summary>
        public static void ForceSpawnDrop(int day, List<string> packet, string type = "debug", int hour = -1)
        {
            try
            {
                if (hour == -1)
                {
                    var timeManager = TimeManager.Instance;
                    hour = timeManager?.CurrentTime ?? 12;
                }

                // Get player context for the drop
                var player = Il2CppScheduleOne.PlayerScripts.Player.Local;
                string playerName = player?.PlayerName ?? "Unknown";
                string orgName = type == "immediate_test" ? "Mrs. Stacks (Test)" : "DevCommand";

                var record = new JsonDataStore.DropRecord
                {
                    Day = day,
                    Items = packet,
                    DropHour = hour,
                    DropTime = $"{hour:D2}:00", // Format as HH:00
                    Org = orgName,
                    CreatedTime = DateTime.Now.ToString("s"),
                    Type = type,
                    Location = "" // Will be filled when dead drop is found
                };

                Logger.Msg($"[DeadDrop] 🔧 Force spawning drop for Day {day} @ {hour:D2}:00 ({type})");
                Logger.Msg($"[DeadDrop] 👤 Target: {playerName} | From: {orgName}");
                Logger.Msg($"[DeadDrop] 📦 Contains: {packet.Count} items");
                
                SpawnDrop(record);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Failed to force spawn drop: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Spawns a drop into an actual game dead drop location
        /// </summary>
        private static void SpawnDrop(JsonDataStore.DropRecord drop)
        {
            try
            {
                Logger.Msg($"[DeadDrop] 📦 Spawning drop for Day {drop.Day}...");
                Logger.Msg($"[DeadDrop] 🧾 From: {drop.Org} | {drop.Type} | {drop.DropTime} | Items: {drop.Items.Count}");

                // Find a suitable dead drop using the game's system
                Il2CppScheduleOne.Economy.DeadDrop? targetDeadDrop = FindSuitableDeadDrop();
                
                if (targetDeadDrop == null)
                {
                    Logger.Warn("[DeadDrop] ❌ No suitable dead drop location found.");
                    return;
                }

                // Get the storage entity from the dead drop
                StorageEntity targetStorage = targetDeadDrop.Storage;
                if (targetStorage == null)
                {
                    Logger.Warn($"[DeadDrop] ❌ Dead drop '{targetDeadDrop.DeadDropName}' has no storage entity.");
                    return;
                }

                Logger.Msg($"[DeadDrop] 📍 Target dead drop: {targetDeadDrop.DeadDropName}");
                Logger.Msg($"[DeadDrop] 📍 Description: {targetDeadDrop.DeadDropDescription}");
                Logger.Msg($"[DeadDrop] 📍 Location: {targetStorage.transform.position}");

                // Update our record with the location
                drop.Location = targetDeadDrop.DeadDropName;

                // IMPROVED: Better item consolidation that respects item properties
                var consolidatedItems = new Dictionary<string, int>();
                var cashAmount = 0;
                
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

                    // Create a stacking key that includes item properties for proper stacking
                    string stackingKey = CreateItemStackingKey(itemId);
                    
                    if (consolidatedItems.ContainsKey(stackingKey))
                        consolidatedItems[stackingKey] += amount;
                    else
                        consolidatedItems[stackingKey] = amount;
                }

                Logger.Msg($"[DeadDrop] 🔄 Consolidated {drop.Items.Count} items into {consolidatedItems.Count} item stacks + ${cashAmount} cash");

                // Process cash first if present
                int success = 0, fail = 0;
                if (cashAmount > 0)
                {
                    bool cashAdded = AddCashToStorage(targetStorage, cashAmount);
                    if (cashAdded)
                    {
                        success++;
                        Logger.Msg($"[DeadDrop] ✅ Added ${cashAmount} cash to dead drop");
                    }
                    else
                    {
                        fail++;
                        Logger.Warn($"[DeadDrop] ❌ Failed to add ${cashAmount} cash to dead drop");
                    }
                }

                // Process consolidated items
                foreach (var kvp in consolidatedItems)
                {
                    string stackingKey = kvp.Key;
                    int totalAmount = kvp.Value;
                    
                    // Extract the base item ID from the stacking key
                    string itemId = ExtractItemIdFromStackingKey(stackingKey);

                    // Get item definition
                    var itemDef = Il2CppScheduleOne.Registry.GetItem(itemId);
                    if (itemDef == null)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ Invalid item ID: '{itemId}' (from key: '{stackingKey}')");
                        fail++;
                        continue;
                    }

                    // Create a single item instance with the total quantity
                    try
                    {
                        var itemInstance = CreateItemInstance(itemDef, totalAmount);
                        if (itemInstance == null)
                        {
                            Logger.Warn($"[DeadDrop] ❌ Failed to create item instance: '{itemId}' x{totalAmount}");
                            fail++;
                            continue;
                        }

                        // Try to add to storage
                        bool added = AddItemToStorage(targetStorage, itemInstance);
                        if (added)
                        {
                            success++;
                            Logger.Msg($"[DeadDrop] ✅ Added {itemId} x{totalAmount} to dead drop");
                        }
                        else
                        {
                            Logger.Warn($"[DeadDrop] ❌ Failed to add {itemId} x{totalAmount} to dead drop");
                            fail++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[DeadDrop] ❌ Error creating/adding item {itemId} x{totalAmount}: {ex.Message}");
                        fail++;
                    }
                }

                Logger.Msg($"[DeadDrop] ✅ Drop complete: {success} stacks added, {fail} failed.");
                Logger.Msg($"[DeadDrop] 📍 Items placed at: {targetDeadDrop.DeadDropName}");
                
                // Send notification to player
                NotifyPlayer(targetDeadDrop, drop);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Failed to spawn drop: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Finds a suitable dead drop location using Schedule I's dead drop system
        /// </summary>
        private static Il2CppScheduleOne.Economy.DeadDrop? FindSuitableDeadDrop()
        {
            try
            {
                // Get all dead drops from the game's system
                var allDeadDrops = Il2CppScheduleOne.Economy.DeadDrop.DeadDrops;
                if (allDeadDrops == null || allDeadDrops.Count == 0)
                {
                    Logger.Warn("[DeadDrop] ⚠️ No dead drops found in game system");
                    return null;
                }

                Logger.Msg($"[DeadDrop] 🎯 Found {allDeadDrops.Count} dead drops in game system");

                // Filter for available dead drops (not currently occupied)
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
                    Logger.Warn("[DeadDrop] ⚠️ No available dead drops found");
                    
                    // Debug: show what we found
                    for (int i = 0; i < Math.Min(allDeadDrops.Count, 10); i++)
                    {
                        var deadDrop = allDeadDrops[i];
                        string reason = GetDeadDropUnavailableReason(deadDrop);
                        Logger.Msg($"[DeadDrop] 📍 {i+1}: {deadDrop?.DeadDropName ?? "NULL"} - {reason}");
                    }
                    
                    return null;
                }

                Logger.Msg($"[DeadDrop] 🎯 Found {availableDrops.Count} available dead drops:");
                
                // Log available dead drops
                for (int i = 0; i < Math.Min(availableDrops.Count, 5); i++)
                {
                    var deadDrop = availableDrops[i];
                    Logger.Msg($"[DeadDrop] 📍 {i+1}: {deadDrop.DeadDropName} - {deadDrop.DeadDropDescription}");
                }

                // Try to use the game's method to get a random empty drop
                var player = Player.Local;
                if (player?.transform != null)
                {
                    Vector3 playerPos = player.transform.position;
                    var randomDrop = Il2CppScheduleOne.Economy.DeadDrop.GetRandomEmptyDrop(playerPos);
                    
                    if (randomDrop != null)
                    {
                        Logger.Msg($"[DeadDrop] 🎯 Game selected dead drop: {randomDrop.DeadDropName}");
                        return randomDrop;
                    }
                    else
                    {
                        Logger.Msg("[DeadDrop] 🎲 Game's GetRandomEmptyDrop returned null, using manual selection");
                    }
                }

                // Fallback: manually select a random available dead drop
                var random = new System.Random();
                var selectedDrop = availableDrops[random.Next(availableDrops.Count)];
                Logger.Msg($"[DeadDrop] 🎲 Manually selected: {selectedDrop.DeadDropName}");
                
                return selectedDrop;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Error finding dead drop: {ex.Message}");
                Logger.Exception(ex);
                return null;
            }
        }

        /// <summary>
        /// Checks if a dead drop is available for use
        /// </summary>
        private static bool IsDeadDropAvailable(Il2CppScheduleOne.Economy.DeadDrop deadDrop)
        {
            try
            {
                if (deadDrop?.Storage == null) return false;
                
                // Check if storage has available space
                var storage = deadDrop.Storage;
                
                // Simple check: if storage has space for at least one item
                return storage.ItemCount < storage.SlotCount;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[DeadDrop] ⚠️ Error checking dead drop availability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the reason why a dead drop is unavailable (for debugging)
        /// </summary>
        private static string GetDeadDropUnavailableReason(Il2CppScheduleOne.Economy.DeadDrop deadDrop)
        {
            try
            {
                if (deadDrop == null) return "NULL_DEAD_DROP";
                if (deadDrop.Storage == null) return "NO_STORAGE";
                if (!deadDrop.gameObject.activeInHierarchy) return "INACTIVE";
                if (deadDrop.Storage.ItemCount >= deadDrop.Storage.SlotCount) return "FULL";
                
                return "AVAILABLE";
            }
            catch
            {
                return "ERROR_CHECKING";
            }
        }

        /// <summary>
        /// Creates an item instance from definition
        /// </summary>
        private static ItemInstance? CreateItemInstance(ItemDefinition definition, int amount = 1)
        {
            try
            {
                Logger.Msg($"[DeadDrop] 🔧 Creating item instance for: {definition.name} (type: {definition.GetType().Name}) x{amount}");
                
                // Method 1: Try StorableItemDefinition.GetDefaultInstance (this is the main method)
                if (definition is StorableItemDefinition storableDef)
                {
                    Logger.Msg($"[DeadDrop] 📦 Using StorableItemDefinition.GetDefaultInstance with amount {amount}");
                    var instance = storableDef.GetDefaultInstance(amount);
                    if (instance != null)
                    {
                        Logger.Msg($"[DeadDrop] ✅ Successfully created instance: {instance.GetType().Name}");
                        return instance;
                    }
                    else
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ StorableItemDefinition.GetDefaultInstance returned null");
                    }
                }

                // Method 2: Try CashDefinition specifically (for cash items)
                if (definition is CashDefinition cashDef)
                {
                    Logger.Msg($"[DeadDrop] 💰 Using CashDefinition.GetDefaultInstance with amount {amount}");
                    var cashInstance = cashDef.GetDefaultInstance(amount);
                    if (cashInstance != null)
                    {
                        Logger.Msg($"[DeadDrop] ✅ Successfully created cash instance: {cashInstance.GetType().Name}");
                        return cashInstance;
                    }
                    else
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ CashDefinition.GetDefaultInstance returned null");
                    }
                }

                // Method 3: Try direct ItemDefinition.GetDefaultInstance (fallback)
                try
                {
                    Logger.Msg($"[DeadDrop] 📦 Trying ItemDefinition.GetDefaultInstance fallback");
                    var getDefaultMethod = definition.GetType().GetMethod("GetDefaultInstance");
                    if (getDefaultMethod != null)
                    {
                        var parameters = getDefaultMethod.GetParameters();
                        object? result = null;
                        
                        if (parameters.Length >= 1 && parameters[0].ParameterType == typeof(int))
                        {
                            // Call with amount parameter
                            result = getDefaultMethod.Invoke(definition, new object[] { amount });
                        }
                        else
                        {
                            // Call without parameters and set quantity later
                            result = getDefaultMethod.Invoke(definition, new object[0]);
                        }
                        
                        if (result is ItemInstance instance)
                        {
                            // If we called without amount, try to set the quantity
                            if (parameters.Length == 0 && amount != 1)
                            {
                                SetItemQuantity(instance, amount);
                            }
                            Logger.Msg($"[DeadDrop] ✅ Successfully created instance: {instance.GetType().Name}");
                            return instance;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ GetDefaultInstance method failed: {ex.Message}");
                }

                /*
                // Method 4: Try creating with constructor (last resort)
                try
                {
                    Logger.Msg($"[DeadDrop] 📦 Trying constructor approach");
                    
                    // Look for StorableItemInstance or CashInstance constructors
                    if (definition is CashDefinition)
                    {
                        // Try to create CashInstance directly - need IntPtr for IL2CPP
                        var definitionPtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(definition);
                        var cashInstance = new CashInstance(definitionPtr);
                        if (cashInstance != null)
                        {
                            // Set the amount manually
                            SetItemQuantity(cashInstance, amount);
                            Logger.Msg($"[DeadDrop] ✅ Successfully created CashInstance via constructor");
                            return cashInstance;
                        }
                    }
                    else if (definition is StorableItemDefinition)
                    {
                        // Try to create StorableItemInstance - need IntPtr for IL2CPP
                        var definitionPtr = Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(definition);
                        var storableInstance = new StorableItemInstance(definitionPtr);
                        if (storableInstance != null)
                        {
                            SetItemQuantity(storableInstance, amount);
                            Logger.Msg($"[DeadDrop] ✅ Successfully created StorableItemInstance via constructor");
                            return storableInstance;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Constructor approach failed: {ex.Message}");
                }
                */

                Logger.Error($"[DeadDrop] ❌ All methods failed for item definition: {definition.name} (type: {definition.GetType().Name})");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Error creating item instance: {ex.Message}");
                Logger.Exception(ex);
                return null;
            }
        }

        /// <summary>
        /// Attempts to set the quantity of an item instance
        /// </summary>
        private static void SetItemQuantity(ItemInstance item, int quantity)
        {
            try
            {
                // Look for quantity-related properties
                var itemType = item.GetType();
                
                // Common property names for quantity
                string[] quantityProps = { "Quantity", "Amount", "Count", "Stack", "StackSize" };
                
                foreach (string propName in quantityProps)
                {
                    var prop = itemType.GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (prop != null && prop.CanWrite && prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(item, quantity);
                        Logger.Msg($"[DeadDrop] 🔢 Set {propName} to {quantity}");
                        return;
                    }
                }
                
                Logger.Warn($"[DeadDrop] ⚠️ Could not find quantity property on {itemType.Name}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[DeadDrop] ⚠️ Failed to set quantity: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds an item to storage using the IL2CPP API
        /// </summary>
        private static bool AddItemToStorage(StorageEntity storage, ItemInstance item)
        {
            try
            {
                Logger.Msg($"[DeadDrop] 🔧 Attempting to add item to storage...");
                Logger.Msg($"[DeadDrop] 📦 Item: {item?.GetType().Name ?? "NULL"}");
                Logger.Msg($"[DeadDrop] 🗃️ Storage: {storage?.GetType().Name ?? "NULL"} (Items: {storage?.ItemCount ?? -1}/{storage?.SlotCount ?? -1})");

                if (item == null)
                {
                    Logger.Error($"[DeadDrop] ❌ Cannot add null item to storage");
                    return false;
                }

                if (storage == null)
                {
                    Logger.Error($"[DeadDrop] ❌ Cannot add item to null storage");
                    return false;
                }

                // Check if the item can fit
                bool canFit = storage.CanItemFit(item, 1);
                Logger.Msg($"[DeadDrop] 🔍 CanItemFit result: {canFit}");
                
                if (!canFit)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Item {item} cannot fit in storage (full or incompatible)");
                    return false;
                }

                // Store count before insertion
                int beforeCount = storage.ItemCount;
                
                // Insert the item
                Logger.Msg($"[DeadDrop] 📥 Inserting item...");
                storage.InsertItem(item, true);
                
                // Verify insertion
                int afterCount = storage.ItemCount;
                Logger.Msg($"[DeadDrop] 📊 Storage count: {beforeCount} → {afterCount}");
                
                if (afterCount > beforeCount)
                {
                    Logger.Msg($"[DeadDrop] ✅ Item successfully added to storage");
                    return true;
                }
                else
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Storage count didn't increase - insertion may have failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Error adding item to storage: {ex.Message}");
                Logger.Exception(ex);
                return false;
            }
        }

        /// <summary>
        /// Adds cash to storage - uses MoneyManager for proper cash handling
        /// </summary>
        private static bool AddCashToStorage(StorageEntity storage, int amount)
        {
            try
            {
                Logger.Msg($"[DeadDrop] 💰 Attempting to add ${amount} cash to storage...");

                // Method 1: Use MoneyManager.GetCashInstance (the proper way!)
                Logger.Msg($"[DeadDrop] 💵 Using MoneyManager.GetCashInstance...");
                var moneyManager = MoneyManager.Instance;
                if (moneyManager != null)
                {
                    try
                    {
                        var cashInstance = moneyManager.GetCashInstance((float)amount);
                        if (cashInstance != null)
                        {
                            Logger.Msg($"[DeadDrop] ✅ MoneyManager created CashInstance: {cashInstance.GetType().Name}");
                            Logger.Msg($"[DeadDrop] 💰 CashInstance Balance: ${cashInstance.Balance}");
                            
                            // Try to add this cash instance to storage
                            bool added = AddItemToStorage(storage, cashInstance);
                            if (added)
                            {
                                Logger.Msg($"[DeadDrop] ✅ Successfully added ${amount} cash to storage via MoneyManager");
                                return true;
                            }
                            else
                            {
                                Logger.Warn($"[DeadDrop] ⚠️ MoneyManager cash instance created but storage insertion failed");
                            }
                        }
                        else
                        {
                            Logger.Warn($"[DeadDrop] ❌ MoneyManager.GetCashInstance returned null");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ MoneyManager.GetCashInstance failed: {ex.Message}");
                    }
                }
                else
                {
                    Logger.Warn($"[DeadDrop] ⚠️ MoneyManager.Instance is null");
                }

                // Method 2: Fallback - Direct cash balance change (if storage doesn't support cash items)
                Logger.Msg($"[DeadDrop] 💵 Storage insertion failed, trying direct player cash balance change...");
                try
                {
                    var player = Player.Local;
                    if (player != null && moneyManager != null)
                    {
                        // Get player's current cash balance for logging
                        float currentBalance = moneyManager.cashBalance;
                        Logger.Msg($"[DeadDrop] 💰 Player current cash balance: ${currentBalance}");
                        
                        // Add cash directly to player's balance
                        moneyManager.ChangeCashBalance((float)amount, true, true);
                        
                        // Verify the change
                        float newBalance = moneyManager.cashBalance;
                        Logger.Msg($"[DeadDrop] 💰 Player new cash balance: ${newBalance}");
                        
                        if (Math.Abs(newBalance - (currentBalance + amount)) < 0.01f)
                        {
                            Logger.Msg($"[DeadDrop] ✅ Successfully added ${amount} cash directly to player balance");
                            Logger.Msg($"[DeadDrop] 📝 Note: Cash was added to player inventory instead of dead drop due to storage limitations");
                            return true;
                        }
                        else
                        {
                            Logger.Warn($"[DeadDrop] ⚠️ Cash balance change verification failed");
                        }
                    }
                    else
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ Player.Local or MoneyManager is null");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[DeadDrop] ⚠️ Direct cash balance change failed: {ex.Message}");
                }

                // Method 3: Try the old approach as final fallback
                Logger.Msg($"[DeadDrop] 💵 Trying fallback cash creation methods...");
                
                // Get cash definition from registry
                var cashDef = Il2CppScheduleOne.Registry.GetItem<CashDefinition>("cash");
                if (cashDef != null)
                {
                    Logger.Msg($"[DeadDrop] 💵 Found CashDefinition: {cashDef.name}");

                    // Try proper CashInstance constructor
                    try
                    {
                        var cashInstance = new CashInstance(cashDef, amount);
                        if (cashInstance != null)
                        {
                            Logger.Msg($"[DeadDrop] ✅ Created CashInstance: {cashInstance.GetType().Name}");
                            Logger.Msg($"[DeadDrop] 💰 Initial Balance: ${cashInstance.Balance}");
                            
                            // Verify and set the balance if needed
                            if (Math.Abs(cashInstance.Balance - amount) > 0.01f)
                            {
                                Logger.Msg($"[DeadDrop] 🔧 Setting balance from ${cashInstance.Balance} to ${amount}");
                                cashInstance.SetBalance((float)amount, false);
                                Logger.Msg($"[DeadDrop] 💰 Final Balance: ${cashInstance.Balance}");
                            }
                            
                            return AddItemToStorage(storage, cashInstance);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ CashInstance constructor failed: {ex.Message}");
                    }

                    // Try GetDefaultInstance fallback
                    try
                    {
                        var itemInstance = cashDef.GetDefaultInstance(amount);
                        if (itemInstance != null && itemInstance is CashInstance cashInst)
                        {
                            Logger.Msg($"[DeadDrop] ✅ GetDefaultInstance created CashInstance");
                            if (Math.Abs(cashInst.Balance - amount) > 0.01f)
                            {
                                cashInst.SetBalance((float)amount, false);
                            }
                            return AddItemToStorage(storage, cashInst);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[DeadDrop] ⚠️ GetDefaultInstance failed: {ex.Message}");
                    }
                }

                Logger.Error($"[DeadDrop] ❌ All cash creation and insertion methods failed!");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Error in cash handling: {ex.Message}");
                Logger.Exception(ex);
                return false;
            }
        }

        /// <summary>
        /// Attempts to set monetary value on any item instance
        /// </summary>
        private static void SetMonetaryValue(ItemInstance item, int value)
        {
            try
            {
                var itemType = item.GetType();
                
                // Try common monetary properties
                string[] monetaryProps = { "Balance", "Amount", "Value", "MonetaryValue", "CashValue", "Money" };
                
                foreach (string propName in monetaryProps)
                {
                    try
                    {
                        var prop = itemType.GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (prop != null && prop.CanWrite)
                        {
                            if (prop.PropertyType == typeof(float))
                            {
                                prop.SetValue(item, (float)value);
                                Logger.Msg($"[DeadDrop] 💰 Set {propName} (float) to {value}");
                                return;
                            }
                            else if (prop.PropertyType == typeof(int))
                            {
                                prop.SetValue(item, value);
                                Logger.Msg($"[DeadDrop] 💰 Set {propName} (int) to {value}");
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Msg($"[DeadDrop] 💔 Failed to set {propName}: {ex.Message}");
                    }
                }
                
                Logger.Warn($"[DeadDrop] ⚠️ Could not find monetary property on {itemType.Name}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[DeadDrop] ⚠️ Failed to set monetary value: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifies the player about the drop location
        /// </summary>
        private static void NotifyPlayer(Il2CppScheduleOne.Economy.DeadDrop deadDrop, JsonDataStore.DropRecord drop)
        {
            try
            {
                string locationDesc = $"{deadDrop.DeadDropName}";
                Logger.Msg($"[DeadDrop] 📱 Drop notification: Package ready at {locationDesc}");
                Logger.Msg($"[DeadDrop] 📍 Location: {deadDrop.DeadDropDescription}");
                
                // Here we could integrate with the messaging system to send an actual in-game message
                // For now, just log it
                Logger.Msg($"[DeadDrop] 📨 \"Your package from {drop.Org} is ready for pickup at {locationDesc}. {deadDrop.DeadDropDescription}\"");
            }
            catch (Exception ex)
            {
                Logger.Error($"[DeadDrop] ❌ Failed to send notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a stacking key that ensures only truly identical items are stacked together
        /// </summary>
        private static string CreateItemStackingKey(string itemId)
        {
            try
            {
                // For now, return the simple item ID, but we could enhance this to include:
                // - Item quality/condition
                // - Item subtype/variant
                // - Any other properties that affect stacking
                
                // Future enhancement: Get the item definition and check properties
                var itemDef = Il2CppScheduleOne.Registry.GetItem(itemId);
                if (itemDef != null)
                {
                    // For different types of items, we might want to include different properties
                    // For example, for soil, we might want to include soil type
                    // For weapons, we might want to include condition
                    // For drugs, we might want to include purity
                    
                    // For now, just use the item ID, but log if we find any relevant properties
                    var itemType = itemDef.GetType();
                    var properties = itemType.GetProperties().Where(p => 
                        p.Name.Contains("Type") || 
                        p.Name.Contains("Quality") || 
                        p.Name.Contains("Variant") ||
                        p.Name.Contains("Grade") ||
                        p.Name.Contains("Condition"));
                    
                    if (properties.Any())
                    {
                        Logger.Msg($"[DeadDrop] 🔍 Item {itemId} has stackable properties: {string.Join(", ", properties.Select(p => p.Name))}");
                        // We could read these properties and include them in the key
                        // For now, just use simple ID but this is where we'd enhance it
                    }
                }
                
                return itemId; // Simple stacking for now
            }
            catch (Exception ex)
            {
                Logger.Warn($"[DeadDrop] ⚠️ Error creating stacking key for {itemId}: {ex.Message}");
                return itemId; // Fallback to simple ID
            }
        }

        /// <summary>
        /// Extracts the base item ID from a stacking key
        /// </summary>
        private static string ExtractItemIdFromStackingKey(string stackingKey)
        {
            // For now, the stacking key is just the item ID
            // In the future, if we enhance stacking keys to include properties,
            // we'd need to parse them here (e.g., "soil:type1:quality2" -> "soil")
            
            // Simple extraction for now
            return stackingKey.Split(':')[0];
        }
    }
} 