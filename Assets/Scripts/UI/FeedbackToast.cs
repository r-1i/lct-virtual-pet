using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// The on-screen part of Feedback: a pill with one line of text that fades in, stays showSeconds and fades out; queued
    /// messages follow one by one (faster when several are waiting). Lives on an always-active object above the windows;
    /// its CanvasGroup doesn't block clicks. Only the one in the scene is used (Instance).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FeedbackToast : MonoBehaviour
    {
        public static FeedbackToast Instance { get; private set; }

        [SerializeField] private TMP_Text label;
        [SerializeField] private float showSeconds = 1.8f;
        [Tooltip("Used instead of Show Seconds while more messages are waiting.")]
        [SerializeField] private float busyShowSeconds = 1f;
        [SerializeField] private float fadeSeconds = 0.2f;

        private readonly Queue<string> _queue = new Queue<string>();
        private CanvasGroup _group;
        private float _hideAt;
        private bool _showing;

        private void Awake()
        {
            Instance = this;
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Feedback.ClearPending();
            }
        }

        public void Enqueue(string message) => _queue.Enqueue(message);

        private void Update()
        {
            while (Feedback.TryTakePending(out string pending))
            {
                _queue.Enqueue(pending);
            }

            if (_showing)
            {
                if (Time.unscaledTime < _hideAt)
                {
                    return;
                }

                _showing = false;
                _group.DOKill();
                _group.DOFade(0f, fadeSeconds).SetUpdate(true);
                _hideAt = Time.unscaledTime + fadeSeconds;
                return;
            }

            if (Time.unscaledTime < _hideAt || _queue.Count == 0)
            {
                return;
            }

            label.text = _queue.Dequeue();
            _showing = true;
            _hideAt = Time.unscaledTime + fadeSeconds + (_queue.Count > 0 ? busyShowSeconds : showSeconds);
            _group.DOKill();
            _group.DOFade(1f, fadeSeconds).SetUpdate(true);
        }
    }
}
