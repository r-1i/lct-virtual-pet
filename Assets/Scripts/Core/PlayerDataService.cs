using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    /// <summary>The only place allowed to mutate PlayerData. Every method here saves immediately and publishes the matching EventBus event.</summary>
    public class PlayerDataService
    {
        private readonly SaveService _saveService;
        private readonly PlayerData _data;

        public PlayerDataService(SaveService saveService)
        {
            _saveService = saveService;
            _data = _saveService.Load();
        }

        public int Coins => _data.coins;
        public int JarCoins => _data.jarCoins;
        public int Level => _data.level;

        /// <summary>Read-only snapshot. To change stats use ApplyStatDelta or SetStats — do not mutate the returned object's fields directly, it won't save or notify anyone.</summary>
        public CharacterStats Stats => _data.stats;
        public JobRuntimeState Job => _data.job;

        /// <summary>Total across every batch of this product — what the UI shows as "x10".</summary>
        public int GetInventoryCount(string productId)
        {
            int total = 0;
            foreach (InventoryStack stack in _data.inventory)
            {
                if (stack.productId == productId)
                {
                    total += stack.count;
                }
            }

            return total;
        }

        /// <summary>Distinct product ids currently owned (count > 0), in no particular order — callers that need a stable display order (e.g. the food table) should sort themselves.</summary>
        public IEnumerable<string> GetOwnedProductIds()
        {
            return _data.inventory.Where(s => s.count > 0).Select(s => s.productId).Distinct();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || _data.coins < amount)
            {
                return false;
            }

            _data.coins -= amount;
            SaveAndPublishCoins();
            return true;
        }

        public void AddCoins(int amount)
        {
            _data.coins += amount;
            SaveAndPublishCoins();
        }

        public void AddJarCoins(int amount)
        {
            _data.jarCoins += amount;
            SaveAndPublishCoins();
        }

        /// <summary>Every purchase is its own batch (own purchasedAtUnix), even buying the same product twice in a row — see PLAN.md section 5.</summary>
        public void AddInventory(string productId, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _data.inventory.Add(new InventoryStack
            {
                stackId = Guid.NewGuid().ToString("N"),
                productId = productId,
                count = amount,
                purchasedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            Save();
            EventBus.Publish(new InventoryChangedEvent(productId, GetInventoryCount(productId)));
        }

        /// <summary>
        /// Consumes across batches oldest-first (FIFO — the one closest to expiring goes first),
        /// removing any batch that hits zero. Returns false without changing anything if the total
        /// owned is less than amount.
        /// </summary>
        public bool TryConsumeInventory(string productId, int amount = 1)
        {
            if (amount <= 0 || GetInventoryCount(productId) < amount)
            {
                return false;
            }

            int remaining = amount;
            foreach (InventoryStack stack in _data.inventory
                         .Where(s => s.productId == productId)
                         .OrderBy(s => s.purchasedAtUnix))
            {
                if (remaining <= 0)
                {
                    break;
                }

                int take = Mathf.Min(stack.count, remaining);
                stack.count -= take;
                remaining -= take;
            }

            _data.inventory.RemoveAll(s => s.count <= 0);

            Save();
            EventBus.Publish(new InventoryChangedEvent(productId, GetInventoryCount(productId)));
            return true;
        }

        public void SetStats(float satiety, float mood, float health)
        {
            _data.stats.satiety = Mathf.Clamp(satiety, 0f, 100f);
            _data.stats.mood = Mathf.Clamp(mood, 0f, 100f);
            _data.stats.health = Mathf.Clamp(health, 0f, 100f);
            Save();
            EventBus.Publish(new StatsChangedEvent(_data.stats));
        }

        public void ApplyStatDelta(float satietyDelta, float moodDelta, float healthDelta)
        {
            SetStats(_data.stats.satiety + satietyDelta, _data.stats.mood + moodDelta, _data.stats.health + healthDelta);
        }

        /// <summary>Temporary/manual for now — no XP system yet (see PLAN.md, section 8, point 3).</summary>
        public void SetLevel(int level)
        {
            _data.level = level;
            Save();
            EventBus.Publish(new LevelChangedEvent(level));
        }

        public void SetJob(string jobId, float durationSeconds)
        {
            _data.job.activeJobId = jobId;
            _data.job.startedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _data.job.durationSeconds = durationSeconds;
            Save();
            EventBus.Publish(new JobStateChangedEvent(_data.job));
        }

        public void ClearJob()
        {
            _data.job.activeJobId = "";
            _data.job.startedAtUnix = 0;
            _data.job.durationSeconds = 0f;
            Save();
            EventBus.Publish(new JobStateChangedEvent(_data.job));
        }

        private void SaveAndPublishCoins()
        {
            Persist();
            EventBus.Publish(new CoinsChangedEvent(_data.coins, _data.jarCoins));
        }

        /// <summary>Writes the current state to disk without changing anything. Bootstrap calls this right after loading (so save.json exists from the first run) and from OnApplicationPause as a safety net.</summary>
        public void Save()
        {
            Persist();
        }

        private void Persist()
        {
            _saveService.Save(_data);
        }
    }
}
