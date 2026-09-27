using System.Collections.Generic;
using System.Linq;
using Content;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Money planning ("Планирование"): one active plan for 1..maxPlanDays real calendar days that splits
    /// planned job income into Надо / Хочу / Копилка; facts are summed from the money history; when the
    /// period is over the plan is evaluated (reward if it matched, hints why if not) and cleared.
    /// Also Barsuk's deposit: coins from the wallet for 1..depositMaxDays days, back with interest.
    /// CheckPeriod must be called periodically (Bootstrap does) — a day can end while the game is open.
    /// </summary>
    public class FinanceService
    {
        private readonly PlayerDataService _playerData;
        private readonly ContentDatabase _content;
        private int _lastCheckedDay = int.MinValue;

        public FinanceService(PlayerDataService playerData, ContentDatabase content)
        {
            _playerData = playerData;
            _content = content;
            CheckPeriod();
        }

        public FinanceSettings Settings => _content.Finance;

        // ---------- Plan ----------

        public FinancePlan Plan => _playerData.Plan;
        public bool HasPlan => Plan.IsActive;

        /// <summary>1-based day of the active plan ("день 2 из 3"). 0 if no plan.</summary>
        public int CurrentPlanDay => HasPlan ? Mathf.Clamp(GameDay.Today - Plan.startDay + 1, 1, Plan.days) : 0;

        public PlanFacts CurrentFacts => HasPlan ? GetFacts(Plan) : new PlanFacts();

        public PlanResult LastResult => _playerData.LastPlanResult;
        public bool HasUnseenResult => LastResult.exists && !LastResult.seen;

        /// <summary>
        /// Creates a plan starting today, or edits the active one (its start day stays; days can't go
        /// below the day we're already on). need + want + jar must equal income.
        /// </summary>
        public bool TrySavePlan(int days, int income, int need, int want, int jar, out string error)
        {
            int minDays = HasPlan ? CurrentPlanDay : 1;
            if (days < minDays || days > Settings.maxPlanDays)
            {
                error = $"Срок плана: от {minDays} до {Settings.maxPlanDays} дней.";
                return false;
            }

            if (income <= 0 || need < 0 || want < 0 || jar < 0 || need + want + jar != income)
            {
                error = "Надо + Хочу + Копилка должны быть равны доходу.";
                return false;
            }

            int startDay = HasPlan ? Plan.startDay : GameDay.Today;
            _playerData.SetPlan(startDay, days, income, need, want, jar);
            _playerData.SetPlannedSavingPerDay(Mathf.RoundToInt(jar / (float)days));
            error = null;
            return true;
        }

        public void CancelPlan()
        {
            if (!HasPlan)
            {
                return;
            }

            _playerData.ClearPlan();
            _playerData.SetPlannedSavingPerDay(0);
        }

        public void MarkResultSeen() => _playerData.MarkPlanResultSeen();

        /// <summary>Evaluates a finished plan and trims old history. Cheap to call often — does real work once per day.</summary>
        public void CheckPeriod()
        {
            int today = GameDay.Today;
            if (today == _lastCheckedDay)
            {
                return;
            }

            _lastCheckedDay = today;
            _playerData.PruneHistoryBefore(today - Settings.historyKeepDays + 1);

            if (HasPlan && today > Plan.EndDay)
            {
                FinishPlan();
            }
        }

        private void FinishPlan()
        {
            FinancePlan plan = Plan;
            PlanFacts facts = GetFacts(plan);
            bool success = IsSuccess(plan, facts);
            int reward = success ? Settings.planRewardCoins : 0;

            if (reward > 0)
            {
                _playerData.AddCoins(reward);
                _playerData.LogMoney(MoneyKind.PlanReward, reward, "Награда Барсука за план");
            }

            _playerData.SetPlanResult(new PlanResult
            {
                exists = true,
                seen = false,
                success = success,
                reward = reward,
                plan = new FinancePlan
                {
                    startDay = plan.startDay,
                    days = plan.days,
                    income = plan.income,
                    need = plan.need,
                    want = plan.want,
                    jar = plan.jar
                },
                facts = facts
            });

            _playerData.ClearPlan();
            _playerData.SetPlannedSavingPerDay(0);
        }

        public PlanFacts GetFacts(FinancePlan plan)
        {
            var facts = new PlanFacts();
            foreach (MoneyEntry entry in _playerData.History)
            {
                if (entry.day < plan.startDay || entry.day > plan.EndDay)
                {
                    continue;
                }

                switch (entry.kind)
                {
                    case MoneyKind.JobIncome: facts.income += entry.amount; break;
                    case MoneyKind.BuyNeed: facts.need += entry.amount; break;
                    case MoneyKind.BuyWant: facts.want += entry.amount; break;
                    case MoneyKind.JarDeposit: facts.jarIn += entry.amount; break;
                    case MoneyKind.JarWithdraw: facts.jarOut += entry.amount; break;
                }
            }

            return facts;
        }

        // Success rules: Надо — at least the plan, Хочу — at most the plan, Копилка — at least the plan.
        public static bool IsNeedOk(FinancePlan plan, PlanFacts facts) => facts.need >= plan.need;
        public static bool IsWantOk(FinancePlan plan, PlanFacts facts) => facts.want <= plan.want;
        public static bool IsJarOk(FinancePlan plan, PlanFacts facts) => facts.JarNet >= plan.jar;

        public static bool IsSuccess(FinancePlan plan, PlanFacts facts) =>
            IsNeedOk(plan, facts) && IsWantOk(plan, facts) && IsJarOk(plan, facts);

        /// <summary>"Why didn't it match" — every rule that fired, the main cause first. Empty if the plan matched.</summary>
        public static List<string> BuildHints(FinancePlan plan, PlanFacts facts)
        {
            var hints = new List<string>();
            int wantOver = facts.want - plan.want;
            int needShort = plan.need - facts.need;
            int needOver = facts.need - plan.need;
            int jarShort = plan.jar - facts.JarNet;
            int incomeShort = plan.income - facts.income;

            if (wantOver > 0)
            {
                hints.Add($"На «Хочу» ушло {facts.want} — на {wantOver} больше плана.");
                if (needShort > 0)
                {
                    hints.Add($"Из-за этого на «Надо» не хватило {needShort}: питомцу могло не хватить еды.");
                }
            }
            else if (needShort > 0)
            {
                hints.Add($"На «Надо» потрачено {facts.need} из {plan.need}: питомцу могло не хватить еды.");
            }

            if (jarShort > 0)
            {
                if (facts.jarOut > 0)
                {
                    hints.Add($"Ты забрал из копилки {facts.jarOut}, поэтому в неё ушло {facts.JarNet} вместо {plan.jar}. Мечта отодвинулась.");
                }
                else
                {
                    hints.Add($"В копилку отложено {facts.JarNet} из {plan.jar}. Мечта отодвинулась.");
                }

                if (needOver > 0 && wantOver <= 0)
                {
                    hints.Add($"На «Надо» ушло на {needOver} больше плана — эти монеты не попали в копилку.");
                }
            }

            if (incomeShort > 0 && hints.Count > 0)
            {
                hints.Add($"Ты заработал {facts.income} вместо {plan.income} — на {incomeShort} меньше, чем планировал.");
            }

            return hints;
        }

        /// <summary>Reward promise or a warning about something already broken in the current plan (for the "план и факт" block while the plan is running).</summary>
        public string BuildLiveHint()
        {
            if (!HasPlan)
            {
                return "";
            }

            PlanFacts facts = CurrentFacts;
            int wantOver = facts.want - Plan.want;
            if (wantOver > 0)
            {
                return $"На «Хочу» уже на {wantOver} больше плана. Следи, чтобы на «Надо» хватило!";
            }

            if (facts.jarOut > 0 && !IsJarOk(Plan, facts))
            {
                return $"Ты забрал из копилки {facts.jarOut}. Верни, чтобы план сошёлся.";
            }

            return $"Совпадёт план и факт — Барсук даст +{Settings.planRewardCoins}. Разойдётся — подскажем почему.";
        }

        /// <summary>Rewards of the jobs at the player's level — for the "сколько я заработаю" hint in the plan editor.</summary>
        public bool TryGetJobRewardRange(out int min, out int max)
        {
            List<int> rewards = _content.JobsForLevel(_playerData.Level).Select(j => j.reward).ToList();
            min = rewards.Count > 0 ? rewards.Min() : 0;
            max = rewards.Count > 0 ? rewards.Max() : 0;
            return rewards.Count > 0;
        }

        // ---------- Barsuk deposit ----------

        public BankDeposit Deposit => _playerData.Deposit;
        public bool HasDeposit => Deposit.IsActive;

        public float DepositRemainingSeconds => HasDeposit ? Mathf.Max(0f, Deposit.MaturesAtUnix - GameDay.NowUnix) : 0f;
        public bool IsDepositMature => HasDeposit && DepositRemainingSeconds <= 0f;

        public int RatePercent(int days)
        {
            int[] rates = Settings.depositRatePercentByDays;
            if (rates == null || rates.Length == 0 || days <= 0)
            {
                return 0;
            }

            return rates[Mathf.Min(days, rates.Length) - 1];
        }

        public int BonusFor(int amount, int days) => Mathf.RoundToInt(amount * RatePercent(days) / 100f);

        public bool TryOpenDeposit(int amount, int days, out string error)
        {
            if (HasDeposit)
            {
                error = "У Барсука уже лежит вклад.";
                return false;
            }

            if (days < 1 || days > Settings.depositMaxDays)
            {
                error = $"Срок вклада: от 1 до {Settings.depositMaxDays} дней.";
                return false;
            }

            if (amount < Settings.depositMinAmount)
            {
                error = $"Минимум {Settings.depositMinAmount} монет.";
                return false;
            }

            if (!_playerData.TrySpendCoins(amount))
            {
                error = "Не хватает монет.";
                return false;
            }

            _playerData.SetDeposit(amount, BonusFor(amount, days), days, GameDay.NowUnix);
            _playerData.LogMoney(MoneyKind.DepositOpen, amount, $"Вклад у Барсука на {days} дн.");
            error = null;
            return true;
        }

        /// <summary>After the term: amount + bonus back to the wallet.</summary>
        public bool TryCollectDeposit()
        {
            if (!IsDepositMature)
            {
                return false;
            }

            BankDeposit deposit = Deposit;
            int amount = deposit.amount;
            int bonus = deposit.bonus;

            _playerData.ClearDeposit();
            _playerData.AddCoins(amount + bonus);
            _playerData.LogMoney(MoneyKind.DepositReturn, amount, "Барсук вернул вклад");
            _playerData.LogMoney(MoneyKind.DepositInterest, bonus, "Проценты по вкладу");
            return true;
        }

        /// <summary>Before the term: only the amount comes back, no bonus.</summary>
        public bool TryWithdrawDepositEarly()
        {
            if (!HasDeposit || IsDepositMature)
            {
                return false;
            }

            int amount = Deposit.amount;
            _playerData.ClearDeposit();
            _playerData.AddCoins(amount);
            _playerData.LogMoney(MoneyKind.DepositReturn, amount, "Вклад забран раньше срока");
            return true;
        }
    }
}
