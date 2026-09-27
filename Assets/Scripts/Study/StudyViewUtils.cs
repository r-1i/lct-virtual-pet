using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>Small null-safe setters shared by the study views (most labels there are optional).</summary>
    internal static class StudyViewUtils
    {
        /// <summary>Sets the text and hides the label (or its container) when the text is empty.</summary>
        public static void SetOptional(TMP_Text label, string text, GameObject container = null)
        {
            bool has = !string.IsNullOrEmpty(text);
            if (label != null)
            {
                label.text = text ?? "";
                (container != null ? container : label.gameObject).SetActive(has);
            }
            else if (container != null)
            {
                container.SetActive(has);
            }
        }

        public static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text ?? "";
            }
        }

        /// <summary>No sprite = keep whatever placeholder the Image already has.</summary>
        public static void SetSprite(Image image, Sprite sprite)
        {
            if (image != null && sprite != null)
            {
                image.sprite = sprite;
            }
        }

        public static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
