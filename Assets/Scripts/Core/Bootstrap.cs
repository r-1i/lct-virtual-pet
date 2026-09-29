using Content;
using UnityEngine;

namespace Core
{
    /// <summary>Creates and registers every core service. Put this on one GameObject in the scene (name it "Bootstrap"). Other scripts must fetch services in Start(), not Awake() — Unity runs every object's Awake before any Start, so Start is always safe regardless of hierarchy order.</summary>
    public class Bootstrap : MonoBehaviour
    {
        /// <summary>How often to check whether a real calendar day ended while the game is open (day snapshot + letter, finishes the finance plan).</summary>
        private const float DayCheckInterval = 10f;

        private PlayerDataService _playerData;
        private FinanceService _financeService;
        private MailService _mailService;

        private void Awake()
        {
            ServiceLocator.Clear();
            EventBus.Clear();

            var saveService = new SaveService();
            _playerData = new PlayerDataService(saveService);
            var contentDatabase = new ContentDatabase();
            _playerData.GiveStarterItems(contentDatabase.Economy.startCoins, contentDatabase.Economy.startStats);
            _playerData.RemoveUnknownInventory(id => contentDatabase.FindProduct(id) != null);
            _playerData.StampUndatedInventory();
            _playerData.StampFirstDay();
            var dayService = new DayService(_playerData, contentDatabase);
            // Before FinanceService: a finished day must be snapshotted before the new day changes anything
            // (MailService snapshots it, then lets DayService run the night: cashback, spoiled food, stat decay).
            _mailService = new MailService(saveService, _playerData, dayService);
            var tutorialService = new TutorialService(_playerData, contentDatabase);
            var jobService = new JobService(_playerData, contentDatabase, tutorialService);
            var dreamService = new DreamService(_playerData, contentDatabase);
            _financeService = new FinanceService(_playerData, contentDatabase);
            var studyService = new StudyService(_playerData, contentDatabase);

            ServiceLocator.Register(saveService);
            ServiceLocator.Register(_playerData);
            ServiceLocator.Register(contentDatabase);
            ServiceLocator.Register(jobService);
            ServiceLocator.Register(tutorialService);
            ServiceLocator.Register(dreamService);
            ServiceLocator.Register(_financeService);
            ServiceLocator.Register(studyService);
            ServiceLocator.Register(_mailService);
            ServiceLocator.Register(dayService);

            // Writes save.json immediately, even on a brand-new save — otherwise the file only
            // appears after the first real mutation (buying something, starting a job, etc.).
            _playerData.Save();

            Debug.Log($"Bootstrap: loaded save (coins={_playerData.Coins}, level={_playerData.Level}), " +
                      $"content ({contentDatabase.Jobs.Count} jobs, {contentDatabase.Products.Count} products).");
        }

        private void Start()
        {
            InvokeRepeating(nameof(CheckDay), DayCheckInterval, DayCheckInterval);
        }

        private void CheckDay()
        {
            _mailService?.CheckDay();
            _financeService?.CheckPeriod();
        }

        // Safety net for mobile: the OS can kill the app right after it's backgrounded, with no OnApplicationQuit.
        // Coming back can also be the next day — check the finance plan right away.
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _playerData?.Save();
            }
            else
            {
                CheckDay();
            }
        }
    }
}
