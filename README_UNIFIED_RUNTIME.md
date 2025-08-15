# PaxDrops Unified Runtime System

This document describes the new unified runtime system that allows PaxDrops to work with both **IL2CPP** and **Mono** versions of Schedule I from a single codebase.

## 🎯 Overview

The unified runtime system automatically detects whether the game is running on IL2CPP or Mono and provides the appropriate implementations through a common interface. This eliminates the need to maintain separate codebases.

## 🏗️ Architecture

### Core Components

1. **RuntimeEnvironment** - Detects IL2CPP vs Mono at startup
2. **Game API Abstractions** - Unified interfaces for all game systems
3. **Runtime-Specific Providers** - IL2CPP and Mono implementations
4. **GameAPIProvider** - Main factory that provides access to all systems

### Directory Structure

```
Runtime/
├── RuntimeEnvironment.cs          # Runtime detection
├── GameAPIProvider.cs            # Main API factory
├── MigrationExample.cs           # Examples for migrating code
├── Abstractions/
│   └── IGameAPIProvider.cs       # All interface definitions
├── IL2CPP/
│   └── IL2CPPProviders.cs        # IL2CPP implementations
└── Mono/
    └── MonoProviders.cs          # Mono implementations (using reflection)
```

## 🚀 Quick Start

### 1. Use the Unified API

Instead of directly accessing IL2CPP types:

```csharp
// OLD: IL2CPP-specific code
var timeManager = Il2CppScheduleOne.GameTime.TimeManager.Instance;
var currentTime = timeManager.CurrentTime;

// NEW: Unified code that works with both runtimes
var gameAPI = GameAPIProvider.Instance;
var gameTime = gameAPI.GameTime;
if (gameTime.IsAvailable())
{
    var formattedTime = gameTime.GetFormattedTime();
    var currentDay = gameTime.GetCurrentDay();
}
```

### 2. Available Providers

- **GameTime** - Time management operations
- **Player** - Player-related operations  
- **Console** - Console command execution
- **DeadDrop** - Dead drop management
- **Storage** - Storage operations
- **NPC** - NPC interactions (TODO)
- **SaveSystem** - Save file operations (TODO)
- **Items** - Item management (TODO)

### 3. Runtime Detection

Check the current runtime when needed:

```csharp
if (RuntimeEnvironment.IsIL2CPP)
{
    // IL2CPP-specific optimizations
}
else
{
    // Mono-specific code paths
}

Logger.Info($"Running on: {RuntimeEnvironment.RuntimeType}");
```

## 🔧 Building

### Build Both Versions

Use the provided build scripts:

```bash
# Linux/Mac
./build_unified.sh

# Windows
build_unified.bat
```

This creates:
- `dist/IL2CPP/PaxDrops.dll` - For IL2CPP games
- `dist/Mono/PaxDrops.Mono.dll` - For Mono games

### Manual Build

```bash
# IL2CPP version (default)
dotnet build --configuration Release

# Mono version
dotnet build --configuration Release -p:RuntimeTarget=Mono
```

## 📝 Migration Guide

### Step 1: Replace Direct IL2CPP Imports

**Before:**
```csharp
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.PlayerScripts;
```

**After:**
```csharp
using PaxDrops.Runtime;
using PaxDrops.Runtime.Abstractions;

#if !MONO_BUILD
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.PlayerScripts;
#endif
```

### Step 2: Use the Unified API

**Before:**
```csharp
public static void GetPlayerInfo()
{
    var player = Il2CppScheduleOne.PlayerScripts.Player.Instance;
    var levelManager = Il2CppScheduleOne.Levelling.LevelManager.Instance;
    var rank = levelManager?.Rank;
}
```

**After:**
```csharp
public static void GetPlayerInfo()
{
    var gameAPI = GameAPIProvider.Instance;
    var player = gameAPI.Player;
    
    if (player.IsPlayerAvailable())
    {
        var rank = player.GetPlayerRank();
        var position = player.GetPlayerPosition();
        
        // Runtime-specific casting when needed
        if (RuntimeEnvironment.IsIL2CPP)
        {
#if !MONO_BUILD
            if (rank is Il2CppScheduleOne.Levelling.ERank il2cppRank)
            {
                // Use IL2CPP-specific enum operations
            }
#endif
        }
    }
}
```

### Step 3: Update Console Commands

