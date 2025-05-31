using System;
using System.Collections.Generic;
using Il2CppScheduleOne;
using PaxDrops.Commands;

namespace PaxDrops
{
    /// <summary>
    /// Handles registration of PaxDrops console commands using Schedule I's built-in console system.
    /// Creates commands that inherit from the game's ConsoleCommand class.
    /// </summary>
    public static class CommandHandler
    {
        private static bool _registered;
        private static List<Il2CppScheduleOne.Console.ConsoleCommand> _registeredCommands = new List<Il2CppScheduleOne.Console.ConsoleCommand>();

        public static void Init()
        {
            if (_registered) return;

            try
            {
                Logger.Msg("[CommandHandler] 🔧 Registering PaxDrops console commands...");

                // Wait for console to be available
                if (!WaitForConsole())
                {
                    Logger.Error("[CommandHandler] ❌ Console not available for command registration.");
                    return;
                }

                // Register all PaxDrops commands
                RegisterPaxDropsCommands();

                _registered = true;
                Logger.Msg($"[CommandHandler] ✅ Successfully registered {_registeredCommands.Count} PaxDrops commands.");
                Logger.Msg("[CommandHandler] 📋 Available commands: paxdrop, paxstatus, paxtrigger, paxmoney");
            }
            catch (Exception ex)
            {
                Logger.Error("[CommandHandler] ❌ Command registration failed.");
                Logger.Exception(ex);
            }
        }

