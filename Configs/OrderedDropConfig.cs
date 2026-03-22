using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppScheduleOne.Levelling;

namespace PaxDrops.Configs
{
    /// <summary>
    /// Dedicated configuration for Mr. Stacks ordered drops.
    /// This is intentionally separate from the 11-tier random/system drop backend.
    /// </summary>
    public static class OrderedDropConfig
    {
        public enum OrderedGroup
        {
            CherryGreen = 1,
            CrimsonVultures = 2,
            GoldenCircle = 3
        }

        public enum OrderedTier
        {
            GoGetter = 1,
            Runner = 2,
            Soldier = 3,
            Enforcer = 4,
            Lieutenant = 5,
            Capo = 6,
            Broker = 7,
            Underboss = 8,
            Kingpin = 9
        }

        public sealed class OrderedTierDefinition
        {
            public OrderedTier Tier { get; }
            public string TierId { get; }
            public string DisplayName { get; }
            public OrderedGroup Group { get; }
            public ERank RequiredRank { get; }
            public int UnlockDay { get; }
            public int Price { get; }
            public int DailyOrderLimit { get; }
            public (int min, int max) CashRange { get; }
            public IReadOnlyList<string> ValuableItems { get; }
            public IReadOnlyList<string> FillerItems { get; }

            public OrderedTierDefinition(
                OrderedTier tier,
                string tierId,
                string displayName,
                OrderedGroup group,
                ERank requiredRank,
                int unlockDay,
                int price,
                int dailyOrderLimit,
                (int min, int max) cashRange,
                IReadOnlyList<string> valuableItems,
                IReadOnlyList<string> fillerItems)
            {
                Tier = tier;
                TierId = tierId;
                DisplayName = displayName;
                Group = group;
                RequiredRank = requiredRank;
                UnlockDay = unlockDay;
                Price = price;
                DailyOrderLimit = dailyOrderLimit;
                CashRange = cashRange;
                ValuableItems = valuableItems;
                FillerItems = fillerItems;
            }
        }

        public static readonly IReadOnlyDictionary<OrderedGroup, string> GroupNames =
            new Dictionary<OrderedGroup, string>
            {
                [OrderedGroup.CherryGreen] = "Cherry Green Gang",
                [OrderedGroup.CrimsonVultures] = "Crimson Vultures",
                [OrderedGroup.GoldenCircle] = "Golden Circle"
            };

        public static readonly IReadOnlyList<OrderedGroup> GroupOrder =
            new List<OrderedGroup>
            {
                OrderedGroup.CherryGreen,
                OrderedGroup.CrimsonVultures,
                OrderedGroup.GoldenCircle
            };

        public static readonly IReadOnlyList<OrderedTier> TierOrder =
            new List<OrderedTier>
            {
                OrderedTier.GoGetter,
                OrderedTier.Runner,
                OrderedTier.Soldier,
                OrderedTier.Enforcer,
                OrderedTier.Lieutenant,
                OrderedTier.Capo,
                OrderedTier.Broker,
                OrderedTier.Underboss,
                OrderedTier.Kingpin
            };

