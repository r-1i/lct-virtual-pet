using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Shop
{
    /// <summary>
    /// One 3D product on the shelf. Click comes through EventSystem + PhysicsRaycaster (on the
    /// camera) — requires the EventSystem to run Input System UI Input Module, see PLAN.md 1.1.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ProductPickable : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private float selectedScaleMultiplier = 1.3f;
        [Tooltip("Local-space offset applied on top of the spawn position when selected, e.g. (0, 0, -0.3) to slide toward the camera. Tune per shelf/product orientation.")]
        [SerializeField] private Vector3 selectedLocalOffset = new Vector3(0f, 0f, -0.3f);
        [SerializeField] private float tweenDuration = 0.3f;

        public string ProductId { get; private set; }

        private Transform _t;
        private Vector3 _homeLocalPosition;
        private Vector3 _homeLocalScale;

        /// <summary>Called once by ProductSpawner right after Instantiate, before anything else touches this object.</summary>
        public void Initialize(string productId)
        {
            ProductId = productId;
            _t = transform;
            _homeLocalPosition = _t.localPosition;
            _homeLocalScale = _t.localScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ShopController.Instance?.SelectProduct(this);
        }

        public void PlaySelected()
        {
            _t.DOKill();
            _t.DOLocalMove(_homeLocalPosition + selectedLocalOffset, tweenDuration).SetEase(Ease.OutBack);
            _t.DOScale(_homeLocalScale * selectedScaleMultiplier, tweenDuration).SetEase(Ease.OutBack);
        }

        public void PlayDeselected()
        {
            _t.DOKill();
            _t.DOLocalMove(_homeLocalPosition, tweenDuration).SetEase(Ease.OutQuad);
            _t.DOScale(_homeLocalScale, tweenDuration).SetEase(Ease.OutQuad);
        }
    }
}
