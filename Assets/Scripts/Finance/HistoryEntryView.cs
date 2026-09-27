using Core;
using TMPro;
using UnityEngine;

namespace Finance
{
    /// <summary>One line of the history feed: "Хочу: Мороженое", "14:32", "−20" (colored by where the money went).</summary>
    public class HistoryEntryView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text amountLabel;
        [Tooltip("\"14:32\". Optional.")]
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Color incomeColor = new Color(0.25f, 0.6f, 0.3f);
        [SerializeField] private Color spendColor = new Color(0.8f, 0.3f, 0.28f);
        [SerializeField] private Color jarColor = new Color(0.3f, 0.45f, 0.75f);

        public void Setup(MoneyEntry entry)
        {
            titleLabel.text = FinanceFormat.EntryTitle(entry);
            amountLabel.text = FinanceFormat.EntryAmount(entry);

            switch (FinanceFormat.Flow(entry.kind))
            {
                case MoneyFlow.WalletIn: amountLabel.color = incomeColor; break;
                case MoneyFlow.WalletOut: amountLabel.color = spendColor; break;
                default: amountLabel.color = jarColor; break;
            }

            if (timeLabel != null)
            {
                timeLabel.text = FinanceFormat.EntryTime(entry);
            }
        }
    }
}
