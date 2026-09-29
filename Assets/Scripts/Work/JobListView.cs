using System.Collections.Generic;
using Content;
using Core;
using TMPro;
using UnityEngine;

namespace Work
{
    /// <summary>
    /// Work screen root. Shows the job list for the player's current level, or the active-job card
    /// if a job is already running. Rebuilds/switches automatically on JobStateChangedEvent,
    /// LevelChangedEvent and StatsChangedEvent (the expected pay depends on the stats) — no other script
    /// needs to poke this one. At most maxShiftsPerDay shifts a day: then the "Выбрать" buttons go grey and the
    /// shifts label says the pet is tired.
    /// </summary>
    public class JobListView : MonoBehaviour
    {
        [SerializeField] private JobCardView cardPrefab;
        [SerializeField] private RectTransform listContent;
        [Tooltip("Parent of the scrollable list (e.g. the ScrollRect root). Shown when not working.")]
        [SerializeField] private GameObject listPanel;
        [Tooltip("ActiveJobView's GameObject. Shown while a job is running.")]
        [SerializeField] private GameObject activeJobPanel;
        [Tooltip("Optional. \"Смен сегодня: 1 / 2\", or the pet's \"Я устал…\" when the limit is reached. Shown with the list.")]
        [SerializeField] private TMP_Text shiftsLabel;
        [Tooltip("Optional. The label's background/holder — hidden together with it while a job is running.")]
        [SerializeField] private GameObject shiftsPanel;
        [SerializeField] private string shiftsFormat = "Смен сегодня: {0} / {1}";

        private ContentDatabase _content;
        private PlayerDataService _playerData;
        private JobService _jobService;
        private readonly List<JobCardView> _spawned = new List<JobCardView>();

        private void Start()
        {
            _content = ServiceLocator.Get<ContentDatabase>();
            _playerData = ServiceLocator.Get<PlayerDataService>();
            _jobService = ServiceLocator.Get<JobService>();

            EventBus.Subscribe<JobStateChangedEvent>(OnJobStateChanged);
            EventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);
            EventBus.Subscribe<StatsChangedEvent>(OnStatsChanged);
            EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);

            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<JobStateChangedEvent>(OnJobStateChanged);
            EventBus.Unsubscribe<LevelChangedEvent>(OnLevelChanged);
            EventBus.Unsubscribe<StatsChangedEvent>(OnStatsChanged);
            EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
        }

        private void OnJobStateChanged(JobStateChangedEvent e) => Refresh();
        private void OnLevelChanged(LevelChangedEvent e) => Refresh();
        private void OnStatsChanged(StatsChangedEvent e) => Refresh();
        // Coming back to the screen on another day: the shift count starts over.
        private void OnZoneChanged(ZoneChangedEvent e) => Refresh();

        private void Refresh()
        {
            bool working = _jobService.IsWorking;

            if (listPanel != null)
            {
                listPanel.SetActive(!working);
            }

            if (activeJobPanel != null)
            {
                activeJobPanel.SetActive(working);
            }

            if (shiftsPanel != null)
            {
                shiftsPanel.SetActive(!working);
            }

            if (shiftsLabel != null)
            {
                shiftsLabel.gameObject.SetActive(!working);
                shiftsLabel.text = _jobService.IsShiftLimitReached
                    ? JobService.TiredMessage
                    : string.Format(shiftsFormat, _jobService.ShiftsToday, _jobService.MaxShiftsPerDay);
            }

            if (!working)
            {
                RebuildList();
            }
        }

        private void RebuildList()
        {
            foreach (JobCardView card in _spawned)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            _spawned.Clear();

            // Buttons stay tappable when today's shifts are used up: the tap is what makes the pet say it's tired (doc v3).
            foreach (JobDefinition job in _content.JobsForLevel(_playerData.Level))
            {
                JobCardView card = Instantiate(cardPrefab, listContent);
                card.Setup(job, _jobService.ShiftOf(job), _jobService.ExpectedReward(job), true, OnJobSelected);
                _spawned.Add(card);
            }
        }

        private void OnJobSelected(string jobId)
        {
            UI.Feedback.StatsSnapshot before = UI.Feedback.Snapshot(_playerData);
            if (_jobService.TryStartJob(jobId, out string error))
            {
                // TryStartJob publishes JobStateChangedEvent, which triggers Refresh() itself.
                string title = _content.FindJob(jobId)?.title ?? "";
                UI.Feedback.Show(UI.Feedback.Join($"Смена началась: {title}", UI.Feedback.StatChanges(before, UI.Feedback.Snapshot(_playerData))));
                return;
            }

            // Usually the daily limit ("Я устал…") — an expected answer, not a problem.
            UI.Feedback.Show(error);
            Debug.Log($"Work: couldn't start job '{jobId}': {error}");
        }
    }
}
