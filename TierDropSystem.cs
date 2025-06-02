using System;
using System.Collections.Generic;
using UnityEngine;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Levelling;
using PaxDrops.Configs;

namespace PaxDrops
{
    /// <summary>
    /// New tier-based drop system that integrates with ERank and uses exact drop tables.
    /// Replaces the old TierLevel.cs with proper tier unlocking logic.
    /// </summary>
    public static class TierDropSystem
    {
        /// <summary>
        /// Represents a complete drop package with cash and items
        /// </summary>
        public class DropPackage
        {
            public int CashAmount { get; set; }
            public List<DropItem> Items { get; set; }
            public TierConfig.Tier Tier { get; set; }

            public DropPackage()
            {
                Items = new List<DropItem>();
            }

            public override string ToString()
            {
                string items = string.Join(", ", Items.ConvertAll(x => $"{x.ItemId} x{x.Quantity}"));
                return $"${CashAmount} + [{items}] (Tier: {TierConfig.GetTierName(Tier)})";
            }

            /// <summary>
            /// Convert to flat list format for database storage
            /// </summary>
            public List<string> ToFlatList()
            {
                var list = new List<string>
                {
                    $"cash:{CashAmount}"
                };

                foreach (var item in Items)
                {
                    list.Add($"{item.ItemId}:{item.Quantity}");
                }

                return list;
            }
        }

        /// <summary>
        /// Represents a single item in a drop
        /// </summary>
        public class DropItem
        {
            public string ItemId { get; set; }
            public int Quantity { get; set; }
            public bool IsValuable { get; set; }

            public DropItem(string itemId, int quantity, bool isValuable = false)
            {
                ItemId = itemId;
                Quantity = quantity;
                IsValuable = isValuable;
            }
        }

        private static bool _initialized = false;

        /// <summary>
        /// Initialize the tier drop system
        /// </summary>
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Logger.Msg("[TierDropSystem] 🎯 New tier-based drop system initialized");
            Logger.Msg($"[TierDropSystem] 📊 {Enum.GetValues(typeof(TierConfig.Tier)).Length} tiers configured");
            Logger.Msg($"[TierDropSystem] 🏢 {Enum.GetValues(typeof(TierConfig.Organization)).Length} organizations available");
        }

        /// <summary>
        /// Generate a standard drop package based on current player state
        /// </summary>
        public static DropPackage GenerateDropPackage()
        {
            var currentTier = DropConfig.GetCurrentPlayerTier();
            return GenerateDropPackage(currentTier);
        }

        /// <summary>
        /// Generate a drop package for a specific tier
        /// </summary>
        public static DropPackage GenerateDropPackage(TierConfig.Tier tier)
        {
            if (!_initialized) Init();

            var package = new DropPackage
            {
                Tier = tier,
                CashAmount = DropConfig.GetCashAmount(tier)
            };

            // Generate items according to drop table specification
            var itemIds = ItemPools.GenerateDropItems(tier);
            
            foreach (var itemId in itemIds)
            {
                bool isValuable = ItemPools.IsValuableItem(tier, itemId);
                int quantity = GetItemQuantity(tier, isValuable);
                
                package.Items.Add(new DropItem(itemId, quantity, isValuable));
            }

            Logger.Msg($"[TierDropSystem] 📦 Generated {TierConfig.GetTierName(tier)} drop: {package}");
            return package;
        }

        /// <summary>
        /// Generate a premium drop package with enhanced rewards
        /// </summary>
        public static DropPackage GeneratePremiumDropPackage()
        {
            var currentTier = DropConfig.GetCurrentPlayerTier();
            return GeneratePremiumDropPackage(currentTier);
        }

        /// <summary>
        /// Generate a premium drop package for a specific tier
        /// </summary>
        public static DropPackage GeneratePremiumDropPackage(TierConfig.Tier tier)
        {
            if (!_initialized) Init();

            var package = new DropPackage
            {
                Tier = tier,
                CashAmount = (int)(DropConfig.GetCashAmount(tier) * 1.5f) // 50% more cash
            };

            // Premium gets more valuable items
            var valuableItems = ItemPools.GetValuableItems(tier);
            var fillerItems = ItemPools.GetFillerItems(tier);

            // Add 2-3 valuable items for premium
            int valuableCount = UnityEngine.Random.Range(2, 4);
            for (int i = 0; i < valuableCount && i < valuableItems.Count; i++)
            {
                var itemId = valuableItems[UnityEngine.Random.Range(0, valuableItems.Count)];
                int quantity = GetItemQuantity(tier, true, true); // Premium quantities
                package.Items.Add(new DropItem(itemId, quantity, true));
                valuableItems.Remove(itemId); // No duplicates
            }

            // Add 1-2 filler items
            int fillerCount = UnityEngine.Random.Range(1, 3);
            for (int i = 0; i < fillerCount && i < fillerItems.Count; i++)
            {
                var itemId = fillerItems[UnityEngine.Random.Range(0, fillerItems.Count)];
                int quantity = GetItemQuantity(tier, false, true); // Premium quantities
                package.Items.Add(new DropItem(itemId, quantity, false));
                fillerItems.Remove(itemId); // No duplicates
            }

            Logger.Msg($"[TierDropSystem] 💎 Generated PREMIUM {TierConfig.GetTierName(tier)} drop: {package}");
            return package;
        }

