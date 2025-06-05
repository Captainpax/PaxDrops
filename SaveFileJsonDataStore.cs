using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;
using MelonLoader;

namespace PaxDrops
{
    /// <summary>
    /// Save file-aware JSON data store that manages PaxDrops data separately for each game save.
    /// Only saves when the actual game save occurs, and loads the appropriate data based on the current save file.
    /// Enhanced with Steam ID, organization name, and save creation date for robust save identification.
    /// </summary>
    public static class SaveFileJsonDataStore
    {
        /// <summary>
        /// Drop record for JSON storage
        /// </summary>
        [Serializable]
        public class DropRecord
        {
            public int Day { get; set; }
            public List<string> Items { get; set; } = new List<string>();
            public int DropHour { get; set; }
            public string DropTime { get; set; } = "";
            public string Org { get; set; } = "";
            public string CreatedTime { get; set; } = "";
            public string Type { get; set; } = "";
            public string Location { get; set; } = "";
            public string ExpiryTime { get; set; } = "";
            public int OrderDay { get; set; }
            public bool IsCollected { get; set; }
            public int InitialItemCount { get; set; }
        }

        /// <summary>
        /// Enhanced save identification metadata
        /// </summary>
        [Serializable]
        public class SaveMetadata
        {
            public string SaveId { get; set; } = "";
            public string SteamId { get; set; } = "";
            public string OrganizationName { get; set; } = "";
            public string SaveName { get; set; } = "";
            public string SavePath { get; set; } = "";
            public string StartDate { get; set; } = "";
            public string CreationTimestamp { get; set; } = "";
            public string LastAccessed { get; set; } = "";
        }
        
        private const string BaseDataDir = "Mods/PaxDrops/SaveFiles";
        
        // Current save file info - enhanced with Steam ID and metadata
        private static string? _currentSaveId;
        private static string? _currentSteamId;
        private static string? _currentSavePath;
        private static string? _currentSaveName;
        private static SaveMetadata? _currentSaveMetadata;
        private static bool _isLoadedForSave = false;
        
        // Data storage (same structure as old JsonDataStore but per-save)
        public static readonly Dictionary<int, List<DropRecord>> PendingDrops = new Dictionary<int, List<DropRecord>>();
        public static readonly Dictionary<int, int> MrsStacksOrdersToday = new Dictionary<int, int>();
        
        private static int _lastMrsStacksOrderDay = -1;

