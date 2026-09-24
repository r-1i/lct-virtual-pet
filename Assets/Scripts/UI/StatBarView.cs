using Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public enum StatType
    {
        Satiety,
        Mood,
        Health
    }

    /// <summary>
    /// One status bar (Сытость/Настроение/Здоровье). Reads the current value from PlayerDataService
    /// in OnEnable (snaps, no animation — the bar may have been hidden in another zone while the stat
    /// changed), then listens to StatsChangedEvent and tweens to each new value. Display only: the
    /// Slider is forced non-interactable so the player can't drag it.
    /// Either a Slider or a Filled Image (or both) can be assigned — whichever the bar prefab uses.
    /// </summary>
    public class StatBarView : MonoBehaviour
    {
        [SerializeField] private StatType stat;

        [Header("Visual (assign at least one)")]
        [SerializeField] private Slider slider;
        [Tooltip("Image with Image Type = Filled. Used instead of / together with the Slider.")]
        [SerializeField] private Image fillImage;

        [Header("Optional")]
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private string valueFormat = "{0:0}%";
        [Tooltip("Tints the fill by value: left = 0, right = 100. Leave the default white-white gradient to keep the fill's own color.")]
        [SerializeField] private Gradient fillColor = new Gradient();
        [Tooltip("Graphic to tint with Fill Color. If empty, uses Fill Image, then the Slider's fill rect.")]
        [SerializeField] private Graphic tintTarget;
        [SerializeField] private float tweenDuration = 0.4f;

        private PlayerDataService _playerData;
        private float _shownValue;
        private Tween _tween;

        /// <summary>
        /// First activation happens here, not in OnEnable: Unity runs Awake+OnEnable object by object,
        /// so OnEnable can fire before Bootstrap.Awake has registered services — and Bootstrap then
        /// calls EventBus.Clear(), which would also wipe a subscription made that early. Start is
        /// guaranteed to run after every Awake in the scene.
        /// </summary>
        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();

            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 100f;
                slider.interactable = false;
            }

            if (tintTarget == null)
            {
                tintTarget = fillImage != null
                    ? fillImage
                    : slider != null && slider.fillRect != null ? slider.fillRect.GetComponent<Graphic>() : null;
            }

            Activate();
        }

        /// <summary>Re-enables after the first Start (e.g. the bar's zone/panel was hidden and shown again) — snap to the current value, then listen.</summary>
        private void OnEnable()
        {
            if (_playerData != null)
            {
                Activate();
            }
        }

        private void Activate()
        {
            Draw(Read(_playerData.Stats));
            EventBus.Unsubscribe<StatsChangedEvent>(OnStatsChanged);
            EventBus.Subscribe<StatsChangedEvent>(OnStatsChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<StatsChangedEvent>(OnStatsChanged);
            _tween?.Kill();
            _tween = null;
        }

        private void OnStatsChanged(StatsChangedEvent e)
        {
            float target = Read(e.Stats);
            if (Mathf.Approximately(target, _shownValue))
            {
                return;
            }

            _tween?.Kill();
            _tween = DOTween.To(() => _shownValue, Draw, target, tweenDuration)
                .SetEase(Ease.OutCubic)
                .SetTarget(this);
        }

        private float Read(CharacterStats stats)
        {
            switch (stat)
            {
                case StatType.Satiety: return stats.satiety;
                case StatType.Mood: return stats.mood;
                default: return stats.health;
            }
        }

        private void Draw(float value)
        {
            _shownValue = value;

            if (slider != null)
            {
                slider.SetValueWithoutNotify(value);
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = value / 100f;
            }

            if (valueLabel != null)
            {
                valueLabel.text = string.Format(valueFormat, value);
            }

            if (tintTarget != null && !IsDefaultGradient())
            {
                tintTarget.color = fillColor.Evaluate(value / 100f);
            }
        }

        /// <summary>A fresh Gradient is plain white→white; treat that as "not configured" so we don't wipe the fill's own color.</summary>
        private bool IsDefaultGradient()
        {
            GradientColorKey[] keys = fillColor.colorKeys;
            foreach (GradientColorKey key in keys)
            {
                if (key.color != Color.white)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
