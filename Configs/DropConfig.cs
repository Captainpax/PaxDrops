using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Levelling;
using Il2CppScheduleOne.GameTime;

namespace PaxDrops.Configs
{
    /// <summary>
    /// Static configuration for drop mechanics, cash ranges, daily limits, and progression.
    /// Now uses pure rank-based unlocking with 1:1 ERank mapping to 11 tiers.
    /// No time restrictions - only player rank determines available tiers.
    /// Uses centralized PlayerDetection system for rank data.
    /// </summary>
    public static class DropConfig
    {
        /// <summary>
        /// Cash amount ranges per tier - scales from $250 to $1000 across all 11 tiers
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, (int min, int max)> CashRanges = new Dictionary<TierConfig.Tier, (int, int)>
        {
            // 🍒 Cherry Green Gang - Entry Level (Tiers 1-3)
            { TierConfig.Tier.TIER_STREET_RAT, (250, 350) },
            { TierConfig.Tier.TIER_HOODLUM, (300, 400) },
            { TierConfig.Tier.TIER_PEDDLER, (350, 450) },

            // 🔴 Crimson Vultures - Mid Tier (Tiers 4-8)
            { TierConfig.Tier.TIER_HUSTLER, (400, 500) },
            { TierConfig.Tier.TIER_BAGMAN, (450, 550) },
            { TierConfig.Tier.TIER_ENFORCER, (500, 600) },
            { TierConfig.Tier.TIER_SHOT_CALLER, (550, 650) },
            { TierConfig.Tier.TIER_BLOCK_BOSS, (600, 700) },

            // 🟡 Golden Circle - Elite Tier (Tiers 9-11)
            { TierConfig.Tier.TIER_UNDERLORD, (650, 750) },
            { TierConfig.Tier.TIER_BARON, (700, 850) },
            { TierConfig.Tier.TIER_KINGPIN, (800, 1000) }
        };

        /// <summary>
        /// Daily order limits based on tier progression
        /// Entry level: 1 order/day → Mid tier: 2 orders/day → Elite: 3+ orders/day
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, int> DailyOrderLimits = new Dictionary<TierConfig.Tier, int>
        {
            // 🍒 Cherry Green Gang - Entry Level
            { TierConfig.Tier.TIER_STREET_RAT, 1 },
            { TierConfig.Tier.TIER_HOODLUM, 1 },
            { TierConfig.Tier.TIER_PEDDLER, 1 },

            // 🔴 Crimson Vultures - Mid Tier
            { TierConfig.Tier.TIER_HUSTLER, 2 },
            { TierConfig.Tier.TIER_BAGMAN, 2 },
            { TierConfig.Tier.TIER_ENFORCER, 2 },
            { TierConfig.Tier.TIER_SHOT_CALLER, 2 },
            { TierConfig.Tier.TIER_BLOCK_BOSS, 2 },

            // 🟡 Golden Circle - Elite Tier
            { TierConfig.Tier.TIER_UNDERLORD, 3 },
            { TierConfig.Tier.TIER_BARON, 3 },
            { TierConfig.Tier.TIER_KINGPIN, 4 } // Max tier gets 4 orders per day
        };

        /// <summary>
        /// Random drop chance for going one tier above current tier
        /// Progressive scaling from 60% to 100%
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, float> RandomDropUpgradeChance = new Dictionary<TierConfig.Tier, float>
        {
            // 🍒 Cherry Green Gang - Entry Level
            { TierConfig.Tier.TIER_STREET_RAT, 0.60f },
            { TierConfig.Tier.TIER_HOODLUM, 0.65f },
            { TierConfig.Tier.TIER_PEDDLER, 0.70f },

            // 🔴 Crimson Vultures - Mid Tier
            { TierConfig.Tier.TIER_HUSTLER, 0.75f },
            { TierConfig.Tier.TIER_BAGMAN, 0.80f },
            { TierConfig.Tier.TIER_ENFORCER, 0.85f },
            { TierConfig.Tier.TIER_SHOT_CALLER, 0.90f },
            { TierConfig.Tier.TIER_BLOCK_BOSS, 0.95f },

            // 🟡 Golden Circle - Elite Tier
            { TierConfig.Tier.TIER_UNDERLORD, 1.00f },
            { TierConfig.Tier.TIER_BARON, 1.00f },
            { TierConfig.Tier.TIER_KINGPIN, 1.00f } // Max tier, guaranteed upgrades
        };

