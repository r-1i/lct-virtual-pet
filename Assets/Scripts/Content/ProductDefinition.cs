using System;

namespace Content
{
    /// <summary>Design-time data for one shop product, loaded from Resources/Content/products.json. Read-only content, not save data.</summary>
    [Serializable]
    public class ProductDefinition
    {
        public string id;
        public string title;

        // Shelf life isn't per product: every food spoils after economy.json → foodExpiryDays, care items never.

        /// <summary>Restored on feeding, not on purchase — see PLAN.md section 4/5.</summary>
        public float happinessBoost;
        public float satietyBoost;

        /// <summary>Food: added on feeding. Care items: what one use in the shower / teeth minigame gives.</summary>
        public float healthBoost;

        /// <summary>"Нужно" tab kind: "food" (goes to the feeding table) or "care" (soap, washcloth, toothbrush — used up by the care minigames). Empty = food.</summary>
        public string category;

        public bool IsCare => category == "care";

        /// <summary>Shown on the feeding table: an inventory product that isn't a care item.</summary>
        public bool IsFood => !instantUse && !IsCare;

        /// <summary>
        /// "Хочу" tab product: never goes to the inventory, buying it applies happinessBoost to mood
        /// right away. Sold one at a time (quantities is just [1]) through WantProductPanel.
        /// </summary>
        public bool instantUse;

        /// <summary>Quantity options offered in the shop, e.g. [1, 5, 10].</summary>
        public int[] quantities;

        /// <summary>Price for each entry in quantities, same index. Cashback isn't stored, it's 10% of price computed at purchase time.</summary>
        public int[] prices;

        public int GetPrice(int quantity)
        {
            if (quantities == null || prices == null)
            {
                return 0;
            }

            for (int i = 0; i < quantities.Length && i < prices.Length; i++)
            {
                if (quantities[i] == quantity)
                {
                    return prices[i];
                }
            }

            return 0;
        }
    }

    /// <summary>JsonUtility can't parse a bare top-level array, so products.json is { "products": [ ... ] }.</summary>
    [Serializable]
    public class ProductDefinitionList
    {
        public ProductDefinition[] products;
    }
}
