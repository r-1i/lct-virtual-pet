using System;

namespace Content
{
    /// <summary>
    /// Design-time data for one job, loaded from Resources/Content/jobs.json. Read-only content, not save data.
    /// Only the name and the level are per job; duration, base income and stat costs come from the shift type
    /// in economy.json (light / normal / heavy), the actual pay from EconomySettings.ShiftReward.
    /// </summary>
    [Serializable]
    public class JobDefinition
    {
        public string id;
        public string title;
        public int level;

        /// <summary>Id of a shift type in economy.json → shiftTypes: "light", "normal" or "heavy".</summary>
        public string shift;

        public string iconKey;
    }

    /// <summary>JsonUtility can't parse a bare top-level array, so jobs.json is { "jobs": [ ... ] }.</summary>
    [Serializable]
    public class JobDefinitionList
    {
        public JobDefinition[] jobs;
    }
}
