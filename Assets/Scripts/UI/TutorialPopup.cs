using Content;
using Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// Tutorial window: full-screen dim + character + cloud with text. Must sit on an object that stays
    /// active (e.g. the canvas), with the popup itself as the child "root" that gets SetActive — an
    /// inactive object never runs Start and couldn't subscribe. A tap anywhere on the root bubbles up
    /// here, hides the window and completes the step.
    /// "{name}" in a step's text is replaced with the pet's name. A step's "sound" (clip name) is played
    /// from Voice Clips while the popup is on screen and stops when it's dismissed.
    /// </summary>
    public class TutorialPopup : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("The popup itself (Background). Its full-screen Image must be a Raycast Target to catch taps.")]
        [SerializeField] private GameObject root;
        [Tooltip("Text inside the cloud.")]
        [SerializeField] private TMP_Text descriptionLabel;
        [Tooltip("Taps are ignored for this long after the popup appears, so the tap that triggered it can't close it right away.")]
        [SerializeField] private float minShowTime = 0.3f;

        [Header("Voice")]
        [Tooltip("Play On Awake and Loop off.")]
        [SerializeField] private AudioSource voiceSource;
        [Tooltip("All voice lines (Sounds/Tutorial). A step's \"sound\" in tutorial.json is the clip name, e.g. \"t7\".")]
        [SerializeField] private AudioClip[] voiceClips = new AudioClip[0];
        [Tooltip("Shown instead of {name} while the pet has no name yet.")]
        [SerializeField] private string defaultPetName = "питомца";

        private TutorialService _tutorial;
        private PlayerDataService _playerData;
        private float _shownAt;
        private readonly System.Collections.Generic.Queue<AudioClip> _queue = new System.Collections.Generic.Queue<AudioClip>();

        private void Start()
        {
            root.SetActive(false);

            _tutorial = ServiceLocator.Get<TutorialService>();
            _playerData = ServiceLocator.Get<PlayerDataService>();
            _tutorial.ShowRequested += Show;

            // TryShow may have been called by another Start before this one subscribed.
            if (_tutorial.ActiveStep != null)
            {
                Show(_tutorial.ActiveStep);
            }
        }

        private void OnDestroy()
        {
            if (_tutorial != null)
            {
                _tutorial.ShowRequested -= Show;
            }
        }

        private void Show(TutorialStepDefinition step)
        {
            string petName = _playerData != null && !string.IsNullOrEmpty(_playerData.PetName) ? _playerData.PetName : defaultPetName;
            descriptionLabel.text = (step.description ?? "").Replace("{name}", petName);
            root.SetActive(true);
            _shownAt = Time.unscaledTime;
            PlayVoice(step.sound);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!root.activeSelf || Time.unscaledTime - _shownAt < minShowTime)
            {
                return;
            }

            root.SetActive(false);
            StopVoice();
            _tutorial.CompleteActive();
        }

        /// <summary>"t1" or several clips in a row: "t1, t2".</summary>
        private void PlayVoice(string sound)
        {
            if (voiceSource == null)
            {
                return;
            }

            StopVoice();
            if (string.IsNullOrEmpty(sound))
            {
                return;
            }

            foreach (string name in sound.Split(','))
            {
                string clipName = name.Trim();
                AudioClip clip = System.Array.Find(voiceClips, c => c != null && c.name == clipName);
                if (clip != null)
                {
                    _queue.Enqueue(clip);
                }
                else if (clipName.Length > 0)
                {
                    Debug.LogWarning($"TutorialPopup: voice clip '{clipName}' is not in Voice Clips.", this);
                }
            }

            PlayNextInQueue();
        }

        private void Update()
        {
            if (voiceSource != null && _queue.Count > 0 && !voiceSource.isPlaying)
            {
                PlayNextInQueue();
            }
        }

        private void PlayNextInQueue()
        {
            if (_queue.Count == 0)
            {
                return;
            }

            voiceSource.clip = _queue.Dequeue();
            voiceSource.Play();
        }

        private void StopVoice()
        {
            _queue.Clear();
            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
        }
    }
}
