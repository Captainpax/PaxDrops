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
                        Logger.Info($"📂 Loaded conversation for save: {saveName} (ID: {saveId}, Steam: {steamId})", "MrsStacksMessaging");
                    }
                    else
                    {
                        // Fallback to default conversation if no save loaded
                        _conversationHistory = new ConversationHistory();
                        Logger.Warn("⚠️ No save loaded - using default conversation", "MrsStacksMessaging");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to load conversation for current save: {ex.Message}", "MrsStacksMessaging");
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
                    Logger.Info($"📤 Unloading conversation for save: {saveName} (ID: {saveId}, Steam: {steamId})", "MrsStacksMessaging");
                    
                    _conversationHistory = new ConversationHistory();
                    _isConversationLoaded = false;
                    
                    Logger.Info("✅ Conversation unloaded", "MrsStacksMessaging");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to unload conversation: {ex.Message}", "MrsStacksMessaging");
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
                
                Logger.Debug($"📁 Conversation file path: {Path.Combine(saveDir, "conversation.json")}", "MrsStacksMessaging");
                Logger.Debug($"📁 Using Steam ID: {steamId}, Save ID: {currentSaveId}", "MrsStacksMessaging");
                
                return Path.Combine(saveDir, "conversation.json");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to get conversation file path: {ex.Message}", "MrsStacksMessaging");
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
                
                Logger.Error("❌ Mrs. Stacks not found", "MrsStacksMessaging");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ NPC search failed: {ex.Message}", "MrsStacksMessaging");
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
                    Logger.Error("❌ No conversation found", "MrsStacksMessaging");
                    return;
                }

                var message = new Message(messageText, Message.ESenderType.Other, true);
                conversation.SendMessage(message, true, true);
                
                // Save to our save-aware JSON storage
                SaveMessageToHistory(messageText, false);
                
                Logger.Info($"📱 Message sent and saved: {messageText}", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Message send failed: {ex.Message}", "MrsStacksMessaging");
            }
        }

        /// <summary>
        /// Setup conversation and send welcome message, then load conversation history for current save
        /// </summary>
        public static void SetupConversation(NPC npc)
        {
            try
            {
                Logger.Info("🔧 Setting up conversation with Mrs. Stacks", "MrsStacksMessaging");
                
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation != null)
                {
                    conversation.contactName = "Mrs. Stacks";
                    
                    Logger.Debug($"🔧 Conversation loaded: {_isConversationLoaded}", "MrsStacksMessaging");
                    Logger.Debug($"🔧 Current message count before loading: {_conversationHistory.Messages.Count}", "MrsStacksMessaging");
                    
                    // Load existing conversation history for current save
                    LoadConversationForCurrentSave();
                    
                    Logger.Debug($"🔧 Message count after loading: {_conversationHistory.Messages.Count}", "MrsStacksMessaging");
                    
                    // Restore previous messages to the conversation
                    RestoreConversationHistory(conversation);
                    
                    Logger.Info($"📱 ✅ Conversation setup complete - restored {_conversationHistory.Messages.Count} messages", "MrsStacksMessaging");
                    Logger.Info($"📱 Note: Welcome/reminder messages are handled by MrsStacksNPC.SendDelayedWelcomeMessage() and OnNewDay()", "MrsStacksMessaging");
                }
                else
                {
                    Logger.Error($"❌ No conversation object found for Mrs. Stacks NPC", "MrsStacksMessaging");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Conversation setup failed: {ex.Message}", "MrsStacksMessaging");
                Logger.Exception(ex, "MrsStacksMessaging");
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
                Logger.Debug($"📝 Message queued for save: {messageRecord.MessageId} (Save: {saveName}, Steam: {steamId}) - will save on next game save", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to save message to history: {ex.Message}", "MrsStacksMessaging");
            }
        }

        /// <summary>
        /// Load conversation history from JSON file for specific save ID
        /// </summary>
        private static void LoadConversationHistory(string saveId)
        {
            try
            {
                Logger.Debug($"🔍 Loading conversation history for save ID: {saveId}", "MrsStacksMessaging");
                
                string conversationFile = GetConversationFilePath(saveId);
                
                Logger.Debug($"📄 Looking for conversation file: {conversationFile}", "MrsStacksMessaging");
                Logger.Debug($"📄 File exists: {File.Exists(conversationFile)}", "MrsStacksMessaging");
                
                if (File.Exists(conversationFile))
                {
                    string json = File.ReadAllText(conversationFile);
                    Logger.Debug($"📄 File content length: {json.Length} characters", "MrsStacksMessaging");
                    
                    var loaded = JsonConvert.DeserializeObject<ConversationHistory>(json);
                    if (loaded != null)
                    {
                        _conversationHistory = loaded;
                        _conversationHistory.SaveId = saveId; // Ensure save ID is set
                        Logger.Info($"📂 ✅ Successfully loaded {_conversationHistory.Messages.Count} messages from save-aware JSON (Save ID: {saveId})", "MrsStacksMessaging");
                        
                        // Debug: show first few messages
                        if (_conversationHistory.Messages.Count > 0)
                        {
                            Logger.Debug($"📝 First message: {_conversationHistory.Messages[0].Text.Substring(0, Math.Min(50, _conversationHistory.Messages[0].Text.Length))}...", "MrsStacksMessaging");
                        }
                        return;
                    }
                    else
                    {
                        Logger.Warn($"⚠️ Failed to deserialize conversation JSON", "MrsStacksMessaging");
                    }
                }
                
                Logger.Debug($"📂 No existing conversation history found for save ID: {saveId} - starting fresh (no file created until save)", "MrsStacksMessaging");
                _conversationHistory = new ConversationHistory { SaveId = saveId };
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to load conversation history for save ID {saveId}: {ex.Message}", "MrsStacksMessaging");
                Logger.Exception(ex, "MrsStacksMessaging");
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
                
                Logger.Debug($"💾 Conversation history saved to save-aware location ({_conversationHistory.Messages.Count} messages, Save: {saveName}, Steam: {steamId})", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to save conversation history: {ex.Message}", "MrsStacksMessaging");
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

                Logger.Info($"✅ Restored {_conversationHistory.Messages.Count} messages to conversation for current save", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to restore conversation history: {ex.Message}", "MrsStacksMessaging");
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
                
                Logger.Debug($"📊 Conversation History (Save-Aware JSON Storage):", "MrsStacksMessaging");
                Logger.Debug($"💾 Current Save: {saveName} (ID: {saveId}, Steam: {steamId})", "MrsStacksMessaging");
                Logger.Debug($"📝 Contact: {_conversationHistory.ContactName}", "MrsStacksMessaging");
                Logger.Debug($"🔢 Messages: {_conversationHistory.Messages.Count}", "MrsStacksMessaging");
                Logger.Debug($"💾 Last Saved: {_conversationHistory.LastSaved}", "MrsStacksMessaging");
                Logger.Debug($"📄 File: {(isLoaded ? GetConversationFilePath(saveId ?? "") : "No save loaded")}", "MrsStacksMessaging");
                Logger.Debug($"📁 Loaded: {_isConversationLoaded}", "MrsStacksMessaging");

                if (_conversationHistory.Messages.Count > 0)
                {
                    Logger.Debug($"📜 Recent Messages:", "MrsStacksMessaging");
                    var recent = _conversationHistory.Messages.TakeLast(3);
                    foreach (var msg in recent)
                    {
                        string sender = msg.IsFromPlayer ? "Player" : "Mrs. Stacks";
                        Logger.Debug($"   [{msg.Timestamp}] {sender}: {msg.Text.Substring(0, Math.Min(50, msg.Text.Length))}...", "MrsStacksMessaging");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ History display failed: {ex.Message}", "MrsStacksMessaging");
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
                Logger.Info($"✅ Force save completed for current save", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Force save failed: {ex.Message}", "MrsStacksMessaging");
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
                Logger.Debug($"🗑️ Conversation history cleared for save: {saveName} (Steam: {steamId}) - will save on next game save", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to clear conversation history: {ex.Message}", "MrsStacksMessaging");
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
                    Logger.Debug("🔧 Applying conversation customizations...", "MrsStacksMessaging");
                    
                    // Try to disable debt-related functionality
                    if (supplier.Debt > 0.01f)
                    {
                        try
                        {
                            supplier.ChangeDebt(-supplier.Debt);
                            Logger.Debug("💰 Cleared Mrs. Stacks debt", "MrsStacksMessaging");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"⚠️ Could not clear debt: {ex.Message}", "MrsStacksMessaging");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Conversation customization failed: {ex.Message}", "MrsStacksMessaging");
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
                
                Logger.Debug($"🔍 HasExistingConversation check - Message count: {_conversationHistory.Messages.Count}", "MrsStacksMessaging");
                return _conversationHistory.Messages.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ HasExistingConversation check failed: {ex.Message}", "MrsStacksMessaging");
                return false;
            }
        }

        /// <summary>
        /// Shutdown the messaging system and force save any pending data
        /// </summary>
        public static void Shutdown()
        {
            try
            {
                Logger.Debug("🧼 Shutting down messaging system...", "MrsStacksMessaging");
                
                // Unload conversation data
                UnloadConversationForCurrentSave();
                
                // Reset state
                _conversationHistory = new ConversationHistory();
                _isConversationLoaded = false;
                
                Logger.Info("✅ Messaging system shutdown complete", "MrsStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Shutdown error: {ex.Message}", "MrsStacksMessaging");
                Logger.Exception(ex, "MrsStacksMessaging");
                
                // Force reset state even if shutdown failed
                _conversationHistory = new ConversationHistory();
                _isConversationLoaded = false;
            }
        }
    }
} 