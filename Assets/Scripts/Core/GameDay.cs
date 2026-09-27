using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Real calendar days (device local time) as plain ints — "day N since 1970-01-01". Finance plans,
    /// history and the Barsuk deposit all read time through here, so the debug day offset
    /// (StatsDebug → "Day: +1") moves all of them together. The offset is kept in PlayerPrefs so a
    /// restart doesn't jump back in time.
    /// </summary>
    public static class GameDay
    {
        private const string DebugOffsetPrefsKey = "debug.dayOffset";
        private const long SecondsPerDay = 86400;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        private static int? _debugOffset;

        private static int DebugOffset
        {
            get
            {
                _debugOffset ??= PlayerPrefs.GetInt(DebugOffsetPrefsKey, 0);
                return _debugOffset.Value;
            }
        }

        public static int Today => (int)(DateTime.Now.Date - Epoch).TotalDays + DebugOffset;

        /// <summary>UTC unix seconds, shifted by the debug offset too.</summary>
        public static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds() + DebugOffset * SecondsPerDay;

        public static DateTime ToDate(int day) => Epoch.AddDays(day);

        public static void AddDebugDays(int days)
        {
            _debugOffset = DebugOffset + days;
            PlayerPrefs.SetInt(DebugOffsetPrefsKey, _debugOffset.Value);
        }

        public static void ResetDebugOffset()
        {
            _debugOffset = 0;
            PlayerPrefs.DeleteKey(DebugOffsetPrefsKey);
        }
    }
}
