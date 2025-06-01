using System;
using System.Collections.Generic;

namespace PaxDrops.Configs
{
    /// <summary>
    /// Static configuration for item pools based on the exact drop table from documentation.
    /// Each tier has valuable items and filler items as specified in droptableidea.md
    /// </summary>
    public static class ItemPools
    {
        /// <summary>
        /// Item category types
        /// </summary>
        public enum ItemCategory
        {
            Valuable,
            Filler
        }

        /// <summary>
        /// Valuable items per tier (1-2 per drop)
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, List<string>> ValuableItems = new Dictionary<TierConfig.Tier, List<string>>
        {
            // 🍒 Cherry Green Gang
            [TierConfig.Tier.TIER_CHERRY_1] = new List<string> { "meth", "baggie", "babyblue" },
            [TierConfig.Tier.TIER_CHERRY_2] = new List<string> { "meth", "liquidmeth", "bikercrank", "cheapskateboard", "combatboots" },
            [TierConfig.Tier.TIER_CHERRY_3] = new List<string> { "meth", "liquidmeth", "highqualitypseudo", "packagingstation", "trimmers" },

            // 🔴 Crimson Vultures
            [TierConfig.Tier.TIER_CRIMSON_1] = new List<string> { "cocaine", "pgr", "sourdiesel", "m1911", "m1911mag" },
            [TierConfig.Tier.TIER_CRIMSON_2] = new List<string> { "cocainebase", "mixingstationmk2", "growtent", "ledgrowlight", "phosphorus" },
            [TierConfig.Tier.TIER_CRIMSON_3] = new List<string> { "cocainebase", "goldbar", "launderingstation", "laboven", "grandfatherclock" },

            // 🟡 Golden Circle
            [TierConfig.Tier.TIER_GOLD_1] = new List<string> { "goldchain", "silverwatch", "ledgrowlight", "fullspectrumgrowlight", "liquidglass", "sourdiesel", "pgr" },
            [TierConfig.Tier.TIER_GOLD_2] = new List<string> { "goldwatch", "brickpress", "grandfatherclock", "brick", "testweed", "granddaddypurpleseed" },
            [TierConfig.Tier.TIER_GOLD_3] = new List<string> { "goldentoilet", "goldwatch", "goldbar", "mixingstationmk2", "cocainebase", "granddaddypurple", "goldenskateboard" }
        };

        /// <summary>
        /// Filler items per tier (2-3 per drop)
        /// </summary>
        public static readonly Dictionary<TierConfig.Tier, List<string>> FillerItems = new Dictionary<TierConfig.Tier, List<string>>
        {
            // 🍒 Cherry Green Gang
            [TierConfig.Tier.TIER_CHERRY_1] = new List<string> { "apron", "cap", "tshirt", "sneakers", "cuke", "chili" },
            [TierConfig.Tier.TIER_CHERRY_2] = new List<string> { "buttonup", "jeans", "mixingstation", "flashlight", "mouthwash" },
            [TierConfig.Tier.TIER_CHERRY_3] = new List<string> { "saucepan", "flatcap", "cargopants", "dryingrack" },

            // 🔴 Crimson Vultures
            [TierConfig.Tier.TIER_CRIMSON_1] = new List<string> { "fingerlessgloves", "paracetamol", "moisturepreservingpot", "soilpourer" },
            [TierConfig.Tier.TIER_CRIMSON_2] = new List<string> { "longskirt", "vest", "cowboyhat", "electrictrimmers" },
            [TierConfig.Tier.TIER_CRIMSON_3] = new List<string> { "safe", "managementclipboard", "wallclock", "moisturepreservingpot" },

            // 🟡 Golden Circle
            [TierConfig.Tier.TIER_GOLD_1] = new List<string> { "dressshoes", "lightweightskateboard", "wallmountedshelf", "granddaddypurple" },
            [TierConfig.Tier.TIER_GOLD_2] = new List<string> { "flats", "rolledbuttonup", "jukebox", "extralonglifesoil" },
            [TierConfig.Tier.TIER_GOLD_3] = new List<string> { "TV", "artworkmenace", "artworkoffer", "woodensign", "porkpiehat" }
        };

        /// <summary>
        /// Get valuable items for a tier
        /// </summary>
        public static List<string> GetValuableItems(TierConfig.Tier tier)
        {
            return ValuableItems.TryGetValue(tier, out var items) ? new List<string>(items) : new List<string>();
        }

        /// <summary>
        /// Get filler items for a tier
        /// </summary>
        public static List<string> GetFillerItems(TierConfig.Tier tier)
        {
            return FillerItems.TryGetValue(tier, out var items) ? new List<string>(items) : new List<string>();
        }

        /// <summary>
        /// Get all items for a tier (valuable + filler combined)
        /// </summary>
        public static List<string> GetAllItems(TierConfig.Tier tier)
        {
            var all = new List<string>();
            all.AddRange(GetValuableItems(tier));
            all.AddRange(GetFillerItems(tier));
            return all;
        }

        /// <summary>
        /// Get random valuable item for a tier
        /// </summary>
        public static string GetRandomValuableItem(TierConfig.Tier tier)
        {
            var items = GetValuableItems(tier);
            return items.Count > 0 ? items[UnityEngine.Random.Range(0, items.Count)] : "meth";
        }

