using Content;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Savings goals ("Мои мечты"). The jar is shared by all goals: progress of the current dream is
    /// simply jarCoins, so switching goals never loses money. There is always a current dream until
    /// every dream is bought — a missing/bought/unknown current id is fixed on construction and after
    /// each purchase by picking the next unbought one in dreams.json order.
    /// </summary>
    public class DreamService
    {
        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;

        public DreamService(PlayerDataService playerData, ContentDatabase content)
        {
            _playerData = playerData;
            _content = content;
            EnsureCurrentDream();
        }

        /// <summary>Null only when every dream is bought.</summary>
        public DreamDefinition CurrentDream => _content.FindDream(_playerData.CurrentDreamId);

        public bool AllDreamsPurchased => CurrentDream == null;

        public bool IsPurchased(string dreamId) => _playerData.IsDreamPurchased(dreamId);

        /// <summary>How many coins the current dream still needs. 0 = can be bought (or no dream).</summary>
        public int RemainingForCurrent
        {
            get
            {
                DreamDefinition dream = CurrentDream;
                return dream == null ? 0 : Mathf.Max(0, dream.price - _playerData.JarCoins);
            }
        }

        public bool CanPurchaseCurrent => CurrentDream != null && RemainingForCurrent == 0;

        /// <summary>Days of planned saving to cover this many coins. False if nothing is planned (plannedSavingPerDay = 0) — then no forecast is shown at all.</summary>
        public bool TryGetDaysFor(int coins, out int days)
        {
            int perDay = _playerData.PlannedSavingPerDay;
            if (perDay <= 0)
            {
                days = 0;
                return false;
            }

            days = Mathf.CeilToInt(Mathf.Max(0, coins) / (float)perDay);
            return true;
        }

        /// <summary>Any unbought dream, any time. Money stays in the jar and counts towards the new goal.</summary>
        public bool TrySelectDream(string dreamId)
        {
            if (_content.FindDream(dreamId) == null || IsPurchased(dreamId))
            {
                return false;
            }

            if (dreamId != _playerData.CurrentDreamId)
            {
                _playerData.SetCurrentDream(dreamId);
            }

            return true;
        }

        /// <summary>Pays the current dream from the jar (the rest stays in the jar), then moves on to the next unbought dream after it.</summary>
        public bool TryPurchaseCurrent()
        {
            DreamDefinition dream = CurrentDream;
            if (dream == null || !_playerData.TryPurchaseDream(dream.id, dream.price))
            {
                return false;
            }

            _playerData.LogMoney(MoneyKind.DreamPurchase, dream.price, dream.title);
            _playerData.SetCurrentDream(FindNextUnpurchasedId(dream.id));
            return true;
        }

        private void EnsureCurrentDream()
        {
            string currentId = _playerData.CurrentDreamId;
            if (_content.FindDream(currentId) != null && !IsPurchased(currentId))
            {
                return;
            }

            string next = FindNextUnpurchasedId(currentId);
            if (next != currentId)
            {
                _playerData.SetCurrentDream(next);
            }
        }

        /// <summary>First unbought dream after afterId in dreams.json order, wrapping around; from the start if afterId isn't in the list. Empty if all are bought.</summary>
        private string FindNextUnpurchasedId(string afterId)
        {
            int count = _content.Dreams.Count;
            int start = 0;
            for (int i = 0; i < count; i++)
            {
                if (_content.Dreams[i].id == afterId)
                {
                    start = i + 1;
                    break;
                }
            }

            for (int offset = 0; offset < count; offset++)
            {
                DreamDefinition dream = _content.Dreams[(start + offset) % count];
                if (!IsPurchased(dream.id))
                {
                    return dream.id;
                }
            }

            return "";
        }
    }
}
