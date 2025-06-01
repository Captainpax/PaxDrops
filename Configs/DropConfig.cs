using System;
using System.Collections.Generic;

namespace PaxDrops.Configs
{
    /// <summary>
    /// Static configuration for drop mechanics, cash ranges, daily limits, and progression.
    /// Based on the exact requirements from the documentation.
    /// </summary>
    public static class DropConfig
    {
        /// <summary>
        /// Cash amount ranges per tier ($250-$1000 as specified)
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, (int min, int max)> CashRanges = new Dictionary<TierConfig.Tier, (int, int)>
        {
            // 🍒 Cherry Green Gang
            { TierConfig.Tier.TIER_CHERRY_1, (250, 400) },
            { TierConfig.Tier.TIER_CHERRY_2, (300, 500) },
            { TierConfig.Tier.TIER_CHERRY_3, (400, 600) },

            // 🔴 Crimson Vultures
            { TierConfig.Tier.TIER_CRIMSON_1, (500, 700) },
            { TierConfig.Tier.TIER_CRIMSON_2, (600, 800) },
            { TierConfig.Tier.TIER_CRIMSON_3, (700, 900) },

            // 🟡 Golden Circle
            { TierConfig.Tier.TIER_GOLD_1, (800, 950) },
            { TierConfig.Tier.TIER_GOLD_2, (900, 1000) },
            { TierConfig.Tier.TIER_GOLD_3, (950, 1000) }
        };

        /// <summary>
        /// Daily order limits based on tier progression
        /// Base tier: 1 order/day → Max tier: 3 orders/day
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, int> DailyOrderLimits = new Dictionary<TierConfig.Tier, int>
        {
            // 🍒 Cherry Green Gang
            { TierConfig.Tier.TIER_CHERRY_1, 1 },
            { TierConfig.Tier.TIER_CHERRY_2, 1 },
            { TierConfig.Tier.TIER_CHERRY_3, 1 },

            // 🔴 Crimson Vultures
            { TierConfig.Tier.TIER_CRIMSON_1, 2 },
            { TierConfig.Tier.TIER_CRIMSON_2, 2 },
            { TierConfig.Tier.TIER_CRIMSON_3, 2 },

            // 🟡 Golden Circle
            { TierConfig.Tier.TIER_GOLD_1, 3 },
            { TierConfig.Tier.TIER_GOLD_2, 3 },
            { TierConfig.Tier.TIER_GOLD_3, 3 }
        };

        /// <summary>
        /// Random drop chance for going one tier above current tier
        /// Cherry Green: 60-70% → Crimson: ~85% → Golden: 100%
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, float> RandomDropUpgradeChance = new Dictionary<TierConfig.Tier, float>
        {
            // 🍒 Cherry Green Gang
            { TierConfig.Tier.TIER_CHERRY_1, 0.60f },
            { TierConfig.Tier.TIER_CHERRY_2, 0.65f },
            { TierConfig.Tier.TIER_CHERRY_3, 0.70f },

            // 🔴 Crimson Vultures
            { TierConfig.Tier.TIER_CRIMSON_1, 0.80f },
            { TierConfig.Tier.TIER_CRIMSON_2, 0.85f },
            { TierConfig.Tier.TIER_CRIMSON_3, 0.90f },

            // 🟡 Golden Circle
            { TierConfig.Tier.TIER_GOLD_1, 0.95f },
            { TierConfig.Tier.TIER_GOLD_2, 1.00f },
            { TierConfig.Tier.TIER_GOLD_3, 1.00f } // Max tier, guaranteed 2 drops
        };

        /// <summary>
        /// Drop delay ranges in hours (never instant, 1-6 hours)
        /// </summary>
        public static readonly (int min, int max) DropDelayHours = (1, 6);

        /// <summary>
        /// Daily random drop limits (max tier gets 2 guaranteed random drops)
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, int> DailyRandomDrops = new Dictionary<TierConfig.Tier, int>
        {
            // 🍒 Cherry Green Gang
            { TierConfig.Tier.TIER_CHERRY_1, 1 },
            { TierConfig.Tier.TIER_CHERRY_2, 1 },
            { TierConfig.Tier.TIER_CHERRY_3, 1 },

            // 🔴 Crimson Vultures
            { TierConfig.Tier.TIER_CRIMSON_1, 1 },
            { TierConfig.Tier.TIER_CRIMSON_2, 1 },
            { TierConfig.Tier.TIER_CRIMSON_3, 1 },

            // 🟡 Golden Circle
            { TierConfig.Tier.TIER_GOLD_1, 1 },
            { TierConfig.Tier.TIER_GOLD_2, 1 },
            { TierConfig.Tier.TIER_GOLD_3, 2 } // Max tier gets 2 guaranteed random drops
        };

