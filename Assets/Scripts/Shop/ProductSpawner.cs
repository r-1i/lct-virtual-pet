using System;
using Content;
using Core;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Spawns the 6 shelf products at their designated points. Each slot pairs a productId
    /// (must match an entry in products.json) with a spawn Transform and the 3D prefab for that
    /// product — the model/prefab isn't in the JSON since every product looks different, JSON only
    /// holds balance/text.
    /// </summary>
    public class ProductSpawner : MonoBehaviour
    {
        [Serializable]
        private class Slot
        {
            public string productId;
            public Transform spawnPoint;
            public ProductPickable prefab;
        }

        [SerializeField] private Slot[] slots;

        private void Start()
        {
            ContentDatabase content = ServiceLocator.Get<ContentDatabase>();

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
            }
        }
    }
}
