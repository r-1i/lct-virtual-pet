using System;
using Content;
using Core;
using DG.Tweening;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dreams
{
    /// <summary>
    /// Read-only "Моя цель" widget for other screens (e.g. the plate on Home): icon, title,
    /// description, jar progress. Updates itself on CoinsChangedEvent / DreamChangedEvent.
    /// Every field except progressFill is optional. Opening the Dreams zone on tap is a regular
    /// Button OnClick → ZoneManager.GoToZone, wired in the scene.
    /// Services and the first subscription are taken in Start, not OnEnable — see StatBarView for why.
    /// </summary>
    public class CurrentDreamView : MonoBehaviour
    {
        [Serializable]
        private struct DreamIcon
        {
            public string iconKey;
            public Sprite sprite;
        }

        [Tooltip("Goal content. Hidden when every dream is bought.")]
        [SerializeField] private GameObject goalRoot;
        [Tooltip("\"Все мечты исполнены!\" — shown instead of goalRoot. Optional.")]
        [SerializeField] private GameObject allDoneRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [Tooltip("Image Type = Filled. fillAmount = jar / price.")]
        [SerializeField] private Image progressFill;
        [Tooltip("\"350 / 500\"")]
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private string progressFormat = "{0} / {1}";
        [Tooltip("\"осталось 150\" / \"Можно покупать!\". Optional.")]
        [SerializeField] private TMP_Text remainingLabel;
        [Tooltip("iconKey from dreams.json → sprite. Keys with no entry keep the current sprite.")]
        [SerializeField] private DreamIcon[] icons = Array.Empty<DreamIcon>();
        [SerializeField] private float progressTweenDuration = 0.4f;

        private PlayerDataService _playerData;
        private DreamService _dreams;
        private string _shownDreamId;

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            _dreams = ServiceLocator.Get<DreamService>();
            Activate();
        }

        /// <summary>Re-enables after the first Start (zone/panel hidden and shown again) — snap to the current state, then listen.</summary>
        private void OnEnable()
        {
            if (_playerData != null)
            {
                Activate();
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
            EventBus.Unsubscribe<DreamChangedEvent>(OnDreamChanged);

            if (progressFill != null)
            {
                progressFill.DOKill();
            }
        }

        private void Activate()
        {
            Refresh(false);

            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
            EventBus.Unsubscribe<DreamChangedEvent>(OnDreamChanged);
            EventBus.Subscribe<DreamChangedEvent>(OnDreamChanged);
        }

        private void OnCoinsChanged(CoinsChangedEvent e) => Refresh(true);
        private void OnDreamChanged(DreamChangedEvent e) => Refresh(true);

        private void Refresh(bool animate)
        {
            DreamDefinition dream = _dreams.CurrentDream;
            string dreamId = dream?.id ?? "";
            bool goalChanged = dreamId != _shownDreamId;
            _shownDreamId = dreamId;

            bool hasGoal = dream != null;
            if (goalRoot != null)
            {
                goalRoot.SetActive(hasGoal);
            }

            if (allDoneRoot != null)
            {
                allDoneRoot.SetActive(!hasGoal);
            }

            if (!hasGoal)
            {
                return;
            }

            int jar = _playerData.JarCoins;

            if (iconImage != null)
            {
                Sprite icon = FindIcon(dream.iconKey);
                if (icon != null)
                {
                    iconImage.sprite = icon;
                }
            }

            if (titleLabel != null)
            {
                titleLabel.text = dream.title;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = dream.description;
            }

            if (progressLabel != null)
            {
                progressLabel.text = string.Format(progressFormat, jar, dream.price);
            }

            if (remainingLabel != null)
            {
                int remaining = _dreams.RemainingForCurrent;
                remainingLabel.text = remaining > 0 ? $"осталось {remaining}" : "Можно покупать!";
            }

            if (progressFill == null)
            {
                return;
            }

            float fill = dream.price > 0 ? Mathf.Clamp01(jar / (float)dream.price) : 1f;
            progressFill.DOKill();
            if (animate && !goalChanged && isActiveAndEnabled && AnimationSettings.Enabled)
            {
                progressFill.DOFillAmount(fill, progressTweenDuration).SetEase(Ease.OutCubic);
            }
            else
            {
                progressFill.fillAmount = fill;
            }
        }

        private Sprite FindIcon(string iconKey)
        {
            foreach (DreamIcon icon in icons)
            {
                if (icon.iconKey == iconKey)
                {
                    return icon.sprite;
                }
            }

            return null;
        }
    }
}
