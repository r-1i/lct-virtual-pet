using Core;
using Creation;
using Home;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Debug commands. Two ways to run them: in the Editor ⋮ on this component (ContextMenu), in the game
    /// Настройки → Debug Menu (UI/DebugMenu lists them with Russian labels — add a new command there too).
    /// Goes through PlayerDataService, so everything saves and publishes events like real gameplay.
    /// </summary>
    public class StatsDebug : MonoBehaviour
    {
        [SerializeField] private float step = 20f;

        private PlayerDataService Service => ServiceLocator.Get<PlayerDataService>();

        [ContextMenu("Stats: all -step")]
        public void DecreaseAll() => Service.ApplyStatDelta(-step, -step, -step);

        [ContextMenu("Stats: all +step")]
        public void IncreaseAll() => Service.ApplyStatDelta(step, step, step);

        [ContextMenu("Stats: randomize")]
        public void Randomize() => Service.SetStats(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));

        [ContextMenu("Stats: reset to 100")]
        public void ResetAll() => Service.SetStats(100f, 100f, 100f);

        [ContextMenu("Coins: +100")]
        public void AddCoins() => Service.AddCoins(100);

        [ContextMenu("Coins: -50")]
        public void SpendCoins() => Service.TrySpendCoins(50);

        [ContextMenu("Food: +5 medium & large feed")]
        public void AddFeed()
        {
            Service.AddInventory("feed_medium", 5);
            Service.AddInventory("feed_large", 5);
        }

        [ContextMenu("Care: +5 soap, washcloth, toothbrush")]
        public void AddCareItems()
        {
            Service.AddInventory("soap", 5);
            Service.AddInventory("washcloth", 5);
            Service.AddInventory("toothbrush", 5);
        }

        [ContextMenu("Jar: +10")]
        public void AddJarCoins() => Service.AddJarCoins(10);

        [ContextMenu("Jar: +500")]
        public void AddJarCoinsBig() => Service.AddJarCoins(500);

        /// <summary>Until the finance planning screen exists — turns the dreams forecast on/off.</summary>
        [ContextMenu("Plan: 40 coins/day")]
        public void SetPlan40() => Service.SetPlannedSavingPerDay(40);

        [ContextMenu("Plan: off (0)")]
        public void SetPlanOff() => Service.SetPlannedSavingPerDay(0);

        /// <summary>
        /// One history line of every kind (today), incl. a long title — to check the history layout
        /// without playing. Only the log: coins/jar don't change. Note: they DO count as plan facts.
        /// </summary>
        [ContextMenu("History: add sample entries")]
        public void AddSampleHistory()
        {
            Service.LogMoney(MoneyKind.JobIncome, 40, "Полить растения");
            Service.LogMoney(MoneyKind.BuyNeed, 75, "Средний корм", 5);
            Service.LogMoney(MoneyKind.Cashback, 8, "Кешбэк за день");
            Service.LogMoney(MoneyKind.BuyWant, 20, "Мороженое с вишнёвым сиропом и шоколадной крошкой");
            Service.LogMoney(MoneyKind.JarDeposit, 25);
            Service.LogMoney(MoneyKind.JarWithdraw, 10);
            Service.LogMoney(MoneyKind.DreamPurchase, 80, "Домик");
            Service.LogMoney(MoneyKind.PlanReward, 5, "Награда Барсука за план");
            Service.LogMoney(MoneyKind.DepositOpen, 50, "Вклад у Барсука на 3 дн.");
            Service.LogMoney(MoneyKind.DepositReturn, 50, "Барсук вернул вклад");
            Service.LogMoney(MoneyKind.DepositInterest, 5, "Проценты по вкладу");
        }

        /// <summary>Moves the active job's start time in the save one day back — any job becomes ready to collect.</summary>
        [ContextMenu("Job: start time -1 day")]
        public void ShiftJobStartDay()
        {
            if (!Service.Job.IsActive)
            {
                Debug.LogWarning("StatsDebug: no active job.");
                return;
            }

            Service.DebugShiftJobStart(24 * 60 * 60);
            Debug.Log("StatsDebug: job start moved 1 day back.");
        }

        /// <summary>Today's "max 2 shifts" limit starts over.</summary>
        [ContextMenu("Job: reset today's shifts")]
        public void ResetShiftsToday() => Service.DebugResetShiftsToday();

        /// <summary>As if one more job was collected — study themes open after N shifts.</summary>
        [ContextMenu("Study: +1 shift")]
        public void AddShift() => Service.AddShift();

        [ContextMenu("Study: +3 shifts")]
        public void AddThreeShifts()
        {
            Service.AddShift();
            Service.AddShift();
            Service.AddShift();
        }

        /// <summary>Passes the first available study task as if answered right (promotion included).</summary>
        [ContextMenu("Study: pass next task")]
        public void PassNextStudyTask()
        {
            bool passed = ServiceLocator.Get<StudyService>().DebugPassNextTask(out string title);
            Debug.Log(passed ? $"StatsDebug: passed study task «{title}»." : "StatsDebug: no available study task (need more shifts?).");
        }

        /// <summary>Forgets passed tasks and shifts, level back to 1.</summary>
        [ContextMenu("Study: reset (tasks, shifts, level 1)")]
        public void ResetStudy()
        {
            Service.ResetStudy();
            Service.SetLevel(1);
        }

        /// <summary>Opens the character creation again right now (the tutorial isn't restarted). The current look stays until the new one is picked.</summary>
        [ContextMenu("Character: reset creation")]
        public void ResetCharacterCreation()
        {
            Service.ResetCharacterCreation();
            var flow = FindFirstObjectByType<CharacterCreationFlow>(FindObjectsInactive.Include);
            if (flow != null)
            {
                flow.Run(null);
            }
            else
            {
                Debug.LogWarning("StatsDebug: no CharacterCreationFlow in the scene — restart Play Mode to see the creation.");
            }
        }

        /// <summary>Cycles through the looks without the creation windows: model → colour → bow (each colour, then none), like an odometer.</summary>
        [ContextMenu("Character: next look (preview only)")]
        public void NextLook()
        {
            var appearance = FindFirstObjectByType<CharacterAppearance>(FindObjectsInactive.Include);
            if (appearance == null || appearance.ModelCount == 0)
            {
                Debug.LogWarning("StatsDebug: no CharacterAppearance with models in the scene.");
                return;
            }

            int bows = appearance.BowColorCount + 1;
            _lookIndex = (_lookIndex + 1) % (appearance.ModelCount * 3 * bows);
            int model = _lookIndex / (3 * bows);
            int color = _lookIndex / bows % 3;
            int bow = _lookIndex % bows;
            bool noBow = bow == bows - 1;
            appearance.Show(model, color, noBow ? CharacterAccessory.None : CharacterAccessory.Bow, bow);
            Debug.Log($"StatsDebug: model {model + 1}, colour {color + 1}, {(noBow ? "no bow" : $"bow {bow + 1}")}.");
        }

        private int _lookIndex;

        [ContextMenu("Anim: Greet")]
        public void AnimGreet() => FireTrigger(CharacterAnimParams.Greet);

        [ContextMenu("Anim: Joy")]
        public void AnimJoy() => FireTrigger(CharacterAnimParams.Joy);

        [ContextMenu("Anim: Play")]
        public void AnimPlay() => FireTrigger(CharacterAnimParams.Play);

        [ContextMenu("Anim: Eat")]
        public void AnimEat() => FireTrigger(CharacterAnimParams.Eat);

        [ContextMenu("Anim: toggle Dance")]
        public void AnimDance() => ToggleBool(CharacterAnimParams.Dancing);

        [ContextMenu("Anim: toggle Talk")]
        public void AnimTalk() => ToggleBool(CharacterAnimParams.Talking);

        [ContextMenu("Anim: toggle Work")]
        public void AnimWork() => ToggleBool(CharacterAnimParams.Working);

        [ContextMenu("Anim: toggle Stomach Ache")]
        public void AnimStomachAche() => ToggleBool(CharacterAnimParams.StomachAche);

        /// <summary>The Animator of the model shown right now, or null with a warning.</summary>
        private static Animator CharacterAnimator()
        {
            var appearance = FindFirstObjectByType<CharacterAppearance>(FindObjectsInactive.Include);
            Animator animator = appearance != null ? appearance.Animator : null;
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("StatsDebug: the shown model has no Animator / controller.");
                return null;
            }

            return animator;
        }

        private static void FireTrigger(string trigger)
        {
            Animator animator = CharacterAnimator();
            if (animator != null)
            {
                animator.SetTrigger(trigger);
                Debug.Log($"StatsDebug: trigger {trigger}.");
            }
        }

        private static void ToggleBool(string parameter)
        {
            Animator animator = CharacterAnimator();
            if (animator != null)
            {
                bool value = !animator.GetBool(parameter);
                animator.SetBool(parameter, value);
                Debug.Log($"StatsDebug: {parameter} = {value}.");
            }
        }

        /// <summary>Pretends a real day passed: finishes the finance plan when its period is over, moves the Barsuk deposit closer to maturity.</summary>
        [ContextMenu("Day: +1 (debug time skip)")]
        public void SkipDay()
        {
            GameDay.AddDebugDays(1);
            ServiceLocator.Get<MailService>().CheckDay();
            ServiceLocator.Get<FinanceService>().CheckPeriod();
            Debug.Log($"StatsDebug: day offset +1, today = {GameDay.ToDate(GameDay.Today):dd.MM.yyyy}.");
        }

        /// <summary>The current state becomes yesterday's snapshot + its letter — change something, then restore from the mail to check the rollback.</summary>
        [ContextMenu("Mail: snapshot as yesterday + letter")]
        public void MailSnapshotYesterday() => ServiceLocator.Get<MailService>().DebugSnapshotYesterday();

        /// <summary>Letters for the last 7 days (all with the current state) — to see a full inbox.</summary>
        [ContextMenu("Mail: fill 7 letters")]
        public void MailFillWeek() => ServiceLocator.Get<MailService>().DebugFillWeek();

        [ContextMenu("Mail: clear all letters and snapshots")]
        public void MailClear() => ServiceLocator.Get<MailService>().DebugClear();

        /// <summary>Back to real time. Entries/plans made on skipped days stay in the "future".</summary>
        [ContextMenu("Day: reset debug offset")]
        public void ResetDayOffset()
        {
            GameDay.ResetDebugOffset();
            Debug.Log("StatsDebug: day offset reset.");
        }
    }
}
