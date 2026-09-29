using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Short "what just changed" message after every player action (ТЗ 2.5.9, ECONOMY_PLAN.md V10): "−75 монет · +5 средний корм",
    /// "+30 сытости", "Смена началась: … · −5 сытости · −3 счастья". Called explicitly where the action happens (not guessed from
    /// events). Messages sent before the FeedbackToast exists (e.g. the day change inside Bootstrap) wait in the queue.
    /// </summary>
    public static class Feedback
    {
        private static readonly Queue<string> Pending = new Queue<string>();

        /// <summary>Shown one after another by the FeedbackToast in the scene.</summary>
        public static void Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            Debug.Log($"Feedback: {message}");
            if (FeedbackToast.Instance != null)
            {
                FeedbackToast.Instance.Enqueue(message);
            }
            else
            {
                Pending.Enqueue(message);
            }
        }

        /// <summary>FeedbackToast takes whatever was sent before it woke up.</summary>
        internal static bool TryTakePending(out string message)
        {
            if (Pending.Count > 0)
            {
                message = Pending.Dequeue();
                return true;
            }

            message = null;
            return false;
        }

        /// <summary>Scene reload / new Play Mode session without a domain reload: old messages must not leak in.</summary>
        internal static void ClearPending() => Pending.Clear();

        /// <summary>Parts joined with " · ", empty ones skipped.</summary>
        public static string Join(params string[] parts) => string.Join(" · ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

        /// <summary>"+16 монет" / "−81 монета" (nominative — RuPlural.Coins is accusative: "81 монету").</summary>
        public static string Coins(int delta)
        {
            int n = Mathf.Abs(delta);
            return $"{(delta >= 0 ? "+" : "−")}{n} {RuPlural.Pick(n, "монета", "монеты", "монет")}";
        }

        /// <summary>Stats right now — take one before the action, one after, and pass both to StatChanges.</summary>
        public static StatsSnapshot Snapshot(PlayerDataService playerData)
        {
            CharacterStats s = playerData.Stats;
            return new StatsSnapshot(s.satiety, s.mood, s.health);
        }

        /// <summary>"+30 сытости · −3 счастья" — only what really changed (overflow over 100 isn't counted). Empty if nothing did.</summary>
        public static string StatChanges(StatsSnapshot before, StatsSnapshot after)
        {
            var parts = new List<string>();
            AddStat(parts, after.Satiety - before.Satiety, "сытости");
            AddStat(parts, after.Mood - before.Mood, "счастья");
            AddStat(parts, after.Health - before.Health, "здоровья");
            return string.Join(" · ", parts);
        }

        private static void AddStat(List<string> parts, float delta, string genitive)
        {
            int n = Mathf.RoundToInt(delta);
            if (n != 0)
            {
                parts.Add(n > 0 ? $"+{n} {genitive}" : $"−{-n} {genitive}");
            }
        }

        public readonly struct StatsSnapshot
        {
            public readonly float Satiety;
            public readonly float Mood;
            public readonly float Health;

            public StatsSnapshot(float satiety, float mood, float health)
            {
                Satiety = satiety;
                Mood = mood;
                Health = health;
            }
        }
    }
}
