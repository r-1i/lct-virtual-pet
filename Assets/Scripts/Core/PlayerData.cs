using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>0..100 gauges shown as the three status bars (Сытость/Настроение/Здоровье).</summary>
    [Serializable]
    public class CharacterStats
    {
        public float satiety = 100f;
        public float mood = 100f;
        public float health = 100f;
    }

    /// <summary>The currently active (or finished-but-not-collected) job. activeJobId empty = not working.</summary>
    [Serializable]
    public class JobRuntimeState
    {
        public string activeJobId = "";
        public long startedAtUnix;
        public float durationSeconds;

        public bool IsActive => !string.IsNullOrEmpty(activeJobId);
    }

    /// <summary>
    /// One purchase batch of a product. Kept separate per purchase (not merged into a single
    /// productId→count entry) because different batches can have different expiry — bought at
    /// different times, so they go bad at different times. Removal of expired stacks is deferred
    /// (see PLAN.md backlog), but the data already supports it since purchasedAtUnix is per stack.
    /// </summary>
    [Serializable]
    public class InventoryStack
    {
        public string stackId;
        public string productId;
        public int count;
        public long purchasedAtUnix;
    }

    /// <summary>Everything that gets written to save.json. Only PlayerDataService is allowed to mutate an instance of this.</summary>
    [Serializable]
    public class PlayerData
    {
        public int coins;
        public int jarCoins;
        public int level = 1;
        public CharacterStats stats = new CharacterStats();
        public JobRuntimeState job = new JobRuntimeState();
        public List<InventoryStack> inventory = new List<InventoryStack>();

        /// <summary>False until the intro cutscene has been watched to the end — then it never plays again. False (not "isFirstLaunch = true") so a save without this field still reads as a first launch.</summary>
        public bool firstLaunchCompleted;

        /// <summary>Starter food (for the feeding tutorial) already handed out — see PlayerDataService.GiveStarterItems.</summary>
        public bool starterItemsGiven;

        /// <summary>Index into tutorial.json of the next tutorial popup to show. >= step count = tutorial finished.</summary>
        public int tutorialStep;

        /// <summary>TutorialHighlight keys that are pulsing right now (dismissed hint, target not clicked yet). Saved so a restart doesn't lose them.</summary>
        public List<string> tutorialHighlights = new List<string>();
    }
}
