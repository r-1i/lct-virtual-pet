using System;
using Content;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>One "Разложи" row (fixed slot): icon, "Еда · надо", "нужно 20", − value +, optional "убрать". Reports clicks; the page owns the values.</summary>
    public class DistributeRowView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private HoldRepeatButton minusButton;
        [SerializeField] private HoldRepeatButton plusButton;
        [SerializeField] private TMP_Text valueLabel;
        [Tooltip("\"убрать\" — sets the row to 0. Shown only if the task has showClearButtons. Optional.")]
        [SerializeField] private Button clearButton;
        [Tooltip("Frame for required rows (\"нужное\"). Optional.")]
        [SerializeField] private GameObject requiredFrame;

        /// <summary>+1 / −1 steps (the page multiplies by row.step).</summary>
        public event Action<int> Stepped;
        public event Action Cleared;

        private void Awake()
        {
            minusButton.onStep.AddListener(() => Stepped?.Invoke(-1));
            plusButton.onStep.AddListener(() => Stepped?.Invoke(1));
            if (clearButton != null)
            {
                clearButton.onClick.AddListener(() => Cleared?.Invoke());
            }
        }

        public void Setup(StudyDistributeRow row, Sprite icon, bool showClear)
        {
            StudyViewUtils.SetSprite(iconImage, icon);
            titleLabel.text = row.title;
            StudyViewUtils.SetOptional(subtitleLabel, row.subtitle);
            StudyViewUtils.SetActive(requiredFrame, row.required);
            if (clearButton != null)
            {
                clearButton.gameObject.SetActive(showClear);
            }
        }

        public void SetValue(int value, bool canMinus, bool canPlus)
        {
            valueLabel.text = value.ToString();
            minusButton.GetComponent<Button>().interactable = canMinus;
            plusButton.GetComponent<Button>().interactable = canPlus;
            if (clearButton != null)
            {
                clearButton.interactable = value > 0;
            }
        }
    }
}
