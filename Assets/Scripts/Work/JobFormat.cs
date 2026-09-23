using System;
using UnityEngine;

namespace Work
{
    /// <summary>Text formatting shared by the job list and the active-job card.</summary>
    public static class JobFormat
    {
        /// <summary>"2 часа" for a whole number of hours (matches the job list mock), "чч:мм" otherwise.</summary>
        public static string Duration(float seconds)
        {
            int totalSeconds = Mathf.RoundToInt(seconds);

            if (totalSeconds > 0 && totalSeconds % 3600 == 0)
            {
                int hours = totalSeconds / 3600;
                return $"{hours} {PluralHours(hours)}";
            }

            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            return span.Hours > 0 ? $"{span.Hours:00}:{span.Minutes:00}" : $"{span.Minutes:00}:{span.Seconds:00}";
        }

        /// <summary>Live countdown, always чч:мм:сс — used on the active-job card.</summary>
        public static string Countdown(float seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }

        private static string PluralHours(int n)
        {
            int rem100 = n % 100;
            int rem10 = n % 10;

            if (rem100 >= 11 && rem100 <= 14)
            {
                return "часов";
            }

            if (rem10 == 1)
            {
                return "час";
            }

            if (rem10 >= 2 && rem10 <= 4)
            {
                return "часа";
            }

            return "часов";
        }
    }
}
