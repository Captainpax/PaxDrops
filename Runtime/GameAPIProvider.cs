using System;
using PaxDrops.Runtime.Abstractions;
using PaxDrops.Runtime.IL2CPP;
using PaxDrops.Runtime.Mono;
using MelonLoader;

namespace PaxDrops.Runtime
{
    /// <summary>
    /// Main factory that provides unified access to all game systems.
    /// Automatically selects IL2CPP or Mono implementations based on runtime detection.
    /// </summary>
    public class GameAPIProvider : IGameAPIProvider
    {
        private static GameAPIProvider _instance;
        private static readonly object _lock = new object();

        // Provider instances
        private IGameTimeProvider _gameTime;
        private IPlayerProvider _player;
        private IConsoleProvider _console;
        private IDeadDropProvider _deadDrop;
        private IStorageProvider _storage;
        private INPCProvider _npc;
        private ISaveSystemProvider _saveSystem;
        private IItemProvider _items;

        private bool _initialized = false;

        /// <summary>
        /// Gets the singleton instance of GameAPIProvider
        /// </summary>
        public static GameAPIProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new GameAPIProvider();
                        }
                    }
                }
                return _instance;
            }
        }

        private GameAPIProvider()
        {
            // Private constructor for singleton
        }

        public IGameTimeProvider GameTime
        {
            get
            {
                EnsureInitialized();
                return _gameTime;
            }
        }

        public IPlayerProvider Player
        {
            get
            {
                EnsureInitialized();
                return _player;
            }
        }

        public IConsoleProvider Console
        {
            get
            {
                EnsureInitialized();
                return _console;
            }
        }

        public IDeadDropProvider DeadDrop
        {
            get
            {
                EnsureInitialized();
                return _deadDrop;
            }
        }

        public IStorageProvider Storage
        {
            get
            {
                EnsureInitialized();
                return _storage;
            }
        }

        public INPCProvider NPC
        {
            get
            {
                EnsureInitialized();
                return _npc;
            }
        }

        public ISaveSystemProvider SaveSystem
        {
            get
            {
                EnsureInitialized();
                return _saveSystem;
            }
        }

        public IItemProvider Items
        {
            get
            {
                EnsureInitialized();
                return _items;
            }
        }

        public string RuntimeType => RuntimeEnvironment.RuntimeType;

        /// <summary>
        /// Initializes all providers based on the detected runtime
        /// </summary>
        public void Initialize()
        {
            if (_initialized) return;

            try
            {
                // Ensure runtime detection is complete
                RuntimeEnvironment.Initialize();

                MelonLogger.Msg($"[GameAPIProvider] 🔧 Initializing providers for {RuntimeType} runtime...");

                if (RuntimeEnvironment.IsIL2CPP)
                {
                    InitializeIL2CPPProviders();
                }
                else
                {
                    InitializeMonoProviders();
                }

                _initialized = true;
                MelonLogger.Msg($"[GameAPIProvider] ✅ All providers initialized for {RuntimeType} runtime");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[GameAPIProvider] ❌ Failed to initialize providers: {ex.Message}");
                throw;
            }
        }

        private void InitializeIL2CPPProviders()
        {
            MelonLogger.Msg("[GameAPIProvider] 🔧 Creating IL2CPP providers...");

            _gameTime = new IL2CPPGameTimeProvider();
            _player = new IL2CPPPlayerProvider();
            _console = new IL2CPPConsoleProvider();
            _deadDrop = new IL2CPPDeadDropProvider();
            _storage = new IL2CPPStorageProvider();
            
            // TODO: Implement remaining IL2CPP providers
            _npc = new NullNPCProvider(); // Placeholder
            _saveSystem = new NullSaveSystemProvider(); // Placeholder
            _items = new NullItemProvider(); // Placeholder

            MelonLogger.Msg("[GameAPIProvider] ✅ IL2CPP providers created");
        }

        private void InitializeMonoProviders()
        {
            MelonLogger.Msg("[GameAPIProvider] 🔧 Creating Mono providers...");

            _gameTime = new MonoGameTimeProvider();
            _player = new MonoPlayerProvider();
            _console = new MonoConsoleProvider();
            _deadDrop = new MonoDeadDropProvider();
            _storage = new MonoStorageProvider();
            
            // TODO: Implement remaining Mono providers
            _npc = new NullNPCProvider(); // Placeholder
            _saveSystem = new NullSaveSystemProvider(); // Placeholder
            _items = new NullItemProvider(); // Placeholder

            MelonLogger.Msg("[GameAPIProvider] ✅ Mono providers created");
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        public bool IsFullyInitialized()
        {
            return _initialized &&
                   _gameTime != null &&
                   _player != null &&
                   _console != null &&
                   _deadDrop != null &&
                   _storage != null &&
                   _npc != null &&
                   _saveSystem != null &&
                   _items != null;
        }

        /// <summary>
        /// Gets diagnostic information about all providers
        /// </summary>
        public string GetDiagnosticInfo()
        {
            EnsureInitialized();

            var info = $"Runtime: {RuntimeType}\n";
            info += $"GameTime Available: {_gameTime?.IsAvailable() ?? false}\n";
            info += $"Player Available: {_player?.IsPlayerAvailable() ?? false}\n";
            info += $"Console Available: {_console?.IsAvailable() ?? false}\n";
            info += $"DeadDrop Available: {_deadDrop?.IsAvailable() ?? false}\n";
            info += $"Storage Available: {_storage?.IsAvailable() ?? false}\n";
            info += $"NPC Available: {_npc?.IsAvailable() ?? false}\n";
            info += $"SaveSystem Available: {_saveSystem?.IsAvailable() ?? false}\n";
            info += $"Items Available: {_items?.IsAvailable() ?? false}\n";

            return info;
        }
    }

    #region Null Object Pattern Implementations

    /// <summary>
    /// Null object implementation for NPC provider (placeholder)
    /// </summary>
    internal class NullNPCProvider : INPCProvider
    {
        public object[] GetAllSuppliers() => new object[0];
        public object GetSupplier(string identifier) => null;
        public object CreateOrUpdateNPC(string name, UnityEngine.Vector3 position) => null;
        public bool IsAvailable() => false;
        public Type GetSupplierType() => null;
    }

    /// <summary>
    /// Null object implementation for save system provider (placeholder)
    /// </summary>
    internal class NullSaveSystemProvider : ISaveSystemProvider
    {
        public object GetSaveManager() => null;
        public string GetCurrentSaveFilePath() => string.Empty;
        public string GetPlayersSavePath() => string.Empty;
        public string GetSaveName() => string.Empty;
        public bool IsAvailable() => false;
        public Type GetSaveManagerType() => null;
    }

    /// <summary>
    /// Null object implementation for item provider (placeholder)
    /// </summary>
    internal class NullItemProvider : IItemProvider
    {
        public object GetItemDefinition(string itemId) => null;
        public object CreateItemInstance(string itemId, int quantity) => null;
        public System.Collections.Generic.Dictionary<string, object> GetAllItemDefinitions() => 
            new System.Collections.Generic.Dictionary<string, object>();
        public bool IsAvailable() => false;
    }

    #endregion
}
