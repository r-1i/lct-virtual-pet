using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Finance
{
    public enum PlanRule
    {
        /// <summary>Надо, Копилка: the fact must reach the plan.</summary>
        AtLeast,
        /// <summary>Хочу: the fact must not go over the plan.</summary>
        AtMost
    }

    /// <summary>One "план и факт" row: bar (fact / plan), "15/20", ✓ or ✗. Used on the planning screen and in the result popup.</summary>
    public class PlanFactRowView : MonoBehaviour
    {
        [Tooltip("Filled → fillAmount = fact / plan. Sliced → its right anchor = fact / plan (anchor left edge at 0).")]
        [SerializeField] private Image fill;
        [Tooltip("\"15/20\"")]
        [SerializeField] private TMP_Text valueLabel;
        [Tooltip("✓ — shown when this row is fine.")]
        [SerializeField] private GameObject okMark;
        [Tooltip("✗ — shown when this row is broken. Optional.")]
        [SerializeField] private GameObject failMark;
        [SerializeField] private Color normalColor = new Color(0.36f, 0.66f, 0.42f);
        [SerializeField] private Color badColor = new Color(0.87f, 0.36f, 0.33f);

        /// <summary>
        /// final = the period is over. While the plan runs, an AtLeast row that hasn't reached the plan
        /// yet isn't a failure (there's still time) — it just has no mark. An AtMost row that went over
        /// is already broken and turns red right away.
        /// </summary>
        public void Set(int plan, int fact, PlanRule rule, bool final)
        {
            bool ok = rule == PlanRule.AtLeast ? fact >= plan : fact <= plan;
            bool failed = !ok && (final || rule == PlanRule.AtMost);

            if (fill != null)
            {
                float amount = plan > 0 ? Mathf.Clamp01(fact / (float)plan) : (fact > 0 ? 1f : 0f);
                if (fill.type == Image.Type.Filled)
                {
                    fill.fillAmount = amount;
                }
                else
                {
                    // Sliced fill (rounded ends): the width follows the right anchor, the fill stays hidden at 0.
                    RectTransform rt = fill.rectTransform;
                    rt.anchorMax = new Vector2(amount, rt.anchorMax.y);
                    fill.enabled = amount > 0f;
                }

                fill.color = failed ? badColor : normalColor;
            }

            if (valueLabel != null)
            {
                valueLabel.text = $"{fact}/{plan}";
            }

            if (okMark != null)
            {
                okMark.SetActive(ok);
            }

            if (failMark != null)
            {
                failMark.SetActive(failed);
            }
        }
    }
}
