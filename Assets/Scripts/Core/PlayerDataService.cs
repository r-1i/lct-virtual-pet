using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Core
{
    /// <summary>The only place allowed to mutate PlayerData. Every method here saves immediately and publishes the matching EventBus event.</summary>
    public class PlayerDataService
    {
        /// <summary>Food on the table for the tutorial's "нажми на еду" step — a fresh save has no coins to buy any.</summary>
        private const string StarterProductId = "apple";
        private const int StarterProductCount = 3;

        private readonly SaveService _saveService;
        private readonly PlayerData _data;

        public PlayerDataService(SaveService saveService)
        {
            _saveService = saveService;
            _data = _saveService.Load();
            GiveStarterItems();
        }

        /// <summary>Once per save. Runs in the constructor (inside Bootstrap.Awake), so the events it publishes have no listeners yet — everyone reads the state in Start anyway.</summary>
        private void GiveStarterItems()
        {
            if (_data.starterItemsGiven)
            {
                return;
            }

            _data.starterItemsGiven = true;
            AddInventory(StarterProductId, StarterProductCount);
        }

        public int Coins => _data.coins;
        public int JarCoins => _data.jarCoins;
        public int Level => _data.level;

        /// <summary>Read-only snapshot. To change stats use ApplyStatDelta or SetStats — do not mutate the returned object's fields directly, it won't save or notify anyone.</summary>
        public CharacterStats Stats => _data.stats;
        public JobRuntimeState Job => _data.job;
        public bool IsFirstLaunch => !_data.firstLaunchCompleted;
        public int TutorialStep => _data.tutorialStep;
        public bool HasTutorialHighlight(string key) => _data.tutorialHighlights.Contains(key);
        public string CurrentDreamId => _data.currentDreamId;
        public bool IsDreamPurchased(string dreamId) => _data.purchasedDreamIds.Contains(dreamId);
        public int PlannedSavingPerDay => _data.plannedSavingPerDay;

        /// <summary>Read-only snapshots — change them only through the methods below (FinanceService does).</summary>
        public FinancePlan Plan => _data.plan;
        public PlanResult LastPlanResult => _data.lastPlanResult;
        public BankDeposit Deposit => _data.deposit;
        public IReadOnlyList<MoneyEntry> History => _data.history;
        public int ShiftsCompleted => _data.shiftsCompleted;
        public bool IsStudyTaskPassed(string taskId) => _data.passedStudyTasks.Contains(taskId);

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

        /// <summary>Wallet → jar. False without changing anything if amount <= 0 or the wallet has less.</summary>
        public bool TryMoveCoinsToJar(int amount)
        {
            if (amount <= 0 || _data.coins < amount)
            {
                return false;
            }

            _data.coins -= amount;
            _data.jarCoins += amount;
            SaveAndPublishCoins();
            LogMoney(MoneyKind.JarDeposit, amount);
            return true;
        }

        /// <summary>Jar → wallet. False without changing anything if amount <= 0 or the jar has less.</summary>
        public bool TryMoveJarToCoins(int amount)
        {
            if (amount <= 0 || _data.jarCoins < amount)
            {
                return false;
            }

            _data.jarCoins -= amount;
            _data.coins += amount;
            SaveAndPublishCoins();
            LogMoney(MoneyKind.JarWithdraw, amount);
            return true;
        }

        /// <summary>Only DreamService should call this. Empty id = no goal (every dream bought).</summary>
        public void SetCurrentDream(string dreamId)
        {
            _data.currentDreamId = dreamId ?? "";
            Save();
            EventBus.Publish(new DreamChangedEvent(_data.currentDreamId));
        }

        /// <summary>Only DreamService should call this. Pays the price from the jar and marks the dream bought. Does not pick the next goal — DreamService does that right after.</summary>
        public bool TryPurchaseDream(string dreamId, int price)
        {
            if (price < 0 || _data.jarCoins < price || IsDreamPurchased(dreamId))
            {
                return false;
            }

            _data.jarCoins -= price;
            _data.purchasedDreamIds.Add(dreamId);
            SaveAndPublishCoins();
            return true;
        }

        /// <summary>For the finance planning screen.</summary>
        public void SetPlannedSavingPerDay(int coinsPerDay)
        {
            _data.plannedSavingPerDay = Mathf.Max(0, coinsPerDay);
            Save();
            EventBus.Publish(new DreamChangedEvent(_data.currentDreamId));
        }

        /// <summary>
        /// Adds a history line (today, now). Coin changes themselves are NOT done here — call this next
        /// to the real AddCoins/TrySpendCoins/etc. Jar moves (TryMoveCoinsToJar/TryMoveJarToCoins) log
        /// themselves; everything else is logged by whoever knows what the money was for.
        /// </summary>
        public void LogMoney(MoneyKind kind, int amount, string title = "", int count = 0)
        {
            if (amount <= 0)
            {
                return;
            }

            _data.history.Add(new MoneyEntry
            {
                day = GameDay.Today,
                unix = GameDay.NowUnix,
                kind = kind,
                amount = amount,
                title = title ?? "",
                count = count
            });

            SaveAndPublishFinance();
        }

        /// <summary>Only FinanceService should call this.</summary>
        public void PruneHistoryBefore(int day)
        {
            if (_data.history.RemoveAll(e => e.day < day) > 0)
            {
                SaveAndPublishFinance();
            }
        }

        /// <summary>Only FinanceService should call this. Copies the values, the argument isn't kept.</summary>
        public void SetPlan(int startDay, int days, int income, int need, int want, int jar)
        {
            _data.plan = new FinancePlan
            {
                startDay = startDay,
                days = days,
                income = income,
                need = need,
                want = want,
                jar = jar
            };
            SaveAndPublishFinance();
        }

        /// <summary>Only FinanceService should call this.</summary>
        public void ClearPlan()
        {
            _data.plan = new FinancePlan();
            SaveAndPublishFinance();
        }

        /// <summary>Only FinanceService should call this.</summary>
        public void SetPlanResult(PlanResult result)
        {
            _data.lastPlanResult = result;
            SaveAndPublishFinance();
        }

        /// <summary>Only FinanceService should call this.</summary>
        public void MarkPlanResultSeen()
        {
            if (_data.lastPlanResult.seen)
            {
                return;
            }

            _data.lastPlanResult.seen = true;
            SaveAndPublishFinance();
        }

        /// <summary>Only FinanceService should call this. Coins must already be taken from the wallet.</summary>
        public void SetDeposit(int amount, int bonus, int days, long openedAtUnix)
        {
            _data.deposit = new BankDeposit
            {
                amount = amount,
                bonus = bonus,
                days = days,
                openedAtUnix = openedAtUnix
            };
            SaveAndPublishFinance();
        }

        /// <summary>Only FinanceService should call this.</summary>
        public void ClearDeposit()
        {
            _data.deposit = new BankDeposit();
            SaveAndPublishFinance();
        }

        /// <summary>Called by JobService on every collected job.</summary>
        public void AddShift()
        {
            _data.shiftsCompleted++;
            SaveAndPublishStudy();
        }

        /// <summary>Only StudyService should call this.</summary>
        public void MarkStudyTaskPassed(string taskId)
        {
            if (_data.passedStudyTasks.Contains(taskId))
            {
                return;
            }

            _data.passedStudyTasks.Add(taskId);
            SaveAndPublishStudy();
        }

        /// <summary>Debug only (StatsDebug): forget passed tasks and shifts.</summary>
        public void ResetStudy()
        {
            _data.passedStudyTasks.Clear();
            _data.shiftsCompleted = 0;
            SaveAndPublishStudy();
        }

        private void SaveAndPublishStudy()
        {
            Persist();
            EventBus.Publish(new StudyChangedEvent());
        }

        private void SaveAndPublishFinance()
        {
            Persist();
            EventBus.Publish(new FinanceChangedEvent());
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

        /// <summary>Called by IntroCutscene after its last slide, so quitting mid-cutscene replays it next launch.</summary>
        public void CompleteFirstLaunch()
        {
            _data.firstLaunchCompleted = true;
            Save();
        }

        /// <summary>Only TutorialService should call this.</summary>
        public void SetTutorialStep(int step)
        {
            _data.tutorialStep = step;
            Save();
        }

        /// <summary>Only TutorialService should call this.</summary>
        public void AddTutorialHighlight(string key)
        {
            if (!_data.tutorialHighlights.Contains(key))
            {
                _data.tutorialHighlights.Add(key);
                Save();
            }
        }

        /// <summary>Only TutorialService should call this. Returns false if the key wasn't active.</summary>
        public bool RemoveTutorialHighlight(string key)
        {
            if (!_data.tutorialHighlights.Remove(key))
            {
                return false;
            }

            Save();
            return true;
        }

        /// <summary>Only TutorialService should call this.</summary>
        public void ClearTutorialHighlights()
        {
            _data.tutorialHighlights.Clear();
            Save();
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