        /// <summary>
        /// Drop preparation time in hours (max 4 hours to prepare)
        /// </summary>
        public static readonly (int min, int max) DropPreparationHours = (1, 4);

        /// <summary>
        /// Target drop time - always 7:30 AM next day
        /// </summary>
        public static readonly (int hour, int minute) DropTargetTime = (7, 30);

        /// <summary>
        /// Drop expiry - expires at 7:30 AM the day after drop day
        /// </summary>
        public static readonly int DropExpiryHours = 24;

        /// <summary>
        /// Daily random drop limits (higher tiers get more random drops)
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, int> DailyRandomDrops = new Dictionary<TierConfig.Tier, int>
        {
            // 🍒 Cherry Green Gang - Entry Level
            { TierConfig.Tier.TIER_STREET_RAT, 1 },
            { TierConfig.Tier.TIER_HOODLUM, 1 },
            { TierConfig.Tier.TIER_PEDDLER, 1 },

            // 🔴 Crimson Vultures - Mid Tier
            { TierConfig.Tier.TIER_HUSTLER, 1 },
            { TierConfig.Tier.TIER_BAGMAN, 1 },
            { TierConfig.Tier.TIER_ENFORCER, 1 },
            { TierConfig.Tier.TIER_SHOT_CALLER, 2 },
            { TierConfig.Tier.TIER_BLOCK_BOSS, 2 },

            // 🟡 Golden Circle - Elite Tier
            { TierConfig.Tier.TIER_UNDERLORD, 2 },
            { TierConfig.Tier.TIER_BARON, 2 },
            { TierConfig.Tier.TIER_KINGPIN, 3 } // Max tier gets 3 random drops per day
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
        /// Get random drop preparation time in hours (1-4 hours)
        /// </summary>
        public static int GetRandomPreparationTime()
        {
            return UnityEngine.Random.Range(DropPreparationHours.min, DropPreparationHours.max + 1);
        }

        /// <summary>
        /// Calculate drop delivery time - always next day at 7:30 AM
        /// </summary>
        public static DateTime CalculateDropDeliveryTime()
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);
            var deliveryTime = tomorrow.AddHours(DropTargetTime.hour).AddMinutes(DropTargetTime.minute);
            
            Logger.Msg($"[DropConfig] 📅 Drop scheduled for: {deliveryTime:yyyy-MM-dd HH:mm}");
            return deliveryTime;
        }

        /// <summary>
        /// Calculate drop expiry time - 7:30 AM the day after delivery
        /// </summary>
        public static DateTime CalculateDropExpiryTime(DateTime deliveryTime)
        {
            var expiryTime = deliveryTime.AddHours(DropExpiryHours);
            Logger.Msg($"[DropConfig] ⏰ Drop expires at: {expiryTime:yyyy-MM-dd HH:mm}");
            return expiryTime;
        }

        /// <summary>
        /// Check if enough preparation time is available before target delivery
        /// </summary>
        public static bool HasSufficientPreparationTime()
        {
            var now = DateTime.Now;
            var deliveryTime = CalculateDropDeliveryTime();
            var hoursUntilDelivery = (deliveryTime - now).TotalHours;
            
            // Need at least minimum preparation time
            return hoursUntilDelivery >= DropPreparationHours.min;
        }

