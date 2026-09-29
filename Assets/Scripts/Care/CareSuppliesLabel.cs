using Content;
using Core;
using TMPro;
using UnityEngine;

namespace Care
{
    /// <summary>"Мыло ×2 · Мочалка ×1 · Щётка ×0" in the bathroom — how many care consumables are left. Updates on InventoryChangedEvent.</summary>
    public class CareSuppliesLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string[] productIds = { "soap", "washcloth", "toothbrush" };
        [SerializeField] private string separator = "   ";

        private PlayerDataService _playerData;
        private ContentDatabase _content;

        private void OnEnable()
        {
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            _content ??= ServiceLocator.Get<ContentDatabase>();
            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnInventoryChanged(InventoryChangedEvent e) => Refresh();

        private void Refresh()
        {
            if (label == null)
            {
                return;
            }

            var parts = new string[productIds.Length];
            for (int i = 0; i < productIds.Length; i++)
            {
                string title = _content.FindProduct(productIds[i])?.title ?? productIds[i];
                parts[i] = $"{title} ×{_playerData.GetInventoryCount(productIds[i])}";
            }

            label.text = string.Join(separator, parts);
        }
    }
}
