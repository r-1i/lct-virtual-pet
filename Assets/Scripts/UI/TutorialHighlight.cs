using Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Put on a button (UI Button or a clickable 3D object) that a tutorial step may point at. When a
    /// step with highlight == key is dismissed, this pulses its scale 1 → 1.1 until it's clicked.
    /// Must sit on the same GameObject that receives the click (the Button itself, not its parent),
    /// since EventSystem delivers the click to every handler on that one object.
    /// </summary>
    public class TutorialHighlight : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Must match a step's \"highlight\" in tutorial.json.")]
        [SerializeField] private string key;
        [SerializeField] private float pulseScale = 1.1f;
        [Tooltip("One way, 1 → 1.1; the way back takes the same time.")]
        [SerializeField] private float pulseDuration = 0.5f;

        private TutorialService _tutorial;
        private Vector3 _baseScale;
        private Tween _pulse;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void Start()
        {
            _tutorial = ServiceLocator.Get<TutorialService>();
            _tutorial.HighlightsChanged += Refresh;
            Refresh();
        }

        // The service isn't guaranteed to exist yet in OnEnable (see Bootstrap), so the first check is
        // in Start; after that, re-check every time the button reappears (zone/panel switched back on).
        private void OnEnable()
        {
            if (_tutorial != null)
            {
                Refresh();
            }
        }

        private void OnDisable()
        {
            StopPulse();
        }

        private void OnDestroy()
        {
            if (_tutorial != null)
            {
                _tutorial.HighlightsChanged -= Refresh;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _tutorial?.ClearHighlight(key);
        }

        private void Refresh()
        {
            bool shouldPulse = isActiveAndEnabled && _tutorial.IsHighlighted(key);

            if (shouldPulse && _pulse == null)
            {
                transform.localScale = _baseScale;
                _pulse = transform.DOScale(_baseScale * pulseScale, pulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
            else if (!shouldPulse)
            {
                StopPulse();
            }
        }

        private void StopPulse()
        {
            if (_pulse == null)
            {
                return;
            }

            _pulse.Kill();
            _pulse = null;
            transform.localScale = _baseScale;
        }
    }
}
