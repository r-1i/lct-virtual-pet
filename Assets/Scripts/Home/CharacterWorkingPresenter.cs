using System;
using System.Collections.Generic;
using Core;
using TMPro;
using UnityEngine;

namespace Home
{
    /// <summary>
    /// While a job is active (until its reward is collected): moves the character to Work Point (position + rotation),
    /// keeps isWorking on and holds off anything else (dance, talk, one-shot triggers), and locks ZoneManager so zone
    /// switches move only the camera. Hides the character everywhere except the Work zone, where it stays visible with
    /// a semi-transparent grey overlay material and a "На работе: чч:мм:сс" label. After collecting, the character goes
    /// back to the current zone's Character Target.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class CharacterWorkingPresenter : MonoBehaviour
    {
        [SerializeField] private ZoneManager zoneManager;
        [Tooltip("Index into ZoneManager's Zones array that is the Work zone.")]
        [SerializeField] private int workZoneIndex = -1;
        [SerializeField] private CharacterMotor motor;
        [Tooltip("Gives the Animator of the model shown right now.")]
        [SerializeField] private CharacterAppearance appearance;
        [Tooltip("Where the character stands while working. Both position and rotation are used.")]
        [SerializeField] private Transform workPoint;
        [Tooltip("Optional. If its music is still on when work ends, the dance resumes.")]
        [SerializeField] private Boombox boombox;
        [Tooltip("Leave empty to auto-collect every Renderer under this object at Awake.")]
        [SerializeField] private Renderer[] renderers;
        [Tooltip("Material created from Shaders/CharacterWorkingOverlay.shader.")]
        [SerializeField] private Material workingOverlayMaterial;
        [Tooltip("Optional. Shown only while on the Work zone and the character is working.")]
        [SerializeField] private TMP_Text workingLabel;
        [SerializeField] private string workingLabelFormat = "На работе: {0}";
        [SerializeField] private string readyLabelText = "Работа готова!";

        private static readonly int WorkingHash = Animator.StringToHash(CharacterAnimParams.Working);
        private static readonly int DancingHash = Animator.StringToHash(CharacterAnimParams.Dancing);
        private static readonly int TalkingHash = Animator.StringToHash(CharacterAnimParams.Talking);
        private static readonly int[] TriggerHashes =
        {
            Animator.StringToHash(CharacterAnimParams.Greet),
            Animator.StringToHash(CharacterAnimParams.Joy),
            Animator.StringToHash(CharacterAnimParams.Play),
            Animator.StringToHash(CharacterAnimParams.Eat),
        };

        private readonly Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
        private JobService _jobService;
        private bool _labelTicking;
        private bool _atWork;
        private Quaternion _rotationBeforeWork = Quaternion.identity;

        private Animator CurrentAnimator => appearance != null ? appearance.Animator : null;

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponentInParent<CharacterMotor>();
            }

            if (appearance == null)
            {
                appearance = GetComponentInParent<CharacterAppearance>();
            }

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

        /// <summary>Runs after every other Update (see DefaultExecutionOrder) but before the Animator evaluates, so anything
        /// that set Dance/Talk/a trigger this frame is overridden before it can start playing.</summary>
        private void Update()
        {
            if (_atWork)
            {
                HoldWorkAnimation();
            }
        }

        private void Refresh()
        {
            bool isWorking = _jobService.IsWorking;
            bool onWorkZone = zoneManager != null && zoneManager.CurrentZoneIndex == workZoneIndex;

            if (isWorking && !_atWork)
            {
                EnterWork();
            }
            else if (!isWorking && _atWork)
            {
                ExitWork();
            }

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

        private void EnterWork()
        {
            _atWork = true;

            if (zoneManager != null)
            {
                zoneManager.CharacterLocked = true;
            }

            if (motor != null && workPoint != null)
            {
                _rotationBeforeWork = motor.transform.rotation;
                motor.ForcePlace(workPoint.position, workPoint.rotation);
            }
            else
            {
                Debug.LogWarning("CharacterWorkingPresenter: Motor or Work Point isn't set — the character stays where it is.");
            }

            HoldWorkAnimation();
        }

        private void ExitWork()
        {
            _atWork = false;

            Animator animator = CurrentAnimator;
            if (animator != null)
            {
                animator.SetBool(WorkingHash, false);
            }

            if (zoneManager != null)
            {
                zoneManager.CharacterLocked = false;
                zoneManager.PlaceCharacterAtCurrentZone(_rotationBeforeWork);
            }

            if (boombox != null)
            {
                boombox.ReapplyPlayerDance();
            }
        }

        private void HoldWorkAnimation()
        {
            Animator animator = CurrentAnimator;
            if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.SetBool(WorkingHash, true);
            animator.SetBool(DancingHash, false);
            animator.SetBool(TalkingHash, false);

            foreach (int trigger in TriggerHashes)
            {
                animator.ResetTrigger(trigger);
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
