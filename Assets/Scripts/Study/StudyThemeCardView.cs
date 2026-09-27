using System;
using System.Collections.Generic;
using Content;
using Core;
using TMPro;
using UI;
using UnityEngine;

namespace Study
{
    /// <summary>
    /// The one "current theme" card on the main study page (StudyScreen always feeds it the first theme
    /// that isn't passed yet). Open: title, "1/2", task rows. Locked: title + "Откроется после 3 смен
    /// на работе" / "…после темы «…»", rows hidden. Task rows are fixed slots — extra slots are hidden,
    /// too few slots logs a warning.
    /// </summary>
    public class StudyThemeCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [Tooltip("\"1/2\" — passed / total.")]
        [SerializeField] private TMP_Text progressLabel;
        [Tooltip("Parent of the task rows — shown when the theme is open.")]
        [SerializeField] private GameObject tasksRoot;
        [SerializeField] private StudyTaskRowView[] taskRows = Array.Empty<StudyTaskRowView>();
        [Tooltip("Lock icon + label — shown when the theme is locked.")]
        [SerializeField] private GameObject lockedRoot;
        [SerializeField] private TMP_Text lockedLabel;

        public void Setup(StudyThemeDefinition theme, StudyService study, Action<StudyTaskDefinition> onStart)
        {
            StudyThemeState state = study.GetState(theme);
            bool locked = state == StudyThemeState.LockedByShifts || state == StudyThemeState.LockedByPrevious;
            List<StudyTaskDefinition> tasks = study.TasksOf(theme);

            titleLabel.text = theme.title;
            StudyViewUtils.SetText(progressLabel, $"{study.PassedCount(theme)}/{tasks.Count}");
            StudyViewUtils.SetActive(tasksRoot, !locked);
            StudyViewUtils.SetActive(lockedRoot, locked);

            if (locked)
            {
                StudyViewUtils.SetText(lockedLabel, LockText(theme, state, study));
            }

            if (tasks.Count > taskRows.Length)
            {
                Debug.LogWarning($"StudyThemeCardView: theme '{theme.id}' has {tasks.Count} tasks but only {taskRows.Length} row slots on '{name}'. Add slots.");
            }

            for (int i = 0; i < taskRows.Length; i++)
            {
                bool used = i < tasks.Count;
                taskRows[i].gameObject.SetActive(used && !locked);
                if (!used)
                {
                    continue;
                }

                StudyTaskDefinition task = tasks[i];
                taskRows[i].Setup(task, study.IsTaskPassed(task), study.IsTaskAvailable(task), () => onStart(task));
            }
        }

        private static string LockText(StudyThemeDefinition theme, StudyThemeState state, StudyService study)
        {
            if (state == StudyThemeState.LockedByPrevious)
            {
                StudyThemeDefinition previous = study.PreviousTheme(theme);
                return $"Откроется после темы «{previous?.title}»";
            }

            int need = theme.unlockShifts;
            int left = study.ShiftsLeft(theme);
            string text = $"Откроется после {need} {RuPlural.Pick(need, "смены", "смен", "смен")} на работе";
            return left < need ? $"{text} · осталось {left}" : text;
        }
    }
}
