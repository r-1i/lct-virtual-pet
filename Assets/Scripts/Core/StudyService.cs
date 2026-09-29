using System.Collections.Generic;
using System.Linq;
using Content;
using UnityEngine;

namespace Core
{
    public enum StudyThemeState
    {
        /// <summary>Not enough shifts yet.</summary>
        LockedByShifts,
        /// <summary>Enough shifts, but the previous theme isn't passed.</summary>
        LockedByPrevious,
        Open,
        Passed
    }

    /// <summary>What the result page shows after a submitted answer.</summary>
    public class StudyResult
    {
        public bool Passed;
        public string Verdict;
        /// <summary>"What happened" lines.</summary>
        public List<string> Outcomes = new List<string>();
        public string Explanation;
        public string Lesson;
        /// <summary>This answer finished the theme and raised the level.</summary>
        public bool Promoted;
        public string NewCareerTitle;
    }

    /// <summary>
    /// Study ("Учёба"): themes open after N completed shifts and strictly in order; tasks inside a
    /// theme open one after another; all tasks of a theme passed → level = theme.rewardLevel (the job
    /// list is per level, so work pays more). Two task engines, both fully described by json:
    /// "choose" (pick an option) and "distribute" (split coins over rows). Answers are only a story —
    /// real coins and stats are never touched.
    /// </summary>
    public class StudyService
    {
        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;

        public StudyService(PlayerDataService playerData, ContentDatabase content)
        {
            _playerData = playerData;
            _content = content;
        }

        public IReadOnlyList<StudyThemeDefinition> Themes => _content.StudyThemes;
        public int Shifts => _playerData.ShiftsCompleted;
        public int Level => _playerData.Level;
        public int MaxLevel => Mathf.Max(1, _content.CareerTitles.Count);

        public string CareerTitle(int level)
        {
            IReadOnlyList<string> titles = _content.CareerTitles;
            return titles.Count == 0 ? $"Уровень {level}" : titles[Mathf.Clamp(level - 1, 0, titles.Count - 1)];
        }

        /// <summary>"Малыш" for level 1 etc. Empty if themes.json has no petStages for this level.</summary>
        public string PetStage(int level)
        {
            IReadOnlyList<string> stages = _content.PetStages;
            return stages.Count == 0 ? "" : stages[Mathf.Clamp(level - 1, 0, stages.Count - 1)];
        }

        /// <summary>The rank's job income multiplier (economy.json).</summary>
        public float IncomeMultiplier(int level) => _content.Economy.RankMultiplier(level);

        /// <summary>"Младший (Молодой), доход ×1.25" — one line for the study screen and the promotion message.</summary>
        public string RankSummary(int level)
        {
            string stage = PetStage(level);
            string title = string.IsNullOrEmpty(stage) ? CareerTitle(level) : $"{CareerTitle(level)} ({stage})";
            return $"{title}, доход ×{IncomeMultiplier(level):0.##}";
        }

        public List<StudyTaskDefinition> TasksOf(StudyThemeDefinition theme) => _content.StudyTasksOf(theme.id).ToList();

        public bool IsTaskPassed(StudyTaskDefinition task) => _playerData.IsStudyTaskPassed(task.id);

        public int PassedCount(StudyThemeDefinition theme) => TasksOf(theme).Count(IsTaskPassed);

        public bool IsThemePassed(StudyThemeDefinition theme)
        {
            List<StudyTaskDefinition> tasks = TasksOf(theme);
            return tasks.Count > 0 && tasks.All(IsTaskPassed);
        }

        public StudyThemeState GetState(StudyThemeDefinition theme)
        {
            if (IsThemePassed(theme))
            {
                return StudyThemeState.Passed;
            }

            if (Shifts < theme.unlockShifts)
            {
                return StudyThemeState.LockedByShifts;
            }

            StudyThemeDefinition previous = PreviousTheme(theme);
            return previous != null && !IsThemePassed(previous) ? StudyThemeState.LockedByPrevious : StudyThemeState.Open;
        }

        public int ShiftsLeft(StudyThemeDefinition theme) => Mathf.Max(0, theme.unlockShifts - Shifts);

