using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Core
{
    /// <summary>
    /// EventSystem.IsPointerOverGameObject() returns true for ANY raycaster hit under the pointer,
    /// including PhysicsRaycaster hits on 3D world objects — not just UI (see PLAN.md 1.1). This
    /// filters specifically for GraphicRaycaster (actual UI) hits. Shared by anything that needs to
    /// ignore a tap/click that landed on a UI element (Home/Boombox.cs, Care/HygieneMinigame.cs).
    /// </summary>
    public static class PointerUtils
    {
        private static readonly List<RaycastResult> Buffer = new List<RaycastResult>();

        public static bool IsPointerOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var pointerData = new PointerEventData(EventSystem.current) { position = screenPosition };
            Buffer.Clear();
            EventSystem.current.RaycastAll(pointerData, Buffer);

            foreach (RaycastResult result in Buffer)
            {
                if (result.module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
