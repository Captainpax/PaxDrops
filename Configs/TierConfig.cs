using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppScheduleOne.Levelling;

namespace PaxDrops.Configs
{
    /// <summary>
    /// Tier configuration system that maps 1:1 with all 11 ERank values.
    /// Each rank gets exactly one tier with appropriate rewards and progression.
    /// Integrated with game's ERank system for tier unlocking
    /// </summary>
    public static class TierConfig
    {
        /// <summary>
        /// Tier levels - maps 1:1 with ERank values (11 total)
        /// </summary>
        public enum Tier
        {
            // 🍒 Cherry Green Gang (1-4) - Street Level
            TIER_STREET_RAT = 1,      // ERank.Street_Rat
            TIER_HOODLUM = 2,         // ERank.Hoodlum  
            TIER_PEDDLER = 3,         // ERank.Peddler
            TIER_HUSTLER = 4,         // ERank.Hustler

            // 🔴 Crimson Vultures (5-8) - Mid Tier
            TIER_BAGMAN = 5,          // ERank.Bagman
            TIER_ENFORCER = 6,        // ERank.Enforcer
            TIER_SHOT_CALLER = 7,     // ERank.Shot_Caller
            TIER_BLOCK_BOSS = 8,      // ERank.Block_Boss

            // 🟡 Golden Circle (9-11) - Elite Tier
            TIER_UNDERLORD = 9,       // ERank.Underlord
            TIER_BARON = 10,          // ERank.Baron
            TIER_KINGPIN = 11         // ERank.Kingpin
        }

        /// <summary>
        /// Organization groups for tier classification
        /// </summary>
        public enum Organization
        {
            CherryGreen,    // Street level operations
            CrimsonVultures, // Mid-tier crime syndicate
            GoldenCircle    // Elite criminal enterprise
        }

        /// <summary>
        /// Tier display names that match their corresponding ERank
        /// </summary>
        public static readonly Dictionary<Tier, string> TierNames = new Dictionary<Tier, string>
        {
            // Cherry Green Gang - Street Level
            { Tier.TIER_STREET_RAT, "Street Rat" },
            { Tier.TIER_HOODLUM, "Hoodlum" },
            { Tier.TIER_PEDDLER, "Peddler" },
            { Tier.TIER_HUSTLER, "Hustler" },

            // Crimson Vultures - Mid Tier
            { Tier.TIER_BAGMAN, "Bagman" },
            { Tier.TIER_ENFORCER, "Enforcer" },
            { Tier.TIER_SHOT_CALLER, "Shot Caller" },
            { Tier.TIER_BLOCK_BOSS, "Block Boss" },

            // Golden Circle - Elite Tier
            { Tier.TIER_UNDERLORD, "Underlord" },
            { Tier.TIER_BARON, "Baron" },
            { Tier.TIER_KINGPIN, "Kingpin" }
        };

        /// <summary>
        /// Organization display names
        /// </summary>
        public static readonly Dictionary<Organization, string> OrganizationNames = new Dictionary<Organization, string>
        {
            { Organization.CherryGreen, "Cherry Green Gang" },
            { Organization.CrimsonVultures, "Crimson Vultures" },
            { Organization.GoldenCircle, "Golden Circle" }
        };

        /// <summary>
        /// Organization descriptions for messaging
        /// </summary>
        public static readonly Dictionary<Organization, string> OrganizationDescriptions = new Dictionary<Organization, string>
        {
            { Organization.CherryGreen, "Street-level operations, basic gear and starter drugs" },
            { Organization.CrimsonVultures, "Mid-tier crime syndicate, weapons and quality narcotics" },
            { Organization.GoldenCircle, "High-end criminal enterprise, luxury items and pure profit" }
        };

        /// <summary>
        /// Tier to organization mapping
        /// </summary>
        public static readonly Dictionary<Tier, Organization> TierOrganizations = new Dictionary<Tier, Organization>
        {
            // Cherry Green Gang - Street Level (Tiers 1-4)
            { Tier.TIER_STREET_RAT, Organization.CherryGreen },
            { Tier.TIER_HOODLUM, Organization.CherryGreen },
            { Tier.TIER_PEDDLER, Organization.CherryGreen },
            { Tier.TIER_HUSTLER, Organization.CherryGreen },

            // Crimson Vultures - Mid Tier (Tiers 5-8)
            { Tier.TIER_BAGMAN, Organization.CrimsonVultures },
            { Tier.TIER_ENFORCER, Organization.CrimsonVultures },
            { Tier.TIER_SHOT_CALLER, Organization.CrimsonVultures },
            { Tier.TIER_BLOCK_BOSS, Organization.CrimsonVultures },

            // Golden Circle - Elite Tier (Tiers 9-11)
            { Tier.TIER_UNDERLORD, Organization.GoldenCircle },
            { Tier.TIER_BARON, Organization.GoldenCircle },
            { Tier.TIER_KINGPIN, Organization.GoldenCircle }
        };