        public StudyThemeDefinition PreviousTheme(StudyThemeDefinition theme)
        {
            int index = IndexOf(theme);
            return index > 0 ? Themes[index - 1] : null;
        }

        /// <summary>First theme not passed yet — "до повышения: сдать тему «...»". Null when all are passed.</summary>
        public StudyThemeDefinition NextTheme => Themes.FirstOrDefault(t => !IsThemePassed(t));

        /// <summary>Theme is open and every earlier task of the theme is passed. Passed tasks aren't available again.</summary>
        public bool IsTaskAvailable(StudyTaskDefinition task)
        {
            StudyThemeDefinition theme = Themes.FirstOrDefault(t => t.id == task.theme);
            if (theme == null || GetState(theme) != StudyThemeState.Open || IsTaskPassed(task))
            {
                return false;
            }

            return TasksOf(theme).TakeWhile(t => t.id != task.id).All(IsTaskPassed);
        }

        // ---------- answers ----------

        public StudyResult SubmitChoice(StudyTaskDefinition task, int optionIndex)
        {
            StudyChoiceOption option = task.options != null && optionIndex >= 0 && optionIndex < task.options.Length
                ? task.options[optionIndex]
                : null;

            bool passed = option != null && option.correct;
            var result = new StudyResult
            {
                Passed = passed,
                Verdict = passed ? task.passVerdict : task.failVerdict,
                Explanation = !string.IsNullOrEmpty(option?.explanation)
                    ? option.explanation
                    : passed ? task.passExplanation : task.failExplanation,
                Lesson = task.lesson
            };

            if (!string.IsNullOrEmpty(option?.outcome))
            {
                result.Outcomes.Add(option.outcome);
            }

            return Finish(task, result);
        }

        /// <summary>values: one per task.rows, same order.</summary>
        public StudyResult SubmitDistribution(StudyTaskDefinition task, int[] values)
        {
            Dictionary<string, string> vars = BuildVars(task, values);
            bool passed = true;
            var result = new StudyResult { Lesson = task.lesson };

            for (int i = 0; i < task.rows.Length; i++)
            {
                StudyDistributeRow row = task.rows[i];
                bool belowMin = values[i] < row.min;
                bool aboveMax = row.max > 0 && values[i] > row.max;
                if (!belowMin && !aboveMax)
                {
                    continue;
                }

                passed = false;
                if (belowMin && !string.IsNullOrEmpty(row.belowMinText))
                {
                    result.Outcomes.Add(Format(row.belowMinText, vars));
                }
            }

            int remaining = Remaining(task, values);
            if (task.budget > 0 && (remaining < 0 || (task.mustSpendAll && remaining != 0)))
            {
                passed = false;
            }

            foreach (StudyOutcomeRule rule in task.outcomes ?? new StudyOutcomeRule[0])
            {
                int index = RowIndex(task, rule.row);
                if (index >= 0 && values[index] >= rule.atLeast && values[index] <= rule.atMost)
                {
                    result.Outcomes.Add(Format(rule.text, vars));
                }
            }

            result.Passed = passed;
            result.Verdict = passed ? task.passVerdict : task.failVerdict;
            result.Explanation = Format(passed ? task.passExplanation : task.failExplanation, vars);
            return Finish(task, result);
        }

        private StudyResult Finish(StudyTaskDefinition task, StudyResult result)
        {
            if (!IsTaskPassed(task))
            {
                _playerData.RegisterStudyAttempt(task.id, result.Passed);
            }

            if (!result.Passed || IsTaskPassed(task))
            {
                return result;
            }

            _playerData.MarkStudyTaskPassed(task.id);

            StudyThemeDefinition theme = Themes.First(t => t.id == task.theme);
            if (IsThemePassed(theme) && theme.rewardLevel > _playerData.Level)
            {
                _playerData.SetLevel(theme.rewardLevel);
                result.Promoted = true;
                string stage = PetStage(theme.rewardLevel);
                result.NewCareerTitle = string.IsNullOrEmpty(stage)
                    ? CareerTitle(theme.rewardLevel)
                    : $"{CareerTitle(theme.rewardLevel)} ({stage})";
            }

            return result;
        }

