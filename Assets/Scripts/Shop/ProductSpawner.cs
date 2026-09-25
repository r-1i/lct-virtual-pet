using System;
using System.Collections.Generic;
using Content;
using Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Shop
{
    /// <summary>
    /// Spawns the shelf products at their designated points, one slot list per shop tab
    /// ("Нужно" / "Хочу"). Each slot pairs a productId (must match an entry in products.json) with a
    /// spawn Transform and the 3D prefab for that product — the model/prefab isn't in the JSON since
    /// every product looks different, JSON only holds balance/text. Only the current tab's products
    /// are active; ShopController switches tabs via ShowTab.
    /// </summary>
    public class ProductSpawner : MonoBehaviour
    {
        public const int NeedTab = 0;
        public const int WantTab = 1;

        [Serializable]
        private class Slot
        {
            public string productId;
            public Transform spawnPoint;
            public ProductPickable prefab;
        }

        [Tooltip("Left tab: products that go to the inventory.")]
        [FormerlySerializedAs("slots")]
        [SerializeField] private Slot[] needSlots;

        [Tooltip("Right tab: instantUse products, bought one at a time, raise mood right away.")]
        [SerializeField] private Slot[] wantSlots;

        private readonly List<ProductPickable> _needInstances = new List<ProductPickable>();
        private readonly List<ProductPickable> _wantInstances = new List<ProductPickable>();
        private int _currentTab = NeedTab;

        private void Start()
        {
            ContentDatabase content = ServiceLocator.Get<ContentDatabase>();

            SpawnSlots(needSlots, content, _needInstances);
            SpawnSlots(wantSlots, content, _wantInstances);

            // ShopController may call ShowTab before or after this Start — apply whatever it asked for.
            ApplyTab();
        }

        public void ShowTab(int tab)
        {
            _currentTab = tab;
            ApplyTab();
        }

        private void ApplyTab()
        {
            foreach (ProductPickable instance in _needInstances)
            {
                instance.gameObject.SetActive(_currentTab == NeedTab);
            }

            foreach (ProductPickable instance in _wantInstances)
            {
                instance.gameObject.SetActive(_currentTab == WantTab);
            }
        }

        private static void SpawnSlots(Slot[] slots, ContentDatabase content, List<ProductPickable> instances)
        {
            if (slots == null)
            {
                return;
            }

            foreach (Slot slot in slots)
            {
                if (slot.prefab == null || slot.spawnPoint == null)
                {
                    Debug.LogWarning($"ProductSpawner: slot '{slot.productId}' is missing its prefab or spawn point.");
                    continue;
                }

                if (content.FindProduct(slot.productId) == null)
                {
                    Debug.LogWarning($"ProductSpawner: '{slot.productId}' not found in products.json.");
                    continue;
                }

                ProductPickable instance = Instantiate(slot.prefab, slot.spawnPoint.position, slot.spawnPoint.rotation, slot.spawnPoint);
                instance.Initialize(slot.productId);
                instances.Add(instance);
            }
        }
    }
}
