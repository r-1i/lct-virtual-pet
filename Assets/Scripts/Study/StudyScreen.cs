using System;
using System.Collections.Generic;
using Content;
using Core;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Study
{
    /// <summary>
    /// Root of the "Учёба" window (its own Zone; showing the window is wired in the scene). Four pages,
    /// one visible at a time: main (career + theme cards), "Разложи", "Реши", result. Every opening of
    /// the window starts on main. Theme cards are fixed slots in theme order — extra slots hidden.
    /// All art (characters, option/row icons) comes from the Sprites list by the keys used in the json.
    /// </summary>
    public class StudyScreen : MonoBehaviour
    {
        [Header("Pages (children of this window)")]
        [SerializeField] private GameObject mainPage;
        [SerializeField] private DistributeTaskPage distributePage;
        [SerializeField] private ChooseTaskPage choosePage;
        [SerializeField] private StudyResultPage resultPage;

        [Header("Career")]
        [Tooltip("\"Карьера: Стажёр\"")]
        [SerializeField] private TMP_Text careerTitleLabel;
        [Tooltip("\"до повышения: сдать тему «…»\" / \"Высшая должность!\"")]
        [SerializeField] private TMP_Text careerNextLabel;
        [Tooltip("(level − 1) / (max level − 1). Filled → fillAmount; Sliced → right anchor (left anchor at 0). Optional.")]
        [SerializeField] private Image careerProgressFill;
        [Tooltip("\"Стажёр → Младший → …\" with the current one in bold. Optional.")]
        [SerializeField] private TMP_Text careerStepsLabel;
        [Tooltip("\"Смен на работе: 4\". Optional.")]
        [SerializeField] private TMP_Text shiftsLabel;

        [Header("Current theme")]
        [Tooltip("The one card — always the first theme not passed yet (locked until enough shifts).")]
        [SerializeField] private StudyThemeCardView themeCard;
        [Tooltip("\"Все темы сданы!\" — shown instead of the card when every theme is passed.")]
        [SerializeField] private GameObject allDoneRoot;

        [Header("Art")]
        [Tooltip("Keys from the task json: speaker (badger, fox) and iconKey of rows/options. Missing key = the Image keeps its placeholder.")]
        [SerializeField] private KeyedSprite[] sprites = Array.Empty<KeyedSprite>();

        private StudyService _study;

        private void Start()
        {
            _study = ServiceLocator.Get<StudyService>();
            EventBus.Subscribe<StudyChangedEvent>(OnStudyChanged);
            EventBus.Subscribe<LevelChangedEvent>(OnLevelChanged);
            ShowMain();
        }

        /// <summary>Every opening of the window starts on the main page.</summary>
        private void OnEnable()
        {
            if (_study != null)
            {
                ShowMain();
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<StudyChangedEvent>(OnStudyChanged);
            EventBus.Unsubscribe<LevelChangedEvent>(OnLevelChanged);
        }

        private void OnStudyChanged(StudyChangedEvent e) => RefreshMain();
        private void OnLevelChanged(LevelChangedEvent e) => RefreshMain();

        private Sprite FindSprite(string key) => KeyedSprite.Find(sprites, key);

        private void ShowPage(GameObject page)
        {
            mainPage.SetActive(page == mainPage);
            distributePage.gameObject.SetActive(page == distributePage.gameObject);
            choosePage.gameObject.SetActive(page == choosePage.gameObject);
            resultPage.gameObject.SetActive(page == resultPage.gameObject);
        }

        private void ShowMain()
        {
            ShowPage(mainPage);
            RefreshMain();
        }

        private void RefreshMain()
        {
            if (_study == null || !mainPage.activeSelf)
            {
                return;
            }

            RefreshCareer();

            StudyThemeDefinition current = _study.NextTheme;
            themeCard.gameObject.SetActive(current != null);
            StudyViewUtils.SetActive(allDoneRoot, current == null);
            if (current != null)
            {
                themeCard.Setup(current, _study, OpenTask);
            }
        }

        private void RefreshCareer()
        {
            int level = _study.Level;
            int max = _study.MaxLevel;

            careerTitleLabel.text = $"Карьера: {_study.RankSummary(level)}";

            StudyThemeDefinition next = _study.NextTheme;
            StudyViewUtils.SetText(careerNextLabel, next != null ? $"до повышения: сдать тему «{next.title}»" : "Высшая должность!");

            if (careerProgressFill != null)
            {
                float amount = max > 1 ? Mathf.Clamp01((level - 1) / (float)(max - 1)) : 1f;
                if (careerProgressFill.type == Image.Type.Filled)
                {
                    careerProgressFill.fillAmount = amount;
                }
                else
                {
                    // Sliced fill (rounded ends): the width follows the right anchor, hidden at 0.
                    RectTransform rt = careerProgressFill.rectTransform;
                    rt.anchorMax = new Vector2(amount, rt.anchorMax.y);
                    careerProgressFill.enabled = amount > 0f;
                }
            }

            if (careerStepsLabel != null)
            {
                var steps = new List<string>();
                for (int l = 1; l <= max; l++)
                {
                    string title = _study.CareerTitle(l);
                    steps.Add(l == level ? $"<b>{title}</b>" : title);
                }

                careerStepsLabel.text = string.Join(" → ", steps);
            }

            StudyViewUtils.SetText(shiftsLabel, $"Смен на работе: {_study.Shifts}");
        }

        private void OpenTask(StudyTaskDefinition task)
        {
            if (!_study.IsTaskAvailable(task))
            {
                return;
            }

            if (task.IsChoose)
            {
                ShowPage(choosePage.gameObject);
                choosePage.Open(task, FindSprite, ShowMain, index => ShowResult(task, _study.SubmitChoice(task, index)));
            }
            else
            {
                ShowPage(distributePage.gameObject);
                distributePage.Open(task, FindSprite, ShowMain, values => ShowResult(task, _study.SubmitDistribution(task, values)));
            }
        }

        private void ShowResult(StudyTaskDefinition task, StudyResult result)
        {
            ShowPage(resultPage.gameObject);
            resultPage.Show(result, ShowMain, () => OpenTask(task));
        }
    }
}
