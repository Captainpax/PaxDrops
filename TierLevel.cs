using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Levelling;

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
            StreetEarner1 = 1,
            StreetEarner2,
            StreetEarner3,

            // 🧥 Capos (4–6)
            Capo1,
            Capo2,
            Capo3,

            // 👑 Dons (7–9)
            Don1,
            Don2,
            Don3
        }

        /// <summary>
        /// Represents a single drop packet with cash and item stacks.
        /// </summary>
        public class DropPacket
        {
            public int CashAmount;
            public List<ItemStack> Loot;

            public DropPacket()
            {
                Loot = new List<ItemStack>();
            }

            public override string ToString()
            {
                string items = string.Join(", ", Loot.ConvertAll(x => $"{x.ItemID} x{x.Quantity}"));
                return $"${CashAmount} + [{items}]";
            }

            /// <summary>
            /// Converts the packet into a list of formatted item strings like "cash:500", "acid:3".
            /// </summary>
            public List<string> ToFlatList()
            {
                var list = new List<string>
                {
                    $"cash:{CashAmount}"
                };

                foreach (var stack in Loot)
                    list.Add($"{stack.ItemID}:{stack.Quantity}");

                return list;
            }

            public class ItemStack
            {
                public string ItemID;
                public int Quantity;

                public ItemStack(string itemId, int quantity)
                {
                    ItemID = itemId;
                    Quantity = quantity;
                }
            }
        }

        private const int MaxTier = 9;
        private const int MinCash = 100;
        private const int MaxCash = 1000;
        private const int MaxSlots = 5;

        private static readonly Dictionary<Tier, List<string>> LootPools = new Dictionary<Tier, List<string>>();
        private static readonly System.Random Rng = new System.Random();
        private static bool _initialized = false;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Msg("[TierLevel] Loading mafia-themed loot pools...");

            LootPools[Tier.StreetEarner1] = new List<string>
            {
                "acid", "baggie", "soil", "pseudo"
            };

            LootPools[Tier.StreetEarner2] = new List<string>(LootPools[Tier.StreetEarner1])
            {
                "battery", "defaultweed", "flashlight"
            };

            LootPools[Tier.StreetEarner3] = new List<string>(LootPools[Tier.StreetEarner2])
            {
                "meth", "cocainebase", "glass", "cocaleaf"
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

            Logger.Msg("[TierLevel] Loot pools loaded with 9 tiers.");
        }

        /// <summary>
        /// Returns a full drop packet with 1 cash stack and up to 4 loot stacks.
        /// </summary>
        public static DropPacket GetDropPacket(int day)
        {
            if (!_initialized) Init();

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
                loot.Add(new DropPacket.ItemStack(itemID, qty));
            }

            int cash = Mathf.Clamp(
                (int)(Rng.Next(MinCash, MaxCash + 1) * (0.7f + tierNum * 0.1f)),
                MinCash, MaxCash
            );

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

        /// <summary>
        /// Determines max tier unlocked by the given day and player rank.
        /// </summary>
        public static Tier GetMaxUnlockedTier(int day)
        {
            try
            {
                var levelManager = Il2CppScheduleOne.Levelling.LevelManager.Instance;
                var rank = levelManager?.Rank ?? Il2CppScheduleOne.Levelling.ERank.Street_Rat;

                int rankLimit = 3;
                if (rank >= Il2CppScheduleOne.Levelling.ERank.Shot_Caller)
                    rankLimit = 9;
                else if (rank >= Il2CppScheduleOne.Levelling.ERank.Peddler)
                    rankLimit = 6;

                int maxTier = Mathf.Min(day, rankLimit);
                return (Tier)Mathf.Clamp(maxTier, 1, MaxTier);
            }
            catch (Exception ex)
            {
                Logger.Error($"[TierLevel] ❌ Error getting rank, defaulting to Tier 1: {ex.Message}");
                return Tier.StreetEarner1;
            }
        }

        /// <summary>
        /// Returns true if the specified tier is currently unlocked.
        /// </summary>
        public static bool IsTierUnlocked(Tier tier)
        {
            try
            {
                int currentDay = TimeManager.Instance?.ElapsedDays ?? 1;
                return (int)tier <= (int)GetMaxUnlockedTier(currentDay);
            }
            catch (Exception ex)
            {
                Logger.Error($"[TierLevel] ❌ Error checking tier unlock: {ex.Message}");
                return tier == Tier.StreetEarner1; // Default to tier 1 only
            }
        }

        /// <summary>
        /// Gets all available tiers for a given tier group
        /// </summary>
        public static Tier[] GetTiersInGroup(string groupName)
        {
            switch (groupName.ToLower())
            {
                case "street":
                case "streetearner":
                    return new[] { Tier.StreetEarner1, Tier.StreetEarner2, Tier.StreetEarner3 };
                case "capo":
                    return new[] { Tier.Capo1, Tier.Capo2, Tier.Capo3 };
                case "don":
                    return new[] { Tier.Don1, Tier.Don2, Tier.Don3 };
                default:
                    return new[] { Tier.StreetEarner1 };
            }
        }

        /// <summary>
        /// Gets a formatted tier name for display
        /// </summary>
        public static string GetTierDisplayName(Tier tier)
        {
            return tier.ToString().Replace("StreetEarner", "Street Earner ");
        }
    }
} 