using System.Collections.Generic;
using S1API.GameTime;

namespace PaxDrops.Commands
{
    public class PaxSetTimeCommand : ScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxtime";
        public override string CommandDescription => "Sets the in-game time. Usage: paxtime <hour>";
        public override string ExampleUsage => "paxtime 2000";

        public override void Execute(List<string> args)
        {
            if (args.Count == 0 || !int.TryParse(args[0], out int hour))
            {
                ScheduleOne.Console.LogWarning("Usage: paxtime <hour> (e.g., paxtime 2000)");
                return;
            }

            TimeManager.SetTime(hour);
            ScheduleOne.Console.Log($"🕒 Time set to {hour} via console.");
        }
    }
}