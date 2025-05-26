using System.Collections.Generic;
using UnityEngine;

namespace PaxDrops
{
    /// <summary>
    /// Manages item tier levels and generates randomized drop packets based on in-game day.
    /// </summary>
    public static class TierLevel
    {
        /// <summary>
        /// Represents item power tiers based on player progression (day count).
        /// </summary>
        public enum Tier
        {
            Tier1 = 1,
            Tier2,
            Tier3,
            Tier4,
            Tier5,
            Tier6,
            Tier7,
            Tier8,
            Tier9
        }

        /// <summary>
        /// Maps each tier to a pool of valid item IDs.
        /// </summary>
        public static Dictionary<Tier, List<string>> ItemPools = new Dictionary<Tier, List<string>>();

        /// <summary>
        /// Initializes the tier-based item pools.
        /// Lower tiers have fewer/cheaper items, higher tiers include rare ones.
        /// </summary>
        public static void Init()
        {
            Logger.Msg("[TierLevel] Initializing item tiers...");

            // Base items — safe early-game pool
            ItemPools[Tier.Tier1] = new List<string>
            {
                "acid", "baggie", "soil", "pseudo", "cokeleaf", "cash"
            };

            // Expand tier 2 with light upgrades
            ItemPools[Tier.Tier2] = new List<string>(ItemPools[Tier.Tier1])
            {
                "defaultweed", "battery", "bucketHat", "flashlight"
            };

            // Tier 3 adds starter drugs/tools
            ItemPools[Tier.Tier3] = new List<string>(ItemPools[Tier.Tier2])
            {
                "meth", "cocainebase", "glass", "jorts"
            };

            // Tier 4 through 9 build on the previous with rare loot
            for (int i = 4; i <= 9; i++)
            {
                Tier tier = (Tier)i;
                Tier prevTier = (Tier)(i - 1);

                ItemPools[tier] = new List<string>(ItemPools[prevTier])
                {
                    "goldchain", "granddaddypurple", "goldbar", "grandfatherclock"
                };
            }

            Logger.Msg("[TierLevel] Tier pools loaded.");
        }

        /// <summary>
        /// Builds a randomized drop packet for a specific in-game day.
        /// Always includes cash, and 2–3 random tier items.
        /// </summary>
        /// <param name="day">In-game day (1–9 maps to Tier1–Tier9)</param>
        /// <returns>A list of item IDs</returns>
        public static List<string> GetDropPacket(int day)
        {
            Tier currentTier = (Tier)Mathf.Clamp(day, 1, 9);
            List<string> pool = ItemPools[currentTier];
            List<string> packet = new List<string>();

            // Always include some cash
            packet.Add("cash");

            // Randomly select 2–3 extra items
            int count = Random.Range(2, 4);
            for (int i = 0; i < count; i++)
            {
                string item = pool[Random.Range(0, pool.Count)];
                packet.Add(item);
            }

            Logger.Msg(string.Format("[TierLevel] Drop packet for Day {0} (Tier {1}): {2}", day, currentTier, string.Join(", ", packet)));
            return packet;
        }
    }
}
