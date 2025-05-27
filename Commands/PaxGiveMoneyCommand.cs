using System.Collections.Generic;
using S1API.Money;

namespace PaxDrops.Commands
{
    public class PaxGiveMoneyCommand : ScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "pax.givemoney";
        public override string CommandDescription => "Adds money to the player.";
        public override string ExampleUsage => "pax.givemoney 500";

        public override void Execute(List<string> args)
        {
            if (args.Count == 0 || !int.TryParse(args[0], out int amount))
            {
                ScheduleOne.Console.LogWarning("Usage: pax.givemoney <amount>");
                return;
            }

            Money.ChangeCashBalance(amount, true, true);
            ScheduleOne.Console.Log($"💵 Gave player ${amount}");
        }
    }
}