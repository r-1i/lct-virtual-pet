using Core;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Temporary helper to test the stat bars: stats start at 100 and nothing lowers them yet, so the
    /// bars would otherwise always sit full. In Play Mode: ⋮ on this component → pick an action.
    /// Goes through PlayerDataService, so it saves and publishes StatsChangedEvent like real gameplay.
    /// </summary>
    public class StatsDebug : MonoBehaviour
    {
        [SerializeField] private float step = 20f;

        private PlayerDataService Service => ServiceLocator.Get<PlayerDataService>();

        [ContextMenu("Stats: all -step")]
        private void DecreaseAll() => Service.ApplyStatDelta(-step, -step, -step);

        [ContextMenu("Stats: all +step")]
        private void IncreaseAll() => Service.ApplyStatDelta(step, step, step);

        [ContextMenu("Stats: randomize")]
        private void Randomize() => Service.SetStats(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));

        [ContextMenu("Stats: reset to 100")]
        private void ResetAll() => Service.SetStats(100f, 100f, 100f);

        [ContextMenu("Coins: +100")]
        private void AddCoins() => Service.AddCoins(100);

        [ContextMenu("Coins: -50")]
        private void SpendCoins() => Service.TrySpendCoins(50);

        [ContextMenu("Jar: +10")]
        private void AddJarCoins() => Service.AddJarCoins(10);
    }
}
