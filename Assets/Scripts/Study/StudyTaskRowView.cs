using System;
using Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>One task line inside a theme card: «Зарплата пришла» + "✓ пройдено" / "Начать" / locked (previous task not passed). A fixed slot placed by hand.</summary>
    public class StudyTaskRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [Tooltip("\"✓ пройдено\". Shown when passed.")]
        [SerializeField] private GameObject passedRoot;
        [Tooltip("\"Начать →\". Shown when the task can be started.")]
        [SerializeField] private Button startButton;
        [Tooltip("Lock icon / \"сначала предыдущее\". Shown when an earlier task isn't passed. Optional.")]
        [SerializeField] private GameObject lockedRoot;

        private Action _onStart;

        private void Awake()
        {
            startButton.onClick.AddListener(() => _onStart?.Invoke());
        }

        public void Setup(StudyTaskDefinition task, bool passed, bool available, Action onStart)
        {
            _onStart = onStart;
            titleLabel.text = $"«{task.title}»";
            StudyViewUtils.SetActive(passedRoot, passed);
            startButton.gameObject.SetActive(available);
            StudyViewUtils.SetActive(lockedRoot, !passed && !available);
        }
    }
}
