namespace Core
{
    /// <summary>Published by PlayerDataService whenever coins or jar coins change.</summary>
    public readonly struct CoinsChangedEvent
    {
        public readonly int Coins;
        public readonly int JarCoins;

        public CoinsChangedEvent(int coins, int jarCoins)
        {
            Coins = coins;
            JarCoins = jarCoins;
        }
    }

    /// <summary>Published by PlayerDataService whenever satiety/mood/health change.</summary>
    public readonly struct StatsChangedEvent
    {
        public readonly CharacterStats Stats;

        public StatsChangedEvent(CharacterStats stats)
        {
            Stats = stats;
        }
    }

    /// <summary>Published by PlayerDataService whenever the active job changes (started, cleared/collected).</summary>
    public readonly struct JobStateChangedEvent
    {
        public readonly JobRuntimeState State;

        public JobStateChangedEvent(JobRuntimeState state)
        {
            State = state;
        }
    }

    /// <summary>Published by PlayerDataService whenever one product's inventory count changes.</summary>
    public readonly struct InventoryChangedEvent
    {
        public readonly string ProductId;
        public readonly int NewCount;

        public InventoryChangedEvent(string productId, int newCount)
        {
            ProductId = productId;
            NewCount = newCount;
        }
    }

    /// <summary>Published by PlayerDataService whenever the player level changes.</summary>
    public readonly struct LevelChangedEvent
    {
        public readonly int Level;

        public LevelChangedEvent(int level)
        {
            Level = level;
        }
    }

    /// <summary>Published by PlayerDataService when the current dream changes (picked, or the previous one bought) or the planned saving per day changes (the forecast depends on it). Empty id = every dream is bought.</summary>
    public readonly struct DreamChangedEvent
    {
        public readonly string CurrentDreamId;

        public DreamChangedEvent(string currentDreamId)
        {
            CurrentDreamId = currentDreamId;
        }
    }

    /// <summary>Published by PlayerDataService when the finance plan, its last result, the Barsuk deposit or the money history changes.</summary>
    public readonly struct FinanceChangedEvent
    {
    }

    /// <summary>Published by PlayerDataService when shifts or passed study tasks change. Level-ups come separately as LevelChangedEvent.</summary>
    public readonly struct StudyChangedEvent
    {
    }

    /// <summary>Published by PlayerDataService when the character is created (or the creation is reset from debug). Read the look from PlayerDataService.</summary>
    public readonly struct CharacterChangedEvent
    {
    }

    /// <summary>Published by MailService when a letter arrives, is read, or letters are removed. Read the list from MailService.</summary>
    public readonly struct MailChangedEvent
    {
    }

    /// <summary>Published by ZoneManager after GoToZone finishes (camera always moves; the character only if it wasn't busy). Index matches ZoneManager's Zones array.</summary>
    public readonly struct ZoneChangedEvent
    {
        public readonly int ZoneIndex;

        public ZoneChangedEvent(int zoneIndex)
        {
            ZoneIndex = zoneIndex;
        }
    }
}