        private static bool WaitForConsole()
        {
            try
            {
                // Check if console system is initialized
                var console = Il2CppScheduleOne.Console.Instance;
                if (console == null)
                {
                    Logger.Warn("[CommandHandler] ⚠️ Console.Instance is null, console may not be initialized yet.");
                    return false;
                }

                Logger.Msg("[CommandHandler] ✅ Console system found and ready.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"[CommandHandler] ⚠️ Console not ready: {ex.Message}");
                return false;
            }
        }

        private static void RegisterPaxDropsCommands()
        {
            try
            {
                // Create instances of our custom commands
                var paxDropCommand = new PaxDropCommand();
                var paxStatusCommand = new PaxStatusCommand();
                var paxTriggerCommand = new PaxTriggerCommand();
                var paxMoneyCommand = new PaxGiveMoneyCommand();

                // Try to register each command
                RegisterCommand(paxDropCommand);
                RegisterCommand(paxStatusCommand);
                RegisterCommand(paxTriggerCommand);
                RegisterCommand(paxMoneyCommand);

                Logger.Msg($"[CommandHandler] 📝 Attempted to register {_registeredCommands.Count} commands.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[CommandHandler] ❌ Failed to create command instances: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        private static void RegisterCommand(Il2CppScheduleOne.Console.ConsoleCommand command)
        {
            try
            {
                var console = Il2CppScheduleOne.Console.Instance;
                if (console == null)
                {
                    Logger.Error($"[CommandHandler] ❌ Cannot register {command.CommandWord} - console is null.");
                    return;
                }

                // Try using RegisterCommand method if it exists
                try
                {
                    // Some console systems use RegisterCommand instead
                    var registerMethod = console.GetType().GetMethod("RegisterCommand");
                    if (registerMethod != null)
                    {
                        registerMethod.Invoke(console, new object[] { command });
                        _registeredCommands.Add(command);
                        Logger.Msg($"[CommandHandler] ✅ Registered command '{command.CommandWord}' via RegisterCommand.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[CommandHandler] ⚠️ RegisterCommand failed for '{command.CommandWord}': {ex.Message}");
                }

                // Primary method: Try accessing the commands collection directly
                try
                {
                    // Access the internal commands dictionary
                    var consoleType = console.GetType();
                    var commandsField = consoleType.GetField("commands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ??
                                       consoleType.GetField("_commands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ??
                                       consoleType.GetField("Commands", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    if (commandsField != null)
                    {
                        var commandsDict = commandsField.GetValue(console);
                        if (commandsDict != null)
                        {
                            Logger.Msg($"[CommandHandler] 🎯 Found commands dictionary: {commandsDict.GetType().Name}");
                            
                            // Try to add to the dictionary directly
                            var addMethod = commandsDict.GetType().GetMethod("Add");
                            if (addMethod != null)
                            {
                                addMethod.Invoke(commandsDict, new object[] { command.CommandWord, command });
                                _registeredCommands.Add(command);
                                Logger.Msg($"[CommandHandler] ✅ Registered command '{command.CommandWord}' via direct dictionary access.");
                                return;
                            }
                            else
                            {
                                // Try indexer if Add method doesn't exist
                                var indexerProperty = commandsDict.GetType().GetProperty("Item");
                                if (indexerProperty != null)
                                {
                                    indexerProperty.SetValue(commandsDict, command, new object[] { command.CommandWord });
                                    _registeredCommands.Add(command);
                                    Logger.Msg($"[CommandHandler] ✅ Registered command '{command.CommandWord}' via dictionary indexer.");
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.Warn($"[CommandHandler] ⚠️ Could not find commands field in console type: {consoleType.Name}");
                        
                        // Log all available fields for debugging
                        var fields = consoleType.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        Logger.Msg($"[CommandHandler] 🔍 Available fields in {consoleType.Name}:");
                        foreach (var field in fields)
                        {
                            Logger.Msg($"[CommandHandler]   - {field.Name} ({field.FieldType.Name})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[CommandHandler] ⚠️ Direct dictionary access failed for '{command.CommandWord}': {ex.Message}");
                }

                Logger.Error($"[CommandHandler] ❌ All registration methods failed for command '{command.CommandWord}'.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[CommandHandler] ❌ Unexpected error registering '{command.CommandWord}': {ex.Message}");
                Logger.Exception(ex);
            }
        }

        /// <summary>
        /// Debug method to list all available console commands
        /// </summary>
        public static void ListAllCommands()
        {
            try
            {
                var console = Il2CppScheduleOne.Console.Instance;
                if (console == null)
                {
                    Logger.Warn("[CommandHandler] ⚠️ Console not available for listing commands.");
                    return;
                }

                // Try to access the commands collection
                var consoleType = console.GetType();
                var commandsField = consoleType.GetField("commands", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (commandsField != null)
                {
                    var commandsDict = commandsField.GetValue(console);
                    if (commandsDict != null)
                    {
                        Logger.Msg($"[CommandHandler] 📋 Available console commands:");
                        Logger.Msg($"[CommandHandler] Commands collection type: {commandsDict.GetType().Name}");
                        
                        // Try to get count if possible
                        var countProperty = commandsDict.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            var count = countProperty.GetValue(commandsDict);
                            Logger.Msg($"[CommandHandler] Total commands: {count}");
                        }
                    }
                }
                else
                {
                    Logger.Msg("[CommandHandler] 📋 Could not access commands collection for listing.");
                }

                // List our registered commands
                if (_registeredCommands.Count > 0)
                {
                    Logger.Msg($"[CommandHandler] 📦 PaxDrops commands ({_registeredCommands.Count}):");
                    foreach (var cmd in _registeredCommands)
                    {
                        Logger.Msg($"[CommandHandler]   - {cmd.CommandWord}: {cmd.CommandDescription}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[CommandHandler] ❌ Failed to list commands: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        public static void Shutdown()
        {
            if (!_registered) return;

            try
            {
                Logger.Msg($"[CommandHandler] 🔌 Unregistering {_registeredCommands.Count} commands...");
                _registeredCommands.Clear();
                _registered = false;
                Logger.Msg("[CommandHandler] ✅ Commands unregistered.");
            }
            catch (Exception ex)
            {
                Logger.Error("[CommandHandler] ❌ Error during command shutdown.");
                Logger.Exception(ex);
            }
        }
    }
} 