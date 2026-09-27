using System;

namespace Content
{
    /// <summary>Finance planning / Barsuk deposit balance, loaded from Resources/Content/finance.json. Read-only content, not save data.</summary>
    [Serializable]
    public class FinanceSettings
    {
        /// <summary>Coins to the wallet when a plan matches the fact.</summary>
        public int planRewardCoins = 5;

        /// <summary>A plan covers 1..maxPlanDays real calendar days.</summary>
        public int maxPlanDays = 3;

        /// <summary>Older history entries are dropped.</summary>
        public int historyKeepDays = 30;

        public int depositMinAmount = 10;

        /// <summary>Deposit term is 1..depositMaxDays days (default = max).</summary>
        public int depositMaxDays = 3;

        /// <summary>Interest per term: [0] = 1 day, [1] = 2 days, ... Terms past the array's end use the last value.</summary>
        public int[] depositRatePercentByDays = { 3, 6, 10 };
    }
}
