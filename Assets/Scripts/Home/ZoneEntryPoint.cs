using Care;
using Content;
using Core;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Home
{
    /// <summary>
    /// Generic "click this 3D object to enter a zone" trigger — e.g. the bathtub or the sink inside
    /// the Care zone. One reusable component instead of a one-off EnterBath()/EnterSink() pair.
    /// Click comes through EventSystem + PhysicsRaycaster on the camera, same mechanism as
    /// Shop/ProductPickable — requires a Collider.
    /// Optionally needs products in the inventory (the bath: soap + washcloth, the sink: toothbrush): without
    /// them the zone isn't entered, the confirm dialog offers to go to the shop instead.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ZoneEntryPoint : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private ZoneManager zoneManager;
        [SerializeField] private int targetZoneIndex;

        [Header("Optional: needed products")]
        [Tooltip("Product ids that must be in the inventory (1 of each), e.g. soap + washcloth. Empty = always enter.")]
        [SerializeField] private string[] requiredProductIds = new string[0];
        [Tooltip("Shown when something is missing. Yes = go to the shop.")]
        [SerializeField] private ConfirmDialog confirmDialog;
        [SerializeField] private int shopZoneIndex = 1;
        [Tooltip("UI window of the zone we're in (e.g. Window_Bathroom) — hidden when going to the shop. The nav buttons switch windows themselves; GoToZone only moves the camera.")]
        [SerializeField] private GameObject currentWindow;
        [Tooltip("Window_Shop — shown when going to the shop.")]
        [SerializeField] private GameObject shopWindow;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (requiredProductIds != null && requiredProductIds.Length > 0)
            {
                var playerData = ServiceLocator.Get<PlayerDataService>();
                if (!CareSupplies.HasAll(playerData, requiredProductIds))
                {
                    string missing = CareSupplies.MissingNames(playerData, ServiceLocator.Get<ContentDatabase>(), requiredProductIds);
                    if (confirmDialog != null)
                    {
                        confirmDialog.Show($"Нужно купить: {missing}.\nСходить в магазин?", GoToShop);
                    }
                    else
                    {
                        Debug.LogWarning($"ZoneEntryPoint '{name}': missing {missing}, and no confirm dialog is set.");
                    }

                    return;
                }
            }

            zoneManager.GoToZone(targetZoneIndex);
        }

        /// <summary>Same as the shop nav button: camera to the shop zone + switch the UI windows.</summary>
        private void GoToShop()
        {
            zoneManager.GoToZone(shopZoneIndex);

            if (currentWindow != null)
            {
                currentWindow.SetActive(false);
            }

            if (shopWindow != null)
            {
                shopWindow.SetActive(true);
            }
        }
    }
}
