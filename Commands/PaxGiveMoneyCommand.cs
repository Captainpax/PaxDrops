using System.Collections.Generic;
using S1API.Money;

namespace PaxDrops.Commands
{
    public class PaxGiveMoneyCommand : ScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxmoney";
        public override string CommandDescription => "Grants cash. Usage: paxmoney [amount]";
        public override string ExampleUsage => "paxmoney 5000";

        public override void Execute(List<string> args)
        {
            int amount = 1000;

            if (args.Count > 0 && !int.TryParse(args[0], out amount))
            {
                Logger.Warn("❌ Invalid amount. Usage: paxmoney [amount]");
                return;
            }

            Money.ChangeCashBalance(amount, true, true);
            Logger.Msg($"💸 Gave player ${amount:n0}.");
        }
    }
}