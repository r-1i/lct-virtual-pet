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
