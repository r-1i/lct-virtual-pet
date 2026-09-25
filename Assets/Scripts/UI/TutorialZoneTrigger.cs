using System.Linq;
using Content;
using Core;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Shows a tutorial step when the player enters a zone (ZoneChangedEvent). Zones are only known by
    /// their index in ZoneManager.zones, so both are set in the inspector. TryShow does nothing unless
    /// that step is the current one, so re-entering the zone later is harmless.
    /// </summary>
    public class TutorialZoneTrigger : MonoBehaviour
    {
        [Tooltip("Index of the zone in ZoneManager's Zones array.")]
        [SerializeField] private int zoneIndex;
        [Tooltip("Step id from tutorial.json.")]
        [SerializeField] private string stepId = TutorialStepIds.TapFood;

        private TutorialService _tutorial;

        private void Start()
        {
            _tutorial = ServiceLocator.Get<TutorialService>();
            EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);

            if (!ServiceLocator.Get<ContentDatabase>().TutorialSteps.Any(s => s.id == stepId))
            {
                Debug.LogWarning($"TutorialZoneTrigger on '{name}': step '{stepId}' not found in tutorial.json — this trigger will never show anything.", this);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
        }

        private void OnZoneChanged(ZoneChangedEvent e)
        {
            if (e.ZoneIndex == zoneIndex)
            {
                _tutorial.TryShow(stepId);
            }
        }
    }
}
