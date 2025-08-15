using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using PaxDrops.Runtime.Abstractions;
using MelonLoader;

namespace PaxDrops.Runtime.Mono
{
    /// <summary>
    /// Mono-specific implementation of game time operations using reflection
    /// </summary>
    public class MonoGameTimeProvider : IGameTimeProvider
    {
        private Type _timeManagerType;
        private object _timeManagerInstance;
        private PropertyInfo _currentTimeProperty;
        private PropertyInfo _dayProperty;
        private PropertyInfo _hourProperty;
        private PropertyInfo _minuteProperty;

        public MonoGameTimeProvider()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                _timeManagerType = Type.GetType("ScheduleOne.GameTime.TimeManager, Assembly-CSharp");
                if (_timeManagerType != null)
                {
                    var instanceProperty = _timeManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _timeManagerInstance = instanceProperty?.GetValue(null);
                    
                    if (_timeManagerInstance != null)
                    {
                        _currentTimeProperty = _timeManagerType.GetProperty("CurrentTime");
                        
                        if (_currentTimeProperty != null)
                        {
                            var currentTimeType = _currentTimeProperty.PropertyType;
                            _dayProperty = currentTimeType.GetProperty("Day");
                            _hourProperty = currentTimeType.GetProperty("Hour");
                            _minuteProperty = currentTimeType.GetProperty("Minute");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoGameTimeProvider] Failed to initialize reflection: {ex.Message}");
            }
        }

        public DateTime GetCurrentTime()
        {
            try
            {
                if (_timeManagerInstance != null && _currentTimeProperty != null)
                {
                    var gameTime = _currentTimeProperty.GetValue(_timeManagerInstance);
                    if (gameTime != null)
                    {
                        var day = (int)(_dayProperty?.GetValue(gameTime) ?? 0);
                        var hour = (int)(_hourProperty?.GetValue(gameTime) ?? 0);
                        var minute = (int)(_minuteProperty?.GetValue(gameTime) ?? 0);
                        
                        return DateTime.MinValue.AddDays(day).AddHours(hour).AddMinutes(minute);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoGameTimeProvider] Failed to get current time: {ex.Message}");
            }
            return DateTime.MinValue;
        }

        public int GetCurrentDay()
        {
            try
            {
                if (_timeManagerInstance != null && _currentTimeProperty != null && _dayProperty != null)
                {
                    var gameTime = _currentTimeProperty.GetValue(_timeManagerInstance);
                    return (int)(_dayProperty.GetValue(gameTime) ?? 0);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoGameTimeProvider] Failed to get current day: {ex.Message}");
            }
            return 0;
        }

        public int GetCurrentHour()
        {
            try
            {
                if (_timeManagerInstance != null && _currentTimeProperty != null && _hourProperty != null)
                {
                    var gameTime = _currentTimeProperty.GetValue(_timeManagerInstance);
                    return (int)(_hourProperty.GetValue(gameTime) ?? 0);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoGameTimeProvider] Failed to get current hour: {ex.Message}");
            }
            return 0;
        }

        public int GetCurrentMinute()
        {
            try
            {
                if (_timeManagerInstance != null && _currentTimeProperty != null && _minuteProperty != null)
                {
                    var gameTime = _currentTimeProperty.GetValue(_timeManagerInstance);
                    return (int)(_minuteProperty.GetValue(gameTime) ?? 0);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoGameTimeProvider] Failed to get current minute: {ex.Message}");
            }
            return 0;
        }

        public string GetFormattedTime()
        {
            try
            {
                var day = GetCurrentDay();
                var hour = GetCurrentHour();
                var minute = GetCurrentMinute();
                return $"Day {day}, {hour:00}:{minute:00}";
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoGameTimeProvider] Failed to get formatted time: {ex.Message}");
            }
            return "Unknown";
        }

        public bool IsAvailable()
        {
            return _timeManagerInstance != null;
        }
    }