        public static readonly IReadOnlyDictionary<OrderedTier, OrderedTierDefinition> TierDefinitions =
            new Dictionary<OrderedTier, OrderedTierDefinition>
            {
                [OrderedTier.GoGetter] = new OrderedTierDefinition(
                    OrderedTier.GoGetter,
                    "TIER_CHERRY_1",
                    "Go-Getter",
                    OrderedGroup.CherryGreen,
                    ERank.Street_Rat,
                    1,
                    900,
                    1,
                    (250, 350),
                    new List<string> { "meth", "baggie", "babyblue" },
                    new List<string> { "apron", "cap", "tshirt", "sneakers", "cuke", "chili" }),

                [OrderedTier.Runner] = new OrderedTierDefinition(
                    OrderedTier.Runner,
                    "TIER_CHERRY_2",
                    "Runner",
                    OrderedGroup.CherryGreen,
                    ERank.Hoodlum,
                    7,
                    1600,
                    1,
                    (300, 400),
                    new List<string> { "meth", "liquidmeth", "bikercrank", "cheapskateboard", "combatboots" },
                    new List<string> { "buttonup", "jeans", "mixingstation", "flashlight", "mouthwash" }),

                [OrderedTier.Soldier] = new OrderedTierDefinition(
                    OrderedTier.Soldier,
                    "TIER_CHERRY_3",
                    "Soldier",
                    OrderedGroup.CherryGreen,
                    ERank.Peddler,
                    14,
                    2500,
                    1,
                    (350, 450),
                    new List<string> { "meth", "liquidmeth", "highqualitypseudo", "packagingstation", "trimmers" },
                    new List<string> { "saucepan", "flatcap", "cargopants", "dryingrack" }),

                [OrderedTier.Enforcer] = new OrderedTierDefinition(
                    OrderedTier.Enforcer,
                    "TIER_CRIMSON_1",
                    "Enforcer",
                    OrderedGroup.CrimsonVultures,
                    ERank.Hustler,
                    21,
                    4200,
                    2,
                    (400, 500),
                    new List<string> { "cocaine", "pgr", "sourdiesel", "m1911", "m1911mag" },
                    new List<string> { "fingerlessgloves", "paracetamol", "moisturepreservingpot", "soilpourer" }),

                [OrderedTier.Lieutenant] = new OrderedTierDefinition(
                    OrderedTier.Lieutenant,
                    "TIER_CRIMSON_2",
                    "Lieutenant",
                    OrderedGroup.CrimsonVultures,
                    ERank.Enforcer,
                    28,
                    6500,
                    2,
                    (500, 600),
                    new List<string> { "cocainebase", "mixingstationmk2", "growtent", "ledgrowlight", "phosphorus" },
                    new List<string> { "longskirt", "vest", "cowboyhat", "electrictrimmers" }),

                [OrderedTier.Capo] = new OrderedTierDefinition(
                    OrderedTier.Capo,
                    "TIER_CRIMSON_3",
                    "Capo",
                    OrderedGroup.CrimsonVultures,
                    ERank.Shot_Caller,
                    35,
                    9000,
                    2,
                    (550, 650),
                    new List<string> { "cocainebase", "goldbar", "launderingstation", "laboven", "grandfatherclock" },
                    new List<string> { "safe", "managementclipboard", "wallclock", "moisturepreservingpot" }),

                [OrderedTier.Broker] = new OrderedTierDefinition(
                    OrderedTier.Broker,
                    "TIER_GOLD_1",
                    "Broker",
                    OrderedGroup.GoldenCircle,
                    ERank.Underlord,
                    42,
                    13000,
                    3,
                    (650, 750),
                    new List<string> { "goldchain", "silverwatch", "ledgrowlight", "fullspectrumgrowlight", "liquidglass", "sourdiesel", "pgr" },
                    new List<string> { "dressshoes", "lightweightskateboard", "wallmountedshelf", "granddaddypurple" }),

                [OrderedTier.Underboss] = new OrderedTierDefinition(
                    OrderedTier.Underboss,
                    "TIER_GOLD_2",
                    "Underboss",
                    OrderedGroup.GoldenCircle,
                    ERank.Baron,
                    49,
                    18000,
                    3,
                    (700, 850),
                    new List<string> { "goldwatch", "brickpress", "grandfatherclock", "brick", "testweed", "granddaddypurpleseed" },
                    new List<string> { "flats", "rolledbuttonup", "jukebox", "extralonglifesoil" }),

                [OrderedTier.Kingpin] = new OrderedTierDefinition(
                    OrderedTier.Kingpin,
                    "TIER_GOLD_3",
                    "Kingpin",
                    OrderedGroup.GoldenCircle,
                    ERank.Kingpin,
                    56,
                    24000,
                    4,
                    (800, 1000),
                    new List<string> { "goldentoilet", "goldwatch", "goldbar", "mixingstationmk2", "cocainebase", "granddaddypurple", "goldenskateboard" },
                    new List<string> { "TV", "artworkmenace", "artworkoffer", "woodensign", "porkpiehat" })
            };

        public static OrderedTierDefinition GetDefinition(OrderedTier tier)
        {
            return TierDefinitions[tier];
        }

        public static OrderedGroup GetGroup(OrderedTier tier)
        {
            return GetDefinition(tier).Group;
        }

        public static string GetGroupName(OrderedGroup group)
        {
            return GroupNames.TryGetValue(group, out var name) ? name : "Unknown Group";
        }

        public static string GetTierName(OrderedTier tier)
        {
            return GetDefinition(tier).DisplayName;
        }

