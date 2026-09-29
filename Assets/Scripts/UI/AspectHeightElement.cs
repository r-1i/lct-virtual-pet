using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Layout element whose preferred height is its current width × Height Per Width. For rows inside a
    /// Vertical Layout Group (Control Child Size on): the group stretches the row to the list width, and the
    /// row keeps its proportions on any screen instead of a fixed pixel height.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AspectHeightElement : MonoBehaviour, ILayoutElement
    {
        [Tooltip("Height / width of the row, e.g. 236 / 1218 = 0.194.")]
        [SerializeField] private float heightPerWidth = 0.194f;

        private float Height => ((RectTransform)transform).rect.width * heightPerWidth;

        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => Height;
        public float preferredHeight => Height;
        public float flexibleHeight => -1f;
        public int layoutPriority => 1;

        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }

        private void OnEnable() => SetDirty();
        private void OnDisable() => SetDirty();
        private void OnRectTransformDimensionsChange() => SetDirty();

#if UNITY_EDITOR
        private void OnValidate() => SetDirty();
#endif

        private void SetDirty()
        {
            if (isActiveAndEnabled)
            {
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            }
        }
    }
}
