using System.Collections.Generic;
using S1API.GameTime;

namespace PaxDrops.Commands
{
    public class PaxSpawnDropCommand : ScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "pax.spawn";
        public override string CommandDescription => "Spawns a debug drop for today.";
        public override string ExampleUsage => "pax.spawn";

        public override void Execute(List<string> args)
        {
            int day = TimeManager.ElapsedDays;
            var drop = TierLevel.GetDropPacket(day);

            DataBase.SaveDrop(day, drop.ToFlatList(), TimeManager.CurrentTime, "console");
            DeadDrop.ForceSpawnDrop(day, drop.ToFlatList(), "console");

            ScheduleOne.Console.Log($"🧪 Spawned debug drop: {string.Join(", ", drop.Loot)} | 💵 ${drop.CashAmount}");
        }
    }
}