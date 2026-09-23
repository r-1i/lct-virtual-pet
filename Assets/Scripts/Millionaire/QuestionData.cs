using System;

namespace Millionaire
{
    /// <summary>
    /// Matches the structure of the qN.json files in Resources/Questions.
    /// correctAnswer is 1-based (1..4) to keep the JSON human-friendly.
    /// </summary>
    [Serializable]
    public class QuestionData
    {
        public string question;
        public string answer1;
        public string answer2;
        public string answer3;
        public string answer4;
        public int correctAnswer;

        public string[] GetAnswers()
        {
            return new[] { answer1, answer2, answer3, answer4 };
        }

        public int GetCorrectIndex()
        {
            return correctAnswer - 1;
        }
    }
}
