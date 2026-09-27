using Core;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    /// <summary>
    /// Root of the "Планирование" window (its own Zone; showing the window is wired in the scene).
    /// Main panel: last result banner, "план и факт" of the running plan (or "составь план"), Barsuk
    /// deposit card, history button. The editor / history / deposit are sub-panels inside the same
    /// window — only one of the four is visible at a time; every opening of the window starts on main.
    /// </summary>
    public class PlanningScreen : MonoBehaviour
    {
        [Header("Panels (children of this window)")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private PlanEditorPanel editorPanel;
        [SerializeField] private HistoryPanel historyPanel;
        [SerializeField] private DepositPanel depositPanel;

        [Header("Last result banner")]
        [Tooltip("Hidden until the first plan is over.")]
        [SerializeField] private GameObject resultBanner;
        [SerializeField] private TMP_Text resultTitleLabel;
        [SerializeField] private TMP_Text resultSubtitleLabel;
        [Tooltip("Tap on the banner → the result popup again. Optional.")]
        [SerializeField] private Button resultBannerButton;
        [SerializeField] private PlanResultPopup resultPopup;

        [Header("Running plan")]
        [SerializeField] private GameObject planBlock;
        [Tooltip("\"План и факт · день 2 из 3\"")]
        [SerializeField] private TMP_Text planHeaderLabel;
        [SerializeField] private PlanFactRowView needRow;
        [SerializeField] private PlanFactRowView wantRow;
        [SerializeField] private PlanFactRowView jarRow;
        [Tooltip("\"Заработано 40 из 120\". Optional.")]
        [SerializeField] private TMP_Text incomeLabel;
        [Tooltip("Reward promise, or a warning if something already went wrong.")]
        [SerializeField] private TMP_Text liveHintLabel;
        [SerializeField] private Button editPlanButton;
        [SerializeField] private Button cancelPlanButton;

        [Header("No plan")]
        [SerializeField] private GameObject noPlanBlock;
        [SerializeField] private Button createPlanButton;

        [Header("Deposit card")]
        [Tooltip("\"Положи на 3 дня — вернётся +10%\"")]
        [SerializeField] private TMP_Text depositRateLabel;
        [Tooltip("\"Вложено 50 → 55 · ещё 2 дня\"")]
        [SerializeField] private TMP_Text depositStatusLabel;
        [SerializeField] private Button openDepositButton;

        [Header("History")]
        [SerializeField] private Button historyButton;

        [Header("Popups")]
        [SerializeField] private ConfirmDialog confirmDialog;

        private FinanceService _finance;
        private float _nextTick;

        private void Start()
        {
            _finance = ServiceLocator.Get<FinanceService>();

            editPlanButton.onClick.AddListener(OpenEditor);
            createPlanButton.onClick.AddListener(OpenEditor);
            cancelPlanButton.onClick.AddListener(HandleCancelPlan);
            openDepositButton.onClick.AddListener(() => ShowPanel(depositPanel.gameObject, () => depositPanel.Open(ShowMain)));
            historyButton.onClick.AddListener(() => ShowPanel(historyPanel.gameObject, () => historyPanel.Open(ShowMain)));
            if (resultBannerButton != null && resultPopup != null)
            {
                resultBannerButton.onClick.AddListener(resultPopup.ShowLastResult);
            }

            EventBus.Subscribe<FinanceChangedEvent>(OnFinanceChanged);

            ShowMain();
        }

        /// <summary>Every opening of the window starts on the main panel.</summary>
        private void OnEnable()
        {
            if (_finance != null)
            {
                _finance.CheckPeriod();
                ShowMain();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<FinanceChangedEvent>(OnFinanceChanged);
        }

        private void OnFinanceChanged(FinanceChangedEvent e) => Refresh();

        /// <summary>The deposit countdown ticks once a second.</summary>
        private void Update()
        {
            if (_finance == null || !mainPanel.activeSelf || Time.unscaledTime < _nextTick)
            {
                return;
            }

            _nextTick = Time.unscaledTime + 1f;
            RefreshDeposit();
        }

        public void ShowMain() => ShowPanel(mainPanel, Refresh);

        private void OpenEditor() => ShowPanel(editorPanel.gameObject, () => editorPanel.Open(ShowMain));

        private void ShowPanel(GameObject panel, System.Action afterShow)
        {
            mainPanel.SetActive(panel == mainPanel);
            editorPanel.gameObject.SetActive(panel == editorPanel.gameObject);
            historyPanel.gameObject.SetActive(panel == historyPanel.gameObject);
            depositPanel.gameObject.SetActive(panel == depositPanel.gameObject);
            afterShow?.Invoke();
        }

        private void Refresh()
        {
            if (_finance == null || !mainPanel.activeSelf)
            {
                return;
            }

            RefreshResultBanner();
            RefreshPlan();
            RefreshDeposit();
        }

        private void RefreshResultBanner()
        {
            PlanResult result = _finance.LastResult;
            resultBanner.SetActive(result.exists);
            if (!result.exists)
            {
                return;
            }

            resultTitleLabel.text = result.success ? "План сошёлся с фактом!" : "План не сошёлся";
            resultSubtitleLabel.text = result.success
                ? $"Барсук доволен: награда +{RuPlural.Coins(result.reward)}"
                : "Нажми, чтобы узнать почему";
        }

        private void RefreshPlan()
        {
            bool hasPlan = _finance.HasPlan;
            planBlock.SetActive(hasPlan);
            noPlanBlock.SetActive(!hasPlan);
            if (!hasPlan)
            {
                return;
            }

            FinancePlan plan = _finance.Plan;
            PlanFacts facts = _finance.CurrentFacts;

            planHeaderLabel.text = plan.days > 1
                ? $"План и факт · день {_finance.CurrentPlanDay} из {plan.days}"
                : "План и факт · сегодня";

            needRow.Set(plan.need, facts.need, PlanRule.AtLeast, false);
            wantRow.Set(plan.want, facts.want, PlanRule.AtMost, false);
            jarRow.Set(plan.jar, facts.JarNet, PlanRule.AtLeast, false);

            if (incomeLabel != null)
            {
                incomeLabel.text = $"Заработано {facts.income} из {plan.income}";
            }

            if (liveHintLabel != null)
            {
                liveHintLabel.text = _finance.BuildLiveHint();
            }
        }

        private void RefreshDeposit()
        {
            int maxDays = _finance.Settings.depositMaxDays;
            if (depositRateLabel != null)
            {
                depositRateLabel.text = $"Положи на {RuPlural.Days(maxDays)} — вернётся +{_finance.RatePercent(maxDays)}%";
            }

            if (depositStatusLabel == null)
            {
                return;
            }

            if (!_finance.HasDeposit)
            {
                depositStatusLabel.text = "Сейчас ничего не вложено";
                return;
            }

            BankDeposit deposit = _finance.Deposit;
            string when = _finance.IsDepositMature ? "можно забрать!" : FinanceFormat.Remaining(_finance.DepositRemainingSeconds);
            depositStatusLabel.text = $"Вложено {deposit.amount} → {deposit.amount + deposit.bonus} · {when}";
        }

        private void HandleCancelPlan()
        {
            confirmDialog.Show("Отменить план? Награду за него уже не получишь.", () => _finance.CancelPlan());
        }
    }
}