        /// <summary>
        /// ERank requirements for each tier - Perfect 1:1 mapping with game's ranking system
        /// </summary>
        public static readonly Dictionary<Tier, ERank> TierRankRequirements = new Dictionary<Tier, ERank>
        {
            // Perfect 1:1 mapping - each ERank gets exactly one tier
            { Tier.TIER_STREET_RAT, ERank.Street_Rat },
            { Tier.TIER_HOODLUM, ERank.Hoodlum },
            { Tier.TIER_PEDDLER, ERank.Peddler },
            { Tier.TIER_HUSTLER, ERank.Hustler },
            { Tier.TIER_BAGMAN, ERank.Bagman },
            { Tier.TIER_ENFORCER, ERank.Enforcer },
            { Tier.TIER_SHOT_CALLER, ERank.Shot_Caller },
            { Tier.TIER_BLOCK_BOSS, ERank.Block_Boss },
            { Tier.TIER_UNDERLORD, ERank.Underlord },
            { Tier.TIER_BARON, ERank.Baron },
            { Tier.TIER_KINGPIN, ERank.Kingpin }
        };

        /// <summary>
        /// Reverse mapping - ERank to Tier for easy lookups
        /// </summary>
        public static readonly Dictionary<ERank, Tier> RankToTierMapping = new Dictionary<ERank, Tier>
        {
            { ERank.Street_Rat, Tier.TIER_STREET_RAT },
            { ERank.Hoodlum, Tier.TIER_HOODLUM },
            { ERank.Peddler, Tier.TIER_PEDDLER },
            { ERank.Hustler, Tier.TIER_HUSTLER },
            { ERank.Bagman, Tier.TIER_BAGMAN },
            { ERank.Enforcer, Tier.TIER_ENFORCER },
            { ERank.Shot_Caller, Tier.TIER_SHOT_CALLER },
            { ERank.Block_Boss, Tier.TIER_BLOCK_BOSS },
            { ERank.Underlord, Tier.TIER_UNDERLORD },
            { ERank.Baron, Tier.TIER_BARON },
            { ERank.Kingpin, Tier.TIER_KINGPIN }
        };

        /// <summary>
        /// Get tiers for a specific organization
        /// </summary>
        public static List<Tier> GetTiersForOrganization(Organization org)
        {
            var tiers = new List<Tier>();
            foreach (var kvp in TierOrganizations)
            {
                if (kvp.Value == org)
                    tiers.Add(kvp.Key);
            }
            tiers.Sort();
            return tiers;
        }

        /// <summary>
        /// Get organization for a tier
        /// </summary>
        public static Organization GetOrganization(Tier tier)
        {
            return TierOrganizations.TryGetValue(tier, out var org) ? org : Organization.CherryGreen;
        }

        /// <summary>
        /// Get display name for a tier
        /// </summary>
        public static string GetTierName(Tier tier)
        {
            return TierNames.TryGetValue(tier, out var name) ? name : "Unknown";
        }

        /// <summary>
        /// Get organization name
        /// </summary>
        public static string GetOrganizationName(Organization org)
        {
            return OrganizationNames.TryGetValue(org, out var name) ? name : "Unknown";
        }

        /// <summary>
        /// Get organization description
        /// </summary>
        public static string GetOrganizationDescription(Organization org)
        {
            return OrganizationDescriptions.TryGetValue(org, out var desc) ? desc : "Unknown organization";
        }

        /// <summary>
        /// Check if a tier is valid
        /// </summary>
        public static bool IsValidTier(Tier tier)
        {
            return TierNames.ContainsKey(tier);
        }

        /// <summary>
        /// Get the highest tier (Kingpin)
        /// </summary>
        public static Tier GetMaxTier()
        {
            return Tier.TIER_KINGPIN;
        }

        /// <summary>
        /// Get the lowest tier (Street Rat)
        /// </summary>
        public static Tier GetMinTier()
        {
            return Tier.TIER_STREET_RAT;
        }

        /// <summary>
        /// Get required ERank for a tier
        /// </summary>
        public static ERank GetRequiredRank(Tier tier)
        {
            return TierRankRequirements.TryGetValue(tier, out var rank) ? rank : ERank.Street_Rat;
        }

        /// <summary>
        /// Get tier for player's current ERank (1:1 mapping)
        /// </summary>
        public static Tier GetTierForRank(ERank playerRank)
        {
            return RankToTierMapping.TryGetValue(playerRank, out var tier) ? tier : Tier.TIER_STREET_RAT;
        }

        /// <summary>
        /// Check if player's ERank is sufficient for a tier
        /// </summary>
        public static bool IsRankSufficient(Tier tier, ERank playerRank)
        {
            var requiredRank = GetRequiredRank(tier);
            return playerRank >= requiredRank;
        }

        /// <summary>
        /// Get all tiers unlocked by player's ERank
        /// </summary>
        public static List<Tier> GetTiersUnlockedByRank(ERank playerRank)
        {
            var unlockedTiers = new List<Tier>();
            foreach (var kvp in TierRankRequirements)
            {
                if (playerRank >= kvp.Value)
                {
                    unlockedTiers.Add(kvp.Key);
                }
            }
            unlockedTiers.Sort();
            return unlockedTiers;
        }

        /// <summary>
        /// Get the next tier for progression (or null if at max)
        /// </summary>
        public static Tier? GetNextTier(Tier currentTier)
        {
            int nextTierValue = (int)currentTier + 1;
            if (nextTierValue <= (int)GetMaxTier())
            {
                return (Tier)nextTierValue;
            }
            return null;
        }

        /// <summary>
        /// Get all tiers as an ordered list
        /// </summary>
        public static List<Tier> GetAllTiers()
        {
            return Enum.GetValues(typeof(Tier)).Cast<Tier>().OrderBy(t => (int)t).ToList();
        }
    }
} 