        public static string GetTierId(OrderedTier tier)
        {
            return GetDefinition(tier).TierId;
        }

        public static ERank GetRequiredRank(OrderedTier tier)
        {
            return GetDefinition(tier).RequiredRank;
        }

        public static int GetUnlockDay(OrderedTier tier)
        {
            return GetDefinition(tier).UnlockDay;
        }

        public static int GetPrice(OrderedTier tier)
        {
            return GetDefinition(tier).Price;
        }

        public static int GetDailyOrderLimit(OrderedTier tier)
        {
            return GetDefinition(tier).DailyOrderLimit;
        }

        public static IReadOnlyList<string> GetValuableItems(OrderedTier tier)
        {
            return GetDefinition(tier).ValuableItems;
        }

        public static IReadOnlyList<string> GetFillerItems(OrderedTier tier)
        {
            return GetDefinition(tier).FillerItems;
        }

        public static int GetCashAmount(OrderedTier tier)
        {
            var range = GetDefinition(tier).CashRange;
            return UnityEngine.Random.Range(range.min, range.max + 1);
        }

        public static List<OrderedTier> GetTiersForGroup(OrderedGroup group)
        {
            return TierOrder.Where(tier => GetGroup(tier) == group).ToList();
        }

        public static bool IsUnlocked(OrderedTier tier, ERank playerRank, int currentDay)
        {
            var definition = GetDefinition(tier);
            return playerRank >= definition.RequiredRank && currentDay >= definition.UnlockDay;
        }

        public static List<OrderedTier> GetUnlockedTiers(ERank playerRank, int currentDay)
        {
            return TierOrder.Where(tier => IsUnlocked(tier, playerRank, currentDay)).ToList();
        }

        public static OrderedTier? GetHighestUnlockedTier(ERank playerRank, int currentDay)
        {
            OrderedTier? highest = null;

            foreach (var tier in TierOrder)
            {
                if (IsUnlocked(tier, playerRank, currentDay))
                {
                    highest = tier;
                }
            }

            return highest;
        }

        public static OrderedTier? GetHighestUnlockedTierForCurrentPlayer()
        {
            return GetHighestUnlockedTier(DropConfig.GetCurrentPlayerRank(), DropConfig.GetCurrentGameDay());
        }

        public static int GetCurrentDailyOrderLimit()
        {
            var highestTier = GetHighestUnlockedTierForCurrentPlayer();
            return highestTier.HasValue ? GetDailyOrderLimit(highestTier.Value) : 0;
        }

        public static string GetLockedReason(OrderedTier tier, ERank playerRank, int currentDay)
        {
            var requiredRank = GetRequiredRank(tier);
            var unlockDay = GetUnlockDay(tier);
            bool needsRank = playerRank < requiredRank;
            bool needsDay = currentDay < unlockDay;

            if (!needsRank && !needsDay)
            {
                return $"{GetTierName(tier)} is already open for business.";
            }

            if (needsRank && needsDay)
            {
                return $"{GetTierName(tier)} unlocks at rank {FormatRankName(requiredRank)} on day {unlockDay}. You're rank {FormatRankName(playerRank)} on day {currentDay}.";
            }

            if (needsRank)
            {
                return $"{GetTierName(tier)} requires rank {FormatRankName(requiredRank)}. You're currently {FormatRankName(playerRank)}.";
            }

            return $"{GetTierName(tier)} unlocks on day {unlockDay}. You're currently on day {currentDay}.";
        }

        public static int CalculateHoursUntilDelivery(int currentDay, int currentTime, int deliveryDay, int deliveryTime)
        {
            int currentHours = currentTime / 100;
            int currentMinutes = currentTime % 100;
            int deliveryHours = deliveryTime / 100;
            int deliveryMinutes = deliveryTime % 100;

            int currentTotalMinutes = (currentDay * 24 * 60) + (currentHours * 60) + currentMinutes;
            int deliveryTotalMinutes = (deliveryDay * 24 * 60) + (deliveryHours * 60) + deliveryMinutes;
            int deltaMinutes = Math.Max(0, deliveryTotalMinutes - currentTotalMinutes);

            return (int)Math.Ceiling(deltaMinutes / 60f);
        }

        public static string FormatRankName(ERank rank)
        {
            return rank.ToString().Replace('_', ' ');
        }
    }
}
