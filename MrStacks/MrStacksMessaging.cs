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
using PaxDrops.Configs;
using ResponseList = Il2CppSystem.Collections.Generic.List<Il2CppScheduleOne.Messaging.Response>;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Handles messaging functionality for Mr. Stacks NPC.
    /// Conversation history is stored per-save through the SQLite-backed SaveFileJsonDataStore facade.
    /// </summary>
    public static class MrStacksMessaging
    {
        /// <summary>
        /// Conversation message record for persistence.
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
            public string ContactName { get; set; } = "Mr. Stacks";
            public string LastSaved { get; set; } = "";
            public string SaveId { get; set; } = "";
        }

        // Per-save conversation history (loaded/unloaded with save data)
        private static ConversationHistory _conversationHistory = new ConversationHistory();
        private static bool _isConversationLoaded = false;

        private static MSGConversation? GetConversation(NPC npc)
        {
            return MessagingManager.Instance?.GetConversation(npc);
        }

        private static Response CreateMenuResponse(string label, global::System.Action callback)
        {
            Il2CppSystem.Action il2CppCallback = callback;
            return new Response(label, label, il2CppCallback, true);
        }

        private static void ShowMenuResponses(MSGConversation conversation, ResponseList responses)
        {
            conversation.EnsureUIExists();
            conversation.ClearResponses(false);
            conversation.ShowResponses(responses, 0f, false);
        }

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
                        Logger.Info($"📂 Loaded conversation for save: {saveName} (ID: {saveId}, Steam: {steamId})", "MrStacksMessaging");
                    }
                    else
                    {
                        // Fallback to default conversation if no save loaded
                        _conversationHistory = new ConversationHistory();
                        Logger.Warn("⚠️ No save loaded - using default conversation", "MrStacksMessaging");
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to load conversation for current save: {ex.Message}", "MrStacksMessaging");
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
                    Logger.Info($"📤 Unloading conversation for save: {saveName} (ID: {saveId}, Steam: {steamId})", "MrStacksMessaging");
                    
                    _conversationHistory = new ConversationHistory();
                    _isConversationLoaded = false;
                    
                    Logger.Info("✅ Conversation unloaded", "MrStacksMessaging");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to unload conversation: {ex.Message}", "MrStacksMessaging");
            }
        }

        /// <summary>
        /// Get the SQLite storage path for a specific save ID.
        /// </summary>
        private static string GetConversationFilePath(string saveId)
        {
            try
            {
                string? databasePath = SaveFileJsonDataStore.GetCurrentDatabasePathForDebug();
                if (!string.IsNullOrEmpty(databasePath))
                {
                    Logger.Debug($"Conversation storage DB: {databasePath}", "MrStacksMessaging");
                    return databasePath;
                }

                return Path.Combine("Mods/PaxDrops/SaveFiles", "unknown", saveId, SaveFileSqliteBackend.GetDatabaseFileName());

                // Use enhanced save structure: SaveFiles/SteamID/SaveID/conversation.json
                // IMPORTANT: Use currentSaveId (not the parameter saveId) because we want to use the 
                // save ID that's currently active in the SaveFileJsonDataStore system
                string steamId = string.Empty;
                string currentSaveId = saveId;
                string saveDir = Path.GetDirectoryName(databasePath) ?? databasePath;

                Logger.Debug($"Conversation storage DB: {databasePath}", "MrStacksMessaging");
                return databasePath;
                
                Logger.Debug($"📁 Conversation file path: {Path.Combine(saveDir, "conversation.json")}", "MrStacksMessaging");
                Logger.Debug($"📁 Using Steam ID: {steamId}, Save ID: {currentSaveId}", "MrStacksMessaging");
                
                return Path.Combine(saveDir, "conversation.json");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to get conversation file path: {ex.Message}", "MrStacksMessaging");
                return Path.Combine("Mods/PaxDrops/SaveFiles", "fallback", saveId, SaveFileSqliteBackend.GetDatabaseFileName());
            }
        }

        /// <summary>
        /// Find the Mr. Stacks NPC using the proper GetNPC method
        /// </summary>
        public static NPC? FindMrStacksNPC()
        {
            try
            {
                // First try to get Mr. Stacks directly by ID
                var mrStacks = NPCManager.GetNPC("mr_stacks_001");
                if (mrStacks != null)
                {
                    return mrStacks;
                }
                
                Logger.Error("❌ Mr. Stacks not found", "MrStacksMessaging");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ NPC search failed: {ex.Message}", "MrStacksMessaging");
                return null;
            }
        }

        /// <summary>
        /// Send a message from Mr. Stacks to the player and save to current save's conversation
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
                    Logger.Error("❌ No conversation found", "MrStacksMessaging");
                    return;
                }

                var message = new Message(messageText, Message.ESenderType.Other, true);
                conversation.SendMessage(message, true, true);
                
                // Queue this message for the next save snapshot.
                SaveMessageToHistory(messageText, false);
                
                Logger.Info($"📱 Message sent and saved: {messageText}", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Message send failed: {ex.Message}", "MrStacksMessaging");
            }
        }

        /// <summary>
        /// Setup conversation and send welcome message, then load conversation history for current save
        /// </summary>
        public static void SetupConversation(NPC npc)
        {
            try
            {
                Logger.Info("🔧 Setting up conversation with Mr. Stacks", "MrStacksMessaging");
                
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation != null)
                {
                    conversation.contactName = "Mr. Stacks";
                    
                    Logger.Debug($"🔧 Conversation loaded: {_isConversationLoaded}", "MrStacksMessaging");
                    Logger.Debug($"🔧 Current message count before loading: {_conversationHistory.Messages.Count}", "MrStacksMessaging");
                    
                    // Load existing conversation history for current save
                    LoadConversationForCurrentSave();
                    
                    Logger.Debug($"🔧 Message count after loading: {_conversationHistory.Messages.Count}", "MrStacksMessaging");
                    
                    // Restore previous messages to the conversation
                    RestoreConversationHistory(conversation);
                    ShowHomeMenu(npc);
                    
                    Logger.Info($"📱 ✅ Conversation setup complete - restored {_conversationHistory.Messages.Count} messages", "MrStacksMessaging");
                    Logger.Info($"📱 Note: Welcome/reminder messages are handled by MrStacksNPC.SendDelayedWelcomeMessage() and OnNewDay()", "MrStacksMessaging");
                }
                else
                {
                    Logger.Error($"❌ No conversation object found for Mr. Stacks NPC", "MrStacksMessaging");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Conversation setup failed: {ex.Message}", "MrStacksMessaging");
                Logger.Exception(ex, "MrStacksMessaging");
            }
        }

        /// <summary>
        /// Show the root response menu with the three ordered-drop groups.
        /// </summary>
        public static void ShowHomeMenu(NPC npc)
        {
            try
            {
                var conversation = GetConversation(npc);
                if (conversation == null)
                {
                    Logger.Warn("No conversation available for Mr. Stacks home menu", "MrStacksMessaging");
                    return;
                }

                var responses = new ResponseList();
                foreach (var group in OrderedDropConfig.GroupOrder)
                {
                    var selectedGroup = group;
                    string groupName = OrderedDropConfig.GetGroupName(selectedGroup);
                    responses.Add(CreateMenuResponse(groupName, () => ShowGroupMenu(npc, selectedGroup)));
                }

                ShowMenuResponses(conversation, responses);
                Logger.Debug("Displayed Mr. Stacks home menu", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to show Mr. Stacks home menu: {ex.Message}", "MrStacksMessaging");
            }
        }

        private static void ShowGroupMenu(NPC npc, OrderedDropConfig.OrderedGroup group)
        {
            try
            {
                var conversation = GetConversation(npc);
                if (conversation == null)
                {
                    Logger.Warn("No conversation available for Mr. Stacks group menu", "MrStacksMessaging");
                    return;
                }

                var responses = new ResponseList();

                foreach (var tier in OrderedDropConfig.GetTiersForGroup(group))
                {
                    var selectedTier = tier;
                    string label = $"{OrderedDropConfig.GetTierName(selectedTier)} (${OrderedDropConfig.GetPrice(selectedTier)})";

                    responses.Add(CreateMenuResponse(label, () =>
                    {
                        int currentDay = DropConfig.GetCurrentGameDay();
                        var currentRank = DropConfig.GetCurrentPlayerRank();

                        if (!OrderedDropConfig.IsUnlocked(selectedTier, currentRank, currentDay))
                        {
                            SendMessage(npc, OrderedDropConfig.GetLockedReason(selectedTier, currentRank, currentDay));
                            ShowGroupMenu(npc, group);
                            return;
                        }

                        bool orderSucceeded = DailyDropOrdering.ProcessMrStacksOrder(selectedTier, true);
                        if (orderSucceeded)
                        {
                            ShowHomeMenu(npc);
                        }
                        else
                        {
                            ShowGroupMenu(npc, group);
                        }
                    }));
                }

                responses.Add(CreateMenuResponse("Home", () => ShowHomeMenu(npc)));
                ShowMenuResponses(conversation, responses);
                Logger.Debug($"Displayed Mr. Stacks submenu for {OrderedDropConfig.GetGroupName(group)}", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to show Mr. Stacks group menu: {ex.Message}", "MrStacksMessaging");
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
                Logger.Debug($"📝 Message queued for save: {messageRecord.MessageId} (Save: {saveName}, Steam: {steamId}) - will save on next game save", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to save message to history: {ex.Message}", "MrStacksMessaging");
            }
        }

        /// <summary>
        /// Load conversation history from the current save's SQLite database.
        /// </summary>
        private static void LoadConversationHistory(string saveId)
        {
            try
            {
                Logger.Debug($"🔍 Loading conversation history for save ID: {saveId}", "MrStacksMessaging");
                
                var loadedMessages = SaveFileJsonDataStore.LoadConversationMessagesForCurrentSave();
                _conversationHistory = new ConversationHistory
                {
                    SaveId = saveId,
                    ContactName = "Mr. Stacks",
                    Messages = loadedMessages,
                    LastSaved = loadedMessages.Count > 0 ? loadedMessages[^1].Timestamp : ""
                };
                Logger.Info($"Loaded {_conversationHistory.Messages.Count} messages from SQLite storage (Save ID: {saveId})", "MrStacksMessaging");
                return;

                SaveFileJsonDataStore.SaveConversationMessagesForCurrentSave(GetConversationMessagesSnapshot());
                Logger.Debug($"Conversation history save shortcut reached for {saveId}", "MrStacksMessaging");
                return;

                List<MessageRecord> snapshot = GetConversationMessagesSnapshot();
                SaveFileJsonDataStore.SaveConversationMessagesForCurrentSave(snapshot);
                _conversationHistory.SaveId = saveId;
                _conversationHistory.LastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Logger.Debug($"Conversation history saved to SQLite ({snapshot.Count} messages)", "MrStacksMessaging");
                return;

                string conversationFile = GetConversationFilePath(saveId);
                _conversationHistory = new ConversationHistory
                {
                    SaveId = saveId,
                    ContactName = "Mr. Stacks",
                    Messages = loadedMessages,
                    LastSaved = loadedMessages.Count > 0 ? loadedMessages[^1].Timestamp : ""
                };

                Logger.Info($"Loaded {_conversationHistory.Messages.Count} messages from SQLite storage (Save ID: {saveId})", "MrStacksMessaging");
                return;

                string unusedConversationFile = GetConversationFilePath(saveId);
                _conversationHistory = new ConversationHistory
                {
                    SaveId = saveId,
                    ContactName = "Mr. Stacks",
                    Messages = loadedMessages,
                    LastSaved = loadedMessages.Count > 0 ? loadedMessages[^1].Timestamp : ""
                };

                Logger.Info($"Loaded {_conversationHistory.Messages.Count} messages from SQLite storage (Save ID: {saveId})", "MrStacksMessaging");
                return;
                
                Logger.Debug($"📄 Looking for conversation file: {conversationFile}", "MrStacksMessaging");
                Logger.Debug($"📄 File exists: {File.Exists(conversationFile)}", "MrStacksMessaging");
                
                if (File.Exists(conversationFile))
                {
                    string json = File.ReadAllText(conversationFile);
                    Logger.Debug($"📄 File content length: {json.Length} characters", "MrStacksMessaging");
                    
                    var loaded = JsonConvert.DeserializeObject<ConversationHistory>(json);
                    if (loaded != null)
                    {
                        _conversationHistory = loaded;
                        _conversationHistory.SaveId = saveId; // Ensure save ID is set
                        Logger.Info($"📂 ✅ Successfully loaded {_conversationHistory.Messages.Count} messages from save-aware JSON (Save ID: {saveId})", "MrStacksMessaging");
                        
                        // Debug: show first few messages
                        if (_conversationHistory.Messages.Count > 0)
                        {
                            Logger.Debug($"📝 First message: {_conversationHistory.Messages[0].Text.Substring(0, Math.Min(50, _conversationHistory.Messages[0].Text.Length))}...", "MrStacksMessaging");
                        }
                        return;
                    }
                    else
                    {
                        Logger.Warn($"⚠️ Failed to deserialize conversation JSON", "MrStacksMessaging");
                    }
                }
                
                Logger.Debug($"📂 No existing conversation history found for save ID: {saveId} - starting fresh (no file created until save)", "MrStacksMessaging");
                _conversationHistory = new ConversationHistory { SaveId = saveId };
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to load conversation history for save ID {saveId}: {ex.Message}", "MrStacksMessaging");
                Logger.Exception(ex, "MrStacksMessaging");
                _conversationHistory = new ConversationHistory { SaveId = saveId };
            }
        }

        /// <summary>
        /// Save conversation history to the current save's SQLite database.
        /// </summary>
        private static void SaveConversationHistory()
        {
            try
            {
                var (saveId, saveName, steamId, isLoaded) = SaveFileJsonDataStore.GetCurrentSaveInfo();
                if (!isLoaded || string.IsNullOrEmpty(saveId))
                {
                    Logger.Warn("[MrStacksMessaging] ⚠️ No save loaded - cannot save conversation");
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
                
                Logger.Debug($"💾 Conversation history saved to save-aware location ({_conversationHistory.Messages.Count} messages, Save: {saveName}, Steam: {steamId})", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to save conversation history: {ex.Message}", "MrStacksMessaging");
            }
        }

        /// <summary>
        /// Restore conversation history to the game conversation
        /// </summary>
        internal static List<MessageRecord> GetConversationMessagesSnapshot()
        {
            return _conversationHistory.Messages
                .Select(message => new MessageRecord
                {
                    Text = message.Text,
                    Timestamp = message.Timestamp,
                    GameDay = message.GameDay,
                    GameTime = message.GameTime,
                    IsFromPlayer = message.IsFromPlayer,
                    MessageId = message.MessageId
                })
                .ToList();
        }

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
                        Logger.Warn($"[MrStacksMessaging] ⚠️ Failed to restore message {messageRecord.MessageId}: {ex.Message}");
                    }
                }

                Logger.Info($"✅ Restored {_conversationHistory.Messages.Count} messages to conversation for current save", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to restore conversation history: {ex.Message}", "MrStacksMessaging");
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
                
                Logger.Debug($"📊 Conversation History (SQLite Storage):", "MrStacksMessaging");
                Logger.Debug($"💾 Current Save: {saveName} (ID: {saveId}, Steam: {steamId})", "MrStacksMessaging");
                Logger.Debug($"📝 Contact: {_conversationHistory.ContactName}", "MrStacksMessaging");
                Logger.Debug($"🔢 Messages: {_conversationHistory.Messages.Count}", "MrStacksMessaging");
                Logger.Debug($"💾 Last Saved: {_conversationHistory.LastSaved}", "MrStacksMessaging");
                Logger.Debug($"📄 Storage: {(isLoaded ? GetConversationFilePath(saveId ?? "") : "No save loaded")}", "MrStacksMessaging");
                Logger.Debug($"📁 Loaded: {_isConversationLoaded}", "MrStacksMessaging");

                if (_conversationHistory.Messages.Count > 0)
                {
                    Logger.Debug($"📜 Recent Messages:", "MrStacksMessaging");
                    var recent = _conversationHistory.Messages.TakeLast(3);
                    foreach (var msg in recent)
                    {
                        string sender = msg.IsFromPlayer ? "Player" : "Mr. Stacks";
                        Logger.Debug($"   [{msg.Timestamp}] {sender}: {msg.Text.Substring(0, Math.Min(50, msg.Text.Length))}...", "MrStacksMessaging");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ History display failed: {ex.Message}", "MrStacksMessaging");
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
                Logger.Info($"✅ Force save completed for current save", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Force save failed: {ex.Message}", "MrStacksMessaging");
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
                Logger.Debug($"🗑️ Conversation history cleared for save: {saveName} (Steam: {steamId}) - will save on next game save", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Failed to clear conversation history: {ex.Message}", "MrStacksMessaging");
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
                    Logger.Debug("🔧 Applying conversation customizations...", "MrStacksMessaging");
                    
                    // Try to disable debt-related functionality
                    if (supplier.Debt > 0.01f)
                    {
                        try
                        {
                            supplier.ChangeDebt(-supplier.Debt);
                            Logger.Debug("💰 Cleared Mr. Stacks debt", "MrStacksMessaging");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"⚠️ Could not clear debt: {ex.Message}", "MrStacksMessaging");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Conversation customization failed: {ex.Message}", "MrStacksMessaging");
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
                
                Logger.Debug($"🔍 HasExistingConversation check - Message count: {_conversationHistory.Messages.Count}", "MrStacksMessaging");
                return _conversationHistory.Messages.Count > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ HasExistingConversation check failed: {ex.Message}", "MrStacksMessaging");
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
                Logger.Debug("🧼 Shutting down messaging system...", "MrStacksMessaging");
                
                // Unload conversation data
                UnloadConversationForCurrentSave();
                
                // Reset state
                _conversationHistory = new ConversationHistory();
                _isConversationLoaded = false;
                
                Logger.Info("✅ Messaging system shutdown complete", "MrStacksMessaging");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Shutdown error: {ex.Message}", "MrStacksMessaging");
                Logger.Exception(ex, "MrStacksMessaging");
                
                // Force reset state even if shutdown failed
                _conversationHistory = new ConversationHistory();
                _isConversationLoaded = false;
            }
        }
    }
} 
