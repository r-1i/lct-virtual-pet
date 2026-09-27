using Content;
using Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    /// <summary>
    /// Single shared popup for every product: name, expiry date, happiness/satiety preview
    /// (restored on feeding, not on purchase — see PLAN.md section 4/5), a 1/5/10 quantity picker
    /// made of 3 plain Buttons that tint themselves when selected, price + cashback, buy button.
    /// </summary>
    public class ProductDetailPanel : MonoBehaviour
    {
        public const float CashbackRate = 0.1f;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text expiryLabel;
        [SerializeField] private TMP_Text happinessLabel;
        [SerializeField] private TMP_Text satietyLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private TMP_Text cashbackLabel;

        [Header("Quantity — 3 buttons, same order as each product's quantities array (1/5/10)")]
        [SerializeField] private Button[] quantityButtons;
        [SerializeField] private Color quantitySelectedColor = new Color(0.25f, 0.7f, 0.3f);
        [SerializeField] private Color quantityUnselectedColor = Color.white;

        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private float fadeDuration = 0.2f;

        private PlayerDataService _playerData;
        private ProductDefinition _product;
        private int _selectedQuantityIndex;

        private void Awake()
        {
            buyButton.onClick.AddListener(HandleBuyClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);

            for (int i = 0; i < quantityButtons.Length; i++)
            {
                int index = i; // capture for the closure
                quantityButtons[i].onClick.AddListener(() => SelectQuantity(index));
            }
        }

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnCoinsChanged(CoinsChangedEvent e) => RefreshPrice();

        public void Show(ProductDefinition product)
        {
            _product = product;

            titleLabel.text = product.title;
            expiryLabel.text = product.expiryDate;
            happinessLabel.text = $"+{product.happinessBoost:0} счастья";
            satietyLabel.text = $"+{product.satietyBoost:0} сытости";

            SelectQuantity(0);

            gameObject.SetActive(true);
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration);
        }

        public void Hide(bool instant)
        {
            _product = null;

            if (instant)
            {
                gameObject.SetActive(false);
                return;
            }

            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration).OnComplete(() => gameObject.SetActive(false));
        }

        private void SelectQuantity(int index)
        {
            _selectedQuantityIndex = index;

            for (int i = 0; i < quantityButtons.Length; i++)
            {
                if (quantityButtons[i].targetGraphic != null)
                {
                    quantityButtons[i].targetGraphic.color = i == index ? quantitySelectedColor : quantityUnselectedColor;
                }
            }

            RefreshPrice();
        }

        private void RefreshPrice()
        {
            if (_product == null)
            {
                return;
            }

            int quantity = _product.quantities[_selectedQuantityIndex];
            int price = _product.GetPrice(quantity);
            int cashback = Mathf.RoundToInt(price * CashbackRate);

            priceLabel.text = $"{price}";
            cashbackLabel.text = $"+{cashback}";
            buyButton.interactable = _playerData != null && _playerData.Coins >= price;
        }

        private void HandleBuyClicked()
        {
            if (_product == null || _playerData == null)
            {
                return;
            }

            int quantity = _product.quantities[_selectedQuantityIndex];
            int price = _product.GetPrice(quantity);

            if (!_playerData.TrySpendCoins(price))
            {
                return;
            }

            int cashback = Mathf.RoundToInt(price * CashbackRate);
            _playerData.AddJarCoins(cashback);
            _playerData.AddInventory(_product.id, quantity);
            _playerData.LogMoney(MoneyKind.BuyNeed, price, _product.title, quantity);
            _playerData.LogMoney(MoneyKind.Cashback, cashback, _product.title);

            Debug.Log($"Shop: bought {quantity}x {_product.title} for {price} (+{cashback} cashback).");

            ShopController.Instance?.DeselectCurrent();
        }

        private void HandleCloseClicked()
        {
            ShopController.Instance?.DeselectCurrent();
        }
    }
}
