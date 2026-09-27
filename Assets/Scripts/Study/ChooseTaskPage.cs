using System;
using Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>
    /// "Реши" task page: character + speech, optional situation, optional helper button that reveals
    /// a hint ("Посчитать за штуку"), question, fixed option slots (extra hidden), footer hint.
    /// Picking an option submits right away.
    /// </summary>
    public class ChooseTaskPage : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [Tooltip("\"Задание: рынок\"")]
        [SerializeField] private TMP_Text headerLabel;

        [Header("Character")]
        [SerializeField] private Image speakerImage;
        [SerializeField] private TMP_Text speakerNameLabel;
        [SerializeField] private TMP_Text speechLabel;
        [Tooltip("Situation block (\"У тебя 10 монет…\"). Hidden when empty. Optional.")]
        [SerializeField] private TMP_Text conditionLabel;
        [Tooltip("Container of the situation block — hidden together with it. Optional.")]
        [SerializeField] private GameObject conditionRoot;

        [Header("Helper (\"Посчитать за штуку\")")]
        [Tooltip("Hidden when the task has no helperButton.")]
        [SerializeField] private Button helperButton;
        [SerializeField] private TMP_Text helperButtonLabel;
        [Tooltip("Revealed by the helper button.")]
        [SerializeField] private TMP_Text helperTextLabel;

        [Header("Options")]
        [Tooltip("\"Что купишь?\" Hidden when empty. Optional.")]
        [SerializeField] private TMP_Text questionLabel;
        [SerializeField] private ChooseOptionView[] optionSlots = Array.Empty<ChooseOptionView>();
        [Tooltip("\"Подумай: …\" Hidden when empty. Optional.")]
        [SerializeField] private TMP_Text footerHintLabel;
        [Tooltip("Background plate of the footer hint — hidden together with it. Optional.")]
        [SerializeField] private GameObject footerHintRoot;

        private Action _onBack;
        private Action<int> _onSubmit;

        private void Awake()
        {
            backButton.onClick.AddListener(() => _onBack?.Invoke());
            if (helperButton != null)
            {
                helperButton.onClick.AddListener(ShowHelper);
            }
        }

        /// <summary>Called by StudyScreen right after activating this page.</summary>
        public void Open(StudyTaskDefinition task, Func<string, Sprite> sprites, Action onBack, Action<int> onSubmit)
        {
            _onBack = onBack;
            _onSubmit = onSubmit;

            headerLabel.text = task.header;
            StudyViewUtils.SetSprite(speakerImage, sprites(task.speaker));
            StudyViewUtils.SetText(speakerNameLabel, task.speakerName);
            speechLabel.text = task.speech;
            StudyViewUtils.SetOptional(conditionLabel, task.condition, conditionRoot);
            StudyViewUtils.SetOptional(questionLabel, task.question);
            StudyViewUtils.SetOptional(footerHintLabel, task.footerHint, footerHintRoot);

            bool hasHelper = !string.IsNullOrEmpty(task.helperButton);
            if (helperButton != null)
            {
                helperButton.gameObject.SetActive(hasHelper);
                helperButton.interactable = true;
            }

            StudyViewUtils.SetText(helperButtonLabel, task.helperButton);
            StudyViewUtils.SetText(helperTextLabel, task.helperText);
            StudyViewUtils.SetActive(helperTextLabel != null ? helperTextLabel.gameObject : null, false);

            if (task.options.Length > optionSlots.Length)
            {
                Debug.LogWarning($"ChooseTaskPage: task '{task.id}' has {task.options.Length} options but only {optionSlots.Length} slots. Add slots.");
            }

            for (int i = 0; i < optionSlots.Length; i++)
            {
                bool used = i < task.options.Length;
                optionSlots[i].gameObject.SetActive(used);
                if (used)
                {
                    int index = i;
                    optionSlots[i].Setup(task.options[i], sprites(task.options[i].iconKey), () => _onSubmit?.Invoke(index));
                }
            }
        }

        private void ShowHelper()
        {
            StudyViewUtils.SetActive(helperTextLabel != null ? helperTextLabel.gameObject : null, true);
            helperButton.interactable = false;
        }
    }
}
