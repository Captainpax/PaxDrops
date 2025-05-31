using Il2CppScheduleOne.GameTime;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Command to manually trigger drop spawning for testing
    /// </summary>
    public class PaxTriggerCommand : Il2CppScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxtrigger";
        public override string CommandDescription => "Manually triggers drop spawning. Usage: paxtrigger [day] - triggers drop for specified day or current day";
        public override string ExampleUsage => "paxtrigger 5";

        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                var timeManager = TimeManager.Instance;
                int targetDay = timeManager?.ElapsedDays ?? 1;

                // Parse day argument if provided
                if (args.Count >= 1 && int.TryParse(args[0], out int parsedDay))
                {
                    targetDay = parsedDay;
                }

                Logger.Msg($"[PaxTriggerCommand] 🎯 Attempting to trigger drop for Day {targetDay}");

                // Check if there's a pending drop for this day
                if (JsonDataStore.PendingDrops.TryGetValue(targetDay, out var drop))
                {
                    Logger.Msg($"[PaxTriggerCommand] 📦 Found pending drop for Day {targetDay}: {drop.Items.Count} items");
                    
                    // Force spawn the drop
                    DeadDrop.ForceSpawnDrop(targetDay, drop.Items, "manual_trigger", drop.DropHour);
                    
                    // Remove from pending
                    JsonDataStore.PendingDrops.Remove(targetDay);
                    
                    Logger.Msg($"[PaxTriggerCommand] ✅ Drop for Day {targetDay} triggered successfully!");
                    Il2CppScheduleOne.Console.Log($"Drop for Day {targetDay} triggered - check your usual spots!");
                }
                else
                {
                    // No existing drop, create a new one
                    Logger.Msg($"[PaxTriggerCommand] 📭 No pending drop for Day {targetDay}, generating new drop...");
                    
                    var packet = TierLevel.GetDropPacket(targetDay);
                    var items = packet.ToFlatList();
                    
                    int currentHour = timeManager?.CurrentTime ?? 12;
                    DeadDrop.ForceSpawnDrop(targetDay, items, "manual_trigger", currentHour);
                    
                    Logger.Msg($"[PaxTriggerCommand] ✅ New drop for Day {targetDay} generated and triggered!");
                    Logger.Msg($"[PaxTriggerCommand] 📦 Contains: {packet.Loot.Count} items + ${packet.CashAmount} cash");
                    Il2CppScheduleOne.Console.Log($"New drop for Day {targetDay} created and triggered!");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[PaxTriggerCommand] ❌ Failed to trigger drop: {ex.Message}");
                Logger.Exception(ex);
                Il2CppScheduleOne.Console.Log("PaxTrigger command failed - check logs");
            }
        }
    }
} 