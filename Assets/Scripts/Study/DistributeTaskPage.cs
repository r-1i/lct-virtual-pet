using System;
using Content;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>
    /// "Разложи" task page: character + speech, optional situation and event banner, fixed row slots
    /// (extra hidden), "Осталось разложить" (hidden when the task has no budget), a live hint and
    /// "Готово". + is blocked once the budget is used up; "Готово" only when StudyService.CanSubmit.
    /// </summary>
    public class DistributeTaskPage : MonoBehaviour
    {
        private const int NoLimitMax = 9999;

        [SerializeField] private Button backButton;
        [Tooltip("\"Задание: зарплата\"")]
        [SerializeField] private TMP_Text headerLabel;

        [Header("Character")]
        [SerializeField] private Image speakerImage;
        [SerializeField] private TMP_Text speakerNameLabel;
        [SerializeField] private TMP_Text speechLabel;
        [Tooltip("Situation text. Hidden when empty. Optional.")]
        [SerializeField] private TMP_Text conditionLabel;
        [Tooltip("Background plate of the situation — hidden together with it. Optional.")]
        [SerializeField] private GameObject conditionRoot;

        [Header("Event (\"Зонт сломался\")")]
        [Tooltip("Banner, hidden when the task has no eventText.")]
        [SerializeField] private GameObject eventRoot;
        [SerializeField] private TMP_Text eventLabel;

        [Header("Rows")]
        [SerializeField] private DistributeRowView[] rowSlots = Array.Empty<DistributeRowView>();

        [Header("Bottom")]
        [Tooltip("\"Осталось разложить\" block, hidden when budget = 0.")]
        [SerializeField] private GameObject remainingRoot;
        [SerializeField] private TMP_Text remainingLabel;
        [Tooltip("Live hint: \"Не сходится…\", \"Еда — это нужное\", \"Успеешь…\".")]
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Button doneButton;

        private StudyTaskDefinition _task;
        private int[] _values;
        private Action _onBack;
        private Action<int[]> _onSubmit;

        private void Awake()
        {
            backButton.onClick.AddListener(() => _onBack?.Invoke());
            doneButton.onClick.AddListener(HandleDone);

            for (int i = 0; i < rowSlots.Length; i++)
            {
                int index = i;
                rowSlots[i].Stepped += dir => Step(index, dir);
                rowSlots[i].Cleared += () => SetRow(index, 0);
            }
        }

        /// <summary>Called by StudyScreen right after activating this page. Every opening starts from the task's start values.</summary>
        public void Open(StudyTaskDefinition task, Func<string, Sprite> sprites, Action onBack, Action<int[]> onSubmit)
        {
            _task = task;
            _onBack = onBack;
            _onSubmit = onSubmit;

            headerLabel.text = task.header;
            StudyViewUtils.SetSprite(speakerImage, sprites(task.speaker));
            StudyViewUtils.SetText(speakerNameLabel, task.speakerName);
            speechLabel.text = task.speech;
            StudyViewUtils.SetOptional(conditionLabel, task.condition, conditionRoot);
            StudyViewUtils.SetOptional(eventLabel, task.eventText, eventRoot);
            StudyViewUtils.SetActive(remainingRoot, task.budget > 0);

            if (task.rows.Length > rowSlots.Length)
            {
                Debug.LogWarning($"DistributeTaskPage: task '{task.id}' has {task.rows.Length} rows but only {rowSlots.Length} slots. Add slots.");
            }

            _values = new int[task.rows.Length];
            for (int i = 0; i < rowSlots.Length; i++)
            {
                bool used = i < task.rows.Length;
                rowSlots[i].gameObject.SetActive(used);
                if (used)
                {
                    _values[i] = task.rows[i].start;
                    rowSlots[i].Setup(task.rows[i], sprites(task.rows[i].iconKey), task.showClearButtons);
                }
            }

            Refresh();
        }

        private void Step(int index, int direction)
        {
            if (_task == null || index >= _values.Length)
            {
                return;
            }

            int step = Mathf.Max(1, _task.rows[index].step);
            int delta = direction * step;
            if (delta > 0 && _task.budget > 0)
            {
                delta = Mathf.Min(delta, Mathf.Max(0, StudyService.Remaining(_task, _values)));
            }

            SetRow(index, _values[index] + delta);
        }

        private void SetRow(int index, int value)
        {
            if (_task == null || index >= _values.Length)
            {
                return;
            }

            _values[index] = Mathf.Clamp(value, 0, MaxOf(_task.rows[index]));
            Refresh();
        }

        private void Refresh()
        {
            int remaining = StudyService.Remaining(_task, _values);

            for (int i = 0; i < _values.Length && i < rowSlots.Length; i++)
            {
                bool canPlus = _values[i] < MaxOf(_task.rows[i]) && (_task.budget <= 0 || remaining > 0);
                rowSlots[i].SetValue(_values[i], _values[i] > 0, canPlus);
            }

            StudyViewUtils.SetText(remainingLabel, remaining.ToString());
            StudyViewUtils.SetText(hintLabel, StudyService.LiveHint(_task, _values));
            doneButton.interactable = StudyService.CanSubmit(_task, _values);
        }

        private static int MaxOf(StudyDistributeRow row) => row.max > 0 ? row.max : NoLimitMax;

        private void HandleDone()
        {
            if (_task != null && StudyService.CanSubmit(_task, _values))
            {
                _onSubmit?.Invoke((int[])_values.Clone());
            }
        }
    }
}
