using System;
using Content;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Single source of truth for whether the character is currently working. State (activeJobId, startedAtUnix,
    /// durationSeconds, reward) lives in PlayerData; everything derived (remaining time, ready-to-collect) is computed
    /// from real time, so an app restart resumes correctly.
    /// Economy v3 (ECONOMY_PLAN.md): a job is a shift type from economy.json (light / normal / heavy) — duration, base
    /// income and stat costs come from there. Pay = base × rank × stats, fixed when the shift starts (stats before the
    /// costs); the costs are taken right at the start; at most maxShiftsPerDay shifts per calendar day.
    /// </summary>
    public class JobService
    {
        public const string TiredMessage = "Я устал, пора спать. Приходи завтра";

        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;
        private readonly TutorialService _tutorial;

        public JobService(PlayerDataService playerData, ContentDatabase content, TutorialService tutorial)
        {
            _playerData = playerData;
            _content = content;
            _tutorial = tutorial;
        }

        private EconomySettings Economy => _content.Economy;

        public bool IsWorking => _playerData.Job.IsActive;

        public JobDefinition ActiveJobDefinition => IsWorking ? _content.FindJob(_playerData.Job.activeJobId) : null;

        public float RemainingSeconds
        {
            get
            {
                if (!IsWorking)
                {
                    return 0f;
                }

                long elapsed = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _playerData.Job.startedAtUnix;
                return Mathf.Max(0f, _playerData.Job.durationSeconds - elapsed);
            }
        }

        public bool IsReadyToCollect => IsWorking && RemainingSeconds <= 0f;

        /// <summary>Coins the running shift will pay (fixed at its start).</summary>
        public int ActiveReward => IsWorking ? ResolveReward(ActiveJobDefinition) : 0;

        // ---------- shift types / limit ----------

        public ShiftType ShiftOf(JobDefinition job) => job != null ? Economy.FindShift(job.shift) : null;

        public int ShiftsToday => _playerData.ShiftsToday;
        public int MaxShiftsPerDay => Economy.maxShiftsPerDay;

        /// <summary>The tutorial's "не будем ждать" shift is instant and doesn't count towards the limit.</summary>
        private bool IsTutorialShift => _tutorial.IsWaitingFor(TutorialStepIds.FirstMoney);

        public bool IsShiftLimitReached => !IsTutorialShift && ShiftsToday >= MaxShiftsPerDay;

        /// <summary>What this job would pay if started right now (current stats and rank).</summary>
        public int ExpectedReward(JobDefinition job)
        {
            CharacterStats s = _playerData.Stats;
            return Economy.ShiftReward(ShiftOf(job), _playerData.Level, s.satiety, s.mood, s.health);
        }

        /// <summary>Fails if already working, the job doesn't exist / isn't this level's / has an unknown shift type, or today's shifts are used up.</summary>
        public bool TryStartJob(string jobId, out string error)
        {
            if (IsWorking)
            {
                error = "Персонаж уже занят другой работой.";
                return false;
            }

            JobDefinition definition = _content.FindJob(jobId);
            if (definition == null)
            {
                error = $"Работа '{jobId}' не найдена в контенте.";
                return false;
            }

            if (definition.level != _playerData.Level)
            {
                error = $"Работа '{jobId}' недоступна на уровне {_playerData.Level}.";
                return false;
            }

            ShiftType shift = ShiftOf(definition);
            if (shift == null)
            {
                error = $"У работы '{jobId}' неизвестный тип смены '{definition.shift}' (нет в economy.json).";
                return false;
            }

            bool tutorial = IsTutorialShift;
            if (!tutorial && ShiftsToday >= MaxShiftsPerDay)
            {
                error = TiredMessage;
                return false;
            }

            // Pay is fixed from the stats before this shift's costs.
            int reward = ExpectedReward(definition);

            // Tutorial "сейчас мы не будем ждать": the job picked on that step is done instantly.
            _playerData.SetJob(jobId, tutorial ? 0f : shift.DurationSeconds, reward);
            _playerData.ApplyStatDelta(-shift.satietyCost, -shift.moodCost, -shift.healthCost);
            if (!tutorial)
            {
                _playerData.RegisterShiftStarted();
            }

            error = null;
            return true;
        }

        /// <summary>Fails (returns false, 0 coins) if the job isn't finished yet. Grants the reward and clears the job otherwise.</summary>
        public bool TryCollect(out int rewardCoins)
        {
            if (!IsReadyToCollect)
            {
                rewardCoins = 0;
                return false;
            }

            JobDefinition definition = ActiveJobDefinition;
            rewardCoins = ResolveReward(definition);

            if (rewardCoins > 0)
            {
                _playerData.AddCoins(rewardCoins);
                _playerData.LogMoney(MoneyKind.JobIncome, rewardCoins, definition?.title ?? "");
            }

            _playerData.ClearJob();
            _playerData.AddShift();
            return true;
        }

        /// <summary>The reward saved at the start; a job started before economy v3 has none saved — count it from the stats now.</summary>
        private int ResolveReward(JobDefinition definition)
        {
            int saved = _playerData.Job.reward;
            return saved > 0 ? saved : ExpectedReward(definition);
        }
    }
}
