using Core;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Coins / jar coins (копилка) counter. Reads the current balance from PlayerDataService in Start,
    /// then listens to CoinsChangedEvent and counts up/down to each new value. Either label can be left
    /// empty — e.g. one CoinsView for the top bar showing only coins, another somewhere showing only the jar.
    /// Services and the first subscription are taken in Start, not OnEnable — see StatBarView for why.
    /// </summary>
    public class CoinsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private string coinsFormat = "{0}";

        [SerializeField] private TMP_Text jarCoinsLabel;
        [SerializeField] private string jarCoinsFormat = "{0}";

        [Header("Animation")]
        [Tooltip("Count-up duration. 0 = change instantly.")]
        [SerializeField] private float countDuration = 0.5f;
        [Tooltip("Scale punch on the label when its value changes. 0 = off.")]
        [SerializeField] private float punchScale = 0.15f;

        private PlayerDataService _playerData;
        private readonly Counter _coins = new Counter();
        private readonly Counter _jarCoins = new Counter();

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            Activate();
        }

        /// <summary>Re-enables after the first Start (panel hidden and shown again) — snap to the current balance, then listen.</summary>
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
            _coins.Kill();
            _jarCoins.Kill();
        }

        private void Activate()
        {
            _coins.Snap(_playerData.Coins, coinsLabel, coinsFormat);
            _jarCoins.Snap(_playerData.JarCoins, jarCoinsLabel, jarCoinsFormat);

            EventBus.Unsubscribe<CoinsChangedEvent>(OnCoinsChanged);
            EventBus.Subscribe<CoinsChangedEvent>(OnCoinsChanged);
        }

        private void OnCoinsChanged(CoinsChangedEvent e)
        {
            _coins.AnimateTo(e.Coins, coinsLabel, coinsFormat, countDuration, punchScale);
            _jarCoins.AnimateTo(e.JarCoins, jarCoinsLabel, jarCoinsFormat, countDuration, punchScale);
        }

        /// <summary>State of one animated number: what's currently shown and the running tweens.</summary>
        private class Counter
        {
            private int _target;
            private int _shown;
            private Tween _countTween;
            private Tween _punchTween;
            private Vector3 _baseScale;
            private bool _hasBaseScale;

            public void Snap(int value, TMP_Text label, string format)
            {
                Kill();
                _target = value;
                Draw(value, label, format);
            }

            public void AnimateTo(int value, TMP_Text label, string format, float duration, float punch)
            {
                if (label == null || value == _target)
                {
                    _target = value;
                    return;
                }

                _target = value;
                _countTween?.Kill();

                if (duration <= 0f)
                {
                    Draw(value, label, format);
                }
                else
                {
                    _countTween = DOTween.To(() => _shown, v => Draw(v, label, format), value, duration)
                        .SetEase(Ease.OutCubic)
                        .SetTarget(label);
                }

                if (punch > 0f)
                {
                    Transform t = label.transform;
                    if (!_hasBaseScale)
                    {
                        _baseScale = t.localScale;
                        _hasBaseScale = true;
                    }

                    _punchTween?.Kill();
                    t.localScale = _baseScale;
                    _punchTween = t.DOPunchScale(_baseScale * punch, 0.3f, 6, 0.5f);
                }
            }

            public void Kill()
            {
                _countTween?.Kill();
                _countTween = null;

                if (_punchTween != null)
                {
                    _punchTween.Kill(true);
                    _punchTween = null;
                }
            }

            private void Draw(int value, TMP_Text label, string format)
            {
                _shown = value;
                if (label != null)
                {
                    label.text = string.Format(format, value);
                }
            }
        }
    }
}
