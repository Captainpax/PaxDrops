using System.Collections.Generic;
using UnityEngine;
using Il2CppScheduleOne;

namespace PaxDrops.Commands
{
    public class PaxTeleportCommand : Il2CppScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxteleport";
        public override string CommandDescription => "Teleports player. Args: [location]";
        public override string ExampleUsage => "paxteleport spawn";

        public override void Execute(Il2CppSystem.Collections.Generic.List<string> args)
        {
            string location = "spawn";

            if (args.Count >= 1)
                location = args[0];

            // TODO: Implement IL2CPP teleportation
            Logger.Msg($"🚀 Would teleport to {location} (not implemented yet)");
        }
    }
} 