        /// <summary>
        /// Get random filler item for a tier
        /// </summary>
        public static string GetRandomFillerItem(TierConfig.Tier tier)
        {
            var items = GetFillerItems(tier);
            return items.Count > 0 ? items[UnityEngine.Random.Range(0, items.Count)] : "tshirt";
        }

        /// <summary>
        /// Generate items for a drop package according to spec:
        /// - 1-2 valuable items
        /// - 2-3 filler items
        /// </summary>
        public static List<string> GenerateDropItems(TierConfig.Tier tier)
        {
            var items = new List<string>();

            // Add 1-2 valuable items
            var valuableCount = UnityEngine.Random.Range(1, 3); // 1 or 2
            var valuablePool = GetValuableItems(tier);
            
            for (int i = 0; i < valuableCount && valuablePool.Count > 0; i++)
            {
                var item = valuablePool[UnityEngine.Random.Range(0, valuablePool.Count)];
                items.Add(item);
                valuablePool.Remove(item); // Don't duplicate within same drop
            }

            // Add 2-3 filler items
            var fillerCount = UnityEngine.Random.Range(2, 4); // 2 or 3
            var fillerPool = GetFillerItems(tier);
            
            for (int i = 0; i < fillerCount && fillerPool.Count > 0; i++)
            {
                var item = fillerPool[UnityEngine.Random.Range(0, fillerPool.Count)];
                items.Add(item);
                fillerPool.Remove(item); // Don't duplicate within same drop
            }

            return items;
        }

        /// <summary>
        /// Check if an item is valuable for a given tier
        /// </summary>
        public static bool IsValuableItem(TierConfig.Tier tier, string itemId)
        {
            return GetValuableItems(tier).Contains(itemId);
        }

        /// <summary>
        /// Check if an item is filler for a given tier
        /// </summary>
        public static bool IsFillerItem(TierConfig.Tier tier, string itemId)
        {
            return GetFillerItems(tier).Contains(itemId);
        }

        /// <summary>
        /// Get all valid items from the game (from itemlist.txt)
        /// </summary>
        public static readonly HashSet<string> ValidItems = new HashSet<string>
        {
            "acid", "addy", "airpot", "antiquewalllamp", "apron", "artworkbeachday", "artworklines", 
            "artworkmenace", "artworkmillie", "artworkoffer", "artworkrapscallion", "babyblue", "baggie",
            "banana", "baseballbat", "battery", "bed", "belt", "bigsprinkler", "bikercrank", "blazer",
            "brick", "brickpress", "brutdugloop", "buckethat", "buttonup", "cap", "cargopants", "cash",
            "cauldron", "chateaulapeepee", "cheapskateboard", "chefhat", "chemistrystation", "chili",
            "cocaine", "cocainebase", "cocaleaf", "cocaseed", "coffeetable", "collarjacket", "combatboots",
            "cowboyhat", "cruiser", "cuke", "defaultweed", "displaycabinet", "donut", "dressshoes",
            "dryingrack", "dumpster", "electrictrimmers", "energydrink", "extralonglifesoil", "fertilizer",
            "filingcabinet", "fingerlessgloves", "flannelshirt", "flashlight", "flatcap", "flats",
            "floorlamp", "flumedicine", "fryingpan", "fullspectrumgrowlight", "gasoline", "glass", "gloves",
            "goldbar", "goldchain", "goldenskateboard", "goldentoilet", "goldwatch", "granddaddypurple",
            "granddaddypurpleseed", "grandfatherclock", "greencrack", "greencrackseed", "growtent",
            "halogengrowlight", "highqualitypseudo", "horsesemen", "iodine", "jar", "jeans", "jorts",
            "jukebox", "laboven", "largestoragerack", "launderingstation", "ledgrowlight", "legendsunglasses",
            "lightweightskateboard", "liquidbabyblue", "liquidbikercrank", "liquidglass", "liquidmeth",
            "longlifesoil", "longskirt", "lowqualitypseudo", "m1911", "m1911mag", "machete",
            "managementclipboard", "mediumstoragerack", "megabean", "metalsign", "metalsquaretable",
            "meth", "mixingstation", "mixingstationmk2", "modernwalllamp", "moisturepreservingpot",
            "motoroil", "mouthwash", "ogkush", "ogkushseed", "oldmanjimmys", "overalls", "packagingstation",
            "packagingstationmk2", "paracetamol", "pgr", "phosphorus", "plasticpot", "porkpiehat",
            "potsprinkler", "pseudo", "rectangleframeglasses", "revolver", "revolvercylinder",
            "rolledbuttonup", "safe", "sandals", "saucepan", "silverchain", "silverwatch", "skateboard",
            "skirt", "smallroundglasses", "smallstoragerack", "smalltrashcan", "sneakers", "soil",
            "soilpourer", "sourdiesel", "sourdieselseed", "speeddealershades", "speedgrow", "suspensionrack",
            "tacticalvest", "testweed", "toilet", "trashbag", "trashcan", "trashgrabber", "trimmers",
            "tshirt", "TV", "vest", "viagor", "vneck", "wallclock", "wallmountedshelf", "wateringcan",
            "woodensign", "woodsquaretable"
        };

        /// <summary>
        /// Validate that an item exists in the game
        /// </summary>
        public static bool IsValidItem(string itemId)
        {
            return ValidItems.Contains(itemId);
        }
    }
} 