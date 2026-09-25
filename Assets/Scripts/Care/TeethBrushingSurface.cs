using UnityEngine;
using UnityEngine.EventSystems;

namespace Care
{
    /// <summary>
    /// Added at runtime by TeethBrushingMinigame onto its panel — don't add by hand. UI pointer events
    /// only bubble up through the panel's own parents, while the minigame itself can live anywhere
    /// (e.g. on the 3D zone object), so this forwards them.
    /// </summary>
    public class TeethBrushingSurface : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private TeethBrushingMinigame _minigame;

        public void Init(TeethBrushingMinigame minigame)
        {
            _minigame = minigame;
        }

        public void OnPointerDown(PointerEventData eventData) => _minigame?.HandlePointerDown(eventData);
        public void OnDrag(PointerEventData eventData) => _minigame?.HandleDrag(eventData);
        public void OnPointerUp(PointerEventData eventData) => _minigame?.HandlePointerUp(eventData);
    }
}
