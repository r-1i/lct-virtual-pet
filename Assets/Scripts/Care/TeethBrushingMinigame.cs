using Core;
using Home;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Care
{
    /// <summary>
    /// Teeth brushing minigame, fully UI (separate from ShowerMinigame by design — see PLAN.md 6.2).
    /// The Sink zone itself is empty; entering it just opens a full-screen panel: clean-teeth image,
    /// the same image with yellow teeth on top, a toothbrush sprite under the finger, a 0-100%
    /// progress bar at the bottom. Dragging anywhere on the panel brushes: progress grows with the
    /// distance the finger travels, the yellow overlay's alpha is 1 - progress. At 100% — one toothbrush
    /// is used up (CareSupplies), health from it, and automatic return to Care, same as the shower.
    /// Can sit on any always-active object (e.g. the zone object) — input is caught on the panel by a
    /// TeethBrushingSurface this adds at Start, since UI events only bubble through the panel's parents.
    /// </summary>
    public class TeethBrushingMinigame : MonoBehaviour
    {
        [Header("Zone")]
        [SerializeField] private ZoneManager zoneManager;
        [Tooltip("The (empty) Sink zone — the panel is shown only while it's the current zone.")]
        [SerializeField] private int activeZoneIndex;

        [Header("UI")]
        [Tooltip("Full-screen root, SetActive'd on zone enter/leave. Its images must be Raycast Targets to catch the drag.")]
        [SerializeField] private GameObject panel;
        [Tooltip("Image with yellow teeth, on top of the clean one. Alpha = 1 - progress.")]
        [SerializeField] private Image dirtyTeeth;
        [Tooltip("Toothbrush sprite, follows the finger while touching. Turn its Raycast Target off.")]
        [SerializeField] private RectTransform toothbrush;
        [Tooltip("Image Type = Filled. fillAmount = progress.")]
        [SerializeField] private Image progressFill;
        [Tooltip("Optional \"42%\" label.")]
        [SerializeField] private TMP_Text progressLabel;

        [Header("Sound")]
        [Tooltip("Brushing sound, Loop = on, Play On Awake = off. Plays while the finger is down.")]
        [SerializeField] private AudioSource brushSound;

        [Header("Balance")]
        [Tooltip("Total finger travel for 0 → 100%, in screen heights (so it's the same on every phone).")]
        [SerializeField] private float fullCleanDistance = 6f;

        [Header("Auto-return to Care")]
        [SerializeField] private int careZoneIndex;
        [SerializeField] private float returnDelay = 0.5f;

        private float _progress;
        private bool _completed;
        private int _activePointerId = int.MinValue;

        private PlayerDataService _playerData;

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            EventBus.Subscribe<ZoneChangedEvent>(OnZoneChanged);

            TeethBrushingSurface surface = panel.GetComponent<TeethBrushingSurface>();
            if (surface == null)
            {
                surface = panel.AddComponent<TeethBrushingSurface>();
            }

            surface.Init(this);

            // "Brush anywhere on the window": the teeth images may not cover the whole screen
            // (Preserve Aspect), so give the full-screen panel an invisible raycast target of its own.
            if (panel.GetComponent<Graphic>() == null)
            {
                Image catcher = panel.AddComponent<Image>();
                catcher.color = Color.clear;
            }

            SetOpen(zoneManager != null && zoneManager.CurrentZoneIndex == activeZoneIndex);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<ZoneChangedEvent>(OnZoneChanged);
        }

        private void OnZoneChanged(ZoneChangedEvent e)
        {
            SetOpen(e.ZoneIndex == activeZoneIndex);
        }

        /// <summary>Every entry starts a fresh round with dirty teeth.</summary>
        private void SetOpen(bool open)
        {
            CancelInvoke(nameof(ReturnToCare));
            StopBrushing();

            if (open)
            {
                _completed = false;
                SetProgress(0f);
            }

            panel.SetActive(open);
        }

        public void HandlePointerDown(PointerEventData eventData)
        {
            // One finger brushes; a second finger is ignored until the first lifts.
            if (_completed || _activePointerId != int.MinValue)
            {
                return;
            }

            _activePointerId = eventData.pointerId;
            MoveToothbrush(eventData);
            toothbrush.gameObject.SetActive(true);

            if (brushSound != null)
            {
                brushSound.Play();
            }
        }

        public void HandleDrag(PointerEventData eventData)
        {
            if (_completed || eventData.pointerId != _activePointerId)
            {
                return;
            }

            MoveToothbrush(eventData);

            float travelled = eventData.delta.magnitude / Screen.height;
            SetProgress(_progress + travelled / fullCleanDistance);

            if (_progress >= 1f)
            {
                Complete();
            }
        }

        public void HandlePointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId)
            {
                StopBrushing();
            }
        }

        private void MoveToothbrush(PointerEventData eventData)
        {
            var parent = (RectTransform)toothbrush.parent;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector3 world))
            {
                toothbrush.position = world;
            }
        }

        private void SetProgress(float value)
        {
            _progress = Mathf.Clamp01(value);

            Color color = dirtyTeeth.color;
            color.a = 1f - _progress;
            dirtyTeeth.color = color;

            progressFill.fillAmount = _progress;

            if (progressLabel != null)
            {
                progressLabel.text = $"{Mathf.FloorToInt(_progress * 100f)}%";
            }
        }

        private void StopBrushing()
        {
            _activePointerId = int.MinValue;
            toothbrush.gameObject.SetActive(false);

            if (brushSound != null)
            {
                brushSound.Stop();
            }
        }

        private void Complete()
        {
            _completed = true;
            StopBrushing();

            // 1 toothbrush; health = its healthBoost (products.json). Entry is gated by ZoneEntryPoint.
            float health = CareSupplies.Consume(_playerData, ServiceLocator.Get<Content.ContentDatabase>(), CareSupplies.Teeth);
            UI.Feedback.StatsSnapshot before = UI.Feedback.Snapshot(_playerData);
            _playerData.ApplyStatDelta(0f, 0f, health);
            UI.Feedback.Show(UI.Feedback.Join("−1 зубная щётка", UI.Feedback.StatChanges(before, UI.Feedback.Snapshot(_playerData))));

            Invoke(nameof(ReturnToCare), returnDelay);
        }

        private void ReturnToCare()
        {
            zoneManager.GoToZone(careZoneIndex);

            // After the return, so "Блестяще! Возвращайся в главную комнату" shows over the Care room.
            ServiceLocator.Get<TutorialService>().TryShow(TutorialStepIds.TeethDone);
        }
    }
}
