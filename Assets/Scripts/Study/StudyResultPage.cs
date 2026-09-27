using System;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>
    /// Result of a submitted task: verdict ("Верно!" / "Можно лучше"), what happened, why, the lesson,
    /// promotion banner if the answer finished the theme. "Дальше" → main page, "Ещё раз" (only
    /// after a wrong answer) → the same task from the start.
    /// </summary>
    public class StudyResultPage : MonoBehaviour
    {
        [SerializeField] private TMP_Text verdictLabel;
        [SerializeField] private Color passColor = new Color(0.25f, 0.6f, 0.3f);
        [SerializeField] private Color failColor = new Color(0.85f, 0.45f, 0.2f);
        [Tooltip("Shown only on a correct answer (e.g. a green check / happy Barsuk). Optional.")]
        [SerializeField] private GameObject passedDecor;
        [Tooltip("Shown only on a wrong answer. Optional.")]
        [SerializeField] private GameObject failedDecor;

        [Tooltip("\"Что получилось\" — outcome lines. Hidden when empty.")]
        [SerializeField] private TMP_Text outcomeLabel;
        [Tooltip("\"Почему так\"")]
        [SerializeField] private TMP_Text explanationLabel;
        [Tooltip("\"Вывод: …\" Hidden when empty.")]
        [SerializeField] private TMP_Text lessonLabel;

        [Header("Promotion")]
        [Tooltip("Banner shown when this answer finished the theme.")]
        [SerializeField] private GameObject promotionRoot;
        [SerializeField] private TMP_Text promotionLabel;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [Tooltip("Shown only after a wrong answer.")]
        [SerializeField] private Button retryButton;

        private Action _onNext;
        private Action _onRetry;

        private void Awake()
        {
            nextButton.onClick.AddListener(() => _onNext?.Invoke());
            retryButton.onClick.AddListener(() => _onRetry?.Invoke());
        }

        /// <summary>Called by StudyScreen right after activating this page.</summary>
        public void Show(StudyResult result, Action onNext, Action onRetry)
        {
            _onNext = onNext;
            _onRetry = onRetry;

            verdictLabel.text = result.Verdict;
            verdictLabel.color = result.Passed ? passColor : failColor;
            StudyViewUtils.SetActive(passedDecor, result.Passed);
            StudyViewUtils.SetActive(failedDecor, !result.Passed);

            StudyViewUtils.SetOptional(outcomeLabel, string.Join("\n", result.Outcomes));
            StudyViewUtils.SetText(explanationLabel, result.Explanation);
            StudyViewUtils.SetOptional(lessonLabel, string.IsNullOrEmpty(result.Lesson) ? "" : $"Вывод: {result.Lesson}");

            StudyViewUtils.SetActive(promotionRoot, result.Promoted);
            if (result.Promoted)
            {
                StudyViewUtils.SetText(promotionLabel, $"Повышение! Теперь ты {result.NewCareerTitle} — работа стала прибыльнее.");
            }

            retryButton.gameObject.SetActive(!result.Passed);
        }
    }
}