        /// <summary>
        /// Get daily random drop count for a tier
        /// </summary>
        public static int GetDailyRandomDropCount(TierConfig.Tier tier)
        {
            return DailyRandomDrops.TryGetValue(tier, out var count) ? count : 1;
        }

        /// <summary>
        /// Check if a tier is unlocked based ONLY on player ERank (rank-based system)
        /// </summary>
        public static bool IsTierUnlocked(TierConfig.Tier tier, ERank playerRank)
        {
            return TierConfig.IsRankSufficient(tier, playerRank);
        }

        /// <summary>
        /// Get the player's current tier based on their ERank (1:1 mapping)
        /// </summary>
        public static TierConfig.Tier GetCurrentPlayerTier()
        {
            var playerRank = GetCurrentPlayerRank();
            var tier = TierConfig.GetTierForRank(playerRank);
            
            // Debug logging to trace the mapping
            Logger.Msg($"[DropConfig] 🔍 Player rank detection: ERank={playerRank} (int={(int)playerRank}) → Tier={tier} (int={(int)tier})");
            
            var org = TierConfig.GetOrganization(tier);
            var tierName = TierConfig.GetTierName(tier);
            var orgName = TierConfig.GetOrganizationName(org);
            
            Logger.Msg($"[DropConfig] 🏢 Organization mapping: {tierName} → {orgName}");
            
            return tier;
        }

        /// <summary>
        /// Get the highest unlocked tier for a player's rank
        /// </summary>
        public static TierConfig.Tier GetMaxUnlockedTier(ERank playerRank)
        {
            return TierConfig.GetTierForRank(playerRank);
        }

        /// <summary>
        /// Get all unlocked tiers for a player's rank
        /// </summary>
        public static List<TierConfig.Tier> GetUnlockedTiers(ERank playerRank)
        {
            return TierConfig.GetTiersUnlockedByRank(playerRank);
        }

        /// <summary>
        /// Get unlocked organizations for a player's rank
        /// </summary>
        public static List<TierConfig.Organization> GetUnlockedOrganizations(ERank playerRank)
        {
            var unlockedTiers = GetUnlockedTiers(playerRank);
            var organizations = new HashSet<TierConfig.Organization>();
            
            foreach (var tier in unlockedTiers)
            {
                organizations.Add(TierConfig.GetOrganization(tier));
            }
            
            return new List<TierConfig.Organization>(organizations);
        }

        /// <summary>
        /// Check if player should get upgraded random drop
        /// </summary>
        public static bool ShouldUpgradeRandomDrop(TierConfig.Tier currentTier)
        {
            float chance = GetRandomDropUpgradeChance(currentTier);
            return UnityEngine.Random.Range(0f, 1f) <= chance;
        }

        /// <summary>
        /// Get the next tier for progression (or current if at max)
        /// </summary>
        public static TierConfig.Tier GetNextTier(TierConfig.Tier currentTier)
        {
            var nextTier = TierConfig.GetNextTier(currentTier);
            return nextTier ?? currentTier; // Return current tier if at max
        }

        /// <summary>
        /// Get current player ERank using the centralized PlayerDetection system
        /// </summary>
        public static ERank GetCurrentPlayerRank()
        {
            // Use the centralized player detection system
            if (PlayerDetection.IsRankDetected)
            {
                return PlayerDetection.CurrentRank;
            }
            
            // Fallback: try to get it directly if PlayerDetection hasn't finished yet
            Logger.LogDebug("[DropConfig] PlayerDetection not ready, using fallback");
            return ERank.Street_Rat;
        }

        /// <summary>
        /// Get current game day safely
        /// </summary>
        public static int GetCurrentGameDay()
        {
            try
            {
                var timeManager = TimeManager.Instance;
                return timeManager?.ElapsedDays ?? 1;
            }
            catch
            {
                return 1; // Safe fallback
            }
        }

