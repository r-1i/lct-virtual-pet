using Core;
using TMPro;
using UnityEngine;

namespace Home
{
    /// <summary>
    /// The pet's name floating above its head — only in the home zone. Put it on a 3D TextMeshPro object (not a UI
    /// one) inside the home zone. Follows the character, always faces the camera, and sits just above the tallest
    /// visible part of the current model (re-measured when the look changes). Hidden until the pet is created.
    /// </summary>
    [DefaultExecutionOrder(10001)]
    public class PetNameLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private ZoneManager zoneManager;
        [Tooltip("Character root (the object with CharacterMotor).")]
        [SerializeField] private Transform character;
        [Tooltip("Index of the home zone in ZoneManager.zones. Before the first zone switch the game is at home too.")]
        [SerializeField] private int homeZoneIndex;
        [Tooltip("Gap between the top of the model and the text, in metres.")]
        [SerializeField] private float gapAboveHead = 0.12f;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Dark outline so the name reads on any background (applied at runtime, 0 = none).")]
        [SerializeField, Range(0f, 1f)] private float outlineWidth = 0.25f;
        [SerializeField] private Color32 outlineColor = new Color32(60, 40, 30, 255);

        private PlayerDataService _playerData;
        private float _headHeight = 1f;
        private bool _measured;

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (label != null && outlineWidth > 0f)
            {
                label.outlineWidth = outlineWidth;
                label.outlineColor = outlineColor;
            }

            EventBus.Subscribe<CharacterChangedEvent>(HandleCharacterChanged);
            Refresh();
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<CharacterChangedEvent>(HandleCharacterChanged);
        }

        private void HandleCharacterChanged(CharacterChangedEvent e)
        {
            _measured = false;
            Refresh();
        }

        private void Refresh()
        {
            if (_playerData != null && label != null)
            {
                label.text = _playerData.PetName;
            }
        }

        private void LateUpdate()
        {
            if (_playerData == null || label == null || character == null)
            {
                return;
            }

            int zone = zoneManager != null ? zoneManager.CurrentZoneIndex : homeZoneIndex;
            bool visible = (zone == homeZoneIndex || zone < 0)
                           && _playerData.IsCharacterCreated
                           && !string.IsNullOrEmpty(_playerData.PetName);
            if (label.enabled != visible)
            {
                label.enabled = visible;
                if (visible)
                {
                    _measured = false;
                }
            }

            if (!visible)
            {
                return;
            }

            if (!_measured)
            {
                _measured = MeasureHead();
            }

            transform.position = character.position + Vector3.up * (_headHeight + gapAboveHead);
            if (viewCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - viewCamera.transform.position, Vector3.up);
            }
        }

        /// <summary>Height of the tallest visible renderer above the character root. False while nothing is visible yet.</summary>
        private bool MeasureHead()
        {
            bool found = false;
            float top = 0f;
            foreach (Renderer r in character.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer)
                {
                    continue;
                }

                top = found ? Mathf.Max(top, r.bounds.max.y) : r.bounds.max.y;
                found = true;
            }

            if (found)
            {
                _headHeight = top - character.position.y;
            }

            return found;
        }
    }
}
