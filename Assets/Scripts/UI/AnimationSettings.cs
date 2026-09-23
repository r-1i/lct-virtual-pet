using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    /// <summary>Global "animations on/off" switch. Off freezes every Animator in the loaded scenes (speed = 0); the choice is saved and re-applied on every scene load.</summary>
    public static class AnimationSettings
    {
        private const string PrefsKey = "settings.animations";

        public static bool Enabled { get; private set; } = true;

        /// <summary>Raised after the value changes. Code-driven effects (lerps, light pulses) can subscribe or just check <see cref="Enabled"/>.</summary>
        public static event Action<bool> Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            Enabled = PlayerPrefs.GetInt(PrefsKey, 1) == 1;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (!Enabled)
            {
                Apply();
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!Enabled)
            {
                Apply();
            }
        }

        public static void SetEnabled(bool enabled)
        {
            if (Enabled == enabled)
            {
                return;
            }

            Enabled = enabled;
            PlayerPrefs.SetInt(PrefsKey, enabled ? 1 : 0);
            Apply();
            Changed?.Invoke(enabled);
        }

        public static void Toggle()
        {
            SetEnabled(!Enabled);
        }

        private static void Apply()
        {
            float speed = Enabled ? 1f : 0f;
            Animator[] animators = UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].speed = speed;
            }
        }
    }
}
