using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Parental gate in Настройки: "Профиль родителя" opens this page first. It shows a multiplication example
    /// (two-digit × one-digit) and four answers; the right one opens the parent page, a wrong one shows a new example.
    /// A new example every time the page is opened.
    /// </summary>
    public class ParentGate : MonoBehaviour
    {
        [SerializeField] private TMP_Text exampleLabel;
        [Tooltip("Four answer buttons; the answer text is taken from each button's child TMP_Text.")]
        [SerializeField] private Button[] answerButtons = new Button[4];
        [Tooltip("\"Неверно\" message under the answers. Optional.")]
        [SerializeField] private TMP_Text errorLabel;
        [Tooltip("This page (hidden when the answer is right).")]
        [SerializeField] private GameObject gatePage;
        [SerializeField] private GameObject parentPage;
        [SerializeField] private string wrongText = "Неверно, попробуйте другой пример";
        [SerializeField] private int minLeft = 12;
        [SerializeField] private int maxLeft = 29;
        [SerializeField] private int minRight = 3;
        [SerializeField] private int maxRight = 9;

        private int _answer;

        private void Awake()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] == null)
                {
                    continue;
                }

                Button button = answerButtons[i];
                button.onClick.AddListener(() => HandleAnswer(button));
            }
        }

        private void OnEnable()
        {
            SetError(false);
            NewExample();
        }

        private void NewExample()
        {
            int left = Random.Range(minLeft, maxLeft + 1);
            int right = Random.Range(minRight, maxRight + 1);
            _answer = left * right;
            if (exampleLabel != null)
            {
                exampleLabel.text = $"{left} × {right} = ?";
            }

            // Wrong answers close to the right one, so guessing by size doesn't work.
            var answers = new int[answerButtons.Length];
            int correctSlot = Random.Range(0, answers.Length);
            for (int i = 0; i < answers.Length; i++)
            {
                if (i == correctSlot)
                {
                    answers[i] = _answer;
                    continue;
                }

                int candidate;
                do
                {
                    int offset = Random.value < 0.5f ? right : Random.Range(1, 11);
                    candidate = _answer + (Random.value < 0.5f ? -offset : offset);
                } while (candidate <= 0 || System.Array.IndexOf(answers, candidate, 0, i) >= 0 || candidate == _answer);

                answers[i] = candidate;
            }

            for (int i = 0; i < answerButtons.Length; i++)
            {
                TMP_Text label = answerButtons[i] != null ? answerButtons[i].GetComponentInChildren<TMP_Text>(true) : null;
                if (label != null)
                {
                    label.text = answers[i].ToString();
                }
            }
        }

        private void HandleAnswer(Button button)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && int.TryParse(label.text, out int value) && value == _answer)
            {
                parentPage.SetActive(true);
                gatePage.SetActive(false);
                return;
            }

            SetError(true);
            NewExample();
        }

        private void SetError(bool visible)
        {
            if (errorLabel == null)
            {
                return;
            }

            errorLabel.text = visible ? wrongText : "";
        }
    }
}