        /// <summary>
        /// Initialize the save file-aware data store (called once at startup)
        /// </summary>
        public static void Init()
        {
            try
            {
                Directory.CreateDirectory(BaseDataDir);
                Logger.Msg("[SaveFileJsonDataStore] ✅ Initialized enhanced save file-aware data store with Steam ID support");
            }
            catch (Exception ex)
            {
                Logger.Error("[SaveFileJsonDataStore] ❌ Failed to initialize save file data store.");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Load data for a specific save file when entering the main scene
        /// </summary>
        public static void LoadForSaveFile(string savePath, string saveName)
        {
            try
            {
                Logger.Msg($"[SaveFileJsonDataStore] 📂 Loading data for save: {saveName}");
                Logger.Msg($"[SaveFileJsonDataStore] 📁 Save path: {savePath}");
                
                // Generate enhanced save identification
                var saveMetadata = GenerateSaveMetadata(savePath, saveName);
                
                Logger.Msg($"[SaveFileJsonDataStore] 🔍 Generated metadata:");
                Logger.Msg($"[SaveFileJsonDataStore]   Steam ID: '{saveMetadata.SteamId}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Organization: '{saveMetadata.OrganizationName}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Start Date: '{saveMetadata.StartDate}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Save ID: '{saveMetadata.SaveId}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Folder: SaveFiles/{saveMetadata.SteamId}/{saveMetadata.SaveId}/");
                
                if (_currentSaveId == saveMetadata.SaveId && _isLoadedForSave)
                {
                    Logger.Msg($"[SaveFileJsonDataStore] ♻️ Data already loaded for save: {saveName} (Steam: {saveMetadata.SteamId})");
                    return;
                }

                // Clear current data
                ClearCurrentData();
                
                // Store the metadata for consistent use throughout the session
                _currentSaveId = saveMetadata.SaveId;
                _currentSteamId = saveMetadata.SteamId;
                _currentSavePath = savePath;
                _currentSaveName = saveName;
                _currentSaveMetadata = saveMetadata;
                
                Logger.Msg($"[SaveFileJsonDataStore] ✅ Cached metadata - Steam: {_currentSteamId}, ID: {_currentSaveId}");
                
                // Load data for this save
                LoadDataForCurrentSave();
                
                // Load conversation data for this save
                PaxDrops.MrStacks.MrsStacksMessaging.LoadConversationForCurrentSave();
                
                _isLoadedForSave = true;
                Logger.Msg($"[SaveFileJsonDataStore] 📂 Successfully loaded data for save: {saveName}");
                Logger.Msg($"[SaveFileJsonDataStore] 🆔 Final IDs - Steam: {saveMetadata.SteamId} | Save: {saveMetadata.SaveId}");
                
                // Save metadata file for reference
                SaveMetadataFile();
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to load data for save file: {saveName}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Unload current save data when exiting to menu
        /// </summary>
        public static void UnloadCurrentSave()
        {
            try
            {
                if (!_isLoadedForSave)
                {
                    Logger.Msg("[SaveFileJsonDataStore] ℹ️ No save data loaded to unload");
                    return;
                }

                Logger.Msg($"[SaveFileJsonDataStore] 📤 Unloading data for save: {_currentSaveName}");
                
                // Unload conversation data for this save
                PaxDrops.MrStacks.MrsStacksMessaging.UnloadConversationForCurrentSave();
                
                ClearCurrentData();
                _currentSaveId = null;
                _currentSteamId = null;
                _currentSavePath = null;
                _currentSaveName = null;
                _currentSaveMetadata = null;
                _isLoadedForSave = false;
                
                Logger.Msg("[SaveFileJsonDataStore] ✅ Save data and conversation unloaded");
            }
            catch (Exception ex)
            {
                Logger.Error("[SaveFileJsonDataStore] ❌ Failed to unload save data");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Save data for the current save file (called when game saves)
        /// </summary>
        public static void SaveForCurrentSaveFile(string savePath, string saveName)
        {
            try
            {
                if (!_isLoadedForSave)
                {
                    Logger.Warn("[SaveFileJsonDataStore] ⚠️ No save loaded - cannot save PaxDrops data");
                    return;
                }

                Logger.Msg($"[SaveFileJsonDataStore] 💾 Starting save for: {saveName}");
                Logger.Msg($"[SaveFileJsonDataStore] 📁 Current save ID: {_currentSaveId}");
                Logger.Msg($"[SaveFileJsonDataStore] 👤 Current Steam ID: {_currentSteamId}");

                // DON'T regenerate metadata - use the existing cached metadata to ensure consistency
                if (_currentSaveMetadata == null || string.IsNullOrEmpty(_currentSaveId))
                {
                    Logger.Error("[SaveFileJsonDataStore] ❌ Missing cached metadata - this shouldn't happen!");
                    
                    // Emergency fallback: regenerate metadata but log the discrepancy
                    var emergencyMetadata = GenerateSaveMetadata(savePath, saveName);
                    Logger.Warn($"[SaveFileJsonDataStore] 🚨 Emergency metadata generation - ID: {emergencyMetadata.SaveId}");
                    Logger.Warn($"[SaveFileJsonDataStore] 🚨 This indicates a bug in the initialization process!");
                    
                    _currentSaveId = emergencyMetadata.SaveId;
                    _currentSteamId = emergencyMetadata.SteamId;
                    _currentSaveMetadata = emergencyMetadata;
                }
                else
                {
                    // Just update the access time in existing metadata
                    _currentSaveMetadata.LastAccessed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    Logger.Msg($"[SaveFileJsonDataStore] ✅ Using cached metadata - ID: {_currentSaveId}");
                }

                SaveDataForCurrentSave();
                SaveMetadataFile();
                
                // Also save conversation data when game saves
                PaxDrops.MrStacks.MrsStacksMessaging.ForceSaveConversation();
                
                Logger.Msg($"[SaveFileJsonDataStore] 💾 Saved PaxDrops data for: {saveName} (Steam: {_currentSteamId}, ID: {_currentSaveId})");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to save data for current save file");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Generate enhanced save metadata including Steam ID, organization name, and start date
        /// </summary>
        private static SaveMetadata GenerateSaveMetadata(string? savePath, string? saveName)
        {
            if (savePath == null || saveName == null)
            {
                Logger.Error("[SaveFileJsonDataStore] ❌ Failed to generate save metadata: savePath or saveName is null");
                return new SaveMetadata();
            }

            try
            {
                var metadata = new SaveMetadata
                {
                    SaveName = saveName?.Trim() ?? "",
                    SavePath = savePath?.Trim() ?? "",
                    CreationTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastAccessed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // Extract Steam ID from save path
                metadata.SteamId = ExtractSteamIdFromPath(savePath);

                // Get organization name from game
                metadata.OrganizationName = GetOrganizationNameFromGame();

                // Get start date from save info
                metadata.StartDate = GetSaveStartDate();

                // Generate save ID from enhanced data
                metadata.SaveId = GenerateEnhancedSaveId(metadata);

                Logger.Msg($"[SaveFileJsonDataStore] 🔍 Enhanced Save Identification:");
                Logger.Msg($"[SaveFileJsonDataStore]   Steam ID: '{metadata.SteamId}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Organization: '{metadata.OrganizationName}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Start Date: '{metadata.StartDate}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Save Name: '{metadata.SaveName}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Generated ID: '{metadata.SaveId}'");

                return metadata;
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to generate save metadata: {ex.Message}");
                
                // Fallback to basic metadata
                var fallbackMetadata = new SaveMetadata
                {
                    SaveName = saveName?.Trim() ?? "",
                    SavePath = savePath?.Trim() ?? "",
                    SteamId = "unknown",
                    OrganizationName = "unknown",
                    StartDate = "unknown",
                    CreationTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    LastAccessed = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                fallbackMetadata.SaveId = GenerateBasicSaveId(savePath, saveName);
                
                return fallbackMetadata;
            }
        }

        /// <summary>
        /// Extract Steam ID from save path (common locations in Steam save paths)
        /// </summary>
        private static string ExtractSteamIdFromPath(string? savePath)
        {
            try
            {
                if (string.IsNullOrEmpty(savePath))
                    return "local";

                // Common Steam ID patterns in save paths:
                // C:/Users/Username/AppData/LocalLow/TVGS/Schedule I/Saves/76561198123456789/SaveGame_1
                // Or path could contain Steam ID in various places
                
                var pathParts = savePath.Replace('\\', '/').Split('/');
                
                foreach (var part in pathParts)
                {
                    // Steam IDs are typically 17 digits starting with 7656119
                    if (part.Length == 17 && part.StartsWith("7656119") && long.TryParse(part, out _))
                    {
                        Logger.Msg($"[SaveFileJsonDataStore] 🔍 Extracted Steam ID from path: {part}");
                        return part;
                    }
                }

                // Try to extract from filename or other numeric patterns
                foreach (var part in pathParts)
                {
                    if (part.Length >= 10 && long.TryParse(part, out var numericPart) && numericPart > 1000000000)
                    {
                        Logger.Msg($"[SaveFileJsonDataStore] 🔍 Found potential user ID in path: {part}");
                        return part;
                    }
                }

                // Fallback: use a hash of the save path to create a consistent user identifier
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(savePath));
                    string pathHash = Convert.ToBase64String(hashedBytes).Replace('/', '_').Replace('+', '-').Substring(0, 8);
                    Logger.Msg($"[SaveFileJsonDataStore] 🔍 No Steam ID found, using path hash as user ID: {pathHash}");
                    return $"user_{pathHash}";
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SaveFileJsonDataStore] ⚠️ Steam ID extraction failed: {ex.Message}");
                return "unknown";
            }
        }

        /// <summary>
        /// Get organization name from game's LoadManager
        /// </summary>
        private static string GetOrganizationNameFromGame()
        {
            try
            {
                var loadManager = LoadManager.Instance;
                if (loadManager?.ActiveSaveInfo != null)
                {
                    string orgName = loadManager.ActiveSaveInfo.OrganisationName;
                    if (!string.IsNullOrEmpty(orgName))
                    {
                        Logger.Msg($"[SaveFileJsonDataStore] 🏢 Organization from game: {orgName}");
                        return orgName.Trim();
                    }
                }

                Logger.Warn("[SaveFileJsonDataStore] ⚠️ Could not get organization name from game");
                return "unknown_org";
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SaveFileJsonDataStore] ⚠️ Organization name retrieval failed: {ex.Message}");
                return "unknown_org";
            }
        }

        /// <summary>
        /// Get save start date from game data
        /// </summary>
        private static string GetSaveStartDate()
        {
            try
            {
                var loadManager = LoadManager.Instance;
                if (loadManager?.ActiveSaveInfo != null)
                {
                    // Try to get creation date from save info
                    var saveInfo = loadManager.ActiveSaveInfo;
                    
                    // Check if there's a creation date property (this may vary depending on Schedule I version)
                    var saveInfoType = saveInfo.GetType();
                    var dateProperties = new[] { "CreationDate", "StartDate", "CreatedAt", "SaveDate" };
                    
                    foreach (var propName in dateProperties)
                    {
                        var prop = saveInfoType.GetProperty(propName);
                        if (prop != null)
                        {
                            var dateValue = prop.GetValue(saveInfo);
                            if (dateValue != null)
                            {
                                string dateStr = dateValue.ToString() ?? "";
                                Logger.Msg($"[SaveFileJsonDataStore] 📅 Save start date from {propName}: {dateStr}");
                                return dateStr;
                            }
                        }
                    }
                }

                // Fallback: use current game day as start reference
                var timeManager = TimeManager.Instance;
                if (timeManager != null)
                {
                    int gameDay = timeManager.ElapsedDays;
                    string gameDate = $"Day_{gameDay}";
                    Logger.Msg($"[SaveFileJsonDataStore] 📅 Using current game day as start reference: {gameDate}");
                    return gameDate;
                }

                Logger.Warn("[SaveFileJsonDataStore] ⚠️ Could not determine save start date");
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SaveFileJsonDataStore] ⚠️ Save start date retrieval failed: {ex.Message}");
                return DateTime.Now.ToString("yyyy-MM-dd");
            }
        }

        /// <summary>
        /// Generate enhanced save ID using Steam ID, organization, start date, and save details
        /// </summary>
        private static string GenerateEnhancedSaveId(SaveMetadata metadata)
        {
            try
            {
                // Combine all identifying information
                string combined = $"{metadata.SteamId}|{metadata.OrganizationName}|{metadata.StartDate}|{metadata.SaveName}|{NormalizePath(metadata.SavePath)}";
                
                Logger.Msg($"[SaveFileJsonDataStore] 🔧 Enhanced ID Components: '{combined}'");
                
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    string saveId = Convert.ToBase64String(hashedBytes).Replace('/', '_').Replace('+', '-').Substring(0, 12);
                    
                    Logger.Msg($"[SaveFileJsonDataStore] 🆔 Enhanced Save ID: '{saveId}'");
                    return saveId;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Enhanced save ID generation failed: {ex.Message}");
                return GenerateBasicSaveId(metadata.SavePath, metadata.SaveName);
            }
        }

        /// <summary>
        /// Fallback to basic save ID generation (original method)
        /// </summary>
        private static string GenerateBasicSaveId(string? savePath, string? saveName)
        {
            if (savePath == null || saveName == null)
            {
                Logger.Error("[SaveFileJsonDataStore] ❌ Failed to generate basic save ID: savePath or saveName is null");
                return "unknown";
            }

            string normalizedPath = NormalizePath(savePath);
            string normalizedName = saveName?.Trim() ?? "";
            string combined = $"{normalizedPath}|{normalizedName}";
            
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hashedBytes).Replace('/', '_').Replace('+', '-').Substring(0, 12);
            }
        }

        /// <summary>
        /// Save metadata file for the current save
        /// </summary>
        private static void SaveMetadataFile()
        {
            try
            {
                if (_currentSaveMetadata == null || string.IsNullOrEmpty(_currentSteamId) || string.IsNullOrEmpty(_currentSaveId))
                    return;

                var metadataFile = GetMetadataFilePath();
                
                // Ensure directory exists
                string? metadataDir = Path.GetDirectoryName(metadataFile);
                if (!string.IsNullOrEmpty(metadataDir))
                {
                    Directory.CreateDirectory(metadataDir);
                }
                
                string json = JsonConvert.SerializeObject(_currentSaveMetadata, Formatting.Indented);
                File.WriteAllText(metadataFile, json);
                
                Logger.Msg($"[SaveFileJsonDataStore] 📄 Metadata saved for Steam user {_currentSteamId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to save metadata file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the enhanced file paths for the current save (includes Steam ID folder)
        /// </summary>
        private static (string dropsFile, string ordersFile) GetCurrentSaveFiles()
        {
            if (string.IsNullOrEmpty(_currentSteamId) || string.IsNullOrEmpty(_currentSaveId))
                throw new InvalidOperationException("No current save loaded or missing Steam ID");
                
            string steamUserDir = Path.Combine(BaseDataDir, _currentSteamId);
            string saveDir = Path.Combine(steamUserDir, _currentSaveId);
            
            return (
                Path.Combine(saveDir, "drops.json"),
                Path.Combine(saveDir, "orders.json")
            );
        }

        /// <summary>
        /// Get metadata file path
        /// </summary>
        private static string GetMetadataFilePath()
        {
            if (string.IsNullOrEmpty(_currentSteamId) || string.IsNullOrEmpty(_currentSaveId))
                throw new InvalidOperationException("No current save loaded or missing Steam ID");
                
            string steamUserDir = Path.Combine(BaseDataDir, _currentSteamId);
            string saveDir = Path.Combine(steamUserDir, _currentSaveId);
            
            return Path.Combine(saveDir, "metadata.json");
        }

        /// <summary>
        /// Load data for the current save
        /// </summary>
        private static void LoadDataForCurrentSave()
        {
            try
            {
                var (dropsFile, ordersFile) = GetCurrentSaveFiles();
                
                // Load drops
                LoadDropsFromFile(dropsFile);
                
                // Load daily orders
                LoadDailyOrdersFromFile(ordersFile);
                
                Logger.Msg($"[SaveFileJsonDataStore] ✅ Loaded {PendingDrops.Values.Sum(list => list.Count)} drops and {MrsStacksOrdersToday.Count} order records");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to load data for current save");
                Logger.Exception(ex);
                ClearCurrentData(); // Ensure clean state on error
            }
        }

        /// <summary>
        /// Save data for the current save
        /// </summary>
        private static void SaveDataForCurrentSave()
        {
            try
            {
                var (dropsFile, ordersFile) = GetCurrentSaveFiles();
                
                // Ensure directory exists
                string? saveDir = Path.GetDirectoryName(dropsFile);
                if (!string.IsNullOrEmpty(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                
                // Save drops
                SaveDropsToFile(dropsFile);
                
                // Save daily orders
                SaveDailyOrdersToFile(ordersFile);
                
                Logger.Msg($"[SaveFileJsonDataStore] ✅ Saved {PendingDrops.Values.Sum(list => list.Count)} drops and {MrsStacksOrdersToday.Count} order records");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to save data for current save");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Load drops from file
        /// </summary>
        private static void LoadDropsFromFile(string dropsFile)
        {
            if (!File.Exists(dropsFile))
            {
                Logger.Msg($"[SaveFileJsonDataStore] 📄 No existing drops file found");
                return;
            }

            try
            {
                string json = File.ReadAllText(dropsFile);
                var loaded = JsonConvert.DeserializeObject<Dictionary<int, List<DropRecord>>>(json);
                
                if (loaded != null)
                {
                    PendingDrops.Clear();
                    foreach (var kvp in loaded)
                    {
                        PendingDrops[kvp.Key] = kvp.Value;
                    }
                    Logger.Msg($"[SaveFileJsonDataStore] 📂 Loaded {PendingDrops.Count} drop days from file");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to load drops from file: {ex.Message}");
            }
        }

        /// <summary>
        /// Save drops to file
        /// </summary>
        private static void SaveDropsToFile(string dropsFile)
        {
            try
            {
                string json = JsonConvert.SerializeObject(PendingDrops, Formatting.Indented);
                File.WriteAllText(dropsFile, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to save drops to file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load daily orders from file
        /// </summary>
        private static void LoadDailyOrdersFromFile(string ordersFile)
        {
            if (!File.Exists(ordersFile))
            {
                Logger.Msg($"[SaveFileJsonDataStore] 📄 No existing orders file found");
                return;
            }

            try
            {
                string json = File.ReadAllText(ordersFile);
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                
                if (data != null)
                {
                    // Load Mrs. Stacks orders
                    if (data.ContainsKey("MrsStacksOrdersToday") && data["MrsStacksOrdersToday"] is Newtonsoft.Json.Linq.JObject ordersObj)
                    {
                        MrsStacksOrdersToday.Clear();
                        foreach (var kvp in ordersObj)
                        {
                            if (int.TryParse(kvp.Key, out int day) && kvp.Value is Newtonsoft.Json.Linq.JValue value && value.Value is long count)
                            {
                                MrsStacksOrdersToday[day] = (int)count;
                            }
                        }
                    }
                    
                    // Load last order day
                    if (data.ContainsKey("LastMrsStacksOrderDay") && data["LastMrsStacksOrderDay"] is Newtonsoft.Json.Linq.JValue lastOrderValue && lastOrderValue.Value is long lastDay)
                    {
                        _lastMrsStacksOrderDay = (int)lastDay;
                    }
                    
                    Logger.Msg($"[SaveFileJsonDataStore] 📂 Loaded {MrsStacksOrdersToday.Count} order records, last order day: {_lastMrsStacksOrderDay}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to load orders from file: {ex.Message}");
            }
        }

        /// <summary>
        /// Save daily orders to file
        /// </summary>
        private static void SaveDailyOrdersToFile(string ordersFile)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    ["MrsStacksOrdersToday"] = MrsStacksOrdersToday,
                    ["LastMrsStacksOrderDay"] = _lastMrsStacksOrderDay
                };
                
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(ordersFile, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to save orders to file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clear all current data
        /// </summary>
        private static void ClearCurrentData()
        {
            PendingDrops.Clear();
            MrsStacksOrdersToday.Clear();
            _lastMrsStacksOrderDay = -1;
        }

        // Public API methods that delegate to current save data
        public static void SaveDrop(int day, List<string> items, int hour, string meta = "manual")
        {
            if (!_isLoadedForSave)
            {
                Logger.Warn("[SaveFileJsonDataStore] ⚠️ No save loaded - cannot save drop");
                return;
            }

            var record = new DropRecord
            {
                Day = day,
                Items = items,
                DropHour = hour,
                DropTime = $"{hour:D2}:00",
                Org = "PaxDrops",
                CreatedTime = DateTime.Now.ToString("s"),
                Type = meta,
                Location = "",
                ExpiryTime = "",
                OrderDay = day,
                IsCollected = false,
                InitialItemCount = items.Count
            };

            if (!PendingDrops.ContainsKey(day))
            {
                PendingDrops[day] = new List<DropRecord>();
            }
            PendingDrops[day].Add(record);

            Logger.Msg($"[SaveFileJsonDataStore] 💾 Drop queued for Day {day} with {items.Count} items (will save on next game save)");
        }

        public static void SaveDropRecord(DropRecord record)
        {
            if (!_isLoadedForSave)
            {
                Logger.Warn("[SaveFileJsonDataStore] ⚠️ No save loaded - cannot save drop record");
                return;
            }

            if (!PendingDrops.ContainsKey(record.Day))
            {
                PendingDrops[record.Day] = new List<DropRecord>();
            }
            PendingDrops[record.Day].Add(record);
            Logger.Msg($"[SaveFileJsonDataStore] 💾 Drop record queued for Day {record.Day} (will save on next game save)");
        }

        public static void RemoveDrop(int day)
        {
            if (!_isLoadedForSave) return;

            if (PendingDrops.ContainsKey(day))
            {
                PendingDrops[day].Clear();
                Logger.Msg($"[SaveFileJsonDataStore] 🗑️ Removed all drops for Day {day}");
            }
        }

        public static List<DropRecord> GetAllDrops()
        {
            if (!_isLoadedForSave) return new List<DropRecord>();

            var drops = new List<DropRecord>();
            foreach (var kvp in PendingDrops)
            {
                drops.AddRange(kvp.Value);
            }
            return drops;
        }

        public static void MarkSpecificDropCollected(int day, string location)
        {
            if (!_isLoadedForSave) return;

            if (PendingDrops.ContainsKey(day))
            {
                foreach (var drop in PendingDrops[day])
                {
                    if (drop.Location == location && !drop.IsCollected)
                    {
                        drop.IsCollected = true;
                        Logger.Msg($"[SaveFileJsonDataStore] ✅ Drop at {location} on day {day} marked as collected");
                        return;
                    }
                }
            }
        }

        public static void MarkDropCollected(int day)
        {
            if (!_isLoadedForSave) return;

            if (PendingDrops.ContainsKey(day))
            {
                foreach (var drop in PendingDrops[day])
                {
                    drop.IsCollected = true;
                }
                Logger.Msg($"[SaveFileJsonDataStore] ✅ All drops for day {day} marked as collected");
            }
        }

        public static void RemoveSpecificDrop(int day, string location)
        {
            if (!_isLoadedForSave) return;

            if (PendingDrops.ContainsKey(day))
            {
                PendingDrops[day].RemoveAll(drop => drop.Location == location);
                Logger.Msg($"[SaveFileJsonDataStore] 🗑️ Removed drop at {location} for Day {day}");
            }
        }

        // Mrs. Stacks order tracking methods
        public static bool HasMrsStacksOrderToday(int day)
        {
            if (!_isLoadedForSave) return false;
            return MrsStacksOrdersToday.ContainsKey(day) && MrsStacksOrdersToday[day] > 0;
        }

        public static int GetMrsStacksOrdersToday(int day)
        {
            if (!_isLoadedForSave) return 0;
            return MrsStacksOrdersToday.GetValueOrDefault(day, 0);
        }

        public static void MarkMrsStacksOrderToday(int day)
        {
            if (!_isLoadedForSave) return;

            if (!MrsStacksOrdersToday.ContainsKey(day))
            {
                MrsStacksOrdersToday[day] = 0;
            }
            MrsStacksOrdersToday[day]++;
            _lastMrsStacksOrderDay = day;
            Logger.Msg($"[SaveFileJsonDataStore] 📋 Mrs. Stacks order marked for day {day} (total: {MrsStacksOrdersToday[day]})");
        }

        public static void ResetMrsStacksOrdersToday(int day)
        {
            if (!_isLoadedForSave) return;

            if (MrsStacksOrdersToday.ContainsKey(day))
            {
                MrsStacksOrdersToday[day] = 0;
                Logger.Msg($"[SaveFileJsonDataStore] 🔄 Reset Mrs. Stacks orders for day {day}");
            }
        }

        public static Dictionary<int, int> GetMrsStacksOrderSummary()
        {
            if (!_isLoadedForSave) return new Dictionary<int, int>();
            return new Dictionary<int, int>(MrsStacksOrdersToday);
        }

        public static int GetLastMrsStacksOrderDay()
        {
            return _lastMrsStacksOrderDay;
        }

        public static int GetDaysSinceLastMrsStacksOrder(int currentDay)
        {
            if (_lastMrsStacksOrderDay == -1) return -1;
            return currentDay - _lastMrsStacksOrderDay;
        }

        /// <summary>
        /// Get current save information including Steam ID
        /// </summary>
        public static (string? saveId, string? saveName, string? steamId, bool isLoaded) GetCurrentSaveInfo()
        {
            return (_currentSaveId, _currentSaveName, _currentSteamId, _isLoadedForSave);
        }

        /// <summary>
        /// Get current save metadata
        /// </summary>
        public static SaveMetadata? GetCurrentSaveMetadata()
        {
            return _currentSaveMetadata;
        }

        /// <summary>
        /// Shutdown the save file data store
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                UnloadCurrentSave();
                Logger.Msg("[SaveFileJsonDataStore] 🔌 Shutdown complete");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Shutdown error: {ex.Message}");
            }
        }

        /// <summary>
        /// Debug method to show all existing save directories organized by Steam ID
        /// </summary>
        public static void DebugShowAllSaveDirectories()
        {
            try
            {
                if (!Directory.Exists(BaseDataDir))
                {
                    Logger.Msg("[SaveFileJsonDataStore] 🔍 No SaveFiles directory exists yet");
                    return;
                }

                var steamUserDirs = Directory.GetDirectories(BaseDataDir);
                Logger.Msg($"[SaveFileJsonDataStore] 🔍 Found {steamUserDirs.Length} Steam user directories:");
                
                foreach (var steamUserDir in steamUserDirs)
                {
                    string steamId = Path.GetFileName(steamUserDir);
                    var saveDirectories = Directory.GetDirectories(steamUserDir);
                    
                    Logger.Msg($"[SaveFileJsonDataStore] 👤 Steam User: {steamId} ({saveDirectories.Length} saves)");
                    
                    foreach (var saveDir in saveDirectories)
                    {
                        string saveId = Path.GetFileName(saveDir);
                        var files = Directory.GetFiles(saveDir, "*.json");
                        
                        Logger.Msg($"[SaveFileJsonDataStore]   📁 Save ID: {saveId} ({files.Length} files)");
                        
                        // Try to load metadata if it exists
                        string metadataFile = Path.Combine(saveDir, "metadata.json");
                        if (File.Exists(metadataFile))
                        {
                            try
                            {
                                string json = File.ReadAllText(metadataFile);
                                var metadata = JsonConvert.DeserializeObject<SaveMetadata>(json);
                                if (metadata != null)
                                {
                                    Logger.Msg($"[SaveFileJsonDataStore]     📋 Name: {metadata.SaveName}");
                                    Logger.Msg($"[SaveFileJsonDataStore]     🏢 Org: {metadata.OrganizationName}");
                                    Logger.Msg($"[SaveFileJsonDataStore]     📅 Start: {metadata.StartDate}");
                                    Logger.Msg($"[SaveFileJsonDataStore]     🕐 Last: {metadata.LastAccessed}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warn($"[SaveFileJsonDataStore]     ⚠️ Failed to read metadata: {ex.Message}");
                            }
                        }
                        
                        foreach (var file in files)
                        {
                            string fileName = Path.GetFileName(file);
                            var fileInfo = new FileInfo(file);
                            Logger.Msg($"[SaveFileJsonDataStore]     📄 {fileName} ({fileInfo.Length} bytes, {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Debug show directories failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Debug method to show current cached metadata
        /// </summary>
        public static void DebugShowCurrentCachedMetadata()
        {
            try
            {
                Logger.Msg($"[SaveFileJsonDataStore] 🔍 Current Cached Metadata:");
                Logger.Msg($"[SaveFileJsonDataStore]   Is Loaded: {_isLoadedForSave}");
                Logger.Msg($"[SaveFileJsonDataStore]   Save ID: '{_currentSaveId ?? "null"}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Steam ID: '{_currentSteamId ?? "null"}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Save Path: '{_currentSavePath ?? "null"}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Save Name: '{_currentSaveName ?? "null"}'");
                
                if (_currentSaveMetadata != null)
                {
                    Logger.Msg($"[SaveFileJsonDataStore]   Metadata Object:");
                    Logger.Msg($"[SaveFileJsonDataStore]     Save ID: '{_currentSaveMetadata.SaveId}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Steam ID: '{_currentSaveMetadata.SteamId}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Organization: '{_currentSaveMetadata.OrganizationName}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Start Date: '{_currentSaveMetadata.StartDate}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Save Name: '{_currentSaveMetadata.SaveName}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Save Path: '{_currentSaveMetadata.SavePath}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Creation: '{_currentSaveMetadata.CreationTimestamp}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Last Access: '{_currentSaveMetadata.LastAccessed}'");
                    Logger.Msg($"[SaveFileJsonDataStore]     Expected Folder: SaveFiles/{_currentSaveMetadata.SteamId}/{_currentSaveMetadata.SaveId}/");
                }
                else
                {
                    Logger.Msg($"[SaveFileJsonDataStore]   Metadata Object: null");
                }
                
                // Also show what the current game would generate
                try
                {
                    var saveManager = Il2CppScheduleOne.Persistence.SaveManager.Instance;
                    if (saveManager != null)
                    {
                        string? currentSavePath = saveManager.PlayersSavePath;
                        string? currentSaveName = saveManager.SaveName;
                        
                        Logger.Msg($"[SaveFileJsonDataStore] 🎮 Current Game Save Info:");
                        Logger.Msg($"[SaveFileJsonDataStore]   Game Save Path: '{currentSavePath ?? "null"}'");
                        Logger.Msg($"[SaveFileJsonDataStore]   Game Save Name: '{currentSaveName ?? "null"}'");
                        
                        if (!string.IsNullOrEmpty(currentSavePath) && !string.IsNullOrEmpty(currentSaveName))
                        {
                            var freshMetadata = GenerateSaveMetadata(currentSavePath, currentSaveName);
                            Logger.Msg($"[SaveFileJsonDataStore] 🔄 Fresh Metadata Generation (for comparison):");
                            Logger.Msg($"[SaveFileJsonDataStore]     Fresh Save ID: '{freshMetadata.SaveId}'");
                            Logger.Msg($"[SaveFileJsonDataStore]     Fresh Steam ID: '{freshMetadata.SteamId}'");
                            Logger.Msg($"[SaveFileJsonDataStore]     Fresh Organization: '{freshMetadata.OrganizationName}'");
                            Logger.Msg($"[SaveFileJsonDataStore]     Fresh Start Date: '{freshMetadata.StartDate}'");
                            
                            bool idsMatch = _currentSaveId == freshMetadata.SaveId;
                            Logger.Msg($"[SaveFileJsonDataStore]   🔍 ID Consistency: {(idsMatch ? "✅ CONSISTENT" : "❌ INCONSISTENT")}");
                            
                            if (!idsMatch)
                            {
                                Logger.Warn($"[SaveFileJsonDataStore] 🚨 Save ID mismatch detected!");
                                Logger.Warn($"[SaveFileJsonDataStore] 🚨 Cached: '{_currentSaveId}' vs Fresh: '{freshMetadata.SaveId}'");
                            }
                        }
                    }
                    else
                    {
                        Logger.Msg($"[SaveFileJsonDataStore] 🎮 SaveManager not available");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[SaveFileJsonDataStore] ⚠️ Could not get current game save info: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Debug metadata display failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Normalize save path to ensure consistent save IDs
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
                
                // IMPORTANT: Extract the base saves directory to handle different path formats
                // Examples:
                // "C:/users/crossover/AppData/LocalLow/TVGS/Schedule I/Saves" 
                // "C:/users/crossover/AppData/LocalLow/TVGS/Schedule I/Saves/76561198832878173/SaveGame_2"
                // Both should resolve to the same base save directory
                
                if (normalized.Contains("/saves/") && normalized.Contains("/schedule i/"))
                {
                    // Extract everything up to and including "/saves" for consistency
                    int savesIndex = normalized.IndexOf("/saves");
                    if (savesIndex != -1)
                    {
                        normalized = normalized.Substring(0, savesIndex + "/saves".Length);
                        Logger.Msg($"[SaveFileJsonDataStore] 🔧 Normalized to base saves directory: '{normalized}'");
                    }
                }
                
                return normalized;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SaveFileJsonDataStore] ⚠️ Path normalization failed for '{path}': {ex.Message}");
                return path?.ToLowerInvariant() ?? "";
            }
        }

        /// <summary>
        /// Debug method to test save ID generation with different path formats - now enhanced
        /// </summary>
        public static void DebugTestSaveIdGeneration(string? testPath, string? testName)
        {
            try
            {
                if (testPath == null || testName == null)
                {
                    Logger.Error("[SaveFileJsonDataStore] ❌ Test save ID generation failed: testPath or testName is null");
                    return;
                }

                Logger.Msg($"[SaveFileJsonDataStore] 🧪 Testing Enhanced Save ID generation:");
                Logger.Msg($"[SaveFileJsonDataStore]   Test Input - Path: '{testPath}', Name: '{testName}'");
                
                // Test enhanced metadata generation
                var testMetadata = GenerateSaveMetadata(testPath, testName);
                
                Logger.Msg($"[SaveFileJsonDataStore] 🔄 Enhanced Metadata Results:");
                Logger.Msg($"[SaveFileJsonDataStore]   Steam ID: '{testMetadata.SteamId}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Organization: '{testMetadata.OrganizationName}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Start Date: '{testMetadata.StartDate}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Save ID: '{testMetadata.SaveId}'");
                Logger.Msg($"[SaveFileJsonDataStore]   Folder Structure: SaveFiles/{testMetadata.SteamId}/{testMetadata.SaveId}/");
                
                // Test with different path variations
                string[] pathVariations = {
                    testPath,
                    testPath.Replace('\\', '/'),
                    testPath.Replace('/', '\\'),
                    testPath.TrimEnd('/', '\\'),
                    testPath.ToLowerInvariant(),
                    testPath.ToUpperInvariant()
                };
                
                Logger.Msg($"[SaveFileJsonDataStore] 🔄 Testing path variations:");
                foreach (var variation in pathVariations)
                {
                    if (variation != null)
                    {
                        var variationMetadata = GenerateSaveMetadata(variation, testName);
                        bool matches = variationMetadata.SaveId == testMetadata.SaveId;
                        Logger.Msg($"[SaveFileJsonDataStore]   '{variation}' → {variationMetadata.SaveId} {(matches ? "✅ MATCH" : "❌ DIFFERENT")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Debug test generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyze all save directories to identify potential duplicates - enhanced for Steam ID structure
        /// </summary>
        public static void AnalyzeSaves()
        {
            try
            {
                if (!Directory.Exists(BaseDataDir))
                {
                    Logger.Msg("[SaveFileJsonDataStore] 🔍 No SaveFiles directory exists");
                    return;
                }

                var steamUserDirs = Directory.GetDirectories(BaseDataDir);
                int totalSaves = 0;
                
                Logger.Msg($"[SaveFileJsonDataStore] 🔍 Analyzing {steamUserDirs.Length} Steam user directories:");
                
                foreach (var steamUserDir in steamUserDirs)
                {
                    string steamId = Path.GetFileName(steamUserDir);
                    var saveDirectories = Directory.GetDirectories(steamUserDir);
                    totalSaves += saveDirectories.Length;
                    
                    Logger.Msg($"[SaveFileJsonDataStore] 👤 Steam User: {steamId} ({saveDirectories.Length} saves)");
                    
                    var saveInfos = new List<SaveDirectoryInfo>();
                    
                    foreach (var saveDir in saveDirectories)
                    {
                        var info = AnalyzeSaveDirectory(saveDir);
                        saveInfos.Add(info);
                        
                        Logger.Msg($"[SaveFileJsonDataStore]   📁 {info.SaveId}:");
                        Logger.Msg($"[SaveFileJsonDataStore]     📄 Files: {info.FileCount} | Data entries: {info.DataEntries}");
                        Logger.Msg($"[SaveFileJsonDataStore]     🕐 Last modified: {info.LastModified:yyyy-MM-dd HH:mm:ss}");
                        if (info.FileCount > 0)
                        {
                            Logger.Msg($"[SaveFileJsonDataStore]     📋 Size: {info.TotalSize} bytes");
                        }
                    }
                    
                    // Look for potential duplicates within this Steam user's saves
                    IdentifyPotentialDuplicatesForUser(steamId, saveInfos);
                }
                
                Logger.Msg($"[SaveFileJsonDataStore] 📊 Total analysis: {steamUserDirs.Length} users, {totalSaves} saves");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Analyze saves failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Identify potential duplicates for a specific Steam user
        /// </summary>
        private static void IdentifyPotentialDuplicatesForUser(string steamId, List<SaveDirectoryInfo> saveInfos)
        {
            var duplicateGroups = IdentifyPotentialDuplicates(saveInfos);
            
            if (duplicateGroups.Count > 0)
            {
                Logger.Msg($"[SaveFileJsonDataStore] 🔍 Found {duplicateGroups.Count} potential duplicate groups for Steam user {steamId}:");
                foreach (var group in duplicateGroups)
                {
                    Logger.Msg($"[SaveFileJsonDataStore]   📁 Duplicate group ({group.Count} saves):");
                    foreach (var dup in group)
                    {
                        Logger.Msg($"[SaveFileJsonDataStore]     {dup.SaveId} ({dup.FileCount} files, {dup.TotalSize} bytes, {dup.LastModified:HH:mm:ss})");
                    }
                }
            }
        }

        /// <summary>
        /// Clean up duplicate save directories by merging them - enhanced for Steam ID structure
        /// </summary>
        public static void CleanupDuplicateSaves()
        {
            try
            {
                if (!Directory.Exists(BaseDataDir))
                {
                    Logger.Msg("[SaveFileJsonDataStore] 🧹 No SaveFiles directory to clean");
                    return;
                }

                var steamUserDirs = Directory.GetDirectories(BaseDataDir);
                int totalCleaned = 0;
                
                Logger.Msg($"[SaveFileJsonDataStore] 🧹 Analyzing {steamUserDirs.Length} Steam user directories for cleanup");
                
                foreach (var steamUserDir in steamUserDirs)
                {
                    string steamId = Path.GetFileName(steamUserDir);
                    var saveDirectories = Directory.GetDirectories(steamUserDir);
                    
                    Logger.Msg($"[SaveFileJsonDataStore] 🧹 Cleaning user {steamId} ({saveDirectories.Length} saves)");
                    
                    var saveInfos = new List<SaveDirectoryInfo>();
                    foreach (var dir in saveDirectories)
                    {
                        saveInfos.Add(AnalyzeSaveDirectory(dir));
                    }
                    
                    var duplicateGroups = IdentifyPotentialDuplicates(saveInfos);
                    
                    foreach (var group in duplicateGroups)
                    {
                        if (group.Count > 1)
                        {
                            int cleaned = MergeDuplicateDirectories(group);
                            totalCleaned += cleaned;
                        }
                    }
                }
                
                Logger.Msg($"[SaveFileJsonDataStore] ✅ Cleanup completed - removed {totalCleaned} duplicate directories");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Cleanup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Merge duplicate directories by keeping the most recent one - returns count of removed directories
        /// </summary>
        private static int MergeDuplicateDirectories(List<SaveDirectoryInfo> duplicates)
        {
            try
            {
                // Sort by last modified time (most recent first)
                duplicates.Sort((a, b) => b.LastModified.CompareTo(a.LastModified));
                
                var keepDirectory = duplicates[0];
                var removeDirectories = duplicates.Skip(1).ToList();
                
                Logger.Msg($"[SaveFileJsonDataStore] 🔄 Merging duplicates into: {keepDirectory.SaveId}");
                
                int removedCount = 0;
                foreach (var removeDir in removeDirectories)
                {
                    Logger.Msg($"[SaveFileJsonDataStore] 🗑️ Removing duplicate: {removeDir.SaveId}");
                    
                    try
                    {
                        Directory.Delete(removeDir.DirectoryPath, true);
                        Logger.Msg($"[SaveFileJsonDataStore] ✅ Deleted: {removeDir.SaveId}");
                        removedCount++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"[SaveFileJsonDataStore] ❌ Failed to delete {removeDir.SaveId}: {ex.Message}");
                    }
                }
                
                return removedCount;
            }
            catch (Exception ex)
            {
                Logger.Error($"[SaveFileJsonDataStore] ❌ Merge failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Information about a save directory
        /// </summary>
        private class SaveDirectoryInfo
        {
            public string SaveId { get; set; } = "";
            public string DirectoryPath { get; set; } = "";
            public int FileCount { get; set; }
            public long TotalSize { get; set; }
            public DateTime LastModified { get; set; }
            public int DataEntries { get; set; }
            public Dictionary<string, long> FileSizes { get; set; } = new Dictionary<string, long>();
        }

        /// <summary>
        /// Analyze a single save directory
        /// </summary>
        private static SaveDirectoryInfo AnalyzeSaveDirectory(string directoryPath)
        {
            var info = new SaveDirectoryInfo
            {
                SaveId = Path.GetFileName(directoryPath),
                DirectoryPath = directoryPath
            };
            
            try
            {
                var files = Directory.GetFiles(directoryPath, "*.json");
                info.FileCount = files.Length;
                info.LastModified = Directory.GetLastWriteTime(directoryPath);
                
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    info.TotalSize += fileInfo.Length;
                    info.FileSizes[Path.GetFileName(file)] = fileInfo.Length;
                    
                    if (fileInfo.LastWriteTime > info.LastModified)
                    {
                        info.LastModified = fileInfo.LastWriteTime;
                    }
                    
                    // Count data entries
                    if (Path.GetFileName(file) == "drops.json")
                    {
                        try
                        {
                            var content = File.ReadAllText(file);
                            var data = JsonConvert.DeserializeObject<Dictionary<int, List<DropRecord>>>(content);
                            info.DataEntries += data?.Values.Sum(list => list.Count) ?? 0;
                        }
                        catch { /* Ignore JSON parsing errors */ }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SaveFileJsonDataStore] ⚠️ Failed to analyze directory {info.SaveId}: {ex.Message}");
            }
            
            return info;
        }

        /// <summary>
        /// Identify potential duplicate save directories
        /// </summary>
        private static List<List<SaveDirectoryInfo>> IdentifyPotentialDuplicates(List<SaveDirectoryInfo> saveInfos)
        {
            var duplicateGroups = new List<List<SaveDirectoryInfo>>();
            var processed = new HashSet<string>();
            
            foreach (var save in saveInfos)
            {
                if (processed.Contains(save.SaveId)) continue;
                
                var potentialDuplicates = new List<SaveDirectoryInfo> { save };
                
                // Look for saves with similar characteristics
                foreach (var other in saveInfos)
                {
                    if (other.SaveId == save.SaveId || processed.Contains(other.SaveId)) continue;
                    
                    // Check if they might be duplicates
                    if (ArePotentialDuplicates(save, other))
                    {
                        potentialDuplicates.Add(other);
                    }
                }
                
                if (potentialDuplicates.Count > 1)
                {
                    duplicateGroups.Add(potentialDuplicates);
                    Logger.Msg($"[SaveFileJsonDataStore] 🔍 Found potential duplicate group:");
                    foreach (var dup in potentialDuplicates)
                    {
                        Logger.Msg($"[SaveFileJsonDataStore]   📁 {dup.SaveId} ({dup.FileCount} files, {dup.TotalSize} bytes, {dup.LastModified:HH:mm:ss})");
                    }
                }
                
                foreach (var dup in potentialDuplicates)
                {
                    processed.Add(dup.SaveId);
                }
            }
            
            return duplicateGroups;
        }

        /// <summary>
        /// Check if two save directories are potential duplicates
        /// </summary>
        private static bool ArePotentialDuplicates(SaveDirectoryInfo save1, SaveDirectoryInfo save2)
        {
            // Same file count and total size
            if (save1.FileCount != save2.FileCount || save1.TotalSize != save2.TotalSize)
                return false;
            
            // Similar modification times (within 10 seconds)
            var timeDiff = Math.Abs((save1.LastModified - save2.LastModified).TotalSeconds);
            if (timeDiff > 10)
                return false;
            
            // Same file sizes
            foreach (var file in save1.FileSizes)
            {
                if (!save2.FileSizes.ContainsKey(file.Key) || save2.FileSizes[file.Key] != file.Value)
                    return false;
            }
            
            return true;
        }
    }
} 