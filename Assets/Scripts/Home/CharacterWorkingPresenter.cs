using System;
using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;

namespace Home
{
    /// <summary>
    /// Hides the character while it's working, everywhere except the Work zone, where it stays
    /// visible with a semi-transparent grey overlay material and a "На работе: чч:мм:сс" label.
    /// Driven entirely by JobService + ZoneManager — CharacterMotor doesn't know any of this exists.
    /// </summary>
    public class CharacterWorkingPresenter : MonoBehaviour
    {
        [SerializeField] private ZoneManager zoneManager;
        [Tooltip("Index into ZoneManager's Zones array that is the Work zone.")]
        [SerializeField] private int workZoneIndex = -1;
        [Tooltip("Leave empty to auto-collect every Renderer under this object at Awake.")]
        [SerializeField] private Renderer[] renderers;
        [Tooltip("Material created from Shaders/CharacterWorkingOverlay.shader.")]
        [SerializeField] private Material workingOverlayMaterial;
        [Tooltip("Optional. Shown only while on the Work zone and the character is working.")]
        [SerializeField] private TMP_Text workingLabel;
        [SerializeField] private string workingLabelFormat = "На работе: {0}";
        [SerializeField] private string readyLabelText = "Работа готова!";

        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private JobService _jobService;
        private bool _labelTicking;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }

            foreach (Renderer r in renderers)
            {
                _originalMaterials[r] = r.sharedMaterials;
            }
        }

        private void Start()
        {
            _jobService = ServiceLocator.Get<JobService>();

            EventBus.Subscribe<JobStateChangedEvent>(OnJobStateChanged);
            EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);

            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<JobStateChangedEvent>(OnJobStateChanged);
            EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
        }

        private void OnJobStateChanged(JobStateChangedEvent e) => Refresh();
        private void OnZoneChanged(ZoneChangedEvent e) => Refresh();

        private void Refresh()
        {
            bool isWorking = _jobService.IsWorking;
            bool onWorkZone = zoneManager != null && zoneManager.CurrentZoneIndex == workZoneIndex;

            if (!isWorking)
            {
                SetHidden(false);
                SetOverlay(false);
                StopLabel();
            }
            else if (onWorkZone)
            {
                SetHidden(false);
                SetOverlay(true);
                StartLabel();
            }
            else
            {
                SetHidden(true);
                StopLabel();
            }
        }

        private void SetHidden(bool hidden)
        {
            foreach (Renderer r in renderers)
            {
                r.enabled = !hidden;
            }
        }

        private void SetOverlay(bool withOverlay)
        {
            foreach (Renderer r in renderers)
            {
                Material[] original = _originalMaterials[r];

                if (!withOverlay || workingOverlayMaterial == null)
                {
                    r.materials = original;
                    continue;
                }

                var combined = new Material[original.Length + 1];
                Array.Copy(original, combined, original.Length);
                combined[original.Length] = workingOverlayMaterial;
                r.materials = combined;
            }
        }

        private void StartLabel()
        {
            if (workingLabel == null || _labelTicking)
            {
                return;
            }

            _labelTicking = true;
            InvokeRepeating(nameof(UpdateLabel), 0f, 1f);
        }

        private void StopLabel()
        {
            if (workingLabel != null)
            {
                workingLabel.gameObject.SetActive(false);
            }

            if (!_labelTicking)
            {
                return;
            }

            _labelTicking = false;
            CancelInvoke(nameof(UpdateLabel));
        }

        private void UpdateLabel()
        {
            if (workingLabel == null)
            {
                return;
            }

            workingLabel.gameObject.SetActive(true);

            if (_jobService.IsReadyToCollect)
            {
                workingLabel.text = readyLabelText;
                return;
            }

            TimeSpan span = TimeSpan.FromSeconds(_jobService.RemainingSeconds);
            workingLabel.text = string.Format(workingLabelFormat, $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}");
        }
    }
}
