using System.Collections.Generic;
using Content;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// What happens when a calendar day ends (economy v3, ECONOMY_PLAN.md — "игровой день = реальный день"), in this
    /// order: the day's cashback goes to the jar in one sum → food older than foodExpiryDays spoils → passive stat
    /// decay → low-stat penalties (decay + penalties once per day passed, so 3 days away = 3 times). Called by
    /// MailService.CheckDay right after it has snapshotted the finished day, and its lines go into that day's letter.
    /// No deaths/blocks — only numbers.
    /// </summary>
    public class DayService
    {
        /// <summary>Longer absences don't add anything new: every stat is at 0 long before that.</summary>
        private const int MaxDaysProcessed = 60;

        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;

        public DayService(PlayerDataService playerData, ContentDatabase content)
        {
            _playerData = playerData;
            _content = content;
        }

        private EconomySettings Economy => _content.Economy;

        /// <summary>From the end of lastDay to the start of today. Returns the letter lines ("Кешбэк за день: +5 в копилку", ...).</summary>
        public List<string> ProcessNewDays(int lastDay, int today)
        {
            var notes = new List<string>();
            int days = Mathf.Clamp(today - lastDay, 0, MaxDaysProcessed);
            if (days == 0)
            {
                return notes;
            }

            // 1. Cashback of the finished day.
            int cashback = _playerData.TakePendingCashback();
            if (cashback > 0)
            {
                _playerData.AddJarCoins(cashback);
                _playerData.LogMoney(MoneyKind.Cashback, cashback, "Кешбэк за день");
                notes.Add($"Кешбэк за покупки: +{cashback} в копилку.");
            }

            // 2. Spoiled food.
            int expiry = Economy.foodExpiryDays;
            if (expiry > 0)
            {
                Dictionary<string, int> spoiled = _playerData.RemoveSpoiled(stack =>
                    _content.FindProduct(stack.productId)?.IsFood == true && today - stack.purchasedDay >= expiry);
                if (spoiled.Count > 0)
                {
                    var parts = new List<string>();
                    foreach (KeyValuePair<string, int> pair in spoiled)
                    {
                        string title = _content.FindProduct(pair.Key)?.title.ToLowerInvariant() ?? pair.Key;
                        parts.Add($"{title} ×{pair.Value}");
                    }

                    notes.Add($"Испортилась еда: {string.Join(", ", parts)}.");
                }
            }

            // 3–4. Passive decay and low-stat penalties, once per day passed.
            CharacterStats s = _playerData.Stats;
            float satiety = s.satiety, mood = s.mood, health = s.health;
            float startSatiety = satiety, startMood = mood, startHealth = health;
            bool hungry = false, sick = false, sad = false;

            for (int i = 0; i < days; i++)
            {
                satiety = Mathf.Clamp(satiety - Economy.passiveDecayPerDay.satiety, 0f, 100f);
                mood = Mathf.Clamp(mood - Economy.passiveDecayPerDay.mood, 0f, 100f);
                health = Mathf.Clamp(health - Economy.passiveDecayPerDay.health, 0f, 100f);

                // Checked on the values after the decay, in the document's order.
                if (satiety < Economy.lowStatThreshold)
                {
                    health = Mathf.Max(0f, health - Economy.hungryHealthPenalty);
                    hungry = true;
                }

                if (health < Economy.lowStatThreshold)
                {
                    mood = Mathf.Max(0f, mood - Economy.sickMoodPenalty);
                    sick = true;
                }

                if (mood < Economy.lowStatThreshold)
                {
                    satiety = Mathf.Max(0f, satiety - Economy.sadSatietyPenalty);
                    sad = true;
                }
            }

            _playerData.SetStats(satiety, mood, health);

            string changes = StatChanges(satiety - startSatiety, mood - startMood, health - startHealth);
            if (!string.IsNullOrEmpty(changes))
            {
                notes.Add(days > 1 ? $"Тебя не было {UI.RuPlural.Days(days)}: {changes}." : $"За ночь: {changes}.");
            }

            if (hungry) notes.Add("Питомец голодал — от голода упало здоровье.");
            if (sick) notes.Add("Питомец приболел — от этого упало счастье.");
            if (sad) notes.Add("Питомцу грустно — пропал аппетит.");

            return notes;
        }

        /// <summary>"сытость −5, здоровье −2" (only what changed).</summary>
        private static string StatChanges(float satiety, float mood, float health)
        {
            var parts = new List<string>();
            if (Mathf.RoundToInt(satiety) != 0) parts.Add($"сытость {Signed(satiety)}");
            if (Mathf.RoundToInt(mood) != 0) parts.Add($"счастье {Signed(mood)}");
            if (Mathf.RoundToInt(health) != 0) parts.Add($"здоровье {Signed(health)}");
            return string.Join(", ", parts);
        }

        private static string Signed(float value)
        {
            int n = Mathf.RoundToInt(value);
            return n > 0 ? $"+{n}" : $"−{-n}";
        }
    }
}
