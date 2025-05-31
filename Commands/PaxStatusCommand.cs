using System.Linq;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops.Commands
{
    /// <summary>
    /// Command to show the current status of the PaxDrops system
    /// </summary>
    public class PaxStatusCommand : Il2CppScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxstatus";
        public override string CommandDescription => "Shows PaxDrops system status, pending drops, and statistics";
        public override string ExampleUsage => "paxstatus";

        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                var timeManager = TimeManager.Instance;
                int currentDay = timeManager?.ElapsedDays ?? 0;
                int currentHour = timeManager?.CurrentTime ?? 0;

                Logger.Msg($"[PaxStatus] 📊 === PaxDrops Status ===");
                Logger.Msg($"[PaxStatus] ⏰ Current Day: {currentDay}, Hour: {currentHour}");
                
                // Show pending drops
                Logger.Msg($"[PaxStatus] 📦 Pending Drops: {JsonDataStore.PendingDrops.Count}");
                foreach (var kvp in JsonDataStore.PendingDrops)
                {
                    var drop = kvp.Value;
                    Logger.Msg($"[PaxStatus]   Day {kvp.Key}: {drop.Items.Count} items @ {drop.DropTime} ({drop.Org})");
                }
                
                // Show dead drop availability
                var deadDrops = Il2CppScheduleOne.Economy.DeadDrop.DeadDrops;
                int availableDrops = 0;
                if (deadDrops != null)
                {
                    for (int i = 0; i < deadDrops.Count; i++)
                    {
                        var deadDrop = deadDrops[i];
                        if (deadDrop?.Storage != null && deadDrop.Storage.ItemCount < deadDrop.Storage.SlotCount)
                        {
                            availableDrops++;
                        }
                    }
                }
                Logger.Msg($"[PaxStatus] 📍 Available Dead Drops: {availableDrops}/{deadDrops?.Count ?? 0}");
                
                // Test some common item IDs
                Logger.Msg($"[PaxStatus] 🔍 Testing item IDs:");
                string[] testItems = { "baggie", "cocaleaf", "acid", "meth", "cocaine", "battery", "soil" };
                foreach (string itemId in testItems)
                {
                    var item = Il2CppScheduleOne.Registry.GetItem(itemId);
                    string status = item != null ? $"✅ {item.GetType().Name}" : "❌ Not Found";
                    Logger.Msg($"[PaxStatus]   {itemId}: {status}");
                }

                // Also log to console
                Il2CppScheduleOne.Console.Log($"PaxDrops Status: {JsonDataStore.PendingDrops.Count} pending drops, {availableDrops} available dead drops - check logs for details");
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[PaxStatus] ❌ Error: {ex.Message}");
                Logger.Exception(ex);
                Il2CppScheduleOne.Console.Log("PaxStatus command failed - check logs");
            }
        }
    }
} 