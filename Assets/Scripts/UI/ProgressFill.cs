using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Sets a 0..1 progress on a bar fill. Image Type = Filled → fillAmount. Any other type (e.g. Sliced, so the
    /// rounded ends stay round) → the fill's right anchor moves instead; put the fill inside its track with
    /// anchors (0,0)-(1,1) and zero offsets. Hidden at 0.
    /// </summary>
    public static class ProgressFill
    {
        public static void Set(Image fill, float value)
        {
            if (fill == null)
            {
                return;
            }

            value = Mathf.Clamp01(value);
            if (fill.type == Image.Type.Filled)
            {
                fill.fillAmount = value;
                return;
            }

            RectTransform rt = fill.rectTransform;
            rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(value, rt.anchorMax.y);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
            fill.enabled = value > 0.001f;
        }
    }
}