    /// <summary>
    /// Mono-specific implementation of player operations using reflection
    /// </summary>
    public class MonoPlayerProvider : IPlayerProvider
    {
        private Type _playerType;
        private Type _levelManagerType;
        private object _playerInstance;
        private object _levelManagerInstance;

        public MonoPlayerProvider()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                _playerType = Type.GetType("ScheduleOne.PlayerScripts.Player, Assembly-CSharp");
                _levelManagerType = Type.GetType("ScheduleOne.Levelling.LevelManager, Assembly-CSharp");

                if (_playerType != null)
                {
                    var instanceProperty = _playerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _playerInstance = instanceProperty?.GetValue(null);
                }

                if (_levelManagerType != null)
                {
                    var instanceProperty = _levelManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _levelManagerInstance = instanceProperty?.GetValue(null);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoPlayerProvider] Failed to initialize reflection: {ex.Message}");
            }
        }

        public object GetPlayer()
        {
            return _playerInstance;
        }

        public string GetPlayerName()
        {
            try
            {
                if (_playerInstance != null)
                {
                    var playerNameProperty = _playerType.GetProperty("PlayerName");
                    var playerName = playerNameProperty?.GetValue(_playerInstance);
                    return playerName?.ToString() ?? "Unknown";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoPlayerProvider] Failed to get player name: {ex.Message}");
            }
            return "Unknown";
        }

        public string GetPlayerRank()
        {
            try
            {
                if (_levelManagerInstance != null)
                {
                    var rankProperty = _levelManagerType.GetProperty("Rank");
                    var rank = rankProperty?.GetValue(_levelManagerInstance);
                    return rank?.ToString() ?? "Street_Rat";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoPlayerProvider] Failed to get player rank: {ex.Message}");
            }
            return "Street_Rat";
        }

        public Vector3 GetPlayerPosition()
        {
            try
            {
                if (_playerInstance != null)
                {
                    var transformProperty = _playerType.GetProperty("transform");
                    var transform = transformProperty?.GetValue(_playerInstance) as Transform;
                    return transform?.position ?? Vector3.zero;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoPlayerProvider] Failed to get player position: {ex.Message}");
            }
            return Vector3.zero;
        }

        public bool IsPlayerAvailable()
        {
            return _playerInstance != null;
        }

        public object GetLevelManager()
        {
            return _levelManagerInstance;
        }
    }

    /// <summary>
    /// Mono-specific implementation of console operations using reflection
    /// </summary>
    public class MonoConsoleProvider : IConsoleProvider
    {
        private Type _consoleType;
        private object _consoleInstance;
        private MethodInfo _submitCommandMethod;
        private MethodInfo _submitCommandListMethod;

        public MonoConsoleProvider()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                _consoleType = Type.GetType("ScheduleOne.Console, Assembly-CSharp");
                if (_consoleType != null)
                {
                    var instanceProperty = _consoleType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    _consoleInstance = instanceProperty?.GetValue(null);

                    // Get submit command methods
                    _submitCommandMethod = _consoleType.GetMethod("SubmitCommand", new[] { typeof(string) });
                    _submitCommandListMethod = _consoleType.GetMethod("SubmitCommand", new[] { typeof(List<string>) });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoConsoleProvider] Failed to initialize reflection: {ex.Message}");
            }
        }

        public object GetConsole()
        {
            return _consoleInstance;
        }

        public void ExecuteCommand(string command)
        {
            try
            {
                if (_consoleInstance != null && _submitCommandMethod != null)
                {
                    _submitCommandMethod.Invoke(_consoleInstance, new object[] { command });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoConsoleProvider] Failed to execute command: {ex.Message}");
            }
        }

        public void ExecuteCommand(List<string> args)
        {
            try
            {
                if (_consoleInstance != null && _submitCommandListMethod != null)
                {
                    _submitCommandListMethod.Invoke(_consoleInstance, new object[] { args });
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoConsoleProvider] Failed to execute command with args: {ex.Message}");
            }
        }

        public bool IsAvailable()
        {
            return _consoleInstance != null;
        }

        public Type GetConsoleType()
        {
            return _consoleType;
        }
    }

    /// <summary>
    /// Mono-specific implementation of dead drop operations using reflection
    /// </summary>
    public class MonoDeadDropProvider : IDeadDropProvider
    {
        private Type _deadDropType;

        public MonoDeadDropProvider()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                _deadDropType = Type.GetType("ScheduleOne.Economy.DeadDrop, Assembly-CSharp");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoDeadDropProvider] Failed to initialize reflection: {ex.Message}");
            }
        }

        public object[] GetAllDeadDrops()
        {
            try
            {
                if (_deadDropType != null)
                {
                    var findObjectsMethod = typeof(UnityEngine.Object).GetMethod("FindObjectsOfType", Type.EmptyTypes);
                    var genericMethod = findObjectsMethod?.MakeGenericMethod(_deadDropType);
                    var deadDrops = genericMethod?.Invoke(null, null) as Array;
                    
                    if (deadDrops != null)
                    {
                        return deadDrops.Cast<object>().ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoDeadDropProvider] Failed to get all dead drops: {ex.Message}");
            }
            return new object[0];
        }

        public object GetDeadDropByGuid(Guid guid)
        {
            try
            {
                var deadDrops = GetAllDeadDrops();
                foreach (var deadDrop in deadDrops)
                {
                    var guidProperty = _deadDropType?.GetProperty("GUID");
                    var deadDropGuid = guidProperty?.GetValue(deadDrop);
                    if (deadDropGuid is Guid ddGuid && ddGuid == guid)
                    {
                        return deadDrop;
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoDeadDropProvider] Failed to get dead drop by GUID: {ex.Message}");
            }
            return null;
        }

        public object CreateDeadDrop()
        {
            try
            {
                if (_deadDropType != null)
                {
                    var go = new GameObject("PaxDrops_DeadDrop");
                    var addComponentMethod = typeof(GameObject).GetMethod("AddComponent", Type.EmptyTypes);
                    var genericMethod = addComponentMethod?.MakeGenericMethod(_deadDropType);
                    return genericMethod?.Invoke(go, null);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoDeadDropProvider] Failed to create dead drop: {ex.Message}");
            }
            return null;
        }

        public object GetStorageEntity(object deadDrop)
        {
            try
            {
                if (deadDrop != null && _deadDropType != null)
                {
                    var storageProperty = _deadDropType.GetProperty("StorageEntity");
                    return storageProperty?.GetValue(deadDrop);
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoDeadDropProvider] Failed to get storage entity: {ex.Message}");
            }
            return null;
        }

        public bool IsAvailable()
        {
            return _deadDropType != null;
        }
    }

    /// <summary>
    /// Mono-specific implementation of storage operations using reflection
    /// </summary>
    public class MonoStorageProvider : IStorageProvider
    {
        private Type _storageEntityType;
        private Type _registryType;
        private Type _itemDefinitionType;
        private Type _cashDefinitionType;
        private Type _cashInstanceType;

        public MonoStorageProvider()
        {
            InitializeReflection();
        }

        private void InitializeReflection()
        {
            try
            {
                _storageEntityType = Type.GetType("ScheduleOne.Storage.StorageEntity, Assembly-CSharp");
                _registryType = Type.GetType("ScheduleOne.Registry, Assembly-CSharp");
                _itemDefinitionType = Type.GetType("ScheduleOne.ItemFramework.ItemDefinition, Assembly-CSharp");
                _cashDefinitionType = Type.GetType("ScheduleOne.Money.CashDefinition, Assembly-CSharp");
                _cashInstanceType = Type.GetType("ScheduleOne.Money.CashInstance, Assembly-CSharp");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoStorageProvider] Failed to initialize reflection: {ex.Message}");
            }
        }

        public bool AddItemToStorage(object storage, string itemId, int quantity)
        {
            try
            {
                if (storage != null && _storageEntityType != null && _registryType != null)
                {
                    // Get item definition using Registry.GetItem<ItemDefinition>
                    var getItemMethod = _registryType.GetMethods()
                        .FirstOrDefault(m => m.Name == "GetItem" && m.IsGenericMethod);
                    
                    if (getItemMethod != null)
                    {
                        var genericMethod = getItemMethod.MakeGenericMethod(_itemDefinitionType);
                        var itemDef = genericMethod.Invoke(null, new object[] { itemId });
                        
                        if (itemDef != null)
                        {
                            var getDefaultInstanceMethod = _itemDefinitionType.GetMethod("GetDefaultInstance");
                            var itemInstance = getDefaultInstanceMethod?.Invoke(itemDef, new object[] { quantity });
                            
                            if (itemInstance != null)
                            {
                                var addItemMethod = _storageEntityType.GetMethod("AddItem");
                                var result = addItemMethod?.Invoke(storage, new object[] { itemInstance });
                                return result is bool success && success;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoStorageProvider] Failed to add item to storage: {ex.Message}");
            }
            return false;
        }

        public bool AddCashToStorage(object storage, int amount)
        {
            try
            {
                if (storage != null && _storageEntityType != null && _registryType != null && _cashDefinitionType != null)
                {
                    // Get cash definition
                    var getItemMethod = _registryType.GetMethods()
                        .FirstOrDefault(m => m.Name == "GetItem" && m.IsGenericMethod);
                    
                    if (getItemMethod != null)
                    {
                        var genericMethod = getItemMethod.MakeGenericMethod(_cashDefinitionType);
                        var cashDef = genericMethod.Invoke(null, new object[] { "cash" });
                        
                        if (cashDef != null)
                        {
                            var getDefaultInstanceMethod = _cashDefinitionType.GetMethod("GetDefaultInstance");
                            var cashInstance = getDefaultInstanceMethod?.Invoke(cashDef, new object[] { amount });
                            
                            if (cashInstance != null && _cashInstanceType != null)
                            {
                                // Set balance
                                var setBalanceMethod = _cashInstanceType.GetMethod("SetBalance");
                                setBalanceMethod?.Invoke(cashInstance, new object[] { (float)amount, false });
                                
                                var addItemMethod = _storageEntityType.GetMethod("AddItem");
                                var result = addItemMethod?.Invoke(storage, new object[] { cashInstance });
                                return result is bool success && success;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoStorageProvider] Failed to add cash to storage: {ex.Message}");
            }
            return false;
        }

        public Dictionary<string, int> GetStorageItems(object storage)
        {
            var items = new Dictionary<string, int>();
            try
            {
                if (storage != null && _storageEntityType != null)
                {
                    var itemsProperty = _storageEntityType.GetProperty("Items");
                    var storageItems = itemsProperty?.GetValue(storage);
                    
                    if (storageItems != null)
                    {
                        // Iterate through items using reflection
                        foreach (var item in (System.Collections.IEnumerable)storageItems)
                        {
                            var definitionProperty = item.GetType().GetProperty("Definition");
                            var quantityProperty = item.GetType().GetProperty("Quantity");
                            
                            var definition = definitionProperty?.GetValue(item);
                            var quantity = quantityProperty?.GetValue(item);
                            
                            if (definition != null && quantity is int qty)
                            {
                                var nameProperty = definition.GetType().GetProperty("Name");
                                var key = nameProperty?.GetValue(definition) as string ?? "unknown";
                                
                                if (items.ContainsKey(key))
                                    items[key] += qty;
                                else
                                    items[key] = qty;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[MonoStorageProvider] Failed to get storage items: {ex.Message}");
            }
            return items;
        }

        public bool IsAvailable()
        {
            return _storageEntityType != null && _registryType != null;
        }
    }
}
