using System.Collections.Generic;
using System.Text;
using Content;
using Core;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Настройки → Профиль родителя: what the child did in Учёба — career title, shifts, and every test
    /// (theme → task) with passed / not passed, number of answers and the day it was passed. Rebuilt on every open.
    /// The page starts inactive and opens only on a click, so services exist by then.
    /// </summary>
    public class ParentStudyReport : MonoBehaviour
    {
        [SerializeField] private TMP_Text reportLabel;
        [SerializeField] private string passedColor = "#2E9E44";
        [SerializeField] private string notPassedColor = "#8A8A8A";
        [SerializeField] private string lockedColor = "#C0782A";

        private void OnEnable()
        {
            if (reportLabel == null || !ServiceLocator.TryGet(out StudyService study)
                || !ServiceLocator.TryGet(out PlayerDataService playerData))
            {
                return;
            }

            reportLabel.text = Build(study, playerData);
        }

        private string Build(StudyService study, PlayerDataService playerData)
        {
            int total = 0;
            int passed = 0;
            var themes = new StringBuilder();

            for (int i = 0; i < study.Themes.Count; i++)
            {
                StudyThemeDefinition theme = study.Themes[i];
                List<StudyTaskDefinition> tasks = study.TasksOf(theme);
                themes.Append($"\n<b>{i + 1}. {theme.title}</b> — {ThemeState(study, theme)}\n");

                foreach (StudyTaskDefinition task in tasks)
                {
                    total++;
                    bool done = study.IsTaskPassed(task);
                    if (done)
                    {
                        passed++;
                    }

                    StudyTaskRecord record = playerData.FindStudyRecord(task.id);
                    themes.Append("    ").Append(task.title).Append(": ");
                    themes.Append(done ? $"<color={passedColor}>сдан</color>" : $"<color={notPassedColor}>не сдан</color>");
                    if (record != null && record.attempts > 0)
                    {
                        themes.Append($", попыток: {record.attempts}");
                    }

                    if (done && record != null && record.passedDay >= 0)
                    {
                        themes.Append(", ").Append(GameDay.ToDate(record.passedDay).ToString("dd.MM"));
                    }

                    themes.Append('\n');
                }
            }

            var text = new StringBuilder();
            text.Append($"<b>Должность:</b> {study.CareerTitle(study.Level)} (уровень {study.Level} из {study.MaxLevel})\n");
            text.Append($"<b>Смен отработано:</b> {study.Shifts}\n");
            text.Append($"<b>Тесты сданы:</b> {passed} из {total}\n");
            text.Append(themes);
            return text.ToString();
        }

        private string ThemeState(StudyService study, StudyThemeDefinition theme)
        {
            switch (study.GetState(theme))
            {
                case StudyThemeState.Passed:
                    return $"<color={passedColor}>пройдена</color>";
                case StudyThemeState.LockedByShifts:
                    return $"<color={lockedColor}>откроется через {study.ShiftsLeft(theme)} {Shifts(study.ShiftsLeft(theme))}</color>";
                case StudyThemeState.LockedByPrevious:
                    return $"<color={lockedColor}>после предыдущей темы</color>";
                default:
                    return "открыта";
            }
        }

        private static string Shifts(int n)
        {
            int mod100 = n % 100;
            int mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 14)
            {
                return "смен";
            }

            return mod10 == 1 ? "смену" : mod10 >= 2 && mod10 <= 4 ? "смены" : "смен";
        }
    }
}
