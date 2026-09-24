using UnityEngine;
using UnityEngine.EventSystems;

namespace Home
{
    /// <summary>
    /// Generic "click this 3D object to enter a zone" trigger — e.g. the bathtub or the sink inside
    /// the Care zone. One reusable component instead of a one-off EnterBath()/EnterSink() pair.
    /// Click comes through EventSystem + PhysicsRaycaster on the camera, same mechanism as
    /// Shop/ProductPickable — requires a Collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZoneEntryPoint : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ZoneManager zoneManager;
        [SerializeField] private int targetZoneIndex;

        public void OnPointerClick(PointerEventData eventData)
        {
            zoneManager.GoToZone(targetZoneIndex);
        }
    }
}
