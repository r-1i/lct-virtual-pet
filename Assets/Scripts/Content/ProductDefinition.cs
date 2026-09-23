using System;

namespace Content
{
    /// <summary>Design-time data for one shop product, loaded from Resources/Content/products.json. Read-only content, not save data.</summary>
    [Serializable]
    public class ProductDefinition
    {
        public string id;
        public string title;

        /// <summary>Plain display text, e.g. "7 дней" — not computed, just shown as-is in the shop panel.</summary>
        public string expiryDate;

        /// <summary>Restored on feeding, not on purchase — see PLAN.md section 4/5.</summary>
        public float happinessBoost;
        public float satietyBoost;

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
