using System.Collections.Generic;
using Content;
using Core;
using UnityEngine;

namespace Work
{
    /// <summary>
    /// Work screen root. Shows the job list for the player's current level, or the active-job card
    /// if a job is already running. Rebuilds/switches automatically on JobStateChangedEvent and
    /// LevelChangedEvent — no other script needs to poke this one.
    /// </summary>
    public class JobListView : MonoBehaviour
    {
        [SerializeField] private JobCardView cardPrefab;
        [SerializeField] private RectTransform listContent;
        [Tooltip("Parent of the scrollable list (e.g. the ScrollRect root). Shown when not working.")]
        [SerializeField] private GameObject listPanel;
        [Tooltip("ActiveJobView's GameObject. Shown while a job is running.")]
        [SerializeField] private GameObject activeJobPanel;

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

            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<JobStateChangedEvent>(OnJobStateChanged);
            EventBus.Unsubscribe<LevelChangedEvent>(OnLevelChanged);
        }

        private void OnJobStateChanged(JobStateChangedEvent e) => Refresh();
        private void OnLevelChanged(LevelChangedEvent e) => Refresh();

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

            foreach (JobDefinition job in _content.JobsForLevel(_playerData.Level))
            {
                JobCardView card = Instantiate(cardPrefab, listContent);
                card.Setup(job, OnJobSelected);
                _spawned.Add(card);
            }
        }

        private void OnJobSelected(string jobId)
        {
            if (_jobService.TryStartJob(jobId, out string error))
            {
                return; // TryStartJob publishes JobStateChangedEvent, which triggers Refresh() itself.
            }

            Debug.LogWarning($"Work: couldn't start job '{jobId}': {error}");
        }
    }
}
