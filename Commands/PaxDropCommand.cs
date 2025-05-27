using System.Collections.Generic;
using ScheduleOne;

namespace PaxDrops.Commands
{
    public class PaxDropCommand : Console.ConsoleCommand
    {
        public override string CommandWord => "paxdrop";
        public override string CommandDescription => "Spawns a debug PaxDrop dead drop immediately.";
        public override string ExampleUsage => "paxdrop";

        public override void Execute(List<string> args)
        {
            Console.Log("Executing PaxDrop debug drop...", null);

            int day = S1API.GameTime.TimeManager.ElapsedDays;
            var packet = PaxDrops.TierLevel.GetDropPacket(day);
            PaxDrops.DeadDrop.ForceSpawnDrop(day, packet.ToFlatList(), "debug");
        }
    }
}