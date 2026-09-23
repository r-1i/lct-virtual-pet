using Content;
using Core;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Shop screen coordinator: tracks the currently selected product, drives its select/deselect
    /// animation, and feeds the shared ProductDetailPanel. ProductPickable reaches this through the
    /// static Instance when EventSystem/PhysicsRaycaster delivers it a click.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        public static ShopController Instance { get; private set; }

        [SerializeField] private ProductDetailPanel detailPanel;

        private ContentDatabase _content;
        private ProductPickable _selected;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _content = ServiceLocator.Get<ContentDatabase>();
            detailPanel.Hide(instant: true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SelectProduct(ProductPickable product)
        {
            if (_selected == product)
            {
                return;
            }

            DeselectCurrent();

            ProductDefinition definition = _content.FindProduct(product.ProductId);
            if (definition == null)
            {
                Debug.LogWarning($"Shop: no content entry for product '{product.ProductId}'.");
                return;
            }

            _selected = product;
            product.PlaySelected();
            detailPanel.Show(definition);
        }

        public void DeselectCurrent()
        {
            if (_selected == null)
            {
                return;
            }

            _selected.PlayDeselected();
            _selected = null;
            detailPanel.Hide(instant: false);
        }
    }
}
