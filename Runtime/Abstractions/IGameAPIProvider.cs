using System;
using System.Collections.Generic;
using UnityEngine;

namespace PaxDrops.Runtime.Abstractions
{
    /// <summary>
    /// Unified interface for game time operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface IGameTimeProvider
    {
        /// <summary>
        /// Gets the current game time
        /// </summary>
        DateTime GetCurrentTime();

        /// <summary>
        /// Gets the current game day
        /// </summary>
        int GetCurrentDay();

        /// <summary>
        /// Gets the current game hour (0-23)
        /// </summary>
        int GetCurrentHour();

        /// <summary>
        /// Gets the current game minute (0-59)
        /// </summary>
        int GetCurrentMinute();

        /// <summary>
        /// Gets the game time as a formatted string
        /// </summary>
        string GetFormattedTime();

        /// <summary>
        /// Checks if the game time system is available
        /// </summary>
        bool IsAvailable();
    }

    /// <summary>
    /// Unified interface for player operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface IPlayerProvider
    {
        /// <summary>
        /// Gets the current player instance
        /// </summary>
        object GetPlayer();

        /// <summary>
        /// Gets the player's name
        /// </summary>
        string GetPlayerName();

        /// <summary>
        /// Gets the player's current rank/level
        /// </summary>
        string GetPlayerRank();

        /// <summary>
        /// Gets the player's position in the world
        /// </summary>
        Vector3 GetPlayerPosition();

        /// <summary>
        /// Checks if a player is currently loaded and available
        /// </summary>
        bool IsPlayerAvailable();

        /// <summary>
        /// Gets the player's level manager
        /// </summary>
        object GetLevelManager();
    }

    /// <summary>
    /// Unified interface for console operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface IConsoleProvider
    {
        /// <summary>
        /// Gets the console instance
        /// </summary>
        object GetConsole();

        /// <summary>
        /// Executes a console command
        /// </summary>
        void ExecuteCommand(string command);

        /// <summary>
        /// Executes a console command with arguments
        /// </summary>
        void ExecuteCommand(List<string> args);

        /// <summary>
        /// Checks if the console system is available
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Gets the console type for patching
        /// </summary>
        Type GetConsoleType();
    }

    /// <summary>
    /// Unified interface for dead drop operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface IDeadDropProvider
    {
        /// <summary>
        /// Gets all dead drop objects in the scene
        /// </summary>
        object[] GetAllDeadDrops();

        /// <summary>
        /// Gets a dead drop by GUID
        /// </summary>
        object GetDeadDropByGuid(Guid guid);

        /// <summary>
        /// Creates a dead drop instance
        /// </summary>
        object CreateDeadDrop();

        /// <summary>
        /// Gets the storage entity from a dead drop
        /// </summary>
        object GetStorageEntity(object deadDrop);

        /// <summary>
        /// Checks if dead drop system is available
        /// </summary>
        bool IsAvailable();
    }

    /// <summary>
    /// Unified interface for storage operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Adds an item to storage
        /// </summary>
        bool AddItemToStorage(object storage, string itemId, int quantity);

        /// <summary>
        /// Adds cash to storage
        /// </summary>
        bool AddCashToStorage(object storage, int amount);

        /// <summary>
        /// Gets items from storage
        /// </summary>
        Dictionary<string, int> GetStorageItems(object storage);

        /// <summary>
        /// Checks if storage system is available
        /// </summary>
        bool IsAvailable();
    }

    /// <summary>
    /// Unified interface for NPC operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface INPCProvider
    {
        /// <summary>
        /// Gets all suppliers in the game
        /// </summary>
        object[] GetAllSuppliers();

        /// <summary>
        /// Gets a supplier by name or ID
        /// </summary>
        object GetSupplier(string identifier);

        /// <summary>
        /// Creates or updates an NPC
        /// </summary>
        object CreateOrUpdateNPC(string name, Vector3 position);

        /// <summary>
        /// Checks if NPC system is available
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Gets the supplier type for patching
        /// </summary>
        Type GetSupplierType();
    }

    /// <summary>
    /// Unified interface for save system operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface ISaveSystemProvider
    {
        /// <summary>
        /// Gets the save manager instance
        /// </summary>
        object GetSaveManager();

        /// <summary>
        /// Gets the current save file path
        /// </summary>
        string GetCurrentSaveFilePath();

        /// <summary>
        /// Gets the players save path
        /// </summary>
        string GetPlayersSavePath();

        /// <summary>
        /// Gets the current save name
        /// </summary>
        string GetSaveName();

        /// <summary>
        /// Checks if save system is available
        /// </summary>
        bool IsAvailable();

        /// <summary>
        /// Gets the save system type for patching
        /// </summary>
        Type GetSaveManagerType();
    }

    /// <summary>
    /// Unified interface for item framework operations across IL2CPP and Mono runtimes
    /// </summary>
    public interface IItemProvider
    {
        /// <summary>
        /// Gets an item definition by ID
        /// </summary>
        object GetItemDefinition(string itemId);

        /// <summary>
        /// Creates an item instance
        /// </summary>
        object CreateItemInstance(string itemId, int quantity);

        /// <summary>
        /// Gets all available item definitions
        /// </summary>
        Dictionary<string, object> GetAllItemDefinitions();

        /// <summary>
        /// Checks if item system is available
        /// </summary>
        bool IsAvailable();
    }

    /// <summary>
    /// Main abstraction factory that provides access to all game system providers
    /// based on the current runtime environment
    /// </summary>
    public interface IGameAPIProvider
    {
        /// <summary>
        /// Gets the game time provider
        /// </summary>
        IGameTimeProvider GameTime { get; }

        /// <summary>
        /// Gets the player provider
        /// </summary>
        IPlayerProvider Player { get; }

        /// <summary>
        /// Gets the console provider
        /// </summary>
        IConsoleProvider Console { get; }

        /// <summary>
        /// Gets the dead drop provider
        /// </summary>
        IDeadDropProvider DeadDrop { get; }

        /// <summary>
        /// Gets the storage provider
        /// </summary>
        IStorageProvider Storage { get; }

        /// <summary>
        /// Gets the NPC provider
        /// </summary>
        INPCProvider NPC { get; }

        /// <summary>
        /// Gets the save system provider
        /// </summary>
        ISaveSystemProvider SaveSystem { get; }

        /// <summary>
        /// Gets the item provider
        /// </summary>
        IItemProvider Items { get; }

        /// <summary>
        /// Gets the current runtime type
        /// </summary>
        string RuntimeType { get; }

        /// <summary>
        /// Initializes all providers
        /// </summary>
        void Initialize();

        /// <summary>
        /// Checks if all core providers are available
        /// </summary>
        bool IsFullyInitialized();
    }
}
