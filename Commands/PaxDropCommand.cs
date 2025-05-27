using System.Collections.Generic;
using ScheduleOne;

namespace PaxDrops.Commands
{
    public class PaxDropCommand : Console.ConsoleCommand
    {
        public override string CommandWord => "paxdrop";
        public override string CommandDescription => "Spawns a debug PaxDrop dead drop. Optional args: [day] [type]";
        public override string ExampleUsage => "paxdrop 12 order:4";

        public override void Execute(List<string> args)
        {
            int day = S1API.GameTime.TimeManager.ElapsedDays;
            string type = "debug";

            // Parse optional args: [day] [type]
            if (args.Count >= 1 && int.TryParse(args[0], out int parsedDay))
                day = parsedDay;

            if (args.Count >= 2)
                type = args[1];

            var packet = TierLevel.GetDropPacket(day);
            DeadDrop.ForceSpawnDrop(day, packet.ToFlatList(), type);

            Console.Log($"[PaxDrop] 📦 Forced drop for Day {day} | Type: {type}", null);
        }
    }
}