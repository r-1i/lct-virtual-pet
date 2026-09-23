using Content;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Work
{
    /// <summary>
    /// Shown instead of the job list while a job is running: title, live чч:мм:сс countdown, and a
    /// "Забрать" button that's only interactable once the job is done. Ticks via OnEnable/OnDisable,
    /// so JobListView just has to SetActive this GameObject — no manual start/stop calls needed.
    /// </summary>
    public class ActiveJobView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private Button collectButton;
        [SerializeField] private string inProgressFormat = "Осталось: {0}";
        [SerializeField] private string readyText = "Готово!";

        private JobService _jobService;

        private void OnEnable()
        {
            _jobService ??= ServiceLocator.Get<JobService>();

            collectButton.onClick.RemoveListener(HandleCollectClicked);
            collectButton.onClick.AddListener(HandleCollectClicked);

            InvokeRepeating(nameof(Tick), 0f, 1f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(Tick));
        }

        private void Tick()
        {
            if (!_jobService.IsWorking)
            {
                // JobStateChangedEvent will make JobListView hide this panel; nothing to draw until then.
                return;
            }

            JobDefinition definition = _jobService.ActiveJobDefinition;
            if (titleLabel != null)
            {
                titleLabel.text = definition != null ? definition.title : string.Empty;
            }

            bool ready = _jobService.IsReadyToCollect;
            collectButton.interactable = ready;

            if (timeLabel != null)
            {
                timeLabel.text = ready ? readyText : string.Format(inProgressFormat, JobFormat.Countdown(_jobService.RemainingSeconds));
            }
        }

        private void HandleCollectClicked()
        {
            if (_jobService.TryCollect(out int reward))
            {
                Debug.Log($"Work: collected {reward} coins.");
            }
        }
    }
}
