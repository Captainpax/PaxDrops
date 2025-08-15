using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PaxDrops.Runtime.Abstractions;
using MelonLoader;

namespace PaxDrops.Runtime.IL2CPP
{
    /// <summary>
    /// IL2CPP-specific implementation of game time operations
    /// Uses reflection to access IL2CPP types safely
    /// </summary>
    public class IL2CPPGameTimeProvider : IGameTimeProvider
    {
        public DateTime GetCurrentTime()
        {
            try
            {
                var timeManagerType = Type.GetType("Il2CppScheduleOne.GameTime.TimeManager, Assembly-CSharp");
                if (timeManagerType != null)
                {
                    var instanceProperty = timeManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var timeManager = instanceProperty?.GetValue(null);
                    
                    if (timeManager != null)
                    {
                        var currentTimeProperty = timeManagerType.GetProperty("CurrentTime");
                        var currentTime = currentTimeProperty?.GetValue(timeManager);
                        if (currentTime is DateTime dt)
                            return dt;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPGameTimeProvider] Failed to get current time: {ex.Message}");
            }
            
            return DateTime.Now; // Fallback
        }

        public int GetCurrentDay()
        {
            try
            {
                var timeManagerType = Type.GetType("Il2CppScheduleOne.GameTime.TimeManager, Assembly-CSharp");
                if (timeManagerType != null)
                {
                    var instanceProperty = timeManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var timeManager = instanceProperty?.GetValue(null);
                    
                    if (timeManager != null)
                    {
                        var dayProperty = timeManagerType.GetProperty("Day");
                        var day = dayProperty?.GetValue(timeManager);
                        if (day != null && int.TryParse(day.ToString(), out int dayValue))
                            return dayValue;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPGameTimeProvider] Failed to get current day: {ex.Message}");
            }
            
            return 1; // Fallback
        }

        public int GetCurrentHour()
        {
            try
            {
                var timeManagerType = Type.GetType("Il2CppScheduleOne.GameTime.TimeManager, Assembly-CSharp");
                if (timeManagerType != null)
                {
                    var instanceProperty = timeManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var timeManager = instanceProperty?.GetValue(null);
                    
                    if (timeManager != null)
                    {
                        var hourProperty = timeManagerType.GetProperty("Hour");
                        var hour = hourProperty?.GetValue(timeManager);
                        if (hour != null && int.TryParse(hour.ToString(), out int hourValue))
                            return hourValue;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPGameTimeProvider] Failed to get current hour: {ex.Message}");
            }
            
            return DateTime.Now.Hour; // Fallback
        }

        public int GetCurrentMinute()
        {
            try
            {
                var timeManagerType = Type.GetType("Il2CppScheduleOne.GameTime.TimeManager, Assembly-CSharp");
                if (timeManagerType != null)
                {
                    var instanceProperty = timeManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var timeManager = instanceProperty?.GetValue(null);
                    
                    if (timeManager != null)
                    {
                        var minuteProperty = timeManagerType.GetProperty("Minute");
                        var minute = minuteProperty?.GetValue(timeManager);
                        if (minute != null && int.TryParse(minute.ToString(), out int minuteValue))
                            return minuteValue;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPGameTimeProvider] Failed to get current minute: {ex.Message}");
            }
            
            return DateTime.Now.Minute; // Fallback
        }

        public string GetFormattedTime()
        {
            try
            {
                var time = GetCurrentTime();
                return time.ToString("HH:mm");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPGameTimeProvider] Failed to get formatted time: {ex.Message}");
                return DateTime.Now.ToString("HH:mm");
            }
        }

        public bool IsAvailable()
        {
            try
            {
                var timeManagerType = Type.GetType("Il2CppScheduleOne.GameTime.TimeManager, Assembly-CSharp");
                if (timeManagerType != null)
                {
                    var instanceProperty = timeManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var timeManager = instanceProperty?.GetValue(null);
                    return timeManager != null;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPGameTimeProvider] Failed to check availability: {ex.Message}");
            }
            
            return false;
        }
    }

    /// <summary>
    /// IL2CPP-specific implementation of player operations
    /// </summary>
    public class IL2CPPPlayerProvider : IPlayerProvider
    {
        public object GetPlayer()
        {
            try
            {
                var playerType = Type.GetType("Il2CppScheduleOne.PlayerScripts.Player, Assembly-CSharp");
                if (playerType != null)
                {
                    var instanceProperty = playerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    return instanceProperty?.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPPlayerProvider] Failed to get player: {ex.Message}");
            }
            return null;
        }

        public string GetPlayerName()
        {
            try
            {
                var player = GetPlayer();
                if (player != null)
                {
                    var playerNameProperty = player.GetType().GetProperty("PlayerName");
                    var playerName = playerNameProperty?.GetValue(player);
                    return playerName?.ToString() ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPPlayerProvider] Failed to get player name: {ex.Message}");
            }
            return "Unknown";
        }

        public string GetPlayerRank()
        {
            try
            {
                var levelManagerType = Type.GetType("Il2CppScheduleOne.Levelling.LevelManager, Assembly-CSharp");
                if (levelManagerType != null)
                {
                    var instanceProperty = levelManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var levelManager = instanceProperty?.GetValue(null);
                    
                    if (levelManager != null)
                    {
                        var rankProperty = levelManagerType.GetProperty("Rank");
                        var rank = rankProperty?.GetValue(levelManager);
                        return rank?.ToString() ?? "Street_Rat";
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPPlayerProvider] Failed to get player rank: {ex.Message}");
            }
            return "Street_Rat";
        }

        public Vector3 GetPlayerPosition()
        {
            try
            {
                var player = GetPlayer();
                if (player != null)
                {
                    var transformProperty = player.GetType().GetProperty("transform");
                    var transform = transformProperty?.GetValue(player);
                    if (transform != null)
                    {
                        var positionProperty = transform.GetType().GetProperty("position");
                        var position = positionProperty?.GetValue(transform);
                        if (position is Vector3 pos)
                            return pos;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPPlayerProvider] Failed to get player position: {ex.Message}");
            }
            return Vector3.zero;
        }

        public bool IsPlayerAvailable()
        {
            return GetPlayer() != null;
        }

        public object GetLevelManager()
        {
            try
            {
                var levelManagerType = Type.GetType("Il2CppScheduleOne.Levelling.LevelManager, Assembly-CSharp");
                if (levelManagerType != null)
                {
                    var instanceProperty = levelManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    return instanceProperty?.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPPlayerProvider] Failed to get level manager: {ex.Message}");
            }
            return null;
        }
    }

    /// <summary>
    /// IL2CPP-specific implementation of console operations
    /// </summary>
    public class IL2CPPConsoleProvider : IConsoleProvider
    {
        public object GetConsole()
        {
            try
            {
                var consoleType = Type.GetType("Il2CppScheduleOne.Console.Console, Assembly-CSharp");
                if (consoleType != null)
                {
                    var instanceProperty = consoleType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    return instanceProperty?.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPConsoleProvider] Failed to get console: {ex.Message}");
            }
            return null;
        }

        public void ExecuteCommand(string command)
        {
            try
            {
                var console = GetConsole();
                if (console != null)
                {
                    var submitCommandMethod = console.GetType().GetMethod("SubmitCommand", new[] { typeof(string) });
                    submitCommandMethod?.Invoke(console, new object[] { command });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPConsoleProvider] Failed to execute command: {ex.Message}");
            }
        }

        public void ExecuteCommand(List<string> args)
        {
            try
            {
                var console = GetConsole();
                if (console != null)
                {
                    var submitCommandListMethod = console.GetType().GetMethod("SubmitCommand", new[] { typeof(List<string>) });
                    if (submitCommandListMethod != null)
                    {
                        submitCommandListMethod.Invoke(console, new object[] { args });
                    }
                    else
                    {
                        // Fallback: execute as joined command
                        if (args.Count > 0)
                        {
                            var joinedCommand = string.Join(" ", args);
                            ExecuteCommand(joinedCommand);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPConsoleProvider] Failed to execute command with args: {ex.Message}");
            }
        }

        public Type GetConsoleType()
        {
            try
            {
                return Type.GetType("Il2CppScheduleOne.Console.Console, Assembly-CSharp");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPConsoleProvider] Failed to get console type: {ex.Message}");
                return null;
            }
        }

        public bool IsAvailable()
        {
            return GetConsole() != null;
        }
    }

    /// <summary>
    /// IL2CPP-specific implementation of dead drop operations
    /// </summary>
    public class IL2CPPDeadDropProvider : IDeadDropProvider
    {
        public object[] GetAllDeadDrops()
        {
            try
            {
                var deadDropManagerType = Type.GetType("Il2CppScheduleOne.DeadDrops.DeadDropManager, Assembly-CSharp");
                if (deadDropManagerType != null)
                {
                    var instanceProperty = deadDropManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var manager = instanceProperty?.GetValue(null);
                    
                    if (manager != null)
                    {
                        var getAllMethod = deadDropManagerType.GetMethod("GetAllDeadDrops");
                        var result = getAllMethod?.Invoke(manager, null);
                        
                        if (result is System.Collections.IEnumerable enumerable)
                        {
                            return enumerable.Cast<object>().ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to get all dead drops: {ex.Message}");
            }
            return new object[0];
        }

        public object GetDeadDropByGuid(Guid guid)
        {
            try
            {
                var allDeadDrops = GetAllDeadDrops();
                foreach (var deadDrop in allDeadDrops)
                {
                    if (deadDrop != null)
                    {
                        var guidProperty = deadDrop.GetType().GetProperty("Guid") ?? deadDrop.GetType().GetProperty("ID");
                        var deadDropGuid = guidProperty?.GetValue(deadDrop);
                        
                        if (deadDropGuid != null && deadDropGuid.Equals(guid))
                        {
                            return deadDrop;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to get dead drop by GUID: {ex.Message}");
            }
            return null;
        }

        public object CreateDeadDrop()
        {
            try
            {
                var deadDropType = Type.GetType("Il2CppScheduleOne.DeadDrops.DeadDrop, Assembly-CSharp");
                if (deadDropType != null)
                {
                    return Activator.CreateInstance(deadDropType);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to create dead drop: {ex.Message}");
            }
            return null;
        }

        public object GetStorageEntity(object deadDrop)
        {
            try
            {
                if (deadDrop != null)
                {
                    var storageProperty = deadDrop.GetType().GetProperty("Storage") ?? 
                                        deadDrop.GetType().GetProperty("StorageEntity") ??
                                        deadDrop.GetType().GetProperty("Container");
                    return storageProperty?.GetValue(deadDrop);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to get storage entity: {ex.Message}");
            }
            return null;
        }

        public object CreateDeadDrop(Vector3 position, List<object> items)
        {
            try
            {
                var deadDrop = CreateDeadDrop();
                if (deadDrop != null)
                {
                    // Set position and items using reflection
                    var positionProperty = deadDrop.GetType().GetProperty("Position");
                    positionProperty?.SetValue(deadDrop, position);
                    
                    var itemsProperty = deadDrop.GetType().GetProperty("Items");
                    itemsProperty?.SetValue(deadDrop, items);
                    
                    return deadDrop;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to create dead drop: {ex.Message}");
            }
            return null;
        }

        public void RemoveDeadDrop(object deadDrop)
        {
            try
            {
                if (deadDrop != null)
                {
                    var removeMethod = deadDrop.GetType().GetMethod("Remove");
                    removeMethod?.Invoke(deadDrop, null);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to remove dead drop: {ex.Message}");
            }
        }

        public List<object> GetNearbyDeadDrops(Vector3 position, float radius)
        {
            try
            {
                var deadDropManagerType = Type.GetType("Il2CppScheduleOne.DeadDrops.DeadDropManager, Assembly-CSharp");
                if (deadDropManagerType != null)
                {
                    var instanceProperty = deadDropManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var manager = instanceProperty?.GetValue(null);
                    
                    if (manager != null)
                    {
                        var getNearbyMethod = deadDropManagerType.GetMethod("GetNearbyDeadDrops", new[] { typeof(Vector3), typeof(float) });
                        var result = getNearbyMethod?.Invoke(manager, new object[] { position, radius });
                        
                        if (result is System.Collections.IEnumerable enumerable)
                        {
                            return enumerable.Cast<object>().ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPDeadDropProvider] Failed to get nearby dead drops: {ex.Message}");
            }
            return new List<object>();
        }

        public bool IsAvailable()
        {
            try
            {
                var deadDropType = Type.GetType("Il2CppScheduleOne.DeadDrops.DeadDrop, Assembly-CSharp");
                return deadDropType != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// IL2CPP-specific implementation of storage operations
    /// </summary>
    public class IL2CPPStorageProvider : IStorageProvider
    {
        public bool AddItemToStorage(object storage, string itemId, int quantity)
        {
            try
            {
                if (storage != null)
                {
                    var addItemMethod = storage.GetType().GetMethod("AddItem", new[] { typeof(string), typeof(int) });
                    if (addItemMethod != null)
                    {
                        var result = addItemMethod.Invoke(storage, new object[] { itemId, quantity });
                        return result != null ? (bool)result : true;
                    }
                    else
                    {
                        // Try alternative method signatures
                        var methods = storage.GetType().GetMethods().Where(m => m.Name == "AddItem").ToArray();
                        foreach (var method in methods)
                        {
                            try
                            {
                                var result = method.Invoke(storage, new object[] { itemId, quantity });
                                return true;
                            }
                            catch { continue; }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to add item to storage: {ex.Message}");
            }
            return false;
        }

        public bool AddCashToStorage(object storage, int amount)
        {
            try
            {
                if (storage != null)
                {
                    var addCashMethod = storage.GetType().GetMethod("AddCash", new[] { typeof(int) });
                    if (addCashMethod != null)
                    {
                        var result = addCashMethod.Invoke(storage, new object[] { amount });
                        return result != null ? (bool)result : true;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to add cash to storage: {ex.Message}");
            }
            return false;
        }

        public Dictionary<string, int> GetStorageItems(object storage)
        {
            try
            {
                if (storage != null)
                {
                    var getItemsMethod = storage.GetType().GetMethod("GetItems") ?? 
                                       storage.GetType().GetMethod("GetContents");
                    
                    if (getItemsMethod != null)
                    {
                        var result = getItemsMethod.Invoke(storage, null);
                        if (result is Dictionary<string, int> itemDict)
                        {
                            return itemDict;
                        }
                        else if (result is System.Collections.IEnumerable enumerable)
                        {
                            // Try to convert to dictionary
                            var dict = new Dictionary<string, int>();
                            foreach (var item in enumerable)
                            {
                                if (item != null)
                                {
                                    var idProp = item.GetType().GetProperty("ID") ?? item.GetType().GetProperty("ItemId");
                                    var quantityProp = item.GetType().GetProperty("Quantity") ?? item.GetType().GetProperty("Count");
                                    
                                    if (idProp != null && quantityProp != null)
                                    {
                                        var id = idProp.GetValue(item)?.ToString();
                                        var quantity = quantityProp.GetValue(item);
                                        
                                        if (id != null && quantity != null && int.TryParse(quantity.ToString(), out int qty))
                                        {
                                            dict[id] = qty;
                                        }
                                    }
                                }
                            }
                            return dict;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to get storage items: {ex.Message}");
            }
            return new Dictionary<string, int>();
        }

        public object GetStorage(string storageId)
        {
            try
            {
                var storageManagerType = Type.GetType("Il2CppScheduleOne.Storage.StorageManager, Assembly-CSharp");
                if (storageManagerType != null)
                {
                    var instanceProperty = storageManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var manager = instanceProperty?.GetValue(null);
                    
                    if (manager != null)
                    {
                        var getStorageMethod = storageManagerType.GetMethod("GetStorage", new[] { typeof(string) });
                        return getStorageMethod?.Invoke(manager, new object[] { storageId });
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to get storage: {ex.Message}");
            }
            return null;
        }

        public void AddItemToStorage(string storageId, object item)
        {
            try
            {
                var storage = GetStorage(storageId);
                if (storage != null)
                {
                    var addItemMethod = storage.GetType().GetMethod("AddItem", new[] { item.GetType() });
                    addItemMethod?.Invoke(storage, new object[] { item });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to add item to storage: {ex.Message}");
            }
        }

        public void RemoveItemFromStorage(string storageId, object item)
        {
            try
            {
                var storage = GetStorage(storageId);
                if (storage != null)
                {
                    var removeItemMethod = storage.GetType().GetMethod("RemoveItem", new[] { item.GetType() });
                    removeItemMethod?.Invoke(storage, new object[] { item });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to remove item from storage: {ex.Message}");
            }
        }

        public List<object> GetStorageContents(string storageId)
        {
            try
            {
                var storage = GetStorage(storageId);
                if (storage != null)
                {
                    var getContentsMethod = storage.GetType().GetMethod("GetContents") ?? 
                                          storage.GetType().GetMethod("GetItems") ??
                                          storage.GetType().GetProperty("Items")?.GetGetMethod();
                    
                    var result = getContentsMethod?.Invoke(storage, null);
                    if (result is System.Collections.IEnumerable enumerable)
                    {
                        return enumerable.Cast<object>().ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPStorageProvider] Failed to get storage contents: {ex.Message}");
            }
            return new List<object>();
        }

        public bool IsAvailable()
        {
            try
            {
                var storageManagerType = Type.GetType("Il2CppScheduleOne.Storage.StorageManager, Assembly-CSharp");
                return storageManagerType != null;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Placeholder implementations for NPC, SaveSystem, and Items providers
    /// These would be implemented with actual IL2CPP-specific logic
    /// </summary>
    
    public class IL2CPPNPCProvider : INPCProvider
    {
        public object[] GetAllSuppliers()
        {
            try
            {
                var supplierManagerType = Type.GetType("Il2CppScheduleOne.NPCs.SupplierManager, Assembly-CSharp");
                if (supplierManagerType != null)
                {
                    var instanceProperty = supplierManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var manager = instanceProperty?.GetValue(null);
                    
                    if (manager != null)
                    {
                        var getAllMethod = supplierManagerType.GetMethod("GetAllSuppliers");
                        var result = getAllMethod?.Invoke(manager, null);
                        
                        if (result is System.Collections.IEnumerable enumerable)
                        {
                            return enumerable.Cast<object>().ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPNPCProvider] Failed to get all suppliers: {ex.Message}");
            }
            return new object[0];
        }

        public object GetSupplier(string identifier)
        {
            try
            {
                var suppliers = GetAllSuppliers();
                foreach (var supplier in suppliers)
                {
                    if (supplier != null)
                    {
                        var nameProperty = supplier.GetType().GetProperty("Name") ?? supplier.GetType().GetProperty("ID");
                        var name = nameProperty?.GetValue(supplier)?.ToString();
                        
                        if (name != null && name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            return supplier;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPNPCProvider] Failed to get supplier: {ex.Message}");
            }
            return null;
        }

        public object CreateOrUpdateNPC(string name, Vector3 position)
        {
            try
            {
                var npcType = Type.GetType("Il2CppScheduleOne.NPCs.NPC, Assembly-CSharp");
                if (npcType != null)
                {
                    var npc = Activator.CreateInstance(npcType);
                    if (npc != null)
                    {
                        var nameProperty = npcType.GetProperty("Name");
                        nameProperty?.SetValue(npc, name);
                        
                        var positionProperty = npcType.GetProperty("Position");
                        positionProperty?.SetValue(npc, position);
                        
                        return npc;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPNPCProvider] Failed to create or update NPC: {ex.Message}");
            }
            return null;
        }

        public Type GetSupplierType()
        {
            try
            {
                return Type.GetType("Il2CppScheduleOne.NPCs.Supplier, Assembly-CSharp");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[IL2CPPNPCProvider] Failed to get supplier type: {ex.Message}");
                return null;
            }
        }

        public object GetNPC(string npcId) => null;
        public void InteractWithNPC(string npcId, string interactionType) { }
        public List<object> GetNearbyNPCs(Vector3 position, float radius) => new List<object>();
        public bool IsAvailable() => GetSupplierType() != null;
    }

    public class IL2CPPSaveSystemProvider : ISaveSystemProvider
    {
        public object GetSaveManager() => null;
        public string GetCurrentSaveFilePath() => string.Empty;
        public string GetPlayersSavePath() => string.Empty;
        public string GetSaveName() => string.Empty;
        public bool IsAvailable() => false;
        public Type GetSaveManagerType() => null;
    }

    public class IL2CPPItemProvider : IItemProvider
    {
        public object GetItemDefinition(string itemId) => null;
        public object CreateItemInstance(string itemId, int quantity) => null;
        public Dictionary<string, object> GetAllItemDefinitions() => new Dictionary<string, object>();
        public bool IsAvailable() => false;
    }
}
