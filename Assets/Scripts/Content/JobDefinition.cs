using System;

namespace Content
{
    /// <summary>Design-time data for one job, loaded from Resources/Content/jobs.json. Read-only content, not save data.</summary>
    [Serializable]
    public class JobDefinition
    {
        public string id;
        public string title;
        public int level;
        public float durationSeconds;
        public int reward;
        public string iconKey;
    }

    /// <summary>JsonUtility can't parse a bare top-level array, so jobs.json is { "jobs": [ ... ] }.</summary>
    [Serializable]
    public class JobDefinitionList
    {
        public JobDefinition[] jobs;
    }
}
