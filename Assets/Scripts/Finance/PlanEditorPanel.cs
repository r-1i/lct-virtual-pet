using System;
using Core;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    /// <summary>
    /// Plan editor sub-panel: days (1..maxPlanDays), planned income, and three sliders that always
    /// split the whole income — moving one rescales the other two proportionally to their current
    /// values (equally if both are 0). Changing the income rescales all three. Opens with the active
    /// plan (editing), else the last finished plan, else the defaults below.
    /// </summary>
    public class PlanEditorPanel : MonoBehaviour
    {
        private const int Need = 0;
        private const int Want = 1;
        private const int Jar = 2;
        private const int MaxIncome = 9999;

        [SerializeField] private Button backButton;
        [Tooltip("\"Новый план\" / \"Изменить план\". Optional.")]
        [SerializeField] private TMP_Text titleLabel;

        [Header("Days")]
        [SerializeField] private Button daysMinusButton;
        [SerializeField] private Button daysPlusButton;
        [SerializeField] private TMP_Text daysLabel;

        [Header("Income")]
        [SerializeField] private HoldRepeatButton incomeMinusButton;
        [SerializeField] private HoldRepeatButton incomePlusButton;
        [SerializeField] private TMP_Text incomeLabel;
        [SerializeField] private int incomeStep = 5;
        [Tooltip("\"Работы твоего уровня приносят от 40 до 160 за раз\". Optional.")]
        [SerializeField] private TMP_Text incomeHintLabel;

        [Header("Split")]
        [SerializeField] private PlanSliderRow needRow;
        [SerializeField] private PlanSliderRow wantRow;
        [SerializeField] private PlanSliderRow jarRow;
        [Tooltip("\"В копилку ≈ 7 в день\". Optional.")]
        [SerializeField] private TMP_Text jarPerDayLabel;

        [Header("Save")]
        [SerializeField] private Button saveButton;
        [Tooltip("Validation error from FinanceService. Optional.")]
        [SerializeField] private TMP_Text errorLabel;

        [Header("Defaults (no previous plan)")]
        [SerializeField] private int defaultIncome = 100;
        [SerializeField, Range(0, 100)] private int defaultNeedPercent = 60;
        [SerializeField, Range(0, 100)] private int defaultWantPercent = 20;

        private FinanceService _finance;
        private Action _onClose;
        private readonly int[] _values = new int[3];
        private int _income;
        private int _days;
        private int _minDays;

        private void Awake()
        {
            backButton.onClick.AddListener(Close);
            daysMinusButton.onClick.AddListener(() => SetDays(_days - 1));
            daysPlusButton.onClick.AddListener(() => SetDays(_days + 1));
            incomeMinusButton.onStep.AddListener(() => SetIncome(_income - incomeStep));
            incomePlusButton.onStep.AddListener(() => SetIncome(_income + incomeStep));
            needRow.Changed += v => SetShare(Need, v);
            wantRow.Changed += v => SetShare(Want, v);
            jarRow.Changed += v => SetShare(Jar, v);
            saveButton.onClick.AddListener(HandleSave);
        }

        /// <summary>Called by PlanningScreen right after activating this panel.</summary>
        public void Open(Action onClose)
        {
            _finance ??= ServiceLocator.Get<FinanceService>();
            _onClose = onClose;

            bool editing = _finance.HasPlan;
            FinancePlan source = editing ? _finance.Plan : _finance.LastResult.exists ? _finance.LastResult.plan : null;

            if (titleLabel != null)
            {
                titleLabel.text = editing ? "Изменить план" : "Новый план";
            }

            _minDays = editing ? _finance.CurrentPlanDay : 1;

            if (source != null && source.income > 0)
            {
                _days = source.days;
                _income = source.income;
                _values[Need] = source.need;
                _values[Want] = source.want;
                _values[Jar] = source.jar;
            }
            else
            {
                _days = 1;
                _income = defaultIncome;
                _values[Need] = Mathf.RoundToInt(defaultIncome * defaultNeedPercent / 100f);
                _values[Want] = Mathf.Min(defaultIncome - _values[Need], Mathf.RoundToInt(defaultIncome * defaultWantPercent / 100f));
                _values[Jar] = defaultIncome - _values[Need] - _values[Want];
            }

            if (incomeHintLabel != null)
            {
                incomeHintLabel.text = _finance.TryGetJobRewardRange(out int min, out int max)
                    ? (min == max ? $"Работы твоего уровня приносят {min} за раз" : $"Работы твоего уровня приносят от {min} до {max} за раз")
                    : "";
            }

            SetError("");
            SetDays(_days);
            RefreshShares();
        }

        private void SetDays(int days)
        {
            _days = Mathf.Clamp(days, _minDays, _finance.Settings.maxPlanDays);
            daysLabel.text = RuPlural.Days(_days);
            daysMinusButton.interactable = _days > _minDays;
            daysPlusButton.interactable = _days < _finance.Settings.maxPlanDays;
            RefreshJarPerDay();
        }

        /// <summary>New total, all three shares rescaled to keep their proportions.</summary>
        private void SetIncome(int income)
        {
            income = Mathf.Clamp(income, incomeStep, MaxIncome);
            int oldTotal = _values[Need] + _values[Want] + _values[Jar];

            if (oldTotal <= 0)
            {
                _values[Need] = income;
                _values[Want] = 0;
            }
            else
            {
                _values[Need] = Mathf.RoundToInt(income * _values[Need] / (float)oldTotal);
                _values[Want] = Mathf.Min(income - _values[Need], Mathf.RoundToInt(income * _values[Want] / (float)oldTotal));
            }

            _values[Jar] = income - _values[Need] - _values[Want];
            _income = income;
            RefreshShares();
        }

        /// <summary>One share set by the player; the rest of the income is split between the other two proportionally to their current values.</summary>
        private void SetShare(int index, int value)
        {
            value = Mathf.Clamp(value, 0, _income);
            int a = (index + 1) % 3;
            int b = (index + 2) % 3;
            int rest = _income - value;
            int othersSum = _values[a] + _values[b];

            int valueA = othersSum > 0
                ? Mathf.RoundToInt(rest * _values[a] / (float)othersSum)
                : rest / 2;
            valueA = Mathf.Clamp(valueA, 0, rest);

            _values[index] = value;
            _values[a] = valueA;
            _values[b] = rest - valueA;
            RefreshShares();
        }

        private void RefreshShares()
        {
            incomeLabel.text = _income.ToString();
            incomeMinusButton.GetComponent<Button>().interactable = _income > incomeStep;
            incomePlusButton.GetComponent<Button>().interactable = _income < MaxIncome;

            needRow.SetValue(_values[Need], _income);
            wantRow.SetValue(_values[Want], _income);
            jarRow.SetValue(_values[Jar], _income);
            RefreshJarPerDay();
        }

        private void RefreshJarPerDay()
        {
            if (jarPerDayLabel != null)
            {
                jarPerDayLabel.text = $"В копилку ≈ {Mathf.RoundToInt(_values[Jar] / (float)Mathf.Max(1, _days))} в день";
            }
        }

        private void HandleSave()
        {
            if (_finance.TrySavePlan(_days, _income, _values[Need], _values[Want], _values[Jar], out string error))
            {
                Close();
            }
            else
            {
                SetError(error);
            }
        }

        private void SetError(string text)
        {
            if (errorLabel != null)
            {
                errorLabel.text = text;
                errorLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
            }
        }

        private void Close()
        {
            _onClose?.Invoke();
        }
    }
}
