using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Настройки → Debug Menu: every StatsDebug command as a button with a Russian label, grouped into sections, on one
    /// scrolling page. The buttons are built in code from the list in BuildCommands (a new command = one line there + a
    /// public method in StatsDebug). Layout: Content (VerticalLayoutGroup + ContentSizeFitter) → per section a header and a
    /// GridLayoutGroup with 2 columns; sizes are recalculated from the content width, so it looks the same on any screen.
    /// After a command a short "Debug: …" message is shown (Feedback).
    /// </summary>
    public class DebugMenu : MonoBehaviour
    {
        [SerializeField] private StatsDebug statsDebug;
        [Tooltip("Closed after every command (like the X button).")]
        [SerializeField] private GameObject settingsWindow;
        [Tooltip("Shown after a command, like the X button does — except for commands that open their own windows (character creation).")]
        [SerializeField] private GameObject homeWindow;
        [Tooltip("Content of the ScrollRect: needs VerticalLayoutGroup + ContentSizeFitter (vertical: Preferred).")]
        [SerializeField] private RectTransform content;

        [Header("Look")]
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private float buttonSpritePixelsPerUnit = 1f;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Color buttonColor = new Color(0.33f, 0.62f, 0.33f);
        [SerializeField] private Color buttonTextColor = Color.white;
        [SerializeField] private Color headerColor = new Color(0.28f, 0.28f, 0.28f);
        [Tooltip("Button height = column width × this.")]
        [SerializeField] private float buttonAspect = 0.3f;
        [Tooltip("Section header height = content width × this.")]
        [SerializeField] private float headerAspect = 0.075f;
        [Tooltip("Gaps = content width × this.")]
        [SerializeField] private float spacingRatio = 0.025f;
        [Tooltip("Max label font size = button height × this (same for every button; long labels shrink below it).")]
        [SerializeField] private float buttonFontShare = 0.3f;

        private readonly List<GridLayoutGroup> _grids = new List<GridLayoutGroup>();
        private readonly List<LayoutElement> _headers = new List<LayoutElement>();
        private readonly List<TMP_Text> _headerTexts = new List<TMP_Text>();
        private readonly List<TMP_Text> _buttonTexts = new List<TMP_Text>();
        private float _builtForWidth = -1f;
        private bool _built;

        private struct Command
        {
            public string Section;
            public string Label;
            public Action Run;

            /// <summary>False for commands that show their own windows (character creation hides Window_Home itself).</summary>
            public bool ShowHome;
        }

        private List<Command> BuildCommands()
        {
            StatsDebug d = statsDebug;
            var list = new List<Command>();
            void Add(string section, string label, Action run, bool showHome = true) =>
                list.Add(new Command { Section = section, Label = label, Run = run, ShowHome = showHome });

            Add("Статы", "Все статы −20", d.DecreaseAll);
            Add("Статы", "Все статы +20", d.IncreaseAll);
            Add("Статы", "Случайные статы", d.Randomize);
            Add("Статы", "Все статы на 100", d.ResetAll);

            Add("Деньги", "+100 монет", d.AddCoins);
            Add("Деньги", "−50 монет", d.SpendCoins);
            Add("Деньги", "+10 в копилку", d.AddJarCoins);
            Add("Деньги", "+500 в копилку", d.AddJarCoinsBig);

            Add("Еда и уход", "+5 среднего и большого корма", d.AddFeed);
            Add("Еда и уход", "+5 мыла, мочалок и щёток", d.AddCareItems);

            Add("Работа", "Закончить текущую смену", d.ShiftJobStartDay);
            Add("Работа", "Сбросить лимит смен", d.ResetShiftsToday);

            Add("Время", "Следующий день", d.SkipDay);
            Add("Время", "Вернуть настоящую дату", d.ResetDayOffset);

            Add("Финансы", "План: 40 монет в день", d.SetPlan40);
            Add("Финансы", "План: выключить", d.SetPlanOff);
            Add("Финансы", "История: примеры записей", d.AddSampleHistory);

            Add("Учёба", "+1 смена для учёбы", d.AddShift);
            Add("Учёба", "+3 смены для учёбы", d.AddThreeShifts);
            Add("Учёба", "Сдать следующее задание", d.PassNextStudyTask);
            Add("Учёба", "Сбросить учёбу и уровень", d.ResetStudy);

            Add("Почта", "Письмо за вчера", d.MailSnapshotYesterday);
            Add("Почта", "Заполнить 7 писем", d.MailFillWeek);
            Add("Почта", "Очистить почту", d.MailClear);

            Add("Персонаж", "Создать персонажа заново", d.ResetCharacterCreation, showHome: false);
            Add("Персонаж", "Следующий вид (просмотр)", d.NextLook);

            Add("Анимации", "Привет", d.AnimGreet);
            Add("Анимации", "Радость", d.AnimJoy);
            Add("Анимации", "Игра", d.AnimPlay);
            Add("Анимации", "Еда", d.AnimEat);
            Add("Анимации", "Танец вкл/выкл", d.AnimDance);
            Add("Анимации", "Разговор вкл/выкл", d.AnimTalk);
            Add("Анимации", "Работа вкл/выкл", d.AnimWork);
            Add("Анимации", "Боль в животе вкл/выкл", d.AnimStomachAche);
            return list;
        }

        private void OnEnable()
        {
            if (!_built)
            {
                Build();
            }

            Resize();
        }

        private void Update()
        {
            // Width can change after the first layout pass (SafeArea, orientation) — keep the sizes proportional.
            if (!Mathf.Approximately(content.rect.width, _builtForWidth))
            {
                Resize();
            }
        }

        private void Build()
        {
            _built = true;
            if (statsDebug == null)
            {
                statsDebug = FindFirstObjectByType<StatsDebug>(FindObjectsInactive.Include);
            }

            if (statsDebug == null || content == null)
            {
                Debug.LogWarning("DebugMenu: StatsDebug or Content is not set.");
                return;
            }

            string section = null;
            RectTransform grid = null;
            foreach (Command command in BuildCommands())
            {
                if (command.Section != section)
                {
                    section = command.Section;
                    CreateHeader(section);
                    grid = CreateGrid(section);
                }

                CreateButton(grid, command);
            }
        }

        private void CreateHeader(string title)
        {
            var go = new GameObject($"Header_{title}", typeof(RectTransform));
            go.transform.SetParent(content, false);
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = title;
            if (font != null) text.font = font;
            text.color = headerColor;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8;
            text.fontSizeMax = 200;
            text.alignment = TextAlignmentOptions.BottomLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            _headerTexts.Add(text);
            _headers.Add(go.AddComponent<LayoutElement>());
        }

        private RectTransform CreateGrid(string title)
        {
            var go = new GameObject($"Grid_{title}", typeof(RectTransform));
            go.transform.SetParent(content, false);
            GridLayoutGroup grid = go.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;
            _grids.Add(grid);
            return (RectTransform)go.transform;
        }

        private void CreateButton(RectTransform parent, Command command)
        {
            var go = new GameObject($"Btn_{command.Label}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = buttonSpritePixelsPerUnit;
            image.color = buttonColor;
            Button button = go.AddComponent<Button>();
            button.onClick.AddListener(() => Run(command));

            var textGo = new GameObject("Text (TMP)", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var rt = (RectTransform)textGo.transform;
            rt.anchorMin = new Vector2(0.06f, 0.12f);
            rt.anchorMax = new Vector2(0.94f, 0.88f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = command.Label;
            if (font != null) text.font = font;
            text.color = buttonTextColor;
            text.enableAutoSizing = true;
            text.fontSizeMin = 8;
            text.fontSizeMax = 200;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            _buttonTexts.Add(text);
        }

        private void Resize()
        {
            if (content == null)
            {
                return;
            }

            float width = content.rect.width;
            if (width <= 0f)
            {
                return;
            }

            _builtForWidth = width;
            float gap = width * spacingRatio;
            float cellWidth = (width - gap) / 2f;

            foreach (GridLayoutGroup grid in _grids)
            {
                grid.cellSize = new Vector2(cellWidth, cellWidth * buttonAspect);
                grid.spacing = new Vector2(gap, gap);
            }

            foreach (LayoutElement header in _headers)
            {
                header.preferredHeight = width * headerAspect;
            }

            // One size cap for every label, so short and long ones look alike; only labels that don't fit shrink.
            float buttonFont = cellWidth * buttonAspect * buttonFontShare;
            foreach (TMP_Text text in _buttonTexts)
            {
                text.fontSizeMax = buttonFont;
            }

            foreach (TMP_Text text in _headerTexts)
            {
                text.fontSizeMax = width * headerAspect * 0.8f;
            }

            if (content.TryGetComponent(out VerticalLayoutGroup vertical))
            {
                vertical.spacing = gap;
            }

            LayoutRebuilder.MarkLayoutForRebuild(content);
        }

        /// <summary>Settings close themselves first (so a command that opens its own windows, like character creation, isn't covered), then the command runs.</summary>
        private void Run(Command command)
        {
            if (settingsWindow != null)
            {
                settingsWindow.SetActive(false);
            }

            if (command.ShowHome && homeWindow != null)
            {
                homeWindow.SetActive(true);
            }

            try
            {
                command.Run();
                Feedback.Show($"Debug: {command.Label}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Feedback.Show($"Debug: не получилось — {command.Label}");
            }
        }
    }
}
