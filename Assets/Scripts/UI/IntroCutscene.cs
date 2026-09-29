using Core;
using Creation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// First-launch slideshow over the whole screen. Plays only while PlayerData.firstLaunchCompleted
    /// is false; a tap cross-fades to the next slide, a tap on the last one fades the cutscene out and
    /// marks the first launch as done. Taps land here through the EventSystem (the slide Images are
    /// raycast targets and the click bubbles up to this object).
    /// </summary>
    public class IntroCutscene : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Covers the whole screen, blocks raycasts to the game below. Faded out at the end.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Two full-screen Images on top of each other: the new slide fades in over the old one.")]
        [SerializeField] private Image slideA;
        [SerializeField] private Image slideB;

        [SerializeField] private Sprite[] slides;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Sound")]
        [Tooltip("Plays the slide sounds. Play On Awake = off, Loop = off.")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Same order as Slides: element N plays when slide N appears. Can be shorter than Slides or have empty elements — those slides are silent.")]
        [SerializeField] private AudioClip[] slideSounds;

        [Header("After the cutscene")]
        [Tooltip("First-launch character creation, shown after the cutscene and before the tutorial. Empty = straight to the tutorial.")]
        [SerializeField] private CharacterCreationFlow characterCreation;

        private PlayerDataService _playerData;
        private Image _front;
        private Image _back;
        private int _index = -1;
        private Tween _tween;
        private bool _finishing;

        private float Duration => AnimationSettings.Enabled ? fadeDuration : 0f;

        private void Start()
        {
            _playerData = ServiceLocator.Get<PlayerDataService>();

            if (!_playerData.IsFirstLaunch || slides == null || slides.Length == 0)
            {
                gameObject.SetActive(false);
                ContinueAfterCutscene();
                return;
            }

            _front = slideA;
            _back = slideB;
            SetAlpha(_front, 0f);
            SetAlpha(_back, 0f);

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            ShowNext();
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            canvasGroup.DOKill();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_finishing)
            {
                return;
            }

            // A tap mid-fade just finishes the current fade instead of skipping a slide.
            if (_tween != null && _tween.IsActive() && _tween.IsPlaying())
            {
                _tween.Complete();
                return;
            }

            if (_index >= slides.Length - 1)
            {
                Finish();
            }
            else
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            _index++;

            // The hidden image gets the new sprite, goes on top and fades in over the current one.
            Image incoming = _back;
            Image outgoing = _front;

            incoming.sprite = slides[_index];
            incoming.transform.SetAsLastSibling();
            SetAlpha(incoming, 0f);

            _tween = incoming.DOFade(1f, Duration).OnComplete(() => SetAlpha(outgoing, 0f));

            _front = incoming;
            _back = outgoing;

            PlaySlideSound(_index);
        }

        /// <summary>Cuts off the previous slide's sound, so a fast tapper doesn't stack them.</summary>
        private void PlaySlideSound(int index)
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.Stop();

            AudioClip clip = slideSounds != null && index < slideSounds.Length ? slideSounds[index] : null;
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        private void Finish()
        {
            _finishing = true;
            _playerData.CompleteFirstLaunch();

            canvasGroup.blocksRaycasts = false;
            canvasGroup.DOFade(0f, Duration).OnComplete(() =>
            {
                gameObject.SetActive(false);
                ContinueAfterCutscene();
            });
        }

        /// <summary>Character creation (skipped at once if the character exists), then the tutorial.</summary>
        private void ContinueAfterCutscene()
        {
            if (characterCreation != null)
            {
                characterCreation.Run(StartTutorial);
            }
            else
            {
                StartTutorial();
            }
        }

        /// <summary>The greeting chain starts right after the character creation. No-op once the tutorial is past that step.</summary>
        private static void StartTutorial()
        {
            ServiceLocator.Get<TutorialService>().TryShow(TutorialStepIds.Welcome);
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }
}
