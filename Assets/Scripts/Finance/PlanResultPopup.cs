using System.Collections.Generic;
using Core;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    /// <summary>
    /// Result of a finished plan, over any zone: pops up by itself as soon as there's an unseen result
    /// (first launch after the period ended, or the day ending while playing), and again from the
    /// banner on the planning screen. Plan vs fact rows, reward, and every "why it didn't match" hint.
    /// Lives on an always-active object; SetActive's only its child root (same as TutorialPopup).
    /// </summary>
    public class PlanResultPopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [Tooltip("On root, for the fade. Optional.")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleLabel;
        [Tooltip("\"Барсук доволен: награда +5 монет\" / \"Барсук разберёт с тобой, что пошло не так\"")]
        [SerializeField] private TMP_Text subtitleLabel;
        [Tooltip("\"План на 26–28 сентября\". Optional.")]
        [SerializeField] private TMP_Text periodLabel;
        [SerializeField] private PlanFactRowView needRow;
        [SerializeField] private PlanFactRowView wantRow;
        [SerializeField] private PlanFactRowView jarRow;
        [Tooltip("\"Заработано 90 из 120\". Optional.")]
        [SerializeField] private TMP_Text incomeLabel;
        [Tooltip("Hints, one per line.")]
        [SerializeField] private TMP_Text hintsLabel;
        [SerializeField] private Button okButton;
        [SerializeField] private float fadeDuration = 0.2f;

        private FinanceService _finance;

        private float Duration => AnimationSettings.Enabled ? fadeDuration : 0f;

        private void Start()
        {
            _finance = ServiceLocator.Get<FinanceService>();
            okButton.onClick.AddListener(Hide);
            EventBus.Subscribe<FinanceChangedEvent>(OnFinanceChanged);

            root.SetActive(false);
            ShowIfUnseen();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<FinanceChangedEvent>(OnFinanceChanged);
        }

        private void OnFinanceChanged(FinanceChangedEvent e) => ShowIfUnseen();

        private void ShowIfUnseen()
        {
            if (_finance.HasUnseenResult && !root.activeSelf)
            {
                ShowLastResult();
            }
        }

        public void ShowLastResult()
        {
            PlanResult result = _finance.LastResult;
            if (!result.exists)
            {
                return;
            }

            FinancePlan plan = result.plan;
            PlanFacts facts = result.facts;

            titleLabel.text = result.success ? "План сошёлся с фактом!" : "План не сошёлся";
            subtitleLabel.text = result.success
                ? $"Барсук доволен: награда +{RuPlural.Coins(result.reward)}"
                : "Барсук разберёт с тобой, что пошло не так";

            if (periodLabel != null)
            {
                periodLabel.text = $"План на {FinanceFormat.Period(plan.startDay, plan.days)}";
            }

            needRow.Set(plan.need, facts.need, PlanRule.AtLeast, true);
            wantRow.Set(plan.want, facts.want, PlanRule.AtMost, true);
            jarRow.Set(plan.jar, facts.JarNet, PlanRule.AtLeast, true);

            if (incomeLabel != null)
            {
                incomeLabel.text = $"Заработано {facts.income} из {plan.income}";
            }

            List<string> hints = FinanceService.BuildHints(plan, facts);
            hintsLabel.text = hints.Count == 0
                ? "Всё по плану — так держать!"
                : "• " + string.Join("\n• ", hints);

            root.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, Duration);
            }
        }

        private void Hide()
        {
            _finance.MarkResultSeen();

            if (canvasGroup == null)
            {
                root.SetActive(false);
                return;
            }

            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, Duration).OnComplete(() => root.SetActive(false));
        }
    }
}
