using System;
using Content;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dreams
{
    /// <summary>One tile in the "Выбери мечту" row. Instantiated by DreamsScreen, one per dream in dreams.json.</summary>
    public class DreamCardView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [Tooltip("Placeholder icon for now — the sprite comes from DreamsScreen's icon list by iconKey; with no match the prefab's own sprite stays.")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text priceLabel;
        [Tooltip("Highlight frame of the current goal.")]
        [SerializeField] private GameObject selectedMarker;
        [Tooltip("\"Куплено\" badge. Bought dreams can't be picked.")]
        [SerializeField] private GameObject purchasedMarker;

        private string _dreamId;
        private Action<string> _onClick;
        private bool _selected;

        public string DreamId => _dreamId;

        public void Setup(DreamDefinition dream, Sprite icon, Action<string> onClick)
        {
            _dreamId = dream.id;
            _onClick = onClick;

            if (icon != null && iconImage != null)
            {
                iconImage.sprite = icon;
            }

            if (titleLabel != null)
            {
                titleLabel.text = dream.title;
            }

            if (priceLabel != null)
            {
                priceLabel.text = dream.price.ToString();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_dreamId));
        }

        public void SetState(bool selected, bool purchased, bool animate)
        {
            bool becameSelected = selected && !_selected;
            _selected = selected;

            if (selectedMarker != null)
            {
                selectedMarker.SetActive(selected);
            }

            if (purchasedMarker != null)
            {
                purchasedMarker.SetActive(purchased);
            }

            button.interactable = !purchased;

            if (becameSelected && animate && AnimationSettings.Enabled)
            {
                transform.DOKill(true);
                transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 6, 0.5f);
            }
        }

        private void OnDestroy()
        {
            transform.DOKill();
        }
    }
}
