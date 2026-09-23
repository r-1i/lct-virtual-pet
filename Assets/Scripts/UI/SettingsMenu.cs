using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Settings menu: music / SFX volume through an AudioMixer and a green/red toggle for all animations.
    /// Put it on an object that is always active (not on the panel itself), otherwise saved volumes are not applied at launch.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        private const string MusicPrefsKey = "settings.musicVolume";
        private const string SfxPrefsKey = "settings.sfxVolume";
        private const float MinLinearVolume = 0.0001f; // -80 dB, i.e. silence

        [Header("Panel (optional)")]
        [Tooltip("The settings window. Open()/Close()/Toggle() show and hide it; wire them to your buttons.")]
        [SerializeField] private GameObject panel;

        [Header("Audio")]
        [SerializeField] private AudioMixer mixer;
        [Tooltip("Name of the exposed Volume parameter of the Music group.")]
        [SerializeField] private string musicParam = "MusicVolume";
        [Tooltip("Name of the exposed Volume parameter of the SFX group.")]
        [SerializeField] private string sfxParam = "SfxVolume";
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 1f;

        [Header("Animations toggle")]
        [SerializeField] private Button animationsButton;
        [Tooltip("Image that changes color. Leave empty to use the button's own Image.")]
        [SerializeField] private Image animationsButtonImage;
        [SerializeField] private TMP_Text animationsLabel;
        [SerializeField] private Color onColor = new Color(0.25f, 0.7f, 0.3f);
        [SerializeField] private Color offColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private string onText = "Вкл";
        [SerializeField] private string offText = "Выкл";

        private void Awake()
        {
            if (animationsButtonImage == null && animationsButton != null)
            {
                animationsButtonImage = animationsButton.GetComponent<Image>();
            }

            ConfigureSlider(musicSlider);
            ConfigureSlider(sfxSlider);

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(SetSfxVolume);
            }

            if (animationsButton != null)
            {
                animationsButton.onClick.AddListener(AnimationSettings.Toggle);
            }
        }

        // AudioMixer.SetFloat is unreliable in Awake, so the saved values are applied here.
        private void Start()
        {
            float music = PlayerPrefs.GetFloat(MusicPrefsKey, defaultMusicVolume);
            float sfx = PlayerPrefs.GetFloat(SfxPrefsKey, defaultSfxVolume);

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(music);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(sfx);
            }

            ApplyVolume(musicParam, music);
            ApplyVolume(sfxParam, sfx);
            RefreshAnimationsButton(AnimationSettings.Enabled);
        }

        private void OnEnable()
        {
            AnimationSettings.Changed += RefreshAnimationsButton;
            RefreshAnimationsButton(AnimationSettings.Enabled);
        }

        private void OnDisable()
        {
            AnimationSettings.Changed -= RefreshAnimationsButton;
        }

        private void OnDestroy()
        {
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
            }

            if (animationsButton != null)
            {
                animationsButton.onClick.RemoveListener(AnimationSettings.Toggle);
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PlayerPrefs.Save();
            }
        }

        public void Open()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Close()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }

            PlayerPrefs.Save();
        }

        public void Toggle()
        {
            if (panel != null && panel.activeSelf)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void SetMusicVolume(float linear)
        {
            PlayerPrefs.SetFloat(MusicPrefsKey, linear);
            ApplyVolume(musicParam, linear);
        }

        public void SetSfxVolume(float linear)
        {
            PlayerPrefs.SetFloat(SfxPrefsKey, linear);
            ApplyVolume(sfxParam, linear);
        }

        private void ApplyVolume(string parameter, float linear)
        {
            if (mixer == null)
            {
                return;
            }

            float decibels = Mathf.Log10(Mathf.Max(linear, MinLinearVolume)) * 20f;
            if (!mixer.SetFloat(parameter, decibels))
            {
                Debug.LogWarning($"SettingsMenu: mixer parameter '{parameter}' is not exposed.");
            }
        }

        private void RefreshAnimationsButton(bool animationsOn)
        {
            if (animationsButtonImage != null)
            {
                animationsButtonImage.color = animationsOn ? onColor : offColor;
            }

            if (animationsLabel != null)
            {
                animationsLabel.text = animationsOn ? onText : offText;
            }
        }

        private static void ConfigureSlider(Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }
    }
}
