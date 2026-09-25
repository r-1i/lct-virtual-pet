using Content;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Feeding
{
    /// <summary>
    /// One 3D pile on the feeding table — all owned batches of one product, grouped into a single
    /// object (e.g. "x10" apples shown as one pile, even if they're several inventory batches under
    /// the hood). A simple click/tap feeds 1 unit straight to the character — no drag, no drop target
    /// check. Comes through EventSystem + PhysicsRaycaster on the camera, same mechanism as
    /// Shop/ProductPickable — requires a Collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FoodPileView : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Optional. Shown only when count > 1 (\"x10\"); hidden for a single item.")]
        [SerializeField] private TMP_Text countLabel;

        private ProductDefinition _product;
        private PlayerDataService _playerData;
        private CharacterFeeding _character;

        public void Setup(ProductDefinition product, int count, PlayerDataService playerData, CharacterFeeding character)
        {
            _product = product;
            _playerData = playerData;
            _character = character;

            if (countLabel != null)
            {
                countLabel.gameObject.SetActive(count > 1);
                countLabel.text = $"x{count}";
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_playerData.TryConsumeInventory(_product.id, 1))
            {
                return;
            }

            // Happiness → Настроение, satiety → Сытость; health isn't affected by feeding (only care/hygiene raises it).
            _playerData.ApplyStatDelta(_product.satietyBoost, _product.happinessBoost, 0f);
            _character.PlayEat();
            ServiceLocator.Get<TutorialService>().TryShow(TutorialStepIds.FedHappy);
            // FoodTableView rebuilds this whole pile (or removes it if it hit 0) on
            // InventoryChangedEvent, so this instance doesn't need to update itself further.
        }
    }
}
