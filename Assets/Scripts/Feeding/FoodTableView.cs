using System;
using System.Collections.Generic;
using System.Linq;
using Content;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Feeding
{
    /// <summary>
    /// The table in front of the character: every owned product shown as one 3D pile in a row
    /// (batches of the same product are grouped for display — see PlayerDataService.GetInventoryCount
    /// / GetOwnedProductIds). Left/Right shift the row by one product at a time when there are more
    /// owned products than visible slots. Rebuilds on InventoryChangedEvent (a feed can empty a pile
    /// and shrink the row, or a shop purchase can grow it).
    /// </summary>
    public class FoodTableView : MonoBehaviour
    {
        [Header("Layout — only 2 points needed, the rest is computed")]
        [Tooltip("World position of slot 1 (leftmost pile).")]
        [SerializeField] private Transform originPoint;
        [Tooltip("World position of slot 2. Only its distance from Origin Point matters — that becomes the per-slot spacing, repeated for slots 3..n. Move this one to tune spacing; the others follow automatically.")]
        [SerializeField] private Transform secondPoint;
        [Tooltip("How many piles are visible at once.")]
        [SerializeField, Min(1)] private int visibleSlotCount = 3;

        [Serializable]
        private class PileEntry
        {
            public string productId;
            [Tooltip("Must match the product's real model/FBX and already have a Collider + FoodPileView component on it — same idea as Shop/ProductSpawner's slots.")]
            public FoodPileView prefab;
        }

        [Tooltip("productId → pile prefab, one entry per food product in products.json (category food).")]
        [SerializeField] private PileEntry[] pilePrefabs;

        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private CharacterFeeding character;

        private ContentDatabase _content;
        private PlayerDataService _playerData;
        private List<string> _ownedProductIds = new List<string>();
        private int _pageOffset;
        private readonly List<FoodPileView> _spawned = new List<FoodPileView>();

        private void Awake()
        {
            leftButton.onClick.AddListener(PagePrev);
            rightButton.onClick.AddListener(PageNext);
        }

        private void Start()
        {
            _content = ServiceLocator.Get<ContentDatabase>();
            _playerData = ServiceLocator.Get<PlayerDataService>();

            EventBus.Subscribe<InventoryChangedEvent>(OnInventoryChanged);

            Rebuild();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<InventoryChangedEvent>(OnInventoryChanged);
        }

        private void OnInventoryChanged(InventoryChangedEvent e) => Rebuild();

        private void PagePrev()
        {
            _pageOffset = Mathf.Max(0, _pageOffset - 1);
            Layout();
        }

        private void PageNext()
        {
            _pageOffset = Mathf.Min(MaxOffset, _pageOffset + 1);
            Layout();
        }

        private int MaxOffset => Mathf.Max(0, _ownedProductIds.Count - visibleSlotCount);

        /// <summary>Slot i's world position: origin + (second - origin) * i.</summary>
        private Vector3 SlotPosition(int index)
        {
            Vector3 origin = originPoint.position;
            Vector3 step = secondPoint.position - origin;
            return origin + step * index;
        }

        private FoodPileView FindPilePrefab(string productId)
        {
            foreach (PileEntry entry in pilePrefabs)
            {
                if (entry.productId == productId)
                {
                    return entry.prefab;
                }
            }

            return null;
        }

        /// <summary>Re-reads what food is owned (care items like soap stay off the table; order kept stable by sorting on productId) and re-clamps the page.</summary>
        private void Rebuild()
        {
            _ownedProductIds = _playerData.GetOwnedProductIds()
                .Where(id => _content.FindProduct(id)?.IsFood == true)
                .OrderBy(id => id)
                .ToList();
            _pageOffset = Mathf.Clamp(_pageOffset, 0, MaxOffset);
            Layout();
        }

        /// <summary>Spawns the piles for the current page without touching _ownedProductIds/_pageOffset.</summary>
        private void Layout()
        {
            foreach (FoodPileView pile in _spawned)
            {
                if (pile != null)
                {
                    Destroy(pile.gameObject);
                }
            }

            _spawned.Clear();

            for (int i = 0; i < visibleSlotCount; i++)
            {
                int productIndex = _pageOffset + i;
                if (productIndex >= _ownedProductIds.Count)
                {
                    break;
                }

                string productId = _ownedProductIds[productIndex];
                ProductDefinition definition = _content.FindProduct(productId);
                if (definition == null)
                {
                    Debug.LogWarning($"FoodTableView: '{productId}' not found in products.json.");
                    continue;
                }

                FoodPileView prefab = FindPilePrefab(productId);
                if (prefab == null)
                {
                    Debug.LogWarning($"FoodTableView: no pile prefab mapped for '{productId}'.");
                    continue;
                }

                int count = _playerData.GetInventoryCount(productId);

                FoodPileView pile = Instantiate(prefab, SlotPosition(i), originPoint.rotation, transform);
                pile.Setup(definition, count, _playerData, character);
                _spawned.Add(pile);
            }

            leftButton.interactable = _pageOffset > 0;
            rightButton.interactable = _pageOffset < MaxOffset;
        }

        private void OnDrawGizmosSelected()
        {
            if (originPoint == null || secondPoint == null)
            {
                return;
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < Mathf.Max(visibleSlotCount, 1); i++)
            {
                Gizmos.DrawWireSphere(SlotPosition(i), 0.08f);
            }
        }
    }
}
