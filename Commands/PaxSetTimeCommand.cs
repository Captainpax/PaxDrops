using System.Collections.Generic;
using Il2CppScheduleOne;

namespace PaxDrops.Commands
{
    public class PaxSetTimeCommand : Il2CppScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxsettime";
        public override string CommandDescription => "Sets game time. Args: [time] (format: HHMM)";
        public override string ExampleUsage => "paxsettime 1200";

        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            if (args.Count < 1)
            {
                Logger.Warn("❌ Usage: paxsettime [time] (format: HHMM)");
                return;
            }

            if (!int.TryParse(args[0], out int time))
            {
                Logger.Warn("❌ Invalid time format. Use HHMM (e.g., 1200 for 12:00)");
                return;
            }

            // TODO: Implement IL2CPP time setting
            Logger.Msg($"🕐 Would set time to {time:D4} (not implemented yet)");
        }
    }
} 