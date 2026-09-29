using Content;
using Core;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    /// <summary>
    /// Single shared popup for every "Нужно" product (food and care items): name, expiry date (hidden if the
    /// product has none), satiety/happiness/health tags (only the non-zero ones)
    /// (restored on feeding, not on purchase — see PLAN.md section 4/5), a 1/5/10 quantity picker
    /// made of 3 plain Buttons that tint themselves when selected, price + cashback, buy button.
    /// </summary>
    public class ProductDetailPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text expiryLabel;
        [Tooltip("Optional. Calendar icon next to the expiry text — hidden with it when the product has no expiry.")]
        [SerializeField] private GameObject expiryIcon;

        [Header("Effects — each label sits in its own tag (the label's parent), hidden when the effect is 0")]
        [SerializeField] private TMP_Text happinessLabel;
        [SerializeField] private TMP_Text satietyLabel;
        [Tooltip("Optional. Food like the large feed and care items (soap, washcloth, toothbrush) give health.")]
        [SerializeField] private TMP_Text healthLabel;
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
        private EconomySettings _economy;
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
            EnsureServices();
            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        /// <summary>The panel starts inactive, so the first Show comes before Start — fetch the services there too (Bootstrap is long done by then).</summary>
        private void EnsureServices()
        {
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            _economy ??= ServiceLocator.Get<ContentDatabase>().Economy;
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnCoinsChanged(CoinsChangedEvent e) => RefreshPrice();

        public void Show(ProductDefinition product)
        {
            EnsureServices();
            _product = product;

            titleLabel.text = product.title;

            // Only food spoils (economy.json → foodExpiryDays); care items never do.
            bool hasExpiry = product.IsFood && _economy != null && _economy.foodExpiryDays > 0;
            expiryLabel.gameObject.SetActive(hasExpiry);
            expiryLabel.text = hasExpiry ? $"Срок годности: {RuPlural.Days(_economy.foodExpiryDays)}" : "";
            if (expiryIcon != null)
            {
                expiryIcon.SetActive(hasExpiry);
            }

            SetEffect(satietyLabel, product.satietyBoost, "сытости");
            SetEffect(happinessLabel, product.happinessBoost, "счастья");
            SetEffect(healthLabel, product.healthBoost, "здоровья");

            SetQuantityCaptions(product);
            SelectQuantity(0);

            gameObject.SetActive(true);
            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeDuration);
        }

        /// <summary>"1 шт" / "3 шт" / "5 шт" on the quantity buttons, from the product's quantities; buttons past its end are hidden.</summary>
        private void SetQuantityCaptions(ProductDefinition product)
        {
            for (int i = 0; i < quantityButtons.Length; i++)
            {
                bool used = product.quantities != null && i < product.quantities.Length;
                quantityButtons[i].gameObject.SetActive(used);

                TMP_Text caption = quantityButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (used && caption != null)
                {
                    caption.text = $"{product.quantities[i]} шт";
                }
            }
        }

        /// <summary>"+30 сытости" in the label; the whole tag (label's parent) is hidden when the effect is 0.</summary>
        private static void SetEffect(TMP_Text label, float value, string statGenitive)
        {
            if (label == null)
            {
                return;
            }

            GameObject tag = label.transform.parent != null ? label.transform.parent.gameObject : label.gameObject;
            tag.SetActive(value > 0f);
            label.text = $"+{value:0} {statGenitive}";
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
            int cashback = Cashback(price, _economy?.cashbackNeedPercent ?? 0f);

            priceLabel.text = $"{price}";
            // Paid into the jar at the day change (DayService), not right away.
            cashbackLabel.text = $"+{cashback} завтра";
            buyButton.interactable = _playerData != null && _playerData.Coins >= price;
        }

        /// <summary>price × percent / 100, rounded — economy.json → cashbackNeedPercent / cashbackWantPercent.</summary>
        public static int Cashback(int price, float percent) => Mathf.RoundToInt(price * percent / 100f);

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

            int cashback = Cashback(price, _economy?.cashbackNeedPercent ?? 0f);
            _playerData.AddPendingCashback(cashback);
            _playerData.AddInventory(_product.id, quantity);
            _playerData.LogMoney(MoneyKind.BuyNeed, price, _product.title, quantity);
            Feedback.Show(Feedback.Join(Feedback.Coins(-price), $"+{quantity} {_product.title.ToLowerInvariant()}",
                cashback > 0 ? $"кешбэк +{cashback} завтра" : ""));

            Debug.Log($"Shop: bought {quantity}x {_product.title} for {price} (+{cashback} cashback).");

            ShopController.Instance?.DeselectCurrent();
        }

        private void HandleCloseClicked()
        {
            ShopController.Instance?.DeselectCurrent();
        }
    }
}
