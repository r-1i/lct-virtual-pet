using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;

namespace Finance
{
    /// <summary>Day header in the history feed: "Сегодня" + "Заработал +120 · Потратил −80 · Копилка +20".</summary>
    public class HistoryDayView : MonoBehaviour
    {
        [SerializeField] private TMP_Text dateLabel;
        [Tooltip("Day totals. Optional.")]
        [SerializeField] private TMP_Text totalsLabel;

        public void Setup(int day, IEnumerable<MoneyEntry> entries)
        {
            dateLabel.text = FinanceFormat.DayTitle(day);

            if (totalsLabel == null)
            {
                return;
            }

            int earned = 0;
            int spent = 0;
            int jar = 0;
            foreach (MoneyEntry entry in entries)
            {
                switch (entry.kind)
                {
                    case MoneyKind.JobIncome:
                    case MoneyKind.PlanReward:
                    case MoneyKind.DepositInterest:
                        earned += entry.amount;
                        break;
                    case MoneyKind.BuyNeed:
                    case MoneyKind.BuyWant:
                        spent += entry.amount;
                        break;
                    case MoneyKind.JarDeposit:
                    case MoneyKind.Cashback:
                        jar += entry.amount;
                        break;
                    case MoneyKind.JarWithdraw:
                    case MoneyKind.DreamPurchase:
                        jar -= entry.amount;
                        break;
                }
            }

            string jarText = jar >= 0 ? $"+{jar}" : $"−{-jar}";
            totalsLabel.text = $"Заработал +{earned} · Потратил −{spent} · Копилка {jarText}";
        }
    }
}
