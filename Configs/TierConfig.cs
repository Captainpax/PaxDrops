using System;
using System.Collections.Generic;

namespace PaxDrops.Configs
{
    /// <summary>
    /// Static configuration for the tier system based on mafia organizations.
    /// Defines the exact 9-tier structure: Cherry Green Gang → Crimson Vultures → Golden Circle
    /// </summary>
    public static class TierConfig
    {
        /// <summary>
        /// The 9 mafia tiers organized into 3 groups
        /// </summary>
        public enum Tier
        {
            // 🍒 Cherry Green Gang (1-3)
            TIER_CHERRY_1 = 1,    // Go-Getter
            TIER_CHERRY_2 = 2,    // Runner  
            TIER_CHERRY_3 = 3,    // Soldier

            // 🔴 Crimson Vultures (4-6)
            TIER_CRIMSON_1 = 4,   // Enforcer
            TIER_CRIMSON_2 = 5,   // Lieutenant
            TIER_CRIMSON_3 = 6,   // Capo

            // 🟡 Golden Circle (7-9)
            TIER_GOLD_1 = 7,      // Broker
            TIER_GOLD_2 = 8,      // Underboss
            TIER_GOLD_3 = 9       // Kingpin
        }

        /// <summary>
        /// Organization groups for tier classification
        /// </summary>
        public enum Organization
        {
            CherryGreen,
            CrimsonVultures,
            GoldenCircle
        }

        /// <summary>
        /// Tier display names
        /// </summary>
        public static readonly Dictionary<Tier, string> TierNames = new Dictionary<Tier, string>
        {
            // Cherry Green Gang
            { Tier.TIER_CHERRY_1, "Go-Getter" },
            { Tier.TIER_CHERRY_2, "Runner" },
            { Tier.TIER_CHERRY_3, "Soldier" },

            // Crimson Vultures
            { Tier.TIER_CRIMSON_1, "Enforcer" },
            { Tier.TIER_CRIMSON_2, "Lieutenant" },
            { Tier.TIER_CRIMSON_3, "Capo" },

            // Golden Circle
            { Tier.TIER_GOLD_1, "Broker" },
            { Tier.TIER_GOLD_2, "Underboss" },
            { Tier.TIER_GOLD_3, "Kingpin" }
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
            // Cherry Green Gang
            { Tier.TIER_CHERRY_1, Organization.CherryGreen },
            { Tier.TIER_CHERRY_2, Organization.CherryGreen },
            { Tier.TIER_CHERRY_3, Organization.CherryGreen },

            // Crimson Vultures
            { Tier.TIER_CRIMSON_1, Organization.CrimsonVultures },
            { Tier.TIER_CRIMSON_2, Organization.CrimsonVultures },
            { Tier.TIER_CRIMSON_3, Organization.CrimsonVultures },

            // Golden Circle
            { Tier.TIER_GOLD_1, Organization.GoldenCircle },
            { Tier.TIER_GOLD_2, Organization.GoldenCircle },
            { Tier.TIER_GOLD_3, Organization.GoldenCircle }
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
            return Tier.TIER_GOLD_3;
        }

        /// <summary>
        /// Get the lowest tier (Go-Getter)
        /// </summary>
        public static Tier GetMinTier()
        {
            return Tier.TIER_CHERRY_1;
        }
    }
} 