        // ---------- distribute helpers (also used live by the page) ----------

        /// <summary>budget − sum. Meaningless when budget = 0.</summary>
        public static int Remaining(StudyTaskDefinition task, int[] values) => task.budget - values.Sum();

        /// <summary>"Готово" allowed: nothing over budget, and everything split if mustSpendAll.</summary>
        public static bool CanSubmit(StudyTaskDefinition task, int[] values)
        {
            if (task.budget <= 0)
            {
                return true;
            }

            int remaining = Remaining(task, values);
            return remaining >= 0 && (!task.mustSpendAll || remaining == 0);
        }

        /// <summary>Live hint under the rows, most specific first: over budget → touched a protected row → goal → split everything → footerHint.</summary>
        public static string LiveHint(StudyTaskDefinition task, int[] values)
        {
            int remaining = Remaining(task, values);
            if (task.budget > 0 && remaining < 0)
            {
                return $"Не сходится: убери ещё {-remaining}.";
            }

            for (int i = 0; i < task.rows.Length; i++)
            {
                StudyDistributeRow row = task.rows[i];
                if (!string.IsNullOrEmpty(row.touchHint) && values[i] < row.start)
                {
                    return row.touchHint;
                }
            }

            if (task.goal != null && !string.IsNullOrEmpty(task.goal.row))
            {
                Dictionary<string, string> vars = BuildVars(task, values);
                int index = RowIndex(task, task.goal.row);
                bool reached = index >= 0 && values[index] * task.goal.days >= task.goal.target;
                return Format(reached ? task.goal.reachText : task.goal.missText, vars);
            }

            if (task.budget > 0 && task.mustSpendAll && remaining > 0)
            {
                return $"Разложи всё до конца: осталось {remaining}.";
            }

            return task.footerHint ?? "";
        }

        private static int RowIndex(StudyTaskDefinition task, string rowId)
        {
            for (int i = 0; i < task.rows.Length; i++)
            {
                if (task.rows[i].id == rowId)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>{rowId} for every row, {budget} {remaining} {sum}; with a goal also {value} {days} {target} {total} {short}.</summary>
        private static Dictionary<string, string> BuildVars(StudyTaskDefinition task, int[] values)
        {
            var vars = new Dictionary<string, string>();
            for (int i = 0; i < task.rows.Length; i++)
            {
                vars[task.rows[i].id] = values[i].ToString();
            }

            vars["budget"] = task.budget.ToString();
            vars["remaining"] = Remaining(task, values).ToString();
            vars["sum"] = values.Sum().ToString();

            if (task.goal != null && !string.IsNullOrEmpty(task.goal.row))
            {
                int index = RowIndex(task, task.goal.row);
                int value = index >= 0 ? values[index] : 0;
                int total = value * task.goal.days;
                vars["value"] = value.ToString();
                vars["days"] = task.goal.days.ToString();
                vars["target"] = task.goal.target.ToString();
                vars["total"] = total.ToString();
                vars["short"] = Mathf.Max(0, task.goal.target - total).ToString();
            }

            return vars;
        }

        private static string Format(string template, Dictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(template))
            {
                return "";
            }

            foreach (KeyValuePair<string, string> pair in vars)
            {
                template = template.Replace("{" + pair.Key + "}", pair.Value);
            }

            return template;
        }

        private int IndexOf(StudyThemeDefinition theme)
        {
            for (int i = 0; i < Themes.Count; i++)
            {
                if (Themes[i].id == theme.id)
                {
                    return i;
                }
            }

            return -1;
        }

        // ---------- debug ----------

        /// <summary>StatsDebug: passes the first available task as if answered correctly (promotion included).</summary>
        public bool DebugPassNextTask(out string taskTitle)
        {
            StudyTaskDefinition task = _content.StudyTasks.FirstOrDefault(IsTaskAvailable);
            taskTitle = task?.title;
            if (task == null)
            {
                return false;
            }

            Finish(task, new StudyResult { Passed = true });
            return true;
        }
    }
}
