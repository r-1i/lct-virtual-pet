using Core;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Temporary helper to test the stat bars: stats start at 100 and nothing lowers them yet, so the
    /// bars would otherwise always sit full. In Play Mode: ⋮ on this component → pick an action.
    /// Goes through PlayerDataService, so it saves and publishes StatsChangedEvent like real gameplay.
    /// </summary>
    public class StatsDebug : MonoBehaviour
    {
        [SerializeField] private float step = 20f;

        private PlayerDataService Service => ServiceLocator.Get<PlayerDataService>();

        [ContextMenu("Stats: all -step")]
        private void DecreaseAll() => Service.ApplyStatDelta(-step, -step, -step);

        [ContextMenu("Stats: all +step")]
        private void IncreaseAll() => Service.ApplyStatDelta(step, step, step);

        [ContextMenu("Stats: randomize")]
        private void Randomize() => Service.SetStats(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));

        [ContextMenu("Stats: reset to 100")]
        private void ResetAll() => Service.SetStats(100f, 100f, 100f);

        [ContextMenu("Coins: +100")]
        private void AddCoins() => Service.AddCoins(100);

        [ContextMenu("Coins: -50")]
        private void SpendCoins() => Service.TrySpendCoins(50);

        [ContextMenu("Jar: +10")]
        private void AddJarCoins() => Service.AddJarCoins(10);

        [ContextMenu("Jar: +500")]
        private void AddJarCoinsBig() => Service.AddJarCoins(500);

        /// <summary>Until the finance planning screen exists — turns the dreams forecast on/off.</summary>
        [ContextMenu("Plan: 40 coins/day")]
        private void SetPlan40() => Service.SetPlannedSavingPerDay(40);

        [ContextMenu("Plan: off (0)")]
        private void SetPlanOff() => Service.SetPlannedSavingPerDay(0);

        /// <summary>
        /// One history line of every kind (today), incl. a long title — to check the history layout
        /// without playing. Only the log: coins/jar don't change. Note: they DO count as plan facts.
        /// </summary>
        [ContextMenu("History: add sample entries")]
        private void AddSampleHistory()
        {
            Service.LogMoney(MoneyKind.JobIncome, 40, "Полить растения");
            Service.LogMoney(MoneyKind.BuyNeed, 30, "Яблоко", 5);
            Service.LogMoney(MoneyKind.Cashback, 3, "Яблоко");
            Service.LogMoney(MoneyKind.BuyWant, 20, "Мороженое с вишнёвым сиропом и шоколадной крошкой");
            Service.LogMoney(MoneyKind.JarDeposit, 25);
            Service.LogMoney(MoneyKind.JarWithdraw, 10);
            Service.LogMoney(MoneyKind.DreamPurchase, 500, "Дом");
            Service.LogMoney(MoneyKind.PlanReward, 5, "Награда Барсука за план");
            Service.LogMoney(MoneyKind.DepositOpen, 50, "Вклад у Барсука на 3 дн.");
            Service.LogMoney(MoneyKind.DepositReturn, 50, "Барсук вернул вклад");
            Service.LogMoney(MoneyKind.DepositInterest, 5, "Проценты по вкладу");
        }

        /// <summary>As if one more job was collected — study themes open after N shifts.</summary>
        [ContextMenu("Study: +1 shift")]
        private void AddShift() => Service.AddShift();

        [ContextMenu("Study: +3 shifts")]
        private void AddThreeShifts()
        {
            Service.AddShift();
            Service.AddShift();
            Service.AddShift();
        }

        /// <summary>Passes the first available study task as if answered right (promotion included).</summary>
        [ContextMenu("Study: pass next task")]
        private void PassNextStudyTask()
        {
            bool passed = ServiceLocator.Get<StudyService>().DebugPassNextTask(out string title);
            Debug.Log(passed ? $"StatsDebug: passed study task «{title}»." : "StatsDebug: no available study task (need more shifts?).");
        }

        /// <summary>Forgets passed tasks and shifts, level back to 1.</summary>
        [ContextMenu("Study: reset (tasks, shifts, level 1)")]
        private void ResetStudy()
        {
            Service.ResetStudy();
            Service.SetLevel(1);
        }

        /// <summary>Pretends a real day passed: finishes the finance plan when its period is over, moves the Barsuk deposit closer to maturity.</summary>
        [ContextMenu("Day: +1 (debug time skip)")]
        private void SkipDay()
        {
            GameDay.AddDebugDays(1);
            ServiceLocator.Get<FinanceService>().CheckPeriod();
            Debug.Log($"StatsDebug: day offset +1, today = {GameDay.ToDate(GameDay.Today):dd.MM.yyyy}.");
        }

        /// <summary>Back to real time. Entries/plans made on skipped days stay in the "future".</summary>
        [ContextMenu("Day: reset debug offset")]
        private void ResetDayOffset()
        {
            GameDay.ResetDebugOffset();
            Debug.Log("StatsDebug: day offset reset.");
        }
    }
}
