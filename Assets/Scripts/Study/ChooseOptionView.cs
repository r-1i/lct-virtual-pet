using System;
using Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>One "Реши" option card (fixed slot): icon, "Взять у Лиса", "10 печений — 10 монет", "Выбрать".</summary>
    public class ChooseOptionView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [Tooltip("\"Выбрать\" — or a Button on the whole card.")]
        [SerializeField] private Button selectButton;

        private Action _onSelect;

        private void Awake()
        {
            selectButton.onClick.AddListener(() => _onSelect?.Invoke());
        }

        public void Setup(StudyChoiceOption option, Sprite icon, Action onSelect)
        {
            _onSelect = onSelect;
            StudyViewUtils.SetSprite(iconImage, icon);
            titleLabel.text = option.title;
            StudyViewUtils.SetOptional(subtitleLabel, option.subtitle);
        }
    }
}