        /// <summary>
        /// Generate a random drop package with items from player's current tier
        /// </summary>
        public static DropPackage GenerateRandomDropPackage()
        {
            if (!_initialized) Init();

            var playerTier = DropConfig.GetCurrentPlayerTier();
            var randomTier = playerTier;

            // Check if we should upgrade the tier based on player's current tier
            if (DropConfig.ShouldUpgradeRandomDrop(playerTier))
            {
                var nextTier = TierConfig.GetNextTier(playerTier);
                if (nextTier.HasValue)
                {
                    randomTier = nextTier.Value;
                    Logger.Msg($"[TierDropSystem] 🎲 Random drop upgraded from {TierConfig.GetTierName(playerTier)} to {TierConfig.GetTierName(randomTier)}");
                }
            }

            var package = GenerateDropPackage(randomTier);
            Logger.Msg($"[TierDropSystem] 🎲 Generated random drop: {package}");
            return package;
        }

        /// <summary>
        /// Get appropriate quantity for an item based on tier and type
        /// </summary>
        private static int GetItemQuantity(TierConfig.Tier tier, bool isValuable, bool isPremium = false)
        {
            int tierLevel = (int)tier;
            int baseQuantity;

            if (isValuable)
            {
                // Valuable items: lower quantities but higher value
                if (tierLevel >= 7) baseQuantity = UnityEngine.Random.Range(2, 4); // Golden Circle: 2-3
                else if (tierLevel >= 4) baseQuantity = UnityEngine.Random.Range(2, 3); // Crimson Vultures: 2-2
                else baseQuantity = UnityEngine.Random.Range(1, 3); // Cherry Green: 1-2
            }
            else
            {
                // Filler items: higher quantities but lower value
                if (tierLevel >= 7) baseQuantity = UnityEngine.Random.Range(3, 6); // Golden Circle: 3-5
                else if (tierLevel >= 4) baseQuantity = UnityEngine.Random.Range(2, 5); // Crimson Vultures: 2-4
                else baseQuantity = UnityEngine.Random.Range(2, 4); // Cherry Green: 2-3
            }

            // Premium gets 25% more
            if (isPremium)
            {
                baseQuantity = Mathf.Max(1, (int)(baseQuantity * 1.25f));
            }

            return baseQuantity;
        }

        /// <summary>
        /// Check if player can access a specific tier (rank-based)
        /// </summary>
        public static bool CanPlayerAccessTier(TierConfig.Tier tier)
        {
            var playerRank = DropConfig.GetCurrentPlayerRank();
            return DropConfig.IsTierUnlocked(tier, playerRank);
        }

        /// <summary>
        /// Get player's current tier (1:1 mapping with their rank)
        /// </summary>
        public static TierConfig.Tier GetPlayerMaxTier()
        {
            return DropConfig.GetCurrentPlayerTier();
        }

        /// <summary>
        /// Get all organizations unlocked by player's rank
        /// </summary>
        public static List<TierConfig.Organization> GetPlayerUnlockedOrganizations()
        {
            return DropConfig.GetCurrentlyUnlockedOrganizations();
        }

        /// <summary>
        /// Get all tiers unlocked by player's rank
        /// </summary>
        public static List<TierConfig.Tier> GetPlayerUnlockedTiers()
        {
            return DropConfig.GetCurrentlyUnlockedTiers();
        }

        /// <summary>
        /// Get unlocked tiers for a specific organization based on player's rank
        /// </summary>
        public static List<TierConfig.Tier> GetPlayerUnlockedTiersForOrganization(TierConfig.Organization org)
        {
            var allUnlockedTiers = GetPlayerUnlockedTiers();
            var orgTiers = TierConfig.GetTiersForOrganization(org);
            
            var unlockedOrgTiers = new List<TierConfig.Tier>();
            foreach (var tier in orgTiers)
            {
                if (allUnlockedTiers.Contains(tier))
                {
                    unlockedOrgTiers.Add(tier);
                }
            }
            
            unlockedOrgTiers.Sort();
            return unlockedOrgTiers;
        }

        /// <summary>
        /// Get requirements for next tier progression
        /// </summary>
        public static string GetNextTierRequirements()
        {
            var playerRank = DropConfig.GetCurrentPlayerRank();
            var currentTier = TierConfig.GetTierForRank(playerRank);
            var nextTier = TierConfig.GetNextTier(currentTier);
            
            if (!nextTier.HasValue)
            {
                return "You've reached the maximum tier (Kingpin)! No further progression available.";
            }
            
            var requiredRank = TierConfig.GetRequiredRank(nextTier.Value);
            var tierName = TierConfig.GetTierName(nextTier.Value);
            var orgName = TierConfig.GetOrganizationName(TierConfig.GetOrganization(nextTier.Value));
            
            return $"Next tier: {tierName} ({orgName}) - Requires rank {requiredRank}";
        }

        /// <summary>
        /// Convert old TierLevel.DropPacket to new DropPackage format (for compatibility)
        /// </summary>
        public static DropPackage ConvertFromLegacyDropPacket(TierLevel.DropPacket legacyPacket)
        {
            var package = new DropPackage
            {
                CashAmount = legacyPacket.CashAmount,
                Tier = TierConfig.Tier.TIER_STREET_RAT // Default tier
            };

            foreach (var legacyItem in legacyPacket.Loot)
            {
                package.Items.Add(new DropItem(legacyItem.ItemID, legacyItem.Quantity));
            }

            return package;
        }
    }
} 