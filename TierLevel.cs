using System.Collections.Generic;
using UnityEngine;
using S1API.GameTime;
using S1API.Leveling; 

namespace PaxDrops
{
    /// <summary>
    /// Manages item tier levels and generates full DropPackets (cash + loot).
    /// Tiers scale by player rank and in-game day progression.
    /// </summary>
    public static class TierLevel
    {
        /// <summary>
        /// Defines mafia-themed drop tiers (1–9), grouped in 3 families.
        /// </summary>
        public enum Tier
        {
            // 👕 Street Earners (1–3)
            StreetEarner1 = 1, // 1
            StreetEarner2,     // 2
            StreetEarner3,     // 3

            // 🧥 Capos (4–6)
            Capo1,             // 4
            Capo2,             // 5
            Capo3,             // 6

            // 👑 Dons (7–9)
            Don1,              // 7
            Don2,              // 8
            Don3               // 9
        }

        /// <summary>
        /// Represents a single drop packet with cash and item stacks.
        /// </summary>
        public class DropPacket
        {
            public int CashAmount;
            public List<ItemStack> Loot;

            public override string ToString()
            {
                string items = string.Join(", ", Loot.ConvertAll(x => $"{x.ItemID} x{x.Quantity}"));
                return $"${CashAmount} + [{items}]";
            }

            public List<string> ToFlatList()
            {
                var list = new List<string> { "cash" };
                foreach (var stack in Loot)
                    list.Add(stack.ItemID);
                return list;
            }

            public class ItemStack
            {
                public string ItemID;
                public int Quantity;
            }
        }

        private const int MaxTier = 9;
        private const int MinCash = 100;
        private const int MaxCash = 1000;
        private const int MaxSlots = 5;

        private static readonly Dictionary<Tier, List<string>> LootPools = new Dictionary<Tier, List<string>>();
        private static readonly System.Random Rng = new System.Random();

        public static void Init()
        {
            Logger.Msg("[TierLevel] Loading mafia-themed loot pools...");

            LootPools[Tier.StreetEarner1] = new List<string>
            {
                "acid", "baggie", "soil", "pseudo", "cokeleaf"
            };

            LootPools[Tier.StreetEarner2] = new List<string>(LootPools[Tier.StreetEarner1])
            {
                "battery", "defaultweed", "flashlight"
            };

            LootPools[Tier.StreetEarner3] = new List<string>(LootPools[Tier.StreetEarner2])
            {
                "meth", "cocainebase", "glass"
            };

            LootPools[Tier.Capo1] = new List<string>(LootPools[Tier.StreetEarner3])
            {
                "granddaddypurple", "cocaleaf", "cowboyhat"
            };

            LootPools[Tier.Capo2] = new List<string>(LootPools[Tier.Capo1])
            {
                "cocaine", "goldbar", "m1911", "machete"
            };

            LootPools[Tier.Capo3] = new List<string>(LootPools[Tier.Capo2])
            {
                "speedgrow", "growtent", "halogengrowlight"
            };

            LootPools[Tier.Don1] = new List<string>(LootPools[Tier.Capo3])
            {
                "laboven", "packagingstation", "sourdiesel"
            };

            LootPools[Tier.Don2] = new List<string>(LootPools[Tier.Don1])
            {
                "grandfatherclock", "goldchain", "revolver"
            };

            LootPools[Tier.Don3] = new List<string>(LootPools[Tier.Don2])
            {
                "goldentoilet", "goldenskateboard", "jukebox"
            };

            Logger.Msg("[TierLevel] Loot pools loaded.");
        }

        /// <summary>
        /// Returns a full drop packet with 1 cash stack and up to 4 loot stacks.
        /// </summary>
        public static DropPacket GetDropPacket(int day)
        {
            Tier tier = GetMaxUnlockedTier(day);
            List<string> pool = LootPools[tier];
            int tierNum = (int)tier;

            // Always reserve 1 slot for cash
            int lootSlots = MaxSlots - 1;

            var loot = new List<DropPacket.ItemStack>();
            for (int i = 0; i < lootSlots; i++)
            {
                string itemID = pool[UnityEngine.Random.Range(0, pool.Count)];
                int qty = GetStackQuantity(tierNum);
                loot.Add(new DropPacket.ItemStack { ItemID = itemID, Quantity = qty });
            }

            int cash = Mathf.Clamp((int)(Rng.Next(MinCash, MaxCash + 1) * (0.7f + tierNum * 0.1f)), MinCash, MaxCash);

            var packet = new DropPacket
            {
                CashAmount = cash,
                Loot = loot
            };

            Logger.Msg($"[TierLevel] Created drop for Day {day} ({tier}) ➤ {packet}");
            return packet;
        }

        /// <summary>
        /// Returns quantity per item stack based on tier strength.
        /// </summary>
        private static int GetStackQuantity(int tierNum)
        {
            if (tierNum >= 7) return UnityEngine.Random.Range(3, 6); // Don: 3–5
            if (tierNum >= 4) return UnityEngine.Random.Range(2, 4); // Capo: 2–3
            return UnityEngine.Random.Range(1, 3);                   // Street: 1–2
        }

        public static Tier GetMaxUnlockedTier(int day)
        {
            Rank rank = LevelManager.Rank;

            int rankLimit = 3;
            if (rank >= Rank.ShotCaller)
                rankLimit = 9;
            else if (rank >= Rank.Peddler)
                rankLimit = 6;

            int maxTier = Mathf.Min(day, rankLimit);
            return (Tier)Mathf.Clamp(maxTier, 1, MaxTier);
        }



        public static bool IsTierUnlocked(Tier tier)
        {
            return (int)tier <= (int)GetMaxUnlockedTier(TimeManager.ElapsedDays);
        }
    }
}
