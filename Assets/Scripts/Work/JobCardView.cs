using System;
using Content;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work
{
    /// <summary>One row in the job list. Instantiated by JobListView, one per job available at the player's current level.</summary>
    public class JobCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text durationLabel;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private Button selectButton;

        private string _jobId;
        private Action<string> _onSelect;

        public void Setup(JobDefinition job, Action<string> onSelect)
        {
            _jobId = job.id;
            _onSelect = onSelect;

            if (titleLabel != null)
            {
                titleLabel.text = job.title;
            }

            if (durationLabel != null)
            {
                durationLabel.text = JobFormat.Duration(job.durationSeconds);
            }

            if (rewardLabel != null)
            {
                rewardLabel.text = $"+{job.reward} монет";
            }

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            _onSelect?.Invoke(_jobId);
        }
    }
}
