using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
    /// <summary>Segmented switch ("Нужно | Хочу"): a highlight pill slides between the options and the label/icon colors cross-fade with it.</summary>
    public class SegmentedToggle : MonoBehaviour
    {
        [Serializable]
        private class Option
        {
            public Button button;
            public TMP_Text label;
            [Tooltip("Optional. Tinted with the two colors below, so use a white sprite.")]
            public Graphic icon;
            public Color iconUnselected = Color.white;
            public Color iconSelected = Color.white;
        }

        [Header("Layout")]
        [Tooltip("The sliding pill. Anchors and offsets are driven by this script.")]
        [SerializeField] private RectTransform highlight;
        [Tooltip("Gap between the highlight and the edge of the background, in pixels. Must match the padding of the Options layout group.")]
        [SerializeField] private float inset = 6f;
        [SerializeField] private Option[] options;

        [Header("State")]
        [SerializeField] private int selectedIndex;
        [SerializeField] private float duration = 0.2f;

        [Header("Label colors")]
        [SerializeField] private Color labelSelected = new Color(0.1f, 0.12f, 0.4f);
        [SerializeField] private Color labelUnselected = Color.white;

        [Header("Events")]
        public UnityEvent<int> onChanged;

        private float _position; // left edge of the highlight, in option units (0 .. count-1)
        private Coroutine _routine;

        public int SelectedIndex => selectedIndex;

        private void Awake()
        {
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                if (options[i].button != null)
                {
                    options[i].button.onClick.AddListener(() => Select(index));
                }
            }
        }

        private void OnEnable()
        {
            _position = selectedIndex;
            ApplyVisual(_position);
        }

        /// <summary>Selects an option with the slide animation and raises <see cref="onChanged"/>.</summary>
        public void Select(int index)
        {
            SetIndex(index, true);
        }

        /// <summary>Selects an option instantly, e.g. when loading saved state.</summary>
        public void SelectImmediate(int index)
        {
            SetIndex(index, false);
        }

        private void SetIndex(int index, bool animate)
        {
            index = Mathf.Clamp(index, 0, options.Length - 1);
            bool changed = index != selectedIndex;
            selectedIndex = index;

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (animate && duration > 0f && AnimationSettings.Enabled && isActiveAndEnabled)
            {
                _routine = StartCoroutine(SlideRoutine(index));
            }
            else
            {
                _position = index;
                ApplyVisual(_position);
            }

            if (changed)
            {
                onChanged.Invoke(index);
            }
        }

        private IEnumerator SlideRoutine(int target)
        {
            float from = _position;

            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                _position = Mathf.SmoothStep(from, target, t / duration);
                ApplyVisual(_position);
                yield return null;
            }

            _position = target;
            ApplyVisual(_position);
            _routine = null;
        }

        private void ApplyVisual(float position)
        {
            int count = options.Length;

            if (highlight != null && count > 0)
            {
                highlight.anchorMin = new Vector2(position / count, 0f);
                highlight.anchorMax = new Vector2((position + 1f) / count, 1f);
                highlight.offsetMin = new Vector2(inset, inset);
                highlight.offsetMax = new Vector2(-inset, -inset);
            }

            for (int i = 0; i < count; i++)
            {
                // 1 when the highlight sits exactly under this option, 0 when it is a full option away.
                float k = 1f - Mathf.Clamp01(Mathf.Abs(position - i));

                if (options[i].label != null)
                {
                    options[i].label.color = Color.Lerp(labelUnselected, labelSelected, k);
                }

                if (options[i].icon != null)
                {
                    options[i].icon.color = Color.Lerp(options[i].iconUnselected, options[i].iconSelected, k);
                }
            }
        }
    }
}
