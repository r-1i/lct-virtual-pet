using Content;
using UnityEngine;

namespace Core
{
    /// <summary>Creates and registers every core service. Put this on one GameObject in the scene (name it "Bootstrap"). Other scripts must fetch services in Start(), not Awake() — Unity runs every object's Awake before any Start, so Start is always safe regardless of hierarchy order.</summary>
    public class Bootstrap : MonoBehaviour
    {
        private PlayerDataService _playerData;

        private void Awake()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            var saveService = new SaveService();
            _playerData = new PlayerDataService(saveService);
            var contentDatabase = new ContentDatabase();
            var jobService = new JobService(_playerData, contentDatabase);

            ServiceLocator.Register(saveService);
            ServiceLocator.Register(_playerData);
            ServiceLocator.Register(contentDatabase);
            ServiceLocator.Register(jobService);

            // Writes save.json immediately, even on a brand-new save — otherwise the file only
            // appears after the first real mutation (buying something, starting a job, etc.).
            _playerData.Save();

            Debug.Log($"Bootstrap: loaded save (coins={_playerData.Coins}, level={_playerData.Level}), " +
                      $"content ({contentDatabase.Jobs.Count} jobs, {contentDatabase.Products.Count} products).");
        }

        // Safety net for mobile: the OS can kill the app right after it's backgrounded, with no OnApplicationQuit.
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _playerData?.Save();
            }
        }
    }
}
