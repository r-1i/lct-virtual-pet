using System;

namespace Content
{
    /// <summary>Design-time data for one dream (savings goal), loaded from Resources/Content/dreams.json. Order in the file = order on screen and the order goals are auto-picked in.</summary>
    [Serializable]
    public class DreamDefinition
    {
        public string id;
        public string title;
        public string description;
        public int price;
        public string iconKey;
    }

    /// <summary>JsonUtility can't parse a bare top-level array, so dreams.json is { "dreams": [ ... ] }.</summary>
    [Serializable]
    public class DreamDefinitionList
    {
        public DreamDefinition[] dreams;
    }
}
