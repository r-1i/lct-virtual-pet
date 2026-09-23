using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Millionaire
{
    /// <summary>
    /// Drives the quiz: pulls a random question, spawns answer buttons inside the
    /// GridLayoutGroup, checks the picked answer and moves on to the next question.
    /// </summary>
    public class QuizGameManager : MonoBehaviour
    {
        [Header("UI references")]
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private RectTransform answerGrid; // holds a GridLayoutGroup
        [SerializeField] private AnswerButtonUI answerButtonPrefab;

        [Header("Game over UI (optional)")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text resultText;

        [Header("Timing")]
        [SerializeField] private float feedbackDelay = 1.2f;

        private QuestionLoader _loader;
        private readonly List<AnswerButtonUI> _spawnedButtons = new List<AnswerButtonUI>();
        private QuestionData _currentQuestion;
        private int _questionNumber;
        private bool _inputLocked;

        private void Start()
        {
            _loader = new QuestionLoader();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            LoadNextQuestion();
        }

        private void LoadNextQuestion()
        {
            ClearAnswerButtons();

            _currentQuestion = _loader.GetNextRandom();
            if (_currentQuestion == null)
            {
                EndGame(true);
                return;
            }

            _questionNumber++;
            _inputLocked = false;

            if (questionText != null)
            {
                questionText.text = _currentQuestion.question;
            }

            string[] answers = _currentQuestion.GetAnswers();
            for (int i = 0; i < answers.Length; i++)
            {
                AnswerButtonUI button = Instantiate(answerButtonPrefab, answerGrid);
                button.Setup(i, answers[i], OnAnswerSelected);
                _spawnedButtons.Add(button);
            }
        }

        private void OnAnswerSelected(int answerIndex)
        {
            if (_inputLocked || _currentQuestion == null)
            {
                return;
            }

            _inputLocked = true;
            foreach (AnswerButtonUI button in _spawnedButtons)
            {
                button.SetInteractable(false);
            }

            int correctIndex = _currentQuestion.GetCorrectIndex();
            _spawnedButtons[correctIndex].ShowAsCorrect();

            bool isCorrect = answerIndex == correctIndex;
            if (!isCorrect)
            {
                _spawnedButtons[answerIndex].ShowAsWrong();
            }

            StartCoroutine(AdvanceAfterDelay(isCorrect));
        }

        private IEnumerator AdvanceAfterDelay(bool wasCorrect)
        {
            yield return new WaitForSeconds(feedbackDelay);

            if (!wasCorrect)
            {
                EndGame(false);
                yield break;
            }

            LoadNextQuestion();
        }

        private void ClearAnswerButtons()
        {
            foreach (AnswerButtonUI button in _spawnedButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _spawnedButtons.Clear();
        }

        private void EndGame(bool won)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (resultText != null)
            {
                resultText.text = won
                    ? $"Поздравляем! Вы ответили на все {_questionNumber} вопрос(ов)."
                    : $"Игра окончена. Правильных ответов подряд: {_questionNumber - 1}.";
            }
        }

        /// <summary>Call from a UI "Play again" button.</summary>
        public void RestartGame()
        {
            _loader.Reset();
            _questionNumber = 0;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            LoadNextQuestion();
        }
    }
}