**Before:**
```csharp
var console = Il2CppScheduleOne.Console.Instance;
var args = new Il2CppSystem.Collections.Generic.List<string>();
args.Add("command");
console.SubmitCommand(args);
```

**After:**
```csharp
var gameAPI = GameAPIProvider.Instance;
var console = gameAPI.Console;

if (console.IsAvailable())
{
    var args = new System.Collections.Generic.List<string> { "command" };
    console.ExecuteCommand(args);
}
```

## 🔍 Debugging

### Runtime Diagnostics

```csharp
var gameAPI = GameAPIProvider.Instance;
Logger.Info(gameAPI.GetDiagnosticInfo());

// Output example:
// Runtime: IL2CPP
// GameTime Available: True
// Player Available: True
// Console Available: True
// DeadDrop Available: True
// Storage Available: True
```

### Force Runtime Re-detection

```csharp
RuntimeEnvironment.ForceRedetection();
```

## ⚠️ Important Notes

### Conditional Compilation

The system uses preprocessor directives to exclude IL2CPP-specific code in Mono builds:

```csharp
#if !MONO_BUILD
// This code only compiles for IL2CPP builds
using Il2CppScheduleOne.GameTime;
#endif
```

### Performance Considerations

- **IL2CPP**: Direct type access, optimal performance
- **Mono**: Uses reflection, slightly slower but cached for efficiency

### Reflection Caching

The Mono providers cache reflection information for better performance:

```csharp
private Type _timeManagerType;
private PropertyInfo _currentTimeProperty;
// These are cached during initialization
```

## 🛠️ Extending the System

### Adding New Providers

1. **Define Interface** in `Abstractions/IGameAPIProvider.cs`
2. **Implement IL2CPP Version** in `IL2CPP/IL2CPPProviders.cs`
3. **Implement Mono Version** in `Mono/MonoProviders.cs` using reflection
4. **Add to GameAPIProvider** factory

### Example New Provider

```csharp
// 1. Interface
public interface ICustomProvider
{
    bool DoSomething();
    bool IsAvailable();
}

// 2. IL2CPP Implementation
public class IL2CPPCustomProvider : ICustomProvider
{
    public bool DoSomething()
    {
#if !MONO_BUILD
        // Direct IL2CPP access
        return Il2CppScheduleOne.Custom.Manager.DoSomething();
#else
        return false;
#endif
    }

    public bool IsAvailable() => /* check availability */;
}

// 3. Mono Implementation
public class MonoCustomProvider : ICustomProvider
{
    private Type _customManagerType;
    
    public MonoCustomProvider()
    {
        _customManagerType = Type.GetType("ScheduleOne.Custom.Manager, Assembly-CSharp");
    }
    
    public bool DoSomething()
    {
        // Use reflection
        var method = _customManagerType?.GetMethod("DoSomething");
        return (bool)(method?.Invoke(null, null) ?? false);
    }
}
```

## 🚦 Testing

### Runtime Detection Test

```csharp
public static void TestRuntimeDetection()
{
    Logger.Info($"Detected Runtime: {RuntimeEnvironment.RuntimeType}");
    Logger.Info($"Is IL2CPP: {RuntimeEnvironment.IsIL2CPP}");
    Logger.Info($"Is Mono: {RuntimeEnvironment.IsMono}");
}
```

### Provider Availability Test

```csharp
public static void TestProviders()
{
    var gameAPI = GameAPIProvider.Instance;
    
    Logger.Info($"GameTime: {gameAPI.GameTime.IsAvailable()}");
    Logger.Info($"Player: {gameAPI.Player.IsPlayerAvailable()}");
    Logger.Info($"Console: {gameAPI.Console.IsAvailable()}");
    // ... test all providers
}
```

## 📋 TODO

- [ ] Complete NPC provider implementations
- [ ] Complete Save System provider implementations  
- [ ] Complete Item provider implementations
- [ ] Add more comprehensive error handling
- [ ] Add performance benchmarks
- [ ] Add unit tests for both runtimes
- [ ] Create automated CI/CD for both builds

## 🤝 Contributing

When adding new functionality:

1. Always use the unified API providers
2. Add both IL2CPP and Mono implementations
3. Test on both runtimes
4. Update this documentation
5. Add migration examples

## 📄 License

Same license as the main PaxDrops project.
