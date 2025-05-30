using System.Collections.Generic;
using System.Reflection;
using ScheduleOne;
using PaxDrops.Commands;

namespace PaxDrops
{
    public static class CommandHandler
    {
        private static bool _registered;

        public static void Init()
        {
            if (_registered) return;

            try
            {
                var field = typeof(Console).GetField("commands", BindingFlags.NonPublic | BindingFlags.Static);
                var commandsDict = field?.GetValue(null) as Dictionary<string, Console.ConsoleCommand>;

                if (commandsDict == null)
                {
                    Logger.Warn("⚠️ Unable to access Console.commands via reflection.");
                    return;
                }

                var list = new List<Console.ConsoleCommand>
                {
                    new PaxDropCommand(),
                    // Add more like: new PaxGiveMoneyCommand()
                };

                foreach (var cmd in list)
                {
                    if (!commandsDict.ContainsKey(cmd.CommandWord))
                    {
                        commandsDict.Add(cmd.CommandWord, cmd);
                        Console.Commands.Add(cmd);
                        Logger.Msg($"✅ Registered PaxDrops command: {cmd.CommandWord}");
                    }
                }

                _registered = true;
                Logger.Msg("✅ All PaxDrops commands registered.");
            }
            catch (System.Exception ex)
            {
                Logger.Error("❌ Command registration failed.");
                Logger.Exception(ex);
            }
        }
    }
}