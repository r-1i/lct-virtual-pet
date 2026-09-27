using System;
using Content;
using UnityEngine;

namespace Core
{
    /// <summary>Single source of truth for whether the character is currently working. State (activeJobId, startedAtUnix, durationSeconds) lives in PlayerData; everything derived (remaining time, ready-to-collect) is computed from real time, so an app restart resumes correctly.</summary>
    public class JobService
    {
        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;
        private readonly TutorialService _tutorial;

        public JobService(PlayerDataService playerData, ContentDatabase content, TutorialService tutorial)
        {
            _playerData = playerData;
            _content = content;
            _tutorial = tutorial;
        }

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

        /// <summary>Fails if already working, the job doesn't exist in content, or it isn't the player's current level (jobs are level-specific, see PLAN.md section 3).</summary>
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

            // Tutorial "сейчас мы не будем ждать": the job picked on that step is done instantly.
            float duration = _tutorial.IsWaitingFor(TutorialStepIds.FirstMoney) ? 0f : definition.durationSeconds;

            _playerData.SetJob(jobId, duration);
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
            rewardCoins = definition?.reward ?? 0;

            if (rewardCoins > 0)
            {
                _playerData.AddCoins(rewardCoins);
                _playerData.LogMoney(MoneyKind.JobIncome, rewardCoins, definition.title);
            }

            _playerData.ClearJob();
            _playerData.AddShift();
            return true;
        }
    }
}
