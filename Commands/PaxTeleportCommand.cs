using System.Collections.Generic;
using UnityEngine;
using S1API.Entities;
using S1API.DeadDrops;

namespace PaxDrops.Commands
{
    public class PaxTeleportCommand : ScheduleOne.Console.ConsoleCommand
    {
        public override string CommandWord => "paxtp";
        public override string CommandDescription => "Teleports player to nearest dead drop.";
        public override string ExampleUsage => "paxtp";

        public override void Execute(List<string> args)
        {
            var player = Player.Local;
            if (player == null)
            {
                ScheduleOne.Console.LogWarning("❌ Player not found.");
                return;
            }

            Vector3 origin = player.Position;
            DeadDropInstance closest = null;
            float closestDist = float.MaxValue;

            foreach (var drop in DeadDropManager.All)
            {
                float dist = Vector3.Distance(origin, drop.Position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = drop;
                }
            }

            if (closest != null)
            {
                player.Position = closest.Position;
                ScheduleOne.Console.Log($"🧭 Teleported to dead drop at {closest.Position}");
            }
            else
            {
                ScheduleOne.Console.LogWarning("❌ No dead drops found.");
            }
        }
    }
}