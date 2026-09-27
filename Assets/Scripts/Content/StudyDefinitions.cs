using System;

namespace Content
{
    /// <summary>
    /// Study ("Учёба") content. Themes + career titles live in Resources/Study/themes.json; every task
    /// is its own file in Resources/Study/Tasks/*.json (any file name) — adding a task of an existing
    /// type is just dropping a new json there. See PLAN.md section 6.5 for the templates.
    /// </summary>
    [Serializable]
    public class StudyThemesFile
    {
        /// <summary>[0] = level 1 ("Стажёр"), [1] = level 2, ...</summary>
        public string[] careerTitles;
        public StudyThemeDefinition[] themes;
    }

    [Serializable]
    public class StudyThemeDefinition
    {
        public string id;
        public string title;
        /// <summary>Themes open strictly in this order: a theme also needs the previous one passed.</summary>
        public int order;
        /// <summary>Completed job shifts ("Забрать" on the Work screen) needed to open the theme.</summary>
        public int unlockShifts;
        /// <summary>Player level after all tasks of the theme are passed (never lowers the level).</summary>
        public int rewardLevel;
    }

    /// <summary>One task. Fields of the other type are simply left out of the json.</summary>
    [Serializable]
    public class StudyTaskDefinition
    {
        public const string TypeChoose = "choose";
        public const string TypeDistribute = "distribute";

        public string id;
        /// <summary>StudyThemeDefinition.id</summary>
        public string theme;
        /// <summary>Order inside the theme; tasks open one after another.</summary>
        public int order;
        /// <summary>"choose" (Реши) or "distribute" (Разложи).</summary>
        public string type;

        /// <summary>Name in the theme card list: "Зарплата пришла".</summary>
        public string title;
        /// <summary>Page header: "Задание: зарплата".</summary>
        public string header;
        /// <summary>Sprite key of the character (badger, fox, ...).</summary>
        public string speaker;
        public string speakerName;
        public string speech;
        /// <summary>Situation block under the speech. Optional.</summary>
        public string condition;
        /// <summary>"Что купишь?" above the options / rows. Optional.</summary>
        public string question;
        /// <summary>Small hint at the bottom of the page when nothing more specific applies. Optional.</summary>
        public string footerHint;

        // ---------- choose ----------
        public StudyChoiceOption[] options;
        /// <summary>Helper button caption ("Посчитать за штуку"). Empty = no helper.</summary>
        public string helperButton;
        public string helperText;

        // ---------- distribute ----------
        /// <summary>Coins to split. 0 = no budget (e.g. "сколько в день": one row, no "осталось").</summary>
        public int budget;
        /// <summary>"Готово" only when everything is split (remaining = 0).</summary>
        public bool mustSpendAll;
        /// <summary>Event banner ("Зонт сломался — минус 10"). Optional.</summary>
        public string eventText;
        /// <summary>Show the "убрать" (set to 0) button on every row.</summary>
        public bool showClearButtons;
        public StudyDistributeRow[] rows;
        /// <summary>Extra result lines depending on row values ("Купил мячик — ...").</summary>
        public StudyOutcomeRule[] outcomes;
        /// <summary>Live "успеешь / не успеешь" hint. goal.row empty = none.</summary>
        public StudyGoalHint goal = new StudyGoalHint();

        // ---------- result ----------
        public string passVerdict = "Верно!";
        public string failVerdict = "Можно лучше";
        /// <summary>For distribute; for choose the picked option's explanation wins when present.</summary>
        public string passExplanation;
        public string failExplanation;
        /// <summary>The takeaway: "Не бери больше, чем успеешь съесть."</summary>
        public string lesson;

        public bool IsChoose => type == TypeChoose;
        public bool IsDistribute => type == TypeDistribute;
    }

    [Serializable]
    public class StudyChoiceOption
    {
        public string title;
        public string subtitle;
        public string iconKey;
        public bool correct;
        /// <summary>What happened: "Съедено 4, выкинуто 6."</summary>
        public string outcome;
        /// <summary>Why it's right/wrong. Optional (falls back to the task's pass/failExplanation).</summary>
        public string explanation;
    }

    [Serializable]
    public class StudyDistributeRow
    {
        public string id;
        public string title;
        public string subtitle;
        public string iconKey;
        public int start;
        /// <summary>Value below this fails the task.</summary>
        public int min;
        /// <summary>0 = no limit (besides the budget).</summary>
        public int max;
        public int step = 1;
        /// <summary>Highlight frame on the row ("нужное").</summary>
        public bool required;
        /// <summary>Live hint when the value drops below start ("Еда — это нужное!"). Optional.</summary>
        public string touchHint;
        /// <summary>Result line when the value is below min. Optional.</summary>
        public string belowMinText;
    }

    /// <summary>Result line shown when row value is in [atLeast, atMost].</summary>
    [Serializable]
    public class StudyOutcomeRule
    {
        public string row;
        public int atLeast = int.MinValue;
        public int atMost = int.MaxValue;
        public string text;
    }

    /// <summary>row × days vs target. Texts may use {value} {days} {total} {target} {short}.</summary>
    [Serializable]
    public class StudyGoalHint
    {
        public string row;
        public int target;
        public int days;
        public string reachText;
        public string missText;
    }
}
