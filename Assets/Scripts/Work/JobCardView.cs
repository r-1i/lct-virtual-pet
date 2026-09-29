using System;
using System.Collections.Generic;
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
        [Tooltip("Optional. What the shift takes from the pet: \"−15 сытости  −5 здоровья  −6 счастья\".")]
        [SerializeField] private TMP_Text costsLabel;
        [SerializeField] private Button selectButton;

        private string _jobId;
        private Action<string> _onSelect;

        /// <param name="expectedReward">What the shift pays if started now (stats and rank included) — JobService.ExpectedReward.</param>
        public void Setup(JobDefinition job, ShiftType shift, int expectedReward, bool canStart, Action<string> onSelect)
        {
            _jobId = job.id;
            _onSelect = onSelect;

            if (titleLabel != null)
            {
                titleLabel.text = job.title;
            }

            if (durationLabel != null)
            {
                durationLabel.text = shift != null ? JobFormat.Duration(shift.DurationSeconds) : "";
            }

            if (rewardLabel != null)
            {
                rewardLabel.text = $"+{expectedReward} монет";
            }

            if (costsLabel != null)
            {
                costsLabel.text = shift != null ? FormatCosts(shift) : "";
            }

            selectButton.interactable = canStart;
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(HandleClick);
        }

        /// <summary>"−15 сытости  −5 здоровья  −6 счастья", zero costs skipped.</summary>
        public static string FormatCosts(ShiftType shift)
        {
            var parts = new List<string>();
            if (shift.satietyCost > 0f) parts.Add($"−{shift.satietyCost:0} сытости");
            if (shift.healthCost > 0f) parts.Add($"−{shift.healthCost:0} здоровья");
            if (shift.moodCost > 0f) parts.Add($"−{shift.moodCost:0} счастья");
            return string.Join("  ", parts);
        }

        private void HandleClick()
        {
            _onSelect?.Invoke(_jobId);
        }
    }
}
