using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.GameTime;
using System.Linq;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Handles messaging functionality for Mrs. Stacks NPC.
    /// Now includes save-file-aware conversation persistence using SaveFileJsonDataStore system.
    /// </summary>
    public static class MrsStacksMessaging
    {
        /// <summary>
        /// Conversation message record for JSON storage
        /// </summary>
        [Serializable]
        public class MessageRecord
        {
            public string Text { get; set; } = "";
            public string Timestamp { get; set; } = "";
            public int GameDay { get; set; }
            public int GameTime { get; set; }
            public bool IsFromPlayer { get; set; }
            public string MessageId { get; set; } = "";
        }

        /// <summary>
        /// Conversation history container
        /// </summary>
        [Serializable]
        public class ConversationHistory
        {
            public List<MessageRecord> Messages { get; set; } = new List<MessageRecord>();
            public string ContactName { get; set; } = "Mrs. Stacks";
            public string LastSaved { get; set; } = "";
            public string SaveId { get; set; } = "";
        }

        // Per-save conversation history (loaded/unloaded with save data)
        private static ConversationHistory _conversationHistory = new ConversationHistory();
        private static bool _isConversationLoaded = false;

        /// <summary>
        /// Load conversation history for the current save file
        /// </summary>
        public static void LoadConversationForCurrentSave()
        {
            try
            {
                if (!_isConversationLoaded)
                {
                    var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                    if (isLoaded && !string.IsNullOrEmpty(saveId))
                    {
                        LoadConversationHistory(saveId);
                        _isConversationLoaded = true;
                        Logger.Msg($"[MrsStacksMessaging] 📂 Loaded conversation for save: {saveName} (ID: {saveId}, Steam: {steamId})");
                    }
                    else
                    {
                        // Fallback to default conversation if no save loaded
                        _conversationHistory = new ConversationHistory();
                        Logger.Warn("[MrsStacksMessaging] ⚠️ No save loaded - using default conversation");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to load conversation for current save: {ex.Message}");
                _conversationHistory = new ConversationHistory();
            }
        }

        /// <summary>
        /// Unload conversation history when exiting save
        /// </summary>
        public static void UnloadConversationForCurrentSave()
        {
            try
            {
                if (_isConversationLoaded)
                {
                    var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                    Logger.Msg($"[MrsStacksMessaging] 📤 Unloading conversation for save: {saveName} (ID: {saveId}, Steam: {steamId})");
                    
                    _conversationHistory = new ConversationHistory();
                    _isConversationLoaded = false;
                    
                    Logger.Msg("[MrsStacksMessaging] ✅ Conversation unloaded");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to unload conversation: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the conversation file path for a specific save ID
        /// </summary>
        private static string GetConversationFilePath(string saveId)
        {
            try
            {
                var (currentSaveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                if (!isLoaded || string.IsNullOrEmpty(steamId) || string.IsNullOrEmpty(currentSaveId))
                {
                    // Fallback to legacy location if save system not available
                    string legacyBaseDir = "Mods/PaxDrops/SaveFiles";
                    return Path.Combine(legacyBaseDir, "unknown", saveId, "conversation.json");
                }

                // Use enhanced save structure: SaveFiles/SteamID/SaveID/conversation.json
                // IMPORTANT: Use currentSaveId (not the parameter saveId) because we want to use the 
                // save ID that's currently active in the SaveFileJsonDataStore system
                string baseDataDir = "Mods/PaxDrops/SaveFiles";
                string steamUserDir = Path.Combine(baseDataDir, steamId);
                string saveDir = Path.Combine(steamUserDir, currentSaveId);  // Use currentSaveId for consistency
                
                Logger.Msg($"[MrsStacksMessaging] 📁 Conversation file path: {Path.Combine(saveDir, "conversation.json")}");
                Logger.Msg($"[MrsStacksMessaging] 📁 Using Steam ID: {steamId}, Save ID: {currentSaveId}");
                
                return Path.Combine(saveDir, "conversation.json");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to get conversation file path: {ex.Message}");
                return Path.Combine("Mods/PaxDrops/SaveFiles", "fallback", saveId, "conversation.json");
            }
        }

        /// <summary>
        /// Find the Mrs. Stacks NPC using the proper GetNPC method
        /// </summary>
        public static NPC? FindMrsStacksNPC()
        {
            try
            {
                // First try to get Mrs. Stacks directly by ID
                var mrsStacks = NPCManager.GetNPC("mrs_stacks_001");
                if (mrsStacks != null)
                {
                    return mrsStacks;
                }
                
                Logger.Error("[MrsStacksMessaging] ❌ Mrs. Stacks not found");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ NPC search failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Send a message from Mrs. Stacks to the player and save to current save's conversation
        /// </summary>
        public static void SendMessage(NPC npc, string messageText)
        {
            try
            {
                // Ensure conversation is loaded for current save
                LoadConversationForCurrentSave();

                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation == null)
                {
                    Logger.Error("[MrsStacksMessaging] ❌ No conversation found");
                    return;
                }

                var message = new Message(messageText, Message.ESenderType.Other, true);
                conversation.SendMessage(message, true, true);
                
                // Save to our save-aware JSON storage
                SaveMessageToHistory(messageText, false);
                
                Logger.Msg($"[MrsStacksMessaging] 📱 Message sent and saved: {messageText}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Message send failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup conversation and send welcome message, then load conversation history for current save
        /// </summary>
        public static void SetupConversation(NPC npc)
        {
            try
            {
                Logger.Msg($"[MrsStacksMessaging] 🔧 Setting up conversation with Mrs. Stacks");
                
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation != null)
                {
                    conversation.contactName = "Mrs. Stacks";
                    
                    Logger.Msg($"[MrsStacksMessaging] 🔧 Conversation loaded: {_isConversationLoaded}");
                    Logger.Msg($"[MrsStacksMessaging] 🔧 Current message count before loading: {_conversationHistory.Messages.Count}");
                    
                    // Load existing conversation history for current save
                    LoadConversationForCurrentSave();
                    
                    Logger.Msg($"[MrsStacksMessaging] 🔧 Message count after loading: {_conversationHistory.Messages.Count}");
                    
                    // Restore previous messages to the conversation
                    RestoreConversationHistory(conversation);
                    
                    Logger.Msg($"[MrsStacksMessaging] 📱 ✅ Conversation setup complete - restored {_conversationHistory.Messages.Count} messages");
                    Logger.Msg($"[MrsStacksMessaging] 📱 Note: Welcome/reminder messages are handled by MrsStacksNPC.SendDelayedWelcomeMessage() and OnNewDay()");
                }
                else
                {
                    Logger.Error($"[MrsStacksMessaging] ❌ No conversation object found for Mrs. Stacks NPC");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Conversation setup failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Save a message to conversation history (queued for next game save)
        /// </summary>
        public static void SaveMessageToHistory(string messageText, bool isFromPlayer = false)
        {
            try
            {
                // Ensure conversation is loaded for current save
                if (!_isConversationLoaded)
                {
                    LoadConversationForCurrentSave();
                }

                var timeManager = TimeManager.Instance;
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                
                var messageRecord = new MessageRecord
                {
                    Text = messageText,
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    GameDay = timeManager?.ElapsedDays ?? 0,
                    GameTime = timeManager?.CurrentTime ?? 0,
                    IsFromPlayer = isFromPlayer,
                    MessageId = Guid.NewGuid().ToString("N")[..8] // Short ID
                };

                _conversationHistory.Messages.Add(messageRecord);
                _conversationHistory.LastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                _conversationHistory.SaveId = saveId ?? "unknown";

                // Don't save immediately - only save when game saves
                // SaveConversationHistory(); // REMOVED - only save when game saves
                Logger.Msg($"[MrsStacksMessaging] 📝 Message queued for save: {messageRecord.MessageId} (Save: {saveName}, Steam: {steamId}) - will save on next game save");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to save message to history: {ex.Message}");
            }
        }

        /// <summary>
        /// Load conversation history from JSON file for specific save ID
        /// </summary>
        private static void LoadConversationHistory(string saveId)
        {
            try
            {
                Logger.Msg($"[MrsStacksMessaging] 🔍 Loading conversation history for save ID: {saveId}");
                
                string conversationFile = GetConversationFilePath(saveId);
                
                Logger.Msg($"[MrsStacksMessaging] 📄 Looking for conversation file: {conversationFile}");
                Logger.Msg($"[MrsStacksMessaging] 📄 File exists: {File.Exists(conversationFile)}");
                
                if (File.Exists(conversationFile))
                {
                    string json = File.ReadAllText(conversationFile);
                    Logger.Msg($"[MrsStacksMessaging] 📄 File content length: {json.Length} characters");
                    
                    var loaded = JsonConvert.DeserializeObject<ConversationHistory>(json);
                    if (loaded != null)
                    {
                        _conversationHistory = loaded;
                        _conversationHistory.SaveId = saveId; // Ensure save ID is set
                        Logger.Msg($"[MrsStacksMessaging] 📂 ✅ Successfully loaded {_conversationHistory.Messages.Count} messages from save-aware JSON (Save ID: {saveId})");
                        
                        // Debug: show first few messages
                        if (_conversationHistory.Messages.Count > 0)
                        {
                            Logger.Msg($"[MrsStacksMessaging] 📝 First message: {_conversationHistory.Messages[0].Text.Substring(0, Math.Min(50, _conversationHistory.Messages[0].Text.Length))}...");
                        }
                        return;
                    }
                    else
                    {
                        Logger.Warn($"[MrsStacksMessaging] ⚠️ Failed to deserialize conversation JSON");
                    }
                }
                
                Logger.Msg($"[MrsStacksMessaging] 📂 No existing conversation history found for save ID: {saveId} - starting fresh (no file created until save)");
                _conversationHistory = new ConversationHistory { SaveId = saveId };
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to load conversation history for save ID {saveId}: {ex.Message}");
                Logger.Exception(ex);
                _conversationHistory = new ConversationHistory { SaveId = saveId };
            }
        }

        /// <summary>
        /// Save conversation history to JSON file for current save
        /// </summary>
        private static void SaveConversationHistory()
        {
            try
            {
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                if (!isLoaded || string.IsNullOrEmpty(saveId))
                {
                    Logger.Warn("[MrsStacksMessaging] ⚠️ No save loaded - cannot save conversation");
                    return;
                }

                string conversationFile = GetConversationFilePath(saveId);
                
                // Ensure directory exists only when we're actually saving
                string? saveDir = Path.GetDirectoryName(conversationFile);
                if (!string.IsNullOrEmpty(saveDir))
                {
                    Directory.CreateDirectory(saveDir);
                }
                
                string json = JsonConvert.SerializeObject(_conversationHistory, Formatting.Indented);
                File.WriteAllText(conversationFile, json);
                
                Logger.Msg($"[MrsStacksMessaging] 💾 Conversation history saved to save-aware location ({_conversationHistory.Messages.Count} messages, Save: {saveName}, Steam: {steamId})");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to save conversation history: {ex.Message}");
            }
        }

        /// <summary>
        /// Restore conversation history to the game conversation
        /// </summary>
        private static void RestoreConversationHistory(MSGConversation conversation)
        {
            try
            {
                if (_conversationHistory.Messages.Count == 0) return;

                // Sort messages by timestamp to maintain order
                _conversationHistory.Messages.Sort((a, b) => 
                    string.Compare(a.Timestamp, b.Timestamp, StringComparison.Ordinal));

                foreach (var messageRecord in _conversationHistory.Messages)
                {
                    try
                    {
                        var senderType = messageRecord.IsFromPlayer ? Message.ESenderType.Player : Message.ESenderType.Other;
                        var message = new Message(messageRecord.Text, senderType, true);
                        
                        // Add message without triggering notifications or network sync
                        conversation.SendMessage(message, false, false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"[MrsStacksMessaging] ⚠️ Failed to restore message {messageRecord.MessageId}: {ex.Message}");
                    }
                }

                Logger.Msg($"[MrsStacksMessaging] ✅ Restored {_conversationHistory.Messages.Count} messages to conversation for current save");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to restore conversation history: {ex.Message}");
            }
        }

        /// <summary>
        /// Show conversation history and persistence status (for debugging)
        /// </summary>
        public static void ShowConversationHistory()
        {
            try
            {
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                
                Logger.Msg($"[MrsStacksMessaging] 📊 Conversation History (Save-Aware JSON Storage):");
                Logger.Msg($"[MrsStacksMessaging] 💾 Current Save: {saveName} (ID: {saveId}, Steam: {steamId})");
                Logger.Msg($"[MrsStacksMessaging] 📝 Contact: {_conversationHistory.ContactName}");
                Logger.Msg($"[MrsStacksMessaging] 🔢 Messages: {_conversationHistory.Messages.Count}");
                Logger.Msg($"[MrsStacksMessaging] 💾 Last Saved: {_conversationHistory.LastSaved}");
                Logger.Msg($"[MrsStacksMessaging] 📄 File: {(isLoaded ? GetConversationFilePath(saveId ?? "") : "No save loaded")}");
                Logger.Msg($"[MrsStacksMessaging] 📁 Loaded: {_isConversationLoaded}");

                if (_conversationHistory.Messages.Count > 0)
                {
                    Logger.Msg($"[MrsStacksMessaging] 📜 Recent Messages:");
                    var recent = _conversationHistory.Messages.TakeLast(3);
                    foreach (var msg in recent)
                    {
                        string sender = msg.IsFromPlayer ? "Player" : "Mrs. Stacks";
                        Logger.Msg($"[MrsStacksMessaging]   [{msg.Timestamp}] {sender}: {msg.Text.Substring(0, Math.Min(50, msg.Text.Length))}...");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ History display failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Force save conversation history (for testing)
        /// </summary>
        public static void ForceSaveConversation()
        {
            try
            {
                SaveConversationHistory();
                Logger.Msg("[MrsStacksMessaging] ✅ Force save completed for current save");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Force save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear conversation history for current save
        /// </summary>
        public static void ClearConversationHistory()
        {
            try
            {
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                
                _conversationHistory.Messages.Clear();
                _conversationHistory.LastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                
                // Don't save immediately - only save when game saves
                // SaveConversationHistory(); // REMOVED - only save when game saves
                Logger.Msg($"[MrsStacksMessaging] 🗑️ Conversation history cleared for save: {saveName} (Steam: {steamId}) - will save on next game save");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to clear conversation history: {ex.Message}");
            }
        }

        /// <summary>
        /// Customize conversation options after creation with delay
        /// </summary>
        public static System.Collections.IEnumerator CustomizeConversationAfterDelay(Supplier supplier)
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            
            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(supplier);
                if (conversation != null)
                {
                    Logger.Msg("[MrsStacksMessaging] 🔧 Applying conversation customizations...");
                    
                    // Try to disable debt-related functionality
                    if (supplier.Debt > 0.01f)
                    {
                        try
                        {
                            supplier.ChangeDebt(-supplier.Debt);
                            Logger.Msg("[MrsStacksMessaging] 💰 Cleared Mrs. Stacks debt");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"[MrsStacksMessaging] ⚠️ Could not clear debt: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Conversation customization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if the current save has existing conversation history
        /// </summary>
        public static bool HasExistingConversation()
        {
            try
            {
                // Ensure conversation is loaded for current save
                if (!_isConversationLoaded)
                {
                    LoadConversationForCurrentSave();
                }
                
                Logger.Msg($"[MrsStacksMessaging] 🔍 HasExistingConversation check - Message count: {_conversationHistory.Messages.Count}");
                return _conversationHistory.Messages.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ HasExistingConversation check failed: {ex.Message}");
                return false;
            }
        }
    }
} 