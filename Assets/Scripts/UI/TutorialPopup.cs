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
    /// </summary>
    public class TutorialPopup : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("The popup itself (Background). Its full-screen Image must be a Raycast Target to catch taps.")]
        [SerializeField] private GameObject root;
        [Tooltip("Text inside the cloud.")]
        [SerializeField] private TMP_Text descriptionLabel;
        [Tooltip("Taps are ignored for this long after the popup appears, so the tap that triggered it can't close it right away.")]
        [SerializeField] private float minShowTime = 0.3f;

        private TutorialService _tutorial;
        private float _shownAt;

        private void Start()
        {
            root.SetActive(false);

            _tutorial = ServiceLocator.Get<TutorialService>();
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
            descriptionLabel.text = step.description;
            root.SetActive(true);
            _shownAt = Time.unscaledTime;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!root.activeSelf || Time.unscaledTime - _shownAt < minShowTime)
            {
                return;
            }

            root.SetActive(false);
            _tutorial.CompleteActive();
        }
    }
}
