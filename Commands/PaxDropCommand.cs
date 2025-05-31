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
                Logger.Msg("[PaxDropCommand] 📦 Processing drop command...");

                if (args.Count == 0)
                {
                    // Show help
                    Logger.Msg("[PaxDropCommand] 💡 Available options:");
                    Logger.Msg("[PaxDropCommand]   paxdrop test_message  - Send test message from Mrs. Stacks dealer");
                    Logger.Msg("[PaxDropCommand]   paxdrop order [type]  - Order a drop via dealer system (standard/premium/surprise)");
                    Logger.Msg("[PaxDropCommand]   paxdrop force [items]  - Force spawn a drop with specified items");
                    Logger.Msg("[PaxDropCommand]   paxdrop show_pending   - Show pending scheduled drops");
                    return;
                }

                string command = args[0].ToString().ToLower();

                switch (command)
                {
                    case "test_message":
                    case "message":
                    case "msg":
                        Logger.Msg("[PaxDropCommand] 🧪 Triggering test order...");
                        MrStacks.TriggerTestOrder();
                        break;

                    case "order":
                        string orderType = args.Count > 1 ? args[1].ToString().ToLower() : "standard";
                        Logger.Msg($"[PaxDropCommand] 🛒 Ordering {orderType} drop via dealer system...");
                        Logger.Msg($"[PaxDropCommand] 📱 This simulates player interaction with Mrs. Stacks dealer");
                        MrStacks.ProcessOrder(orderType);
                        break;

                    case "force":
                    case "spawn":
                        Logger.Msg("[PaxDropCommand] 🚀 Force spawning drop...");
                        HandleForceSpawn(args);
                        break;

                    case "show_pending":
                    case "pending":
                    case "list":
                        Logger.Msg("[PaxDropCommand] 📋 Showing pending drops...");
                        ShowPendingDrops();
                        break;

                    default:
                        Logger.Warn($"[PaxDropCommand] ❓ Unknown command: {command}");
                        Logger.Msg("[PaxDropCommand] Use 'paxdrop' with no args to see available options");
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[PaxDropCommand] ❌ Command failed: {ex.Message}");
                Logger.Exception(ex);
            }
        }

        private void HandleForceSpawn(Il2CppSystem.Collections.Generic.List<string> args)
        {
            var timeManager = TimeManager.Instance;
            if (timeManager == null)
            {
                Logger.Error("[PaxDropCommand] ❌ TimeManager not available");
                return;
            }

            int currentDay = timeManager.ElapsedDays;

            if (args.Count > 1)
            {
                // Custom items specified
                var items = new List<string>();
                for (int i = 1; i < args.Count; i++)
                {
                    items.Add(args[i].ToString());
                }
                
                Logger.Msg($"[PaxDropCommand] 📦 Force spawning custom items: {string.Join(", ", items)}");
                DeadDrop.ForceSpawnDrop(currentDay, items, "command_custom");
            }
            else
            {
                // Generate tier-appropriate drop
                var packet = TierLevel.GetDropPacket(currentDay);
                var items = packet.ToFlatList();
                
                Logger.Msg($"[PaxDropCommand] 📦 Force spawning tier drop: {packet}");
                DeadDrop.ForceSpawnDrop(currentDay, items, "command_tier");
            }
        }

        private void ShowPendingDrops()
        {
            if (JsonDataStore.PendingDrops.Count == 0)
            {
                Logger.Msg("[PaxDropCommand] 📭 No pending drops scheduled");
                return;
            }

            Logger.Msg($"[PaxDropCommand] 📋 {JsonDataStore.PendingDrops.Count} pending drops:");
            foreach (var kvp in JsonDataStore.PendingDrops)
            {
                var drop = kvp.Value;
                Logger.Msg($"[PaxDropCommand]   Day {kvp.Key}: {drop.Items.Count} items, ${drop.Items.Where(i => i.StartsWith("cash:")).Sum(i => int.Parse(i.Split(':')[1]))} cash");
                Logger.Msg($"[PaxDropCommand]     Time: {drop.DropTime} | From: {drop.Org} | Type: {drop.Type}");
            }
        }
    }
} 