        /// <summary>
        /// Get current player's maximum unlocked tier
        /// </summary>
        public static TierConfig.Tier GetCurrentMaxUnlockedTier()
        {
            var playerRank = GetCurrentPlayerRank();
            return GetMaxUnlockedTier(playerRank);
        }

        /// <summary>
        /// Get current player's unlocked tiers
        /// </summary>
        public static List<TierConfig.Tier> GetCurrentlyUnlockedTiers()
        {
            var playerRank = GetCurrentPlayerRank();
            return GetUnlockedTiers(playerRank);
        }

        /// <summary>
        /// Get current player's unlocked organizations
        /// </summary>
        public static List<TierConfig.Organization> GetCurrentlyUnlockedOrganizations()
        {
            var playerRank = GetCurrentPlayerRank();
            return GetUnlockedOrganizations(playerRank);
        }

        /// <summary>
        /// Check if player can order today based on daily limits and current orders
        /// </summary>
        public static bool CanPlayerOrderToday(int currentDay)
        {
            try
            {
                var currentTier = GetCurrentPlayerTier();
                var dailyLimit = GetDailyOrderLimit(currentTier);
                var ordersToday = SaveFileJsonDataStore.GetMrsStacksOrdersToday(currentDay);
                
                return ordersToday < dailyLimit;
            }
            catch (Exception ex)
            {
                Logger.Error($"[DropConfig] ❌ Error checking order availability: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get remaining orders available today
        /// </summary>
        public static int GetRemainingOrdersToday(int currentDay)
        {
            try
            {
                var currentTier = GetCurrentPlayerTier();
                var dailyLimit = GetDailyOrderLimit(currentTier);
                var ordersToday = SaveFileJsonDataStore.GetMrsStacksOrdersToday(currentDay);
                
                return Math.Max(0, dailyLimit - ordersToday);
            }
            catch (Exception ex)
            {
                Logger.Error($"[DropConfig] ❌ Error getting remaining orders: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Get player detection status for debugging
        /// </summary>
        public static string GetPlayerDetectionStatus()
        {
            return $"Player: {(PlayerDetection.IsPlayerDetected ? "✅" : "❌")} | " +
                   $"Rank: {(PlayerDetection.IsRankDetected ? "✅" : "❌")} | " +
                   $"Current: {PlayerDetection.CurrentRank} | " +
                   $"Info: {PlayerDetection.GetPlayerInfo()}";
        }

        /// <summary>
        /// Format game time (HHMM) to readable string
        /// </summary>
        public static string FormatGameTime(int gameTime)
        {
            int hours = gameTime / 100;
            int minutes = gameTime % 100;
            return $"{hours:D2}:{minutes:D2}";
        }

        /// <summary>
        /// Parse expiry time from string format "day:hour" to day and hour
        /// </summary>
        public static (int day, int hour) ParseExpiryTime(string expiryTime)
        {
            try
            {
                var parts = expiryTime.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out int day) && int.TryParse(parts[1], out int hour))
                {
                    return (day, hour);
                }
            }
            catch { }
            
            return (-1, -1); // Invalid
        }

        /// <summary>
        /// Check if a drop has expired based on current game day and time
        /// </summary>
        public static bool IsDropExpired(string expiryTime, int currentDay, int currentHour)
        {
            var (expiryDay, expiryHour) = ParseExpiryTime(expiryTime);
            if (expiryDay == -1) return false; // Invalid expiry time, don't expire
            
            // Drop is expired if current time is past expiry time
            return (currentDay > expiryDay) || (currentDay == expiryDay && currentHour >= expiryHour);
        }

        /// <summary>
        /// Check if a drop is available for pickup based on current game day and time
        /// </summary>
        public static bool IsDropAvailable(int deliveryDay, int deliveryHour, int currentDay, int currentHour)
        {
            // Drop is available if current time is at or past delivery time
            return (currentDay > deliveryDay) || (currentDay == deliveryDay && currentHour >= deliveryHour);
        }
    }
} 