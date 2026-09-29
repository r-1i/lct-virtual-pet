using System;
using System.Collections.Generic;
using Content;
using Core;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dreams
{
    /// <summary>
    /// "Мои мечты" screen (its own Zone; showing/hiding the panel is wired up in the scene). Current
    /// goal card with jar progress and forecast, a +/- amount stepper shared by "Отложить" (wallet → jar)
    /// and "Забрать из баночки" (jar → wallet, after a confirmation that warns how far the goal moves),
    /// "Купить мечту" once the jar covers the price, and the row of all dreams to pick from.
    /// Jar/wallet numbers themselves are shown by a regular CoinsView placed on the panel.
    /// </summary>
    public class DreamsScreen : MonoBehaviour
    {
        [Serializable]
        private struct DreamIcon
        {
            public string iconKey;
            public Sprite sprite;
        }

        [Header("Current goal")]
        [Tooltip("Card with the current goal. Hidden when every dream is bought.")]
        [SerializeField] private GameObject goalRoot;
        [Tooltip("\"Все мечты исполнены!\" — shown instead of goalRoot when every dream is bought.")]
        [SerializeField] private GameObject allDoneRoot;
        [SerializeField] private Image goalIcon;
        [SerializeField] private TMP_Text goalTitleLabel;
        [SerializeField] private TMP_Text goalDescriptionLabel;
        [Tooltip("Image Type = Filled. fillAmount = jar / price.")]
        [SerializeField] private Image progressFill;
        [Tooltip("\"350/500\"")]
        [SerializeField] private TMP_Text progressLabel;
        [Tooltip("\"осталось 150\" / \"Можно покупать!\"")]
        [SerializeField] private TMP_Text remainingLabel;
        [Tooltip("\"Прогноз: ≈ 4 дня\". Hidden while nothing is planned on the finance planning screen, and once the goal is reached.")]
        [SerializeField] private TMP_Text forecastLabel;
        [Tooltip("Shown only when the jar covers the current dream.")]
        [SerializeField] private Button buyDreamButton;

        [Header("Amount")]
        [SerializeField] private HoldRepeatButton minusButton;
        [SerializeField] private HoldRepeatButton plusButton;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private int defaultAmount = 10;
        [Tooltip("Отложить: wallet → jar.")]
        [SerializeField] private Button depositButton;
        [Tooltip("Забрать из баночки: jar → wallet, after confirmation.")]
        [SerializeField] private Button withdrawButton;

        [Header("Dream list")]
        [SerializeField] private DreamCardView cardPrefab;
        [SerializeField] private RectTransform listContent;
        [Tooltip("iconKey from dreams.json → sprite. Keys with no entry keep the prefab's placeholder.")]
        [SerializeField] private DreamIcon[] icons = Array.Empty<DreamIcon>();

        [Header("Popups")]
        [SerializeField] private ConfirmDialog confirmDialog;

        [Header("Animation")]
        [SerializeField] private float progressTweenDuration = 0.4f;

        private PlayerDataService _playerData;
        private DreamService _dreams;
        private ContentDatabase _content;
        private readonly List<DreamCardView> _cards = new List<DreamCardView>();
        private int _amount;
        private string _shownDreamId;

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            _dreams = ServiceLocator.Get<DreamService>();
            _content = ServiceLocator.Get<ContentDatabase>();

            minusButton.onStep.AddListener(() => SetAmount(_amount - 1));
            plusButton.onStep.AddListener(() => SetAmount(_amount + 1));
            depositButton.onClick.AddListener(HandleDeposit);
            withdrawButton.onClick.AddListener(HandleWithdraw);
            buyDreamButton.onClick.AddListener(HandleBuyDream);

            BuildList();

            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
            EventBus.Subscribe<DreamChangedEvent>(OnDreamChanged);

            _amount = defaultAmount;
            Refresh(false);
        }

        /// <summary>Panel shown again after the first Start — snap to the current state, no tweens.</summary>
        private void OnEnable()
        {
            if (_playerData != null)
            {
                Refresh(false);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
            EventBus.Unsubscribe<DreamChangedEvent>(OnDreamChanged);
            if (progressFill != null)
            {
                progressFill.DOKill();
            }

            if (goalRoot != null)
            {
                goalRoot.transform.DOKill();
            }
        }

        private void OnCoinsChanged(CoinsChangedEvent e) => Refresh(true);
        private void OnDreamChanged(DreamChangedEvent e) => Refresh(true);

        private void BuildList()
        {
            foreach (DreamDefinition dream in _content.Dreams)
            {
                DreamCardView card = Instantiate(cardPrefab, listContent);
                card.Setup(dream, FindIcon(dream.iconKey), HandleCardClicked);
                _cards.Add(card);
            }
        }

        private void Refresh(bool animate)
        {
            animate &= isActiveAndEnabled && AnimationSettings.Enabled;

            DreamDefinition dream = _dreams.CurrentDream;
            string dreamId = dream?.id ?? "";
            bool goalChanged = dreamId != _shownDreamId;
            _shownDreamId = dreamId;

            RefreshGoal(dream, animate && !goalChanged);

            foreach (DreamCardView card in _cards)
            {
                card.SetState(card.DreamId == dreamId, _dreams.IsPurchased(card.DreamId), animate);
            }

            SetAmount(_amount);
        }

        private void RefreshGoal(DreamDefinition dream, bool animateProgress)
        {
            bool hasGoal = dream != null;
            goalRoot.SetActive(hasGoal);
            if (allDoneRoot != null)
            {
                allDoneRoot.SetActive(!hasGoal);
            }

            buyDreamButton.gameObject.SetActive(hasGoal && _dreams.CanPurchaseCurrent);

            if (!hasGoal)
            {
                return;
            }

            int jar = _playerData.JarCoins;
            int remaining = _dreams.RemainingForCurrent;

            if (goalIcon != null)
            {
                Sprite icon = FindIcon(dream.iconKey);
                if (icon != null)
                {
                    goalIcon.sprite = icon;
                }
            }

            goalTitleLabel.text = dream.title;
            if (goalDescriptionLabel != null)
            {
                goalDescriptionLabel.text = dream.description;
            }

            if (progressLabel != null)
            {
                progressLabel.text = $"{jar}/{dream.price}";
            }

            if (remainingLabel != null)
            {
                remainingLabel.text = remaining > 0 ? $"осталось {remaining}" : "Можно покупать!";
            }

            if (forecastLabel != null)
            {
                int days = 0;
                bool showForecast = remaining > 0 && _dreams.TryGetDaysFor(remaining, out days);
                forecastLabel.gameObject.SetActive(showForecast);
                if (showForecast)
                {
                    forecastLabel.text = $"Прогноз: ≈ {RuPlural.Days(days)}";
                }
            }

            float fill = dream.price > 0 ? Mathf.Clamp01(jar / (float)dream.price) : 1f;
            progressFill.DOKill();
            if (animateProgress)
            {
                progressFill.DOFillAmount(fill, progressTweenDuration).SetEase(Ease.OutCubic);
            }
            else
            {
                progressFill.fillAmount = fill;
            }
        }

        /// <summary>Clamped to 1..max(wallet, jar), so the same number works for both buttons; each button is only active if its side can cover it.</summary>
        private void SetAmount(int value)
        {
            int coins = _playerData.Coins;
            int jar = _playerData.JarCoins;
            int max = Mathf.Max(1, Mathf.Max(coins, jar));

            _amount = Mathf.Clamp(value, 1, max);
            amountLabel.text = _amount.ToString();

            minusButton.GetComponent<Button>().interactable = _amount > 1;
            plusButton.GetComponent<Button>().interactable = _amount < max;
            depositButton.interactable = coins >= _amount;
            withdrawButton.interactable = jar >= _amount;
        }

        private void HandleCardClicked(string dreamId)
        {
            _dreams.TrySelectDream(dreamId);
        }

        private void HandleDeposit()
        {
            int amount = _amount;
            if (_playerData.TryMoveCoinsToJar(amount))
            {
                Feedback.Show(Feedback.Join(Feedback.Coins(-amount), $"+{amount} в копилку"));
            }
        }

        private void HandleWithdraw()
        {
            int amount = _amount;
            if (_playerData.JarCoins < amount)
            {
                return;
            }

            confirmDialog.Show(BuildWithdrawWarning(amount), () =>
            {
                if (_playerData.TryMoveJarToCoins(amount))
                {
                    Feedback.Show(Feedback.Join($"−{amount} из копилки", Feedback.Coins(amount)));
                }
            });
        }

        private string BuildWithdrawWarning(int amount)
        {
            string coins = RuPlural.Coins(amount);

            if (_dreams.AllDreamsPurchased)
            {
                return $"Забрать {coins} из баночки?";
            }

            if (_dreams.TryGetDaysFor(amount, out int days))
            {
                return $"Если ты заберёшь сейчас {coins}, то отдалишь свою цель на {RuPlural.Days(days)}, и твой питомец будет горько плакать.";
            }

            return $"Если ты заберёшь сейчас {coins}, то отдалишь свою цель, и твой питомец будет горько плакать.";
        }

        private void HandleBuyDream()
        {
            DreamDefinition dream = _dreams.CurrentDream;
            if (dream == null || !_dreams.CanPurchaseCurrent)
            {
                return;
            }

            string message = $"Купить «{dream.title}» за {RuPlural.Coins(dream.price)}? Деньги спишутся из баночки.";
            confirmDialog.Show(message, () =>
            {
                if (_dreams.TryPurchaseCurrent())
                {
                    Feedback.Show($"Мечта сбылась: {dream.title}! · −{dream.price} из копилки");
                    Debug.Log($"Dreams: bought '{dream.id}' for {dream.price}.");
                    PlayPurchasePop();
                }
            });
        }

        private void PlayPurchasePop()
        {
            if (!AnimationSettings.Enabled)
            {
                return;
            }

            // goalRoot may already show the next dream (or be hidden, if that was the last one) — pop whichever card is visible.
            Transform target = goalRoot.activeInHierarchy ? goalRoot.transform : allDoneRoot != null ? allDoneRoot.transform : null;
            if (target == null)
            {
                return;
            }

            target.DOKill(true);
            target.DOPunchScale(Vector3.one * 0.12f, 0.4f, 6, 0.6f);
        }

        private Sprite FindIcon(string iconKey)
        {
            foreach (DreamIcon icon in icons)
            {
                if (icon.iconKey == iconKey)
                {
                    return icon.sprite;
                }
            }

            return null;
        }
    }
}
