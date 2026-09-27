using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Reusable "are you sure?" popup: message + Да / Нет. Show(message, onYes) — onYes runs only on "Да".
    /// Put it on the popup root (with a CanvasGroup), inactive by default. A full-screen dimmer
    /// behind the window, inside the root, blocks clicks on the screen underneath.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("The window itself — gets the pop-in scale. Optional.")]
        [SerializeField] private RectTransform window;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private float fadeDuration = 0.2f;

        private Action _onYes;

        private float Duration => AnimationSettings.Enabled ? fadeDuration : 0f;

        private void Awake()
        {
            yesButton.onClick.AddListener(HandleYes);
            noButton.onClick.AddListener(Hide);
        }

        public void Show(string message, Action onYes)
        {
            _onYes = onYes;
            messageLabel.text = message;

            gameObject.SetActive(true);
            canvasGroup.DOKill();
            canvasGroup.interactable = true;
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, Duration);

            if (window != null)
            {
                window.DOKill();
                window.localScale = Vector3.one * 0.85f;
                window.DOScale(1f, Duration).SetEase(Ease.OutBack);
            }
        }

        public void Hide()
        {
            _onYes = null;
            canvasGroup.DOKill();
            canvasGroup.interactable = false;
            canvasGroup.DOFade(0f, Duration).OnComplete(() => gameObject.SetActive(false));
        }

        private void HandleYes()
        {
            Action onYes = _onYes;
            Hide();
            onYes?.Invoke();
        }
    }
}
