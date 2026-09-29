using System;
using Core;
using Home;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Creation
{
    /// <summary>
    /// First-launch character creation: welcome → name + model → colour → bow (3 colours or none). Started by IntroCutscene after
    /// the cutscene (or at launch when the cutscene was already watched); calls back right away if the character
    /// already exists. Every tap previews on the real 3D character through CharacterAppearance; nothing is saved
    /// until the last step, so quitting midway shows the creation again next launch.
    /// Lives on an always-active object — the four windows are switched on and off from here (all hidden at start,
    /// whatever state they were left in the scene). The game windows (Window_Home) are off during the creation and
    /// switched on after it — or right away when the character already exists.
    /// </summary>
    public class CharacterCreationFlow : MonoBehaviour
    {
        [SerializeField] private CharacterAppearance appearance;

        [Tooltip("The game's start screen (Window_Home): off while the creation is open, switched on after it and on every launch when the character already exists.")]
        [FormerlySerializedAs("hideWhileCreating")]
        [SerializeField] private GameObject[] gameWindows = Array.Empty<GameObject>();

        [Header("1. Welcome")]
        [SerializeField] private GameObject welcomeWindow;
        [SerializeField] private Button welcomeNextButton;

        [Header("2. Name + model")]
        [SerializeField] private GameObject profileWindow;
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private int maxNameLength = 12;
        [Tooltip("Same order as CharacterAppearance → Models.")]
        [SerializeField] private CreationOption[] modelOptions = Array.Empty<CreationOption>();
        [Tooltip("Inactive while the name is empty.")]
        [SerializeField] private Button profileNextButton;

        [Header("3. Colour")]
        [SerializeField] private GameObject colorWindow;
        [Tooltip("Colour 1, 2, 3 — same order as each model's Color Textures.")]
        [SerializeField] private CreationOption[] colorOptions = Array.Empty<CreationOption>();
        [SerializeField] private Button colorBackButton;
        [SerializeField] private Button colorNextButton;

        [Header("4. Bow")]
        [SerializeField] private GameObject accessoryWindow;
        [Tooltip("Bow colour 1, 2, 3 (same order as CharacterAppearance → Bow Colors), then \"Без бабочки\" as the LAST card.")]
        [SerializeField] private CreationOption[] accessoryOptions = Array.Empty<CreationOption>();
        [SerializeField] private Button accessoryBackButton;
        [SerializeField] private Button accessoryDoneButton;

        private PlayerDataService _playerData;
        private Action _onDone;
        private bool _running;
        private int _model;
        private int _color;
        private CharacterAccessory _accessory;
        private int _bowColor;

        /// <summary>Last card = no bow; the ones before it = bow colours.</summary>
        private int NoBowIndex => Mathf.Max(0, accessoryOptions.Length - 1);

        private void Awake()
        {
            ShowStep(null);

            AddListener(welcomeNextButton, () => ShowStep(profileWindow));
            AddListener(profileNextButton, () => ShowStep(colorWindow));
            AddListener(colorBackButton, () => ShowStep(profileWindow));
            AddListener(colorNextButton, () => ShowStep(accessoryWindow));
            AddListener(accessoryBackButton, () => ShowStep(colorWindow));
            AddListener(accessoryDoneButton, Finish);

            ListenOptions(modelOptions, i =>
            {
                _model = i;
                _color = 0;
            });
            ListenOptions(colorOptions, i => _color = i);
            ListenOptions(accessoryOptions, i =>
            {
                bool noBow = i >= NoBowIndex;
                _accessory = noBow ? CharacterAccessory.None : CharacterAccessory.Bow;
                if (!noBow)
                {
                    _bowColor = i;
                }
            });

            if (nameInput != null)
            {
                nameInput.onValueChanged.AddListener(_ => RefreshNameButton());
            }
        }

        /// <summary>Opens the creation if the character doesn't exist yet, otherwise calls onDone right away. onDone also runs after the last step.</summary>
        public void Run(Action onDone)
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();
            if (_playerData.IsCharacterCreated)
            {
                ShowStep(null);
                SetGameWindows(true);
                onDone?.Invoke();
                return;
            }

            _onDone = onDone;
            if (_running)
            {
                // Already open (e.g. Debug Menu pressed twice) — just make sure the game windows are still hidden under it.
                SetGameWindows(false);
                return;
            }

            _running = true;
            _model = 0;
            _color = 0;
            _accessory = CharacterAccessory.Bow;
            _bowColor = 0;

            SetGameWindows(false);

            if (nameInput != null)
            {
                nameInput.characterLimit = maxNameLength;
                nameInput.text = _playerData.PetName;
            }

            ShowStep(welcomeWindow != null ? welcomeWindow : profileWindow);
            Refresh();
        }

        private void Finish()
        {
            // The windows can also be opened by hand (left on in the scene) without Run — don't rely on its state.
            _playerData ??= ServiceLocator.Get<PlayerDataService>();
            string petName = nameInput != null ? nameInput.text.Trim() : "";
            _playerData.CreateCharacter(petName, _model, _color, _accessory, _bowColor);

            ShowStep(null);
            SetGameWindows(true);

            _running = false;
            Action onDone = _onDone;
            _onDone = null;
            onDone?.Invoke();
        }

        private void SetGameWindows(bool active)
        {
            foreach (GameObject window in gameWindows)
            {
                SetActive(window, active);
            }
        }

        private void ShowStep(GameObject window)
        {
            SetActive(welcomeWindow, window == welcomeWindow);
            SetActive(profileWindow, window == profileWindow);
            SetActive(colorWindow, window == colorWindow);
            SetActive(accessoryWindow, window == accessoryWindow);
        }

        private void Refresh()
        {
            SetSelected(modelOptions, _model);
            SetSelected(colorOptions, _color);
            SetSelected(accessoryOptions, _accessory == CharacterAccessory.Bow ? _bowColor : NoBowIndex);
            RefreshNameButton();

            if (appearance != null)
            {
                appearance.Show(_model, _color, _accessory, _bowColor);
            }
        }

        private void RefreshNameButton()
        {
            if (profileNextButton != null)
            {
                profileNextButton.interactable = nameInput == null || !string.IsNullOrWhiteSpace(nameInput.text);
            }
        }

        private void ListenOptions(CreationOption[] options, Action<int> pick)
        {
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                if (options[i] != null && options[i].Button != null)
                {
                    options[i].Button.onClick.AddListener(() =>
                    {
                        pick(index);
                        Refresh();
                    });
                }
            }
        }

        private static void SetSelected(CreationOption[] options, int selected)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null)
                {
                    options[i].SetSelected(i == selected);
                }
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null)
            {
                go.SetActive(active);
            }
        }
    }
}
