using Content;
using Core;
using UI;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// Shop screen coordinator: switches the "Нужно | Хочу" tab (SegmentedToggle → ProductSpawner),
    /// tracks the currently selected product, drives its select/deselect animation, and feeds the
    /// matching panel — ProductDetailPanel for regular products, WantProductPanel for instantUse ones.
    /// ProductPickable reaches this through the static Instance when EventSystem/PhysicsRaycaster
    /// delivers it a click.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        public static ShopController Instance { get; private set; }

        [SerializeField] private ProductDetailPanel detailPanel;
        [SerializeField] private WantProductPanel wantPanel;
        [SerializeField] private ProductSpawner spawner;
        [SerializeField] private SegmentedToggle tabToggle;

        private ContentDatabase _content;
        private ProductPickable _selected;
        private bool _selectedIsWant;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            _content = ServiceLocator.Get<ContentDatabase>();
            detailPanel.Hide(instant: true);
            wantPanel.Hide(instant: true);

            // The left tab is the one available at start.
            tabToggle.SelectImmediate(ProductSpawner.NeedTab);
            spawner.ShowTab(ProductSpawner.NeedTab);
            tabToggle.onChanged.AddListener(OnTabChanged);
        }

        private void OnDestroy()
        {
            if (tabToggle != null)
            {
                tabToggle.onChanged.RemoveListener(OnTabChanged);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnTabChanged(int tab)
        {
            DeselectCurrent();
            spawner.ShowTab(tab);
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
            _selectedIsWant = definition.instantUse;
            product.PlaySelected();

            if (_selectedIsWant)
            {
                wantPanel.Show(definition);
            }
            else
            {
                detailPanel.Show(definition);
            }
        }

        public void DeselectCurrent()
        {
            if (_selected == null)
            {
                return;
            }

            _selected.PlayDeselected();
            _selected = null;

            if (_selectedIsWant)
            {
                wantPanel.Hide(instant: false);
            }
            else
            {
                detailPanel.Hide(instant: false);
            }
        }
    }
}
