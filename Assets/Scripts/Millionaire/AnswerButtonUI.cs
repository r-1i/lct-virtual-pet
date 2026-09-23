using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Millionaire
{
    /// <summary>
    /// Controller for a single answer button spawned inside the GridLayoutGroup.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AnswerButtonUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image background;

        [Header("Feedback colors")]
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.25f, 0.75f, 0.25f);
        [SerializeField] private Color wrongColor = new Color(0.8f, 0.2f, 0.2f);

        private Button _button;
        private int _answerIndex;
        private Action<int> _onClicked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (background == null)
            {
                background = GetComponent<Image>();
            }
        }

        public void Setup(int answerIndex, string answerText, Action<int> onClicked)
        {
            _answerIndex = answerIndex;
            _onClicked = onClicked;

            if (label != null)
            {
                label.text = answerText;
            }

            ResetVisual();

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(HandleClick);
        }

        public void SetInteractable(bool interactable)
        {
            _button.interactable = interactable;
        }

        public void ShowAsCorrect()
        {
            if (background != null) background.color = correctColor;
        }

        public void ShowAsWrong()
        {
            if (background != null) background.color = wrongColor;
        }

        public void ResetVisual()
        {
            if (background != null) background.color = defaultColor;
            _button.interactable = true;
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_answerIndex);
        }
    }
}
