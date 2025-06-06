using System;
using System.IO;
using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using MelonLoader;
using System.Collections.Generic;

namespace PaxDrops.Patches
{
    /// <summary>
    /// Harmony patches for hooking into the game's save system to trigger our JSON saves
    /// and manage different save files separately.
    /// </summary>
    [HarmonyPatch]
    public static class SaveSystemPatch
    {
        private static HarmonyLib.Harmony? _harmony;
        private static bool _initialized;
        
        // Save deduplication mechanism
        private static DateTime _lastSaveTime = DateTime.MinValue;
        private static string? _lastSavePath;
        private static string? _lastSaveName;
        private const int SAVE_COOLDOWN_MS = 1000; // 1 second cooldown between saves

        // Cache the last full save path captured from actual save operations
        private static string? _lastCapturedFullPath = null;
        private static DateTime _lastPathCaptureTime = DateTime.MinValue;

        /// <summary>
        /// Initialize the save system patches
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;

            try
            {
                Logger.Debug("🔧 Setting up save system hooks...", "SaveSystemPatch");
                SetupHarmonyPatches();
                _initialized = true;
                Logger.Debug("✅ Save system patches ready", "SaveSystemPatch");
            }
            catch (Exception ex)
            {
                Logger.Error("❌ Save system patch initialization failed.", "SaveSystemPatch");
                Logger.Exception(ex, "SaveSystemPatch");
            }
        }

