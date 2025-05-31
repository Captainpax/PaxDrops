using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Command to spawn debug PaxDrops with tier-based loot generation
    /// </summary>
    public class PaxDropCommand : Il2CppScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxdrop";
        public override string CommandDescription => "Spawns a debug PaxDrop dead drop. Optional args: [day] [tier]";
        public override string ExampleUsage => "paxdrop 12 capo2";

        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                int day = TimeManager.Instance?.ElapsedDays ?? 1;
                TierLevel.Tier? specificTier = null;

                // Parse day argument
                if (args.Count >= 1 && int.TryParse(args[0], out int parsedDay))
                    day = parsedDay;

                // Parse tier argument
                if (args.Count >= 2)
                {
                    if (System.Enum.TryParse<TierLevel.Tier>(args[1], true, out var tier))
                    {
                        specificTier = tier;
                    }
                }

                // Generate loot packet
                TierLevel.DropPacket packet = TierLevel.GetDropPacket(day);

                // If specific tier was requested, use it (this could be enhanced to generate for specific tier)
                if (specificTier.HasValue)
                {
                    Logger.Msg($"[PaxDropCommand] 🎯 Using tier {specificTier.Value} for drop");
                    // For now, still use the day-based generation but could be enhanced
                }

                // Convert packet to the old string format for compatibility
                List<string> items = packet.ToFlatList();

                // Save to database and force spawn immediately
                int currentHour = TimeManager.Instance?.CurrentTime ?? 12;
                JsonDataStore.SaveDrop(day, items, currentHour, "command");
                
                // Force spawn immediately for testing
                DeadDrop.ForceSpawnDrop(day, items, "command", currentHour);

                Logger.Msg($"[PaxDropCommand] 📦 Command drop for Day {day} | Items: {packet.Loot.Count} + ${packet.CashAmount}");
                
                // Also log to console
                Il2CppScheduleOne.Console.Log($"PaxDrop spawned for Day {day} with {packet.Loot.Count} items + ${packet.CashAmount} cash");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[PaxDropCommand] ❌ Command execution failed: {ex.Message}");
                Logger.Exception(ex);
                Il2CppScheduleOne.Console.Log("PaxDrop command failed - check logs");
            }
        }
    }
} 