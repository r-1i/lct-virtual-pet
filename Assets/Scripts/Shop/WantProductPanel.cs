using Content;
using Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shop
{
    /// <summary>
    /// Popup for "Хочу" tab products (ProductDefinition.instantUse): name, mood preview, price +
    /// cashback, buy button. Always one piece, nothing goes to the inventory — buying applies
    /// happinessBoost to mood immediately.
    /// </summary>
    public class WantProductPanel : MonoBehaviour
    {
        private const int Quantity = 1;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text happinessLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private TMP_Text cashbackLabel;
        [Tooltip("Optional. The cashback badge (label + icon) — hidden when the cashback is 0.")]
        [SerializeField] private GameObject cashbackGroup;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private float fadeDuration = 0.2f;

        private PlayerDataService _playerData;
        private ProductDefinition _product;

        private float CashbackPercent => ServiceLocator.Get<ContentDatabase>().Economy.cashbackWantPercent;

        private void Awake()
        {
            buyButton.onClick.AddListener(HandleBuyClicked);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        private void Start()
        {
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnCoinsChanged(CoinsChangedEvent e) => RefreshPrice();

        public void Show(ProductDefinition product)
        {
            // The panel starts inactive, so the first Show comes before Start.
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            _product = product;

            titleLabel.text = product.title;
            happinessLabel.text = $"+{product.happinessBoost:0} настроения";

            RefreshPrice();

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

        private void RefreshPrice()
        {
            if (_product == null)
            {
                return;
            }

            int price = _product.GetPrice(Quantity);
            int cashback = ProductDetailPanel.Cashback(price, CashbackPercent);

            priceLabel.text = $"{price}";
            cashbackLabel.text = $"+{cashback} завтра";
            // "Хочу" gives no cashback by default (economy.json → cashbackWantPercent = 0) — hide the badge then.
            if (cashbackGroup != null)
            {
                cashbackGroup.SetActive(cashback > 0);
            }

            buyButton.interactable = _playerData != null && _playerData.Coins >= price;
        }

        private void HandleBuyClicked()
        {
            if (_product == null || _playerData == null)
            {
                return;
            }

            int price = _product.GetPrice(Quantity);

            if (!_playerData.TrySpendCoins(price))
            {
                return;
            }

            int cashback = ProductDetailPanel.Cashback(price, CashbackPercent);
            _playerData.AddPendingCashback(cashback);
            UI.Feedback.StatsSnapshot before = UI.Feedback.Snapshot(_playerData);
            _playerData.ApplyStatDelta(0f, _product.happinessBoost, 0f);
            _playerData.LogMoney(MoneyKind.BuyWant, price, _product.title, Quantity);
            // Overflow: at mood 70 a +100 item gives only +30 — show what really arrived.
            string joy = UI.Feedback.StatChanges(before, UI.Feedback.Snapshot(_playerData));
            UI.Feedback.Show(UI.Feedback.Join(UI.Feedback.Coins(-price), string.IsNullOrEmpty(joy) ? "питомец и так счастлив" : joy));

            Debug.Log($"Shop: bought {_product.title} for {price} (+{cashback} cashback, +{_product.happinessBoost} mood).");

            ShopController.Instance?.DeselectCurrent();
        }

        private void HandleCloseClicked()
        {
            ShopController.Instance?.DeselectCurrent();
        }
    }
}
