using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    /// <summary>
    /// "История трат" sub-panel: a plain feed, newest day first. Each day = a HistoryDayView header
    /// (date + day totals) followed by its HistoryEntryView lines, newest first. Rebuilt on every
    /// Open and on FinanceChangedEvent while open.
    /// </summary>
    public class HistoryPanel : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [Tooltip("Content of the vertical ScrollRect (Vertical Layout Group + Content Size Fitter).")]
        [SerializeField] private RectTransform content;
        [SerializeField] private HistoryDayView dayPrefab;
        [SerializeField] private HistoryEntryView entryPrefab;
        [Tooltip("\"Пока пусто\" — shown when there's no history. Optional.")]
        [SerializeField] private GameObject emptyState;
        [Tooltip("Scrolled back to the top on Open. Optional.")]
        [SerializeField] private ScrollRect scrollRect;

        private PlayerDataService _playerData;
        private Action _onClose;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        private void Awake()
        {
            backButton.onClick.AddListener(() => _onClose?.Invoke());
        }

        // Only matters while the panel is open, and it's only ever opened at runtime (after Bootstrap cleared the bus).
        private void OnEnable()
        {
            EventBus.Subscribe<FinanceChangedEvent>(OnFinanceChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<FinanceChangedEvent>(OnFinanceChanged);
        }

        /// <summary>Called by PlanningScreen right after activating this panel.</summary>
        public void Open(Action onClose)
        {
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            _onClose = onClose;
            Rebuild();

            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void OnFinanceChanged(FinanceChangedEvent e)
        {
            if (_playerData != null && isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            foreach (GameObject go in _spawned)
            {
                Destroy(go);
            }

            _spawned.Clear();

            IReadOnlyList<MoneyEntry> history = _playerData.History;
            if (emptyState != null)
            {
                emptyState.SetActive(history.Count == 0);
            }

            foreach (IGrouping<int, MoneyEntry> day in history.GroupBy(e => e.day).OrderByDescending(g => g.Key))
            {
                HistoryDayView dayView = Instantiate(dayPrefab, content);
                dayView.Setup(day.Key, day);
                _spawned.Add(dayView.gameObject);

                foreach (MoneyEntry entry in day.OrderByDescending(e => e.unix))
                {
                    HistoryEntryView entryView = Instantiate(entryPrefab, content);
                    entryView.Setup(entry);
                    _spawned.Add(entryView.gameObject);
                }
            }
        }
    }
}
