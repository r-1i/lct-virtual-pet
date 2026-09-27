using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>0..100 gauges shown as the three status bars (Сытость/Настроение/Здоровье).</summary>
    [Serializable]
    public class CharacterStats
    {
        public float satiety = 100f;
        public float mood = 100f;
        public float health = 100f;
    }

    /// <summary>The currently active (or finished-but-not-collected) job. activeJobId empty = not working.</summary>
    [Serializable]
    public class JobRuntimeState
    {
        public string activeJobId = "";
        public long startedAtUnix;
        public float durationSeconds;

        public bool IsActive => !string.IsNullOrEmpty(activeJobId);
    }

    /// <summary>
    /// One purchase batch of a product. Kept separate per purchase (not merged into a single
    /// productId→count entry) because different batches can have different expiry — bought at
    /// different times, so they go bad at different times. Removal of expired stacks is deferred
    /// (see PLAN.md backlog), but the data already supports it since purchasedAtUnix is per stack.
    /// </summary>
    [Serializable]
    public class InventoryStack
    {
        public string stackId;
        public string productId;
        public int count;
        public long purchasedAtUnix;
    }

    /// <summary>What happened to the money in one history entry. Explicit values — they're stored as ints in save.json, never renumber.</summary>
    public enum MoneyKind
    {
        JobIncome = 0,
        BuyNeed = 1,
        BuyWant = 2,
        Cashback = 3,
        JarDeposit = 4,
        JarWithdraw = 5,
        DreamPurchase = 6,
        PlanReward = 7,
        DepositOpen = 8,
        DepositReturn = 9,
        DepositInterest = 10
    }

    /// <summary>One line of the spending history. Plan facts are summed from these too, so the history is the single source of truth for "what actually happened".</summary>
    [Serializable]
    public class MoneyEntry
    {
        /// <summary>GameDay index the entry belongs to.</summary>
        public int day;
        public long unix;
        public MoneyKind kind;
        /// <summary>Always positive; the direction comes from kind.</summary>
        public int amount;
        public string title = "";
        /// <summary>Item count for purchases, 0 otherwise.</summary>
        public int count;
    }

    /// <summary>A money plan for 1..N real calendar days. days = 0 means no plan. need + want + jar = income.</summary>
    [Serializable]
    public class FinancePlan
    {
        public int startDay;
        public int days;
        public int income;
        public int need;
        public int want;
        public int jar;

        public bool IsActive => days > 0;
        public int EndDay => startDay + days - 1;
    }

    /// <summary>What actually happened in a plan's period, summed from the history.</summary>
    [Serializable]
    public class PlanFacts
    {
        public int income;
        public int need;
        public int want;
        public int jarIn;
        public int jarOut;

        public int JarNet => jarIn - jarOut;
    }

    /// <summary>Outcome of the last finished plan. Kept until the next one finishes, shown as the banner on the planning screen.</summary>
    [Serializable]
    public class PlanResult
    {
        public bool exists;
        /// <summary>False until the player closed the result popup.</summary>
        public bool seen;
        public bool success;
        public int reward;
        public FinancePlan plan = new FinancePlan();
        public PlanFacts facts = new PlanFacts();
    }

    /// <summary>Barsuk's deposit. amount = 0 means none. Coins leave the wallet on open and come back (with bonus if matured) on collect.</summary>
    [Serializable]
    public class BankDeposit
    {
        public int amount;
        public int bonus;
        public int days;
        public long openedAtUnix;

        public bool IsActive => amount > 0;
        public long MaturesAtUnix => openedAtUnix + days * 86400L;
    }

    /// <summary>Everything that gets written to save.json. Only PlayerDataService is allowed to mutate an instance of this.</summary>
    [Serializable]
    public class PlayerData
    {
        public int coins;
        public int jarCoins;
        public int level = 1;
        public CharacterStats stats = new CharacterStats();
        public JobRuntimeState job = new JobRuntimeState();
        public List<InventoryStack> inventory = new List<InventoryStack>();

        /// <summary>The dream jarCoins are being saved towards. Empty only when every dream is bought — DreamService picks one otherwise.</summary>
        public string currentDreamId = "";

        /// <summary>Bought dreams, forever (they'll show up in the reserve later).</summary>
        public List<string> purchasedDreamIds = new List<string>();

        /// <summary>Coins per day the player plans to put in the jar. Set by the (future) finance planning screen; 0 = not planned, no forecast is shown.</summary>
        public int plannedSavingPerDay;

        public FinancePlan plan = new FinancePlan();
        public PlanResult lastPlanResult = new PlanResult();
        public BankDeposit deposit = new BankDeposit();

        /// <summary>Oldest first. Trimmed to FinanceSettings.historyKeepDays by FinanceService.</summary>
        public List<MoneyEntry> history = new List<MoneyEntry>();

        /// <summary>Jobs collected ("Забрать") over the whole game — opens study themes.</summary>
        public int shiftsCompleted;

        /// <summary>Ids of study tasks passed at least once.</summary>
        public List<string> passedStudyTasks = new List<string>();

        /// <summary>False until the intro cutscene has been watched to the end — then it never plays again. False (not "isFirstLaunch = true") so a save without this field still reads as a first launch.</summary>
        public bool firstLaunchCompleted;

        /// <summary>Starter food (for the feeding tutorial) already handed out — see PlayerDataService.GiveStarterItems.</summary>
        public bool starterItemsGiven;

        /// <summary>Index into tutorial.json of the next tutorial popup to show. >= step count = tutorial finished.</summary>
        public int tutorialStep;

        /// <summary>TutorialHighlight keys that are pulsing right now (dismissed hint, target not clicked yet). Saved so a restart doesn't lose them.</summary>
        public List<string> tutorialHighlights = new List<string>();
    }
}
