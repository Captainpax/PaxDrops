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

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Handles messaging functionality for Mrs. Stacks NPC.
    /// Includes simple JSON-based conversation persistence.
    /// </summary>
    public static class MrsStacksMessaging
    {
        private const string ConversationFile = "Mods/PaxDrops/Data/mrs_stacks_conversation.json";

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
        }

        private static ConversationHistory _conversationHistory = new ConversationHistory();

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
        /// Send a message from Mrs. Stacks to the player and save to JSON
        /// </summary>
        public static void SendMessage(NPC npc, string messageText)
        {
            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation == null)
                {
                    Logger.Error("[MrsStacksMessaging] ❌ No conversation found");
                    return;
                }

                var message = new Message(messageText, Message.ESenderType.Other, true);
                conversation.SendMessage(message, true, true);
                
                // Save to our JSON storage
                SaveMessageToHistory(messageText, false);
                
                Logger.Msg($"[MrsStacksMessaging] 📱 Message sent and saved: {messageText}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Message send failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Setup conversation and send welcome message, then load conversation history
        /// </summary>
        public static void SetupConversation(NPC npc)
        {
            try
            {
                var conversation = MessagingManager.Instance?.GetConversation(npc);
                if (conversation != null)
                {
                    conversation.contactName = "Mrs. Stacks";
                    
                    // Load existing conversation history first
                    LoadConversationHistory();
                    
                    // Restore previous messages to the conversation
                    RestoreConversationHistory(conversation);
                    
                    // Send welcome message only if this is the first time
                    if (_conversationHistory.Messages.Count == 0)
                    {
                        var player = Player.Local;
                        string playerName = player?.PlayerName ?? "friend";

                        var welcomeMessage = $"Hey {playerName}! Mrs. Stacks here - premium dead drop supplier. " +
                                           "No catalogs, just quality surprise packages. Order and I'll text the pickup location.";

                        SendMessage(npc, welcomeMessage);
                        Logger.Msg("[MrsStacksMessaging] 📱 Welcome sent for new conversation");
                    }
                    else
                    {
                        Logger.Msg($"[MrsStacksMessaging] 📱 Restored {_conversationHistory.Messages.Count} previous messages");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Conversation setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Save a message to our JSON conversation history
        /// </summary>
        private static void SaveMessageToHistory(string messageText, bool isFromPlayer)
        {
            try
            {
                var timeManager = TimeManager.Instance;
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

                SaveConversationHistory();
                Logger.Msg($"[MrsStacksMessaging] 💾 Message saved to JSON: {messageRecord.MessageId}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to save message to history: {ex.Message}");
            }
        }

        /// <summary>
        /// Load conversation history from JSON file
        /// </summary>
        private static void LoadConversationHistory()
        {
            try
            {
                if (File.Exists(ConversationFile))
                {
                    string json = File.ReadAllText(ConversationFile);
                    var loaded = JsonConvert.DeserializeObject<ConversationHistory>(json);
                    if (loaded != null)
                    {
                        _conversationHistory = loaded;
                        Logger.Msg($"[MrsStacksMessaging] 📂 Loaded {_conversationHistory.Messages.Count} messages from JSON");
                        return;
                    }
                }
                
                Logger.Msg("[MrsStacksMessaging] 📂 No existing conversation history found - starting fresh");
                _conversationHistory = new ConversationHistory();
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Failed to load conversation history: {ex.Message}");
                _conversationHistory = new ConversationHistory();
            }
        }

        /// <summary>
        /// Save conversation history to JSON file
        /// </summary>
        private static void SaveConversationHistory()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConversationFile) ?? "");
                
                string json = JsonConvert.SerializeObject(_conversationHistory, Formatting.Indented);
                File.WriteAllText(ConversationFile, json);
                
                Logger.Msg($"[MrsStacksMessaging] 💾 Conversation history saved ({_conversationHistory.Messages.Count} messages)");
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

                Logger.Msg($"[MrsStacksMessaging] ✅ Restored {_conversationHistory.Messages.Count} messages to conversation");
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
                Logger.Msg($"[MrsStacksMessaging] 📊 Conversation History (JSON Storage):");
                Logger.Msg($"[MrsStacksMessaging] 📝 Contact: {_conversationHistory.ContactName}");
                Logger.Msg($"[MrsStacksMessaging] 🔢 Messages: {_conversationHistory.Messages.Count}");
                Logger.Msg($"[MrsStacksMessaging] 💾 Last Saved: {_conversationHistory.LastSaved}");
                Logger.Msg($"[MrsStacksMessaging] 📄 File: {ConversationFile}");

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
                Logger.Msg("[MrsStacksMessaging] ✅ Force save completed");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Force save failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear conversation history (for testing/reset)
        /// </summary>
        public static void ClearConversationHistory()
        {
            try
            {
                _conversationHistory = new ConversationHistory();
                SaveConversationHistory();
                
                // Also clear the in-game conversation
                var npc = FindMrsStacksNPC();
                if (npc != null)
                {
                    var conversation = MessagingManager.Instance?.GetConversation(npc);
                    if (conversation?.messageHistory != null)
                    {
                        conversation.messageHistory.Clear();
                        Logger.Msg("[MrsStacksMessaging] 🧹 In-game conversation cleared");
                    }
                }
                
                Logger.Msg("[MrsStacksMessaging] 🧹 Conversation history cleared");
            }
            catch (Exception ex)
            {
                Logger.Error($"[MrsStacksMessaging] ❌ Clear history failed: {ex.Message}");
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
    }
} 