using System.Collections.Generic;
using UnityEngine;
using PaxDrops.Configs;

namespace PaxDrops.MrStacks
{
    /// <summary>
    /// Generates ordered Mr. Stacks drops from the dedicated 3x3 menu tier config.
    /// </summary>
    public static class OrderedDropSystem
    {
        public sealed class OrderedDropItem
        {
            public string ItemId { get; }
            public int Quantity { get; }
            public bool IsValuable { get; }

            public OrderedDropItem(string itemId, int quantity, bool isValuable)
            {
                ItemId = itemId;
                Quantity = quantity;
                IsValuable = isValuable;
            }
        }

        public sealed class OrderedDropPackage
        {
            public OrderedDropConfig.OrderedTier Tier { get; set; }
            public int CashAmount { get; set; }
            public List<OrderedDropItem> Items { get; } = new List<OrderedDropItem>();

            public List<string> ToFlatList()
            {
                var flatList = new List<string>
                {
                    $"cash:{CashAmount}"
                };

                foreach (var item in Items)
                {
                    flatList.Add($"{item.ItemId}:{item.Quantity}");
                }

                return flatList;
            }

            public override string ToString()
            {
                var parts = new List<string>();
                foreach (var item in Items)
                {
                    parts.Add($"{item.ItemId} x{item.Quantity}");
                }

                return $"{OrderedDropConfig.GetTierName(Tier)} => ${CashAmount} + [{string.Join(", ", parts)}]";
            }
        }

        private static readonly HashSet<string> MultiQuantityItems = new HashSet<string>
        {
            "meth",
            "liquidmeth",
            "babyblue",
            "bikercrank",
            "highqualitypseudo",
            "cocaine",
            "pgr",
            "sourdiesel",
            "m1911mag",
            "cocainebase",
            "phosphorus",
            "liquidglass",
            "granddaddypurple",
            "brick",
            "testweed",
            "granddaddypurpleseed",
            "baggie",
            "cuke",
            "chili",
            "paracetamol",
            "mouthwash"
        };

        public static OrderedDropPackage GenerateDropPackage(OrderedDropConfig.OrderedTier tier)
        {
            var package = new OrderedDropPackage
            {
                Tier = tier,
                CashAmount = OrderedDropConfig.GetCashAmount(tier)
            };

            var valuableIds = TakeRandomDistinct(OrderedDropConfig.GetValuableItems(tier), UnityEngine.Random.Range(1, 3));
            var fillerIds = TakeRandomDistinct(OrderedDropConfig.GetFillerItems(tier), UnityEngine.Random.Range(2, 4));

            foreach (var itemId in valuableIds)
            {
                package.Items.Add(new OrderedDropItem(itemId, GetItemQuantity(itemId, true), true));
            }

            foreach (var itemId in fillerIds)
            {
                package.Items.Add(new OrderedDropItem(itemId, GetItemQuantity(itemId, false), false));
            }

            Logger.Debug($"Generated ordered Mr. Stacks package: {package}", "OrderedDropSystem");
            return package;
        }

        private static List<string> TakeRandomDistinct(IReadOnlyList<string> pool, int count)
        {
            var workingPool = new List<string>(pool);
            var selected = new List<string>();
            int targetCount = Mathf.Min(count, workingPool.Count);

            for (int i = 0; i < targetCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, workingPool.Count);
                selected.Add(workingPool[randomIndex]);
                workingPool.RemoveAt(randomIndex);
            }

            return selected;
        }

        private static int GetItemQuantity(string itemId, bool isValuable)
        {
            if (!MultiQuantityItems.Contains(itemId))
            {
                return 1;
            }

            if (isValuable)
            {
                return UnityEngine.Random.Range(1, 3);
            }

            return UnityEngine.Random.Range(1, 3);
        }
    }
}
