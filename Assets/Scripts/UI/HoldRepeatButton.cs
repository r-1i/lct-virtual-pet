using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Put on a Button instead of using its OnClick: fires once on press, then after a short delay
    /// repeats while held, faster and faster (for +/- steppers). Listen to <see cref="onStep"/>.
    /// Stops when released, when the finger leaves the button, or when the Button becomes non-interactable.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Hold time before auto-repeat starts.")]
        [SerializeField] private float initialDelay = 0.4f;
        [Tooltip("Repeat interval right after the delay.")]
        [SerializeField] private float startInterval = 0.15f;
        [Tooltip("Fastest repeat interval.")]
        [SerializeField] private float minInterval = 0.02f;
        [Tooltip("Each repeat multiplies the interval by this (< 1 = accelerates).")]
        [SerializeField] private float acceleration = 0.85f;

        public UnityEvent onStep = new UnityEvent();

        private Button _button;
        private bool _held;
        private float _nextStepTime;
        private float _interval;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            _held = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !_button.IsInteractable())
            {
                return;
            }

            _held = true;
            _interval = startInterval;
            _nextStepTime = Time.unscaledTime + initialDelay;
            onStep.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData) => _held = false;

        public void OnPointerExit(PointerEventData eventData) => _held = false;

        private void Update()
        {
            if (!_held)
            {
                return;
            }

            if (!_button.IsInteractable())
            {
                _held = false;
                return;
            }

            if (Time.unscaledTime < _nextStepTime)
            {
                return;
            }

            onStep.Invoke();
            _nextStepTime = Time.unscaledTime + _interval;
            _interval = Mathf.Max(minInterval, _interval * acceleration);
        }
    }
}
