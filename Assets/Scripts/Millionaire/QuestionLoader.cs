using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Millionaire
{
    /// <summary>
    /// Loads every qN.json file from Resources/Questions and hands out random,
    /// non-repeating questions for the current game session.
    /// </summary>
    public class QuestionLoader
    {
        private const string ResourcesFolder = "Questions";

        private readonly List<QuestionData> _allQuestions;
        private readonly List<QuestionData> _remaining;

        public QuestionLoader()
        {
            _allQuestions = Resources.LoadAll<TextAsset>(ResourcesFolder)
                .Select(json => JsonUtility.FromJson<QuestionData>(json.text))
                .Where(q => q != null)
                .ToList();

            if (_allQuestions.Count == 0)
            {
                Debug.LogError($"QuestionLoader: no question files found in Resources/{ResourcesFolder}");
            }

            _remaining = new List<QuestionData>(_allQuestions);
        }

        public int TotalCount => _allQuestions.Count;

        public bool HasNext => _remaining.Count > 0;

        /// <summary>Returns a random question and removes it from the pool so it won't repeat.</summary>
        public QuestionData GetNextRandom()
        {
            if (!HasNext)
            {
                return null;
            }

            int index = Random.Range(0, _remaining.Count);
            QuestionData question = _remaining[index];
            _remaining.RemoveAt(index);
            return question;
        }

        /// <summary>Refills the pool so the same set of questions can be replayed.</summary>
        public void Reset()
        {
            _remaining.Clear();
            _remaining.AddRange(_allQuestions);
        }
    }
}
