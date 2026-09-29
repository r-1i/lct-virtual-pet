using System;
using UnityEngine;

namespace Content
{
    /// <summary>
    /// Pet economy balance (shifts, income formula, day change, cashback, food expiry, start), loaded from
    /// Resources/Content/economy.json. Read-only content, not save data. Numbers come from the game designer's
    /// economy doc v3 — see ECONOMY_PLAN.md. The income formula lives here so there's exactly one copy of it.
    /// </summary>
    [Serializable]
    public class EconomySettings
    {
        /// <summary>light / normal / heavy. Jobs in jobs.json point to one of these by id.</summary>
        public ShiftType[] shiftTypes =
        {
            new ShiftType { id = "light", hours = 1f, baseIncome = 12, satietyCost = 5f, healthCost = 0f, moodCost = 3f },
            new ShiftType { id = "normal", hours = 3f, baseIncome = 30, satietyCost = 15f, healthCost = 5f, moodCost = 6f },
            new ShiftType { id = "heavy", hours = 9f, baseIncome = 60, satietyCost = 40f, healthCost = 15f, moodCost = 12f }
        };

        /// <summary>Income multiplier per rank: [0] = level 1 (Стажёр). Levels past the end use the last value.</summary>
        public float[] rankIncomeMultipliers = { 1f, 1.25f, 1.5f, 1.8f };

        /// <summary>Stat factor = (incomeBaseMin + (1 − incomeBaseMin) × min(satiety, health) / 100) × (1 + incomeMoodBonus × mood / 100).</summary>
        public float incomeBaseMin = 0.2f;
        public float incomeMoodBonus = 0.3f;

        /// <summary>Taken from the stats once per calendar day, before the low-stat penalties. Mood doesn't drop by itself.</summary>
        public StatAmounts passiveDecayPerDay = new StatAmounts { satiety = 5f, mood = 0f, health = 2f };

        /// <summary>A stat below this (after the daily decay) drags another one down, see the penalties below.</summary>
        public float lowStatThreshold = 30f;

        /// <summary>Satiety below the threshold → health −this (a hungry pet gets weak).</summary>
        public float hungryHealthPenalty = 8f;

        /// <summary>Health below the threshold → mood −this (being ill is sad).</summary>
        public float sickMoodPenalty = 8f;

        /// <summary>Mood below the threshold → satiety −this (no appetite under stress).</summary>
        public float sadSatietyPenalty = 5f;

        /// <summary>Shifts the pet can start per calendar day.</summary>
        public int maxShiftsPerDay = 2;

        /// <summary>Cashback from "Нужно" / "Хочу" purchases, % of the price. Collected over the day, paid into the jar at the day change.</summary>
        public float cashbackNeedPercent = 10f;
        public float cashbackWantPercent = 0f;

        /// <summary>Food (category food) spoils when a batch is this many calendar days old. Care items never spoil.</summary>
        public int foodExpiryDays = 3;

        /// <summary>Coins a brand-new profile starts with ("после аферы Лиса").</summary>
        public int startCoins = 50;

        /// <summary>Stats of a brand-new profile (existing saves keep theirs).</summary>
        public StatAmounts startStats = new StatAmounts { satiety = 70f, mood = 70f, health = 70f };

        public ShiftType FindShift(string id) => Array.Find(shiftTypes ?? Array.Empty<ShiftType>(), s => s.id == id);

        public float RankMultiplier(int level)
        {
            if (rankIncomeMultipliers == null || rankIncomeMultipliers.Length == 0)
            {
                return 1f;
            }

            return rankIncomeMultipliers[Mathf.Clamp(level - 1, 0, rankIncomeMultipliers.Length - 1)];
        }

        /// <summary>1 at 100/100/100, incomeBaseMin at satiety or health 0 and mood 0; the worse of satiety/health counts.</summary>
        public float StatIncomeFactor(float satiety, float mood, float health)
        {
            float basics = Mathf.Clamp(Mathf.Min(satiety, health), 0f, 100f) / 100f;
            float joy = Mathf.Clamp(mood, 0f, 100f) / 100f;
            return (incomeBaseMin + (1f - incomeBaseMin) * basics) * (1f + incomeMoodBonus * joy);
        }

        /// <summary>Coins for one shift: base × rank × stats, rounded to the nearest coin. 0 for an unknown shift.</summary>
        public int ShiftReward(ShiftType shift, int level, float satiety, float mood, float health)
        {
            if (shift == null)
            {
                return 0;
            }

            return Mathf.RoundToInt(shift.baseIncome * RankMultiplier(level) * StatIncomeFactor(satiety, mood, health));
        }
    }

    /// <summary>One kind of shift: how long it takes, what it pays before multipliers, what it costs the pet (taken at the start).</summary>
    [Serializable]
    public class ShiftType
    {
        public string id;
        public float hours;
        public int baseIncome;
        public float satietyCost;
        public float healthCost;
        public float moodCost;

        public float DurationSeconds => hours * 3600f;
    }

    /// <summary>Satiety / mood (счастье) / health amounts — same names as PlayerData.stats.</summary>
    [Serializable]
    public class StatAmounts
    {
        public float satiety;
        public float mood;
        public float health;

        public override string ToString() => $"сытость {satiety}, счастье {mood}, здоровье {health}";
    }
}
