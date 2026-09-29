using Core;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// "Период N" in the top bar of the windows: N = days since the profile's first launch + 1, so it grows by one
    /// every calendar day (PlayerDataService.Period). Put it on the TMP text; it re-checks the day once a second.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class PeriodLabel : MonoBehaviour
    {
        private const float CheckInterval = 1f;

        [Tooltip("{0} = period number.")]
        [SerializeField] private string format = "Период {0}";

        private TMP_Text _label;
        private PlayerDataService _playerData;
        private int _shown = -1;
        private float _timer;

        private void OnEnable()
        {
            _label = GetComponent<TMP_Text>();
            ServiceLocator.TryGet(out _playerData);
            Refresh();
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            _timer = CheckInterval;
            if (_playerData == null && !ServiceLocator.TryGet(out _playerData))
            {
                return;
            }

            int period = _playerData.Period;
            if (period == _shown)
            {
                return;
            }

            _shown = period;
            _label.text = string.Format(format, period);
        }
    }
}
