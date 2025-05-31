using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Command to give money to the player for testing
    /// </summary>
    public class PaxGiveMoneyCommand : Il2CppScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxmoney";
        public override string CommandDescription => "Gives money to the player. Usage: [amount]";
        public override string ExampleUsage => "paxmoney 500";

        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                int amount = 100; // Default amount

                // Parse amount argument
                if (args.Count >= 1 && int.TryParse(args[0], out int parsedAmount))
                    amount = parsedAmount;

                // Try to give money to player
                var player = Player.Local;
                if (player?.Inventory != null)
                {
                    MoneyManager.Instance.ChangeCashBalance(amount, true, true);
                    Logger.Msg($"[PaxGiveMoneyCommand] 💰 Gave ${amount} to player");
                    Il2CppScheduleOne.Console.Log($"Added ${amount} to your account");
                }
                else
                {
                    Logger.Warn("[PaxGiveMoneyCommand] ⚠️ Player or inventory not found");
                    Il2CppScheduleOne.Console.Log("Failed to find player - try again later");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[PaxGiveMoneyCommand] ❌ Command execution failed: {ex.Message}");
                Logger.Exception(ex);
                Il2CppScheduleOne.Console.Log("PaxMoney command failed - check logs");
            }
        }
    }
} 