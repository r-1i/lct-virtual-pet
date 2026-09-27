using System;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    /// <summary>
    /// One share row in the plan editor (Надо / Хочу / Копилка): slider + value + fine -/+ buttons.
    /// Doesn't balance anything itself — reports the wanted value through Changed, PlanEditorPanel
    /// rebalances the other two rows and sets all three back via SetValue.
    /// </summary>
    public class PlanSliderRow : MonoBehaviour
    {
        [Tooltip("Whole Numbers is forced on; min/max are driven by the editor.")]
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private HoldRepeatButton minusButton;
        [SerializeField] private HoldRepeatButton plusButton;

        public event Action<int> Changed;

        private int _value;

        private void Awake()
        {
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.onValueChanged.AddListener(v => Changed?.Invoke(Mathf.RoundToInt(v)));
            minusButton.onStep.AddListener(() => Changed?.Invoke(_value - 1));
            plusButton.onStep.AddListener(() => Changed?.Invoke(_value + 1));
        }

        /// <summary>Doesn't raise Changed.</summary>
        public void SetValue(int value, int max)
        {
            _value = value;
            slider.maxValue = Mathf.Max(1, max);
            slider.SetValueWithoutNotify(value);
            valueLabel.text = value.ToString();
        }
    }
}