        /// <summary>
        /// Tier unlock progression based on in-game days
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, int> TierUnlockDays = new Dictionary<TierConfig.Tier, int>
        {
            // 🍒 Cherry Green Gang
            { TierConfig.Tier.TIER_CHERRY_1, 0 },   // Available from start
            { TierConfig.Tier.TIER_CHERRY_2, 3 },   // Day 3
            { TierConfig.Tier.TIER_CHERRY_3, 7 },   // Day 7

            // 🔴 Crimson Vultures
            { TierConfig.Tier.TIER_CRIMSON_1, 14 }, // Day 14
            { TierConfig.Tier.TIER_CRIMSON_2, 21 }, // Day 21
            { TierConfig.Tier.TIER_CRIMSON_3, 28 }, // Day 28

            // 🟡 Golden Circle
            { TierConfig.Tier.TIER_GOLD_1, 35 },    // Day 35
            { TierConfig.Tier.TIER_GOLD_2, 42 },    // Day 42
            { TierConfig.Tier.TIER_GOLD_3, 50 }     // Day 50
        };

        /// <summary>
        /// Get cash amount for a tier
        /// </summary>
        public static int GetCashAmount(TierConfig.Tier tier)
        {
            if (CashRanges.TryGetValue(tier, out var range))
            {
                return UnityEngine.Random.Range(range.min, range.max + 1);
            }
            return 250; // Fallback
        }

        /// <summary>
        /// Get daily order limit for a tier
        /// </summary>
        public static int GetDailyOrderLimit(TierConfig.Tier tier)
        {
            return DailyOrderLimits.TryGetValue(tier, out var limit) ? limit : 1;
        }

        /// <summary>
        /// Get random drop upgrade chance for a tier
        /// </summary>
        public static float GetRandomDropUpgradeChance(TierConfig.Tier tier)
        {
            return RandomDropUpgradeChance.TryGetValue(tier, out var chance) ? chance : 0.6f;
        }

        /// <summary>
        /// Get random drop delay in hours
        /// </summary>
        public static int GetRandomDropDelay()
        {
            return UnityEngine.Random.Range(DropDelayHours.min, DropDelayHours.max + 1);
        }

        /// <summary>
        /// Get daily random drop count for a tier
        /// </summary>
        public static int GetDailyRandomDropCount(TierConfig.Tier tier)
        {
            return DailyRandomDrops.TryGetValue(tier, out var count) ? count : 1;
        }

        /// <summary>
        /// Check if a tier is unlocked based on game day
        /// </summary>
        public static bool IsTierUnlocked(TierConfig.Tier tier, int gameDay)
        {
            return TierUnlockDays.TryGetValue(tier, out var unlockDay) && gameDay >= unlockDay;
        }

        /// <summary>
        /// Get the highest unlocked tier for a given day
        /// </summary>
        public static TierConfig.Tier GetMaxUnlockedTier(int gameDay)
        {
            var maxTier = TierConfig.Tier.TIER_CHERRY_1;
            
            foreach (var kvp in TierUnlockDays)
            {
                if (gameDay >= kvp.Value && kvp.Key > maxTier)
                {
                    maxTier = kvp.Key;
                }
            }
            
            return maxTier;
        }

        /// <summary>
        /// Get all unlocked tiers for a given day
        /// </summary>
        public static List<TierConfig.Tier> GetUnlockedTiers(int gameDay)
        {
            var unlockedTiers = new List<TierConfig.Tier>();
            
            foreach (var kvp in TierUnlockDays)
            {
                if (gameDay >= kvp.Value)
                {
                    unlockedTiers.Add(kvp.Key);
                }
            }
            
            unlockedTiers.Sort();
            return unlockedTiers;
        }

        /// <summary>
        /// Get unlocked organizations for a given day
        /// </summary>
        public static List<TierConfig.Organization> GetUnlockedOrganizations(int gameDay)
        {
            var organizations = new HashSet<TierConfig.Organization>();
            var unlockedTiers = GetUnlockedTiers(gameDay);
            
            foreach (var tier in unlockedTiers)
            {
                organizations.Add(TierConfig.GetOrganization(tier));
            }
            
            return new List<TierConfig.Organization>(organizations);
        }

        /// <summary>
        /// Check if random drop should be upgraded to next tier
        /// </summary>
        public static bool ShouldUpgradeRandomDrop(TierConfig.Tier currentTier)
        {
            var chance = GetRandomDropUpgradeChance(currentTier);
            return UnityEngine.Random.Range(0f, 1f) <= chance;
        }

        /// <summary>
        /// Get the next tier (for random drop upgrades)
        /// </summary>
        public static TierConfig.Tier GetNextTier(TierConfig.Tier currentTier)
        {
            int nextTierValue = (int)currentTier + 1;
            if (nextTierValue <= (int)TierConfig.GetMaxTier())
            {
                return (TierConfig.Tier)nextTierValue;
            }
            return currentTier; // Already at max
        }
    }
} 