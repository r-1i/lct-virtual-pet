using System;
using Core;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    /// <summary>
    /// "Вклад у Барсука" sub-panel. No deposit: amount (+/- with hold, depositMinAmount..wallet),
    /// term (1..depositMaxDays, default max), preview of what comes back, "Положить". Active deposit:
    /// amount → amount + bonus, countdown, "Забрать" once matured, "Забрать раньше" (confirm, no bonus).
    /// </summary>
    public class DepositPanel : MonoBehaviour
    {
        [SerializeField] private Button backButton;

        [Header("Open a deposit")]
        [SerializeField] private GameObject openGroup;
        [SerializeField] private HoldRepeatButton amountMinusButton;
        [SerializeField] private HoldRepeatButton amountPlusButton;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private Button daysMinusButton;
        [SerializeField] private Button daysPlusButton;
        [SerializeField] private TMP_Text daysLabel;
        [Tooltip("\"Вернётся 55 (+5, 10%) через 3 дня\"")]
        [SerializeField] private TMP_Text previewLabel;
        [SerializeField] private Button openButton;

        [Header("Active deposit")]
        [SerializeField] private GameObject activeGroup;
        [Tooltip("\"Вложено 50 → вернётся 55\"")]
        [SerializeField] private TMP_Text activeAmountLabel;
        [Tooltip("\"ещё 2 дня\" / \"Можно забирать!\"")]
        [SerializeField] private TMP_Text activeTimeLabel;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button earlyWithdrawButton;

        [Header("Popups")]
        [SerializeField] private ConfirmDialog confirmDialog;

        private FinanceService _finance;
        private PlayerDataService _playerData;
        private Action _onClose;
        private int _amount;
        private int _days;
        private float _nextTick;

        private void Awake()
        {
            backButton.onClick.AddListener(() => _onClose?.Invoke());
            amountMinusButton.onStep.AddListener(() => SetAmount(_amount - 1));
            amountPlusButton.onStep.AddListener(() => SetAmount(_amount + 1));
            daysMinusButton.onClick.AddListener(() => SetDays(_days - 1));
            daysPlusButton.onClick.AddListener(() => SetDays(_days + 1));
            openButton.onClick.AddListener(HandleOpen);
            collectButton.onClick.AddListener(HandleCollect);
            earlyWithdrawButton.onClick.AddListener(HandleEarlyWithdraw);
        }

        // Only matters while the panel is open, and it's only ever opened at runtime (after Bootstrap cleared the bus).
        private void OnEnable()
        {
            EventBus.Subscribe<FinanceChangedEvent>(OnFinanceChanged);
            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FinanceChangedEvent>(OnFinanceChanged);
            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        /// <summary>Called by PlanningScreen right after activating this panel.</summary>
        public void Open(Action onClose)
        {
            _finance ??= ServiceLocator.Get<FinanceService>();
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            _onClose = onClose;

            _amount = _finance.Settings.depositMinAmount;
            _days = _finance.Settings.depositMaxDays;
            Refresh();
        }

        private void OnFinanceChanged(FinanceChangedEvent e) => Refresh();
        private void OnCoinsChanged(CoinsChangedEvent e) => Refresh();

        private void Update()
        {
            if (_finance == null || !_finance.HasDeposit || Time.unscaledTime < _nextTick)
            {
                return;
            }

            _nextTick = Time.unscaledTime + 1f;
            RefreshActive();
        }

        private void Refresh()
        {
            if (_finance == null)
            {
                return;
            }

            bool hasDeposit = _finance.HasDeposit;
            openGroup.SetActive(!hasDeposit);
            activeGroup.SetActive(hasDeposit);

            if (hasDeposit)
            {
                RefreshActive();
            }
            else
            {
                SetDays(_days);
            }
        }

        private void SetAmount(int value)
        {
            int min = _finance.Settings.depositMinAmount;
            int max = Mathf.Max(min, _playerData.Coins);
            _amount = Mathf.Clamp(value, min, max);

            amountLabel.text = _amount.ToString();
            amountMinusButton.GetComponent<Button>().interactable = _amount > min;
            amountPlusButton.GetComponent<Button>().interactable = _amount < max;
            RefreshPreview();
        }

        private void SetDays(int value)
        {
            int max = _finance.Settings.depositMaxDays;
            _days = Mathf.Clamp(value, 1, max);

            daysLabel.text = RuPlural.Days(_days);
            daysMinusButton.interactable = _days > 1;
            daysPlusButton.interactable = _days < max;
            SetAmount(_amount);
        }

        private void RefreshPreview()
        {
            bool enough = _playerData.Coins >= _amount;
            openButton.interactable = enough;

            if (!enough)
            {
                previewLabel.text = $"Нужно хотя бы {RuPlural.Coins(_finance.Settings.depositMinAmount)} в кошельке";
                return;
            }

            int bonus = _finance.BonusFor(_amount, _days);
            previewLabel.text = $"Вернётся {_amount + bonus} (+{bonus}, {_finance.RatePercent(_days)}%) через {RuPlural.Days(_days)}";
        }

        private void RefreshActive()
        {
            BankDeposit deposit = _finance.Deposit;
            bool mature = _finance.IsDepositMature;

            activeAmountLabel.text = $"Вложено {deposit.amount} → вернётся {deposit.amount + deposit.bonus}";
            activeTimeLabel.text = mature ? "Можно забирать!" : FinanceFormat.Remaining(_finance.DepositRemainingSeconds);
            collectButton.interactable = mature;
            earlyWithdrawButton.gameObject.SetActive(!mature);
        }

        private void HandleOpen()
        {
            int amount = _amount, days = _days;
            if (!_finance.TryOpenDeposit(amount, days, out string error))
            {
                Feedback.Show(error);
                Debug.LogWarning($"Deposit: {error}");
                return;
            }

            Feedback.Show(Feedback.Join(Feedback.Coins(-amount), $"вклад у Барсука на {RuPlural.Days(days)}"));
        }

        private void HandleCollect()
        {
            BankDeposit deposit = _finance.Deposit;
            int amount = deposit.amount, bonus = deposit.bonus;
            if (_finance.TryCollectDeposit())
            {
                Feedback.Show(Feedback.Join(Feedback.Coins(amount + bonus), $"из них проценты +{bonus}"));
            }
        }

        private void HandleEarlyWithdraw()
        {
            BankDeposit deposit = _finance.Deposit;
            int amount = deposit.amount;
            string message = $"Если забрать сейчас, Барсук вернёт только {RuPlural.Coins(amount)}, без +{deposit.bonus}. Забрать?";
            confirmDialog.Show(message, () =>
            {
                if (_finance.TryWithdrawDepositEarly())
                {
                    Feedback.Show(Feedback.Join(Feedback.Coins(amount), "без процентов"));
                }
            });
        }
    }
}
