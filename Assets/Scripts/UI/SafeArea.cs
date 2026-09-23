using UnityEngine;

namespace UI
{
    /// <summary>
    /// Fits the RectTransform to Screen.safeArea (notches, rounded corners, gesture bars).
    /// Put it on a full-stretch child of the Canvas and parent all HUD elements under it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        [Header("Which sides respect the safe area")]
        [SerializeField] private bool left = true;
        [SerializeField] private bool right = true;
        [SerializeField] private bool top = true;
        [SerializeField] private bool bottom = true;

        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Apply();
        }

        // Cheap check; covers rotation, resolution changes and the Device Simulator.
        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenSize.x ||
                Screen.height != _lastScreenSize.y)
            {
                Apply();
            }
        }

        private void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (_lastScreenSize.x <= 0 || _lastScreenSize.y <= 0)
            {
                return;
            }

            Rect safe = _lastSafeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;

            min.x = left ? min.x / Screen.width : 0f;
            max.x = right ? max.x / Screen.width : 1f;
            min.y = bottom ? min.y / Screen.height : 0f;
            max.y = top ? max.y / Screen.height : 1f;

            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