        /// <summary>
        /// Set up Harmony patches to intercept save operations
        /// </summary>
        private static void SetupHarmonyPatches()
        {
            try
            {
                _harmony = new HarmonyLib.Harmony("PaxDrops.SaveSystemPatch");
                
                var saveManagerType = typeof(SaveManager);
                
                // Patch SaveManager.Save() (no parameters)
                var saveMethod = saveManagerType.GetMethod("Save", new Type[] { });
                if (saveMethod != null)
                {
                    var patchMethod = typeof(SaveSystemPatch).GetMethod(nameof(SavePostfix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(saveMethod, postfix: new HarmonyMethod(patchMethod));
                    Logger.Info("⚙️ SaveManager.Save() patch applied", "SaveSystemPatch");
                }
                else
                {
                    Logger.Error("❌ Could not find SaveManager.Save() method", "SaveSystemPatch");
                }
                
                // Patch SaveManager.Save(string) for manual saves with specific path
                var saveWithPathMethod = saveManagerType.GetMethod("Save", new[] { typeof(string) });
                if (saveWithPathMethod != null)
                {
                    var patchWithPathMethod = typeof(SaveSystemPatch).GetMethod(nameof(SaveWithPathPostfix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony.Patch(saveWithPathMethod, postfix: new HarmonyMethod(patchWithPathMethod));
                    Logger.Debug("⚙️ SaveManager.Save(string) patch applied", "SaveSystemPatch");
                }
                else
                {
                    Logger.Error("❌ Could not find SaveManager.Save(string) method", "SaveSystemPatch");
                }

                // Add file system hooks to capture actual save paths
                ApplyFileSystemHooks();
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Harmony patch setup failed: {ex.Message}", "SaveSystemPatch");
            }
        }

        /// <summary>
        /// Check if we should skip this save operation due to deduplication
        /// </summary>
        private static bool ShouldSkipSave(string savePath, string saveName, string methodName)
        {
            var now = DateTime.Now;
            var timeSinceLastSave = now - _lastSaveTime;
            
            // Normalize both paths for consistent comparison
            string normalizedPath = NormalizePath(savePath);
            string normalizedLastPath = NormalizePath(_lastSavePath ?? "");
            
            // If we're within the cooldown period and it's the same save, skip it
            if (timeSinceLastSave.TotalMilliseconds < SAVE_COOLDOWN_MS &&
                normalizedLastPath == normalizedPath && _lastSaveName == saveName)
            {
                Logger.Debug($"⏳ Skipping duplicate save within cooldown: {methodName} ({timeSinceLastSave.TotalMilliseconds:F0}ms ago)", "SaveSystemPatch");
                Logger.Debug($"   Same normalized path: '{normalizedPath}' == '{normalizedLastPath}'", "SaveSystemPatch");
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Normalize save path for consistent comparison (matches SaveFileJsonDataStore logic)
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "";
                
            try
            {
                // Convert to full path and normalize separators
                string normalized = Path.GetFullPath(path);
                
                // Always use forward slashes for consistency
                normalized = normalized.Replace('\\', '/');
                
                // Remove trailing slashes
                normalized = normalized.TrimEnd('/');
                
                // Convert to lowercase for case-insensitive comparison
                normalized = normalized.ToLowerInvariant();
                
                // Extract the base saves directory to handle different path formats
                if (normalized.Contains("/saves/") && normalized.Contains("/schedule i/"))
                {
                    // Extract everything up to and including "/saves" for consistency
                    int savesIndex = normalized.IndexOf("/saves");
                    if (savesIndex != -1)
                    {
                        normalized = normalized.Substring(0, savesIndex + "/saves".Length);
                    }
                }
                
                return normalized;
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Path normalization failed for '{path}': {ex.Message}", "SaveSystemPatch");
                return path?.ToLowerInvariant() ?? "";
            }
        }

        /// <summary>
        /// Update the last save tracking with normalized path
        /// </summary>
        private static void UpdateLastSave(string savePath, string saveName)
        {
            _lastSaveTime = DateTime.Now;
            _lastSavePath = NormalizePath(savePath); // Store normalized path
            _lastSaveName = saveName;
        }

        /// <summary>
        /// Harmony postfix patch - called after SaveManager.Save() completes
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), "Save", new Type[] { })]
        private static void SavePostfix(SaveManager __instance)
        {
            try
            {
                // Get the current save info to identify which save file we're dealing with
                string? savePath = __instance?.PlayersSavePath;
                string? saveName = __instance?.SaveName;
                
                // Enhanced logging for debugging multiple saves
                Logger.Info("📊 Save() Method Triggered:", "SaveSystemPatch");
                Logger.Debug($"   Instance: {(__instance != null ? "Valid" : "NULL")}", "SaveSystemPatch");
                Logger.Debug($"   PlayersSavePath: '{savePath ?? "NULL"}'", "SaveSystemPatch");
                Logger.Debug($"   SaveName: '{saveName ?? "NULL"}'", "SaveSystemPatch");
                
                if (string.IsNullOrEmpty(savePath) || string.IsNullOrEmpty(saveName))
                {
                    Logger.Warn("⚠️ Save() - Missing path or name, using defaults", "SaveSystemPatch");
                    savePath = "";
                    saveName = "default";
                }
                
                // Check for duplicate save
                if (ShouldSkipSave(savePath, saveName, "Save()"))
                    return;
                
                // Normalize path for consistent save ID generation
                string normalizedPath = NormalizePath(savePath);
                Logger.Debug($"💾 Processing Save() - Normalized Path: {normalizedPath}, Name: {saveName}", "SaveSystemPatch");
                
                // Update tracking and trigger save
                UpdateLastSave(savePath, saveName);
                SaveFileJsonDataStore.SaveForCurrentSaveFile(normalizedPath, saveName);
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Save() postfix error: {ex.Message}", "SaveSystemPatch");
                Logger.Exception(ex, "SaveSystemPatch");
            }
        }

        /// <summary>
        /// Harmony postfix patch - called after SaveManager.Save(string) completes
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveManager), "Save", new[] { typeof(string) })]
        private static void SaveWithPathPostfix(SaveManager __instance, string saveFolderPath)
        {
            try
            {
                string? saveName = __instance?.SaveName;
                
                // Enhanced logging for debugging multiple saves  
                Logger.Debug("📊 Save(string) Method Triggered:", "SaveSystemPatch");
                Logger.Debug($"   Instance: {(__instance != null ? "Valid" : "NULL")}", "SaveSystemPatch");
                Logger.Debug($"   saveFolderPath param: '{saveFolderPath ?? "NULL"}'", "SaveSystemPatch");
                Logger.Debug($"   SaveName: '{saveName ?? "NULL"}'", "SaveSystemPatch");
                Logger.Debug($"   PlayersSavePath: '{(__instance?.PlayersSavePath ?? "NULL")}'", "SaveSystemPatch");
                
                if (string.IsNullOrEmpty(saveFolderPath))
                {
                    Logger.Warn("⚠️ Save(string) - Empty folder path, using defaults", "SaveSystemPatch");
                    saveFolderPath = "";
                    saveName = saveName ?? "manual";
                }
                
                // Check for duplicate save
                if (ShouldSkipSave(saveFolderPath, saveName ?? "manual", "Save(string)"))
                    return;
                
                // Normalize path for consistent save ID generation
                string normalizedPath = NormalizePath(saveFolderPath);
                Logger.Debug($"💾 Processing Save(string) - Normalized FolderPath: {normalizedPath}, Name: {saveName ?? "manual"}", "SaveSystemPatch");
                
                // Update tracking and trigger save
                UpdateLastSave(saveFolderPath, saveName ?? "manual");
                SaveFileJsonDataStore.SaveForCurrentSaveFile(normalizedPath, saveName ?? "manual");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Save(string) postfix error: {ex.Message}", "SaveSystemPatch");
                Logger.Exception(ex, "SaveSystemPatch");
            }
        }

        /// <summary>
        /// Shutdown the save system patches
        /// </summary>
        public static void Shutdown()
        {
            if (!_initialized) return;
            
            _harmony?.UnpatchSelf();
            _harmony = null;
            _initialized = false;
            
            // Reset save tracking
            _lastSaveTime = DateTime.MinValue;
            _lastSavePath = null;
            _lastSaveName = null;
            
            Logger.Info("🔌 Save system patches shutdown", "SaveSystemPatch");
        }

        /// <summary>
        /// Get the last captured full save path from actual save operations
        /// </summary>
        public static string? GetLastCapturedSavePath()
        {
            // Only return if captured recently (within last 30 seconds)
            if (DateTime.Now - _lastPathCaptureTime < TimeSpan.FromSeconds(30))
            {
                return _lastCapturedFullPath;
            }
            return null;
        }

        /// <summary>
        /// Apply file system hooks to capture actual save paths
        /// </summary>
        private static void ApplyFileSystemHooks()
        {
            try
            {
                // Hook File.WriteAllText to capture save file paths
                var fileWriteAllTextMethod = typeof(File).GetMethod("WriteAllText", new[] { typeof(string), typeof(string) });
                if (fileWriteAllTextMethod != null)
                {
                    var hookMethod = typeof(SaveSystemPatch).GetMethod(nameof(FileWriteAllTextPrefix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony?.Patch(fileWriteAllTextMethod, prefix: new HarmonyMethod(hookMethod));
                    Logger.Debug("⚙️ File.WriteAllText hook applied", "SaveSystemPatch");
                }

                // Hook Directory.CreateDirectory to capture directory creation
                var dirCreateMethod = typeof(Directory).GetMethod("CreateDirectory", new[] { typeof(string) });
                if (dirCreateMethod != null)
                {
                    var hookMethod = typeof(SaveSystemPatch).GetMethod(nameof(DirectoryCreatePrefix), 
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    _harmony?.Patch(dirCreateMethod, prefix: new HarmonyMethod(hookMethod));
                    Logger.Debug("⚙️ Directory.CreateDirectory hook applied", "SaveSystemPatch");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ File system hooks failed (non-critical): {ex.Message}", "SaveSystemPatch");
            }
        }

        /// <summary>
        /// Hook File.WriteAllText to capture save file paths
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(File), "WriteAllText", new Type[] { typeof(string), typeof(string) })]
        private static bool FileWriteAllTextPrefix(string path, string contents)
        {
            try
            {
                // Check if this is a game save file operation
                if (path.Contains("SaveGame_") || path.Contains("Saves") || path.Contains(".s1"))
                {
                    Logger.Debug($"🔍 File save detected: {path}", "SaveSystemPatch");
                    
                    // Extract Steam ID from the actual save path
                    if (path.Contains("/") || path.Contains("\\"))
                    {
                        var pathParts = path.Replace('\\', '/').Split('/');
                        foreach (var part in pathParts)
                        {
                            if (part.Length == 17 && long.TryParse(part, out _))
                            {
                                _lastCapturedFullPath = path;
                                _lastPathCaptureTime = DateTime.Now;
                                Logger.Info($"🎯 Steam ID captured from file operation: {part} (path: {path})", "SaveSystemPatch");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"🔍 File hook error (non-critical): {ex.Message}", "SaveSystemPatch");
            }
            
            // Always allow the original file operation to proceed
            return true;
        }

        /// <summary>
        /// Hook Directory.CreateDirectory to capture save directory creation
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Directory), "CreateDirectory", new Type[] { typeof(string) })]
        private static bool DirectoryCreatePrefix(string path)
        {
            try
            {
                // Check if this is a save directory creation
                if (path.Contains("SaveGame_") || path.Contains("Saves"))
                {
                    Logger.Debug($"🔍 Directory creation detected: {path}", "SaveSystemPatch");
                    
                    // Extract Steam ID from the directory path
                    if (path.Contains("/") || path.Contains("\\"))
                    {
                        var pathParts = path.Replace('\\', '/').Split('/');
                        foreach (var part in pathParts)
                        {
                            if (part.Length == 17 && long.TryParse(part, out _))
                            {
                                _lastCapturedFullPath = path;
                                _lastPathCaptureTime = DateTime.Now;
                                Logger.Info($"🎯 Steam ID captured from directory operation: {part} (path: {path})", "SaveSystemPatch");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"🔍 Directory hook error (non-critical): {ex.Message}", "SaveSystemPatch");
            }
            
            // Always allow the original directory operation to proceed
            return true;
        }
    }
} 