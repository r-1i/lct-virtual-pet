using System.Collections.Generic;
using System.Linq;
using Content;
using Core;

namespace Care
{
    /// <summary>
    /// Care consumables (products.json, category "care"): the shower uses 1 soap + 1 washcloth, teeth brushing
    /// 1 toothbrush. One round gives the sum of the used items' healthBoost. Checked when entering a minigame
    /// (Home/ZoneEntryPoint), taken when it's completed — leaving halfway costs nothing.
    /// </summary>
    public static class CareSupplies
    {
        public static readonly string[] Shower = { "soap", "washcloth" };
        public static readonly string[] Teeth = { "toothbrush" };

        public static bool HasAll(PlayerDataService playerData, IEnumerable<string> productIds) =>
            productIds.All(id => playerData.GetInventoryCount(id) > 0);

        /// <summary>"мыло и мочалка" — the missing ones, for the "купи в магазине" prompt.</summary>
        public static string MissingNames(PlayerDataService playerData, ContentDatabase content, IEnumerable<string> productIds)
        {
            List<string> names = productIds
                .Where(id => playerData.GetInventoryCount(id) <= 0)
                .Select(id => content.FindProduct(id)?.title.ToLowerInvariant() ?? id)
                .ToList();
            return names.Count <= 1 ? names.FirstOrDefault() ?? "" : string.Join(", ", names.Take(names.Count - 1)) + " и " + names.Last();
        }

        /// <summary>Takes one of each (whatever is there) and returns the health they give together.</summary>
        public static float Consume(PlayerDataService playerData, ContentDatabase content, IEnumerable<string> productIds)
        {
            float health = 0f;
            foreach (string id in productIds)
            {
                if (playerData.TryConsumeInventory(id, 1))
                {
                    health += content.FindProduct(id)?.healthBoost ?? 0f;
                }
            }

            return health;
        }
    }
}
