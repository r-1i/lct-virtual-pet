using System.Collections.Generic;
using Study;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools → Study → Build Missing UI. Finds Window_Study in the open scene, wires the hand-made main page into
/// StudyScreen and builds the missing task pages (Разложи / Реши / разбор) as rough placeholders in the style
/// of MainPage (same area, backgrounds, font, buttons), with every inspector field assigned.
/// Existing pages and already assigned fields are kept. One Ctrl+Z undoes the whole run.
/// Layout is anchors-only (anchors = rect, offsets 0), like the rest of the UI — move things freely afterwards.
/// </summary>
public static class StudyUIBuilder
{
    private const string WindowName = "Window_Study";

    private static readonly Color RequiredGreen = new Color(0.35f, 0.75f, 0.35f, 0.35f);
    private static readonly Color EventOrange = new Color(1f, 0.8f, 0.55f);
    private static readonly Color PlaceholderGray = new Color(0.82f, 0.82f, 0.82f);

    // Style references taken from the hand-made main page (any of them may be missing — then plain defaults).
    private static Image _pageBg;
    private static Image _panelBg;
    private static Image _rowBg;
    private static GameObject _header;
    private static GameObject _buttonTemplate;
    private static TMP_Text _textRef;
    private static Sprite _coinSprite;
    private static Sprite _passedSprite;
    private static Sprite _failedSprite;
    private static Sprite _circleSprite;
    private static Sprite _squareSprite;

    private static readonly List<string> Report = new List<string>();

    [MenuItem("Tools/Study/Build Missing UI")]
    private static void Build()
    {
        Transform window = FindInScene(WindowName);
        if (window == null)
        {
            Debug.LogError($"[StudyUIBuilder] '{WindowName}' not found in the open scene.");
            return;
        }

        Transform main = window.Find("MainPage");
        if (main == null)
        {
            Debug.LogError("[StudyUIBuilder] 'Window_Study/MainPage' not found — build the main page first.");
            return;
        }

        Report.Clear();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Study UI");

        CollectStyle(window, main);
        RemoveStrayPlanningScreen(window);
        FixMainPage(main);

        DistributeTaskPage distribute = FindPage<DistributeTaskPage>(window, "DistributePage");
        if (distribute == null && window.Find("DistributePage") == null)
        {
            distribute = BuildDistributePage(window, main);
        }

        ChooseTaskPage choose = FindPage<ChooseTaskPage>(window, "ChoosePage");
        if (choose == null && window.Find("ChoosePage") == null)
        {
            choose = BuildChoosePage(window, main);
        }

        StudyResultPage result = FindPage<StudyResultPage>(window, "ResultPage");
        if (result == null && window.Find("ResultPage") == null)
        {
            result = BuildResultPage(window, main);
        }

        WireStudyScreen(window, main, distribute, choose, result);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(window.gameObject.scene);
        Selection.activeGameObject = window.gameObject;

        Report.Add("Готово. Сохрани сцену (Ctrl+S). Откатить всё — Ctrl+Z.");
        Debug.Log("[StudyUIBuilder]\n• " + string.Join("\n• ", Report));
    }

    // ───────────────────────── existing objects ─────────────────────────

    private static Transform FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }
        }

        return null;
    }

    private static T FindPage<T>(Transform window, string name) where T : Component
    {
        Transform page = window.Find(name);
        if (page == null)
        {
            return null;
        }

        T script = page.GetComponent<T>();
        if (script == null)
        {
            Report.Add($"'{name}' уже есть, но без скрипта {typeof(T).Name} — добавь его сам или удали объект и запусти сборку ещё раз.");
            return null;
        }

        Report.Add($"'{name}' уже есть — не трогаю.");
        return script;
    }

    private static Image FindImage(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.GetComponent<Image>() : null;
    }

    private static void CollectStyle(Transform window, Transform main)
    {
        _pageBg = main.GetComponent<Image>();
        _panelBg = FindImage(main, "CareerCard");
        _rowBg = FindImage(main, "ThemeCard/Tasks/TaskRow_1");

        Transform header = main.Find("Header_1");
        _header = header != null ? header.gameObject : null;

        Transform startButton = main.Find("ThemeCard/Tasks/TaskRow_1/StartButton");
        _buttonTemplate = startButton != null ? startButton.gameObject : null;

        Transform title = main.Find("ThemeCard/Title");
        _textRef = title != null ? title.GetComponent<TMP_Text>() : null;

        Image coin = FindImage(window, "TopPanel/TopBar/Coins/Icon");
        _coinSprite = coin != null ? coin.sprite : null;
        Image passed = FindImage(main, "ThemeCard/PassedMark");
        _passedSprite = passed != null ? passed.sprite : null;
        Image failed = FindImage(main, "ThemeCard/Locked/LockIcon");
        _failedSprite = failed != null ? failed.sprite : null;

        _circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        _squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    /// <summary>The window was copied from Планирование — its PlanningScreen has almost no fields and would throw.</summary>
    private static void RemoveStrayPlanningScreen(Transform window)
    {
        var planning = window.GetComponent<Finance.PlanningScreen>();
        if (planning != null)
        {
            Undo.DestroyObjectImmediate(planning);
            Report.Add("С Window_Study убран лишний компонент PlanningScreen (остался от копии окна «Планирование»).");
        }
    }

    private static void FixMainPage(Transform main)
    {
        Transform cardT = main.Find("ThemeCard");
        StudyThemeCardView card = cardT != null ? cardT.GetComponent<StudyThemeCardView>() : null;
        Transform lockText = main.Find("ThemeCard/Locked/LockText");
        if (card != null && lockText != null)
        {
            var so = new SerializedObject(card);
            SerializedProperty prop = so.FindProperty("lockedLabel");
            var label = lockText.GetComponent<TMP_Text>();
            if (label != null && prop.objectReferenceValue != label)
            {
                prop.objectReferenceValue = label;
                so.ApplyModifiedProperties();
                Report.Add("ThemeCard → Locked Label исправлен на ThemeCard/Locked/LockText (был TaskRow_2/Locked).");
            }
        }

        Image fill = FindImage(main, "CareerCard/CareerBar/Fill");
        if (fill != null && fill.type != Image.Type.Filled)
        {
            Undo.RecordObject(fill, "Build Study UI");
            Undo.RecordObject(fill.rectTransform, "Build Study UI");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            SetRect(fill.rectTransform, 0f, 0f, 1f, 1f);
            Report.Add("CareerBar/Fill: Image Type = Filled (Horizontal, Left), растянут на всю полосу.");
        }
    }

    private static void WireStudyScreen(Transform window, Transform main, DistributeTaskPage distribute,
        ChooseTaskPage choose, StudyResultPage result)
    {
        StudyScreen screen = window.GetComponent<StudyScreen>();
        if (screen == null)
        {
            screen = Undo.AddComponent<StudyScreen>(window.gameObject);
            Report.Add("На Window_Study добавлен StudyScreen.");
        }

        var so = new SerializedObject(screen);
        SetIfEmpty(so, "mainPage", main.gameObject);
        SetIfEmpty(so, "distributePage", distribute);
        SetIfEmpty(so, "choosePage", choose);
        SetIfEmpty(so, "resultPage", result);
        SetIfEmpty(so, "careerTitleLabel", FindText(main, "CareerCard/CareerBar/CareerTitle"));
        SetIfEmpty(so, "careerNextLabel", FindText(main, "CareerCard/CareerNext"));
        SetIfEmpty(so, "careerProgressFill", FindImage(main, "CareerCard/CareerBar/Fill"));
        SetIfEmpty(so, "shiftsLabel", FindText(main, "CareerCard/Shifts"));
        Transform card = main.Find("ThemeCard");
        SetIfEmpty(so, "themeCard", card != null ? card.GetComponent<StudyThemeCardView>() : null);
        Transform allDone = window.Find("AllDone");
        SetIfEmpty(so, "allDoneRoot", allDone != null ? allDone.gameObject : null);
        so.ApplyModifiedProperties();
    }

    private static TMP_Text FindText(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static void SetIfEmpty(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (value == null || prop.objectReferenceValue != null)
        {
            return;
        }

        prop.objectReferenceValue = value;
        Report.Add($"{so.targetObject.GetType().Name} → {field} = {value.name}");
    }

    // ───────────────────────── pages ─────────────────────────

    private static DistributeTaskPage BuildDistributePage(Transform window, Transform main)
    {
        GameObject page = NewPage("DistributePage", window, main);
        (Button back, TMP_Text header) = NewHeader(page.transform, "Задание: зарплата");
        (Image speaker, TMP_Text speakerName, TMP_Text speech) = NewSpeaker(page.transform, "Барсук");

        Image condition = NewImage("Condition", page.transform, 0.03f, 0.555f, 0.97f, 0.645f, _panelBg);
        TMP_Text conditionText = NewText("Text", condition.transform, 0.03f, 0.08f, 0.97f, 0.92f,
            "Зарплата пришла: 40 монет…", TextAlignmentOptions.Left, 14f);
        Image evt = NewImage("Event", page.transform, 0.03f, 0.555f, 0.97f, 0.645f, _panelBg);
        evt.color = EventOrange;
        TMP_Text eventText = NewText("Text", evt.transform, 0.03f, 0.08f, 0.97f, 0.92f,
            "Зонт сломался: −10…", TextAlignmentOptions.Left, 14f);
        evt.gameObject.SetActive(false);

        var rows = new[]
        {
            NewDistributeRow("Row_1", page.transform, 0.450f, 0.545f),
            NewDistributeRow("Row_2", page.transform, 0.345f, 0.440f),
            NewDistributeRow("Row_3", page.transform, 0.240f, 0.335f),
        };

        Image remaining = NewImage("Remaining", page.transform, 0.03f, 0.13f, 0.60f, 0.225f, _panelBg);
        NewText("Label", remaining.transform, 0.04f, 0.1f, 0.62f, 0.9f, "Осталось разложить", TextAlignmentOptions.Left);
        Image coin = NewImage("CoinIcon", remaining.transform, 0.64f, 0.15f, 0.76f, 0.85f, null, _coinSprite);
        coin.preserveAspect = true;
        coin.raycastTarget = false;
        TMP_Text remainingValue = NewText("Value", remaining.transform, 0.78f, 0.1f, 0.97f, 0.9f, "20", TextAlignmentOptions.Center);

        Button done = NewButton("DoneButton", page.transform, 0.63f, 0.13f, 0.97f, 0.225f, "Готово");
        TMP_Text hint = NewText("Hint", page.transform, 0.03f, 0.02f, 0.97f, 0.115f,
            "Подсказка мелким шрифтом", TextAlignmentOptions.Center, 12f, 40f);

        var script = page.AddComponent<DistributeTaskPage>();
        var so = new SerializedObject(script);
        Set(so, "backButton", back);
        Set(so, "headerLabel", header);
        Set(so, "speakerImage", speaker);
        Set(so, "speakerNameLabel", speakerName);
        Set(so, "speechLabel", speech);
        Set(so, "conditionLabel", conditionText);
        Set(so, "conditionRoot", condition.gameObject);
        Set(so, "eventRoot", evt.gameObject);
        Set(so, "eventLabel", eventText);
        SetArray(so, "rowSlots", rows);
        Set(so, "remainingRoot", remaining.gameObject);
        Set(so, "remainingLabel", remainingValue);
        Set(so, "hintLabel", hint);
        Set(so, "doneButton", done);
        so.ApplyModifiedPropertiesWithoutUndo();

        page.SetActive(false);
        Report.Add("Собрана DistributePage (Разложи), все поля назначены.");
        return script;
    }

    private static DistributeRowView NewDistributeRow(string name, Transform parent, float y0, float y1)
    {
        Image row = NewImage(name, parent, 0.03f, y0, 0.97f, y1, _rowBg);

        Image frame = NewImage("RequiredFrame", row.transform, 0f, 0f, 1f, 1f, _rowBg);
        frame.color = RequiredGreen;
        frame.raycastTarget = false;

        Image icon = NewImage("Icon", row.transform, 0.02f, 0.1f, 0.13f, 0.9f, null, _squareSprite);
        icon.color = PlaceholderGray;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TMP_Text title = NewText("Title", row.transform, 0.15f, 0.5f, 0.50f, 0.95f, "Еда · надо", TextAlignmentOptions.Left);
        TMP_Text subtitle = NewText("Subtitle", row.transform, 0.15f, 0.05f, 0.50f, 0.5f, "нужно 20", TextAlignmentOptions.Left);

        Button minus = NewButton("Minus", row.transform, 0.52f, 0.15f, 0.61f, 0.85f, "−");
        HoldRepeatButton minusHold = minus.gameObject.AddComponent<HoldRepeatButton>();
        TMP_Text value = NewText("Value", row.transform, 0.62f, 0.1f, 0.71f, 0.9f, "20", TextAlignmentOptions.Center);
        Button plus = NewButton("Plus", row.transform, 0.72f, 0.15f, 0.81f, 0.85f, "+");
        HoldRepeatButton plusHold = plus.gameObject.AddComponent<HoldRepeatButton>();
        Button clear = NewButton("ClearButton", row.transform, 0.83f, 0.2f, 0.98f, 0.8f, "убрать");

        var view = row.gameObject.AddComponent<DistributeRowView>();
        var so = new SerializedObject(view);
        Set(so, "iconImage", icon);
        Set(so, "titleLabel", title);
        Set(so, "subtitleLabel", subtitle);
        Set(so, "minusButton", minusHold);
        Set(so, "plusButton", plusHold);
        Set(so, "valueLabel", value);
        Set(so, "clearButton", clear);
        Set(so, "requiredFrame", frame.gameObject);
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static ChooseTaskPage BuildChoosePage(Transform window, Transform main)
    {
        GameObject page = NewPage("ChoosePage", window, main);
        (Button back, TMP_Text header) = NewHeader(page.transform, "Задание: рынок");
        (Image speaker, TMP_Text speakerName, TMP_Text speech) = NewSpeaker(page.transform, "Лис");

        Image condition = NewImage("Condition", page.transform, 0.03f, 0.56f, 0.97f, 0.645f, _panelBg);
        TMP_Text conditionText = NewText("Text", condition.transform, 0.03f, 0.08f, 0.97f, 0.92f,
            "У тебя 10 монет…", TextAlignmentOptions.Left, 14f);

        Button helper = NewButton("HelperButton", page.transform, 0.03f, 0.47f, 0.45f, 0.545f, "Посчитать за штуку");
        TMP_Text helperLabel = helper.GetComponentInChildren<TMP_Text>(true);
        TMP_Text helperText = NewText("HelperText", page.transform, 0.47f, 0.47f, 0.97f, 0.545f,
            "Ответ помощника", TextAlignmentOptions.Left, 14f);
        helperText.gameObject.SetActive(false);

        TMP_Text question = NewText("Question", page.transform, 0.03f, 0.405f, 0.97f, 0.465f,
            "Что купишь?", TextAlignmentOptions.Center);

        var options = new[]
        {
            NewChooseOption("Option_1", page.transform, 0.03f, 0.33f),
            NewChooseOption("Option_2", page.transform, 0.35f, 0.65f),
            NewChooseOption("Option_3", page.transform, 0.67f, 0.97f),
        };

        Image footer = NewImage("FooterHint", page.transform, 0.03f, 0.02f, 0.97f, 0.105f, _panelBg);
        TMP_Text footerText = NewText("Text", footer.transform, 0.03f, 0.08f, 0.97f, 0.92f,
            "Подумай: …", TextAlignmentOptions.Center, 12f);

        var script = page.AddComponent<ChooseTaskPage>();
        var so = new SerializedObject(script);
        Set(so, "backButton", back);
        Set(so, "headerLabel", header);
        Set(so, "speakerImage", speaker);
        Set(so, "speakerNameLabel", speakerName);
        Set(so, "speechLabel", speech);
        Set(so, "conditionLabel", conditionText);
        Set(so, "conditionRoot", condition.gameObject);
        Set(so, "helperButton", helper);
        Set(so, "helperButtonLabel", helperLabel);
        Set(so, "helperTextLabel", helperText);
        Set(so, "questionLabel", question);
        SetArray(so, "optionSlots", options);
        Set(so, "footerHintLabel", footerText);
        Set(so, "footerHintRoot", footer.gameObject);
        so.ApplyModifiedPropertiesWithoutUndo();

        page.SetActive(false);
        Report.Add("Собрана ChoosePage (Реши), все поля назначены.");
        return script;
    }

    private static ChooseOptionView NewChooseOption(string name, Transform parent, float x0, float x1)
    {
        Image card = NewImage(name, parent, x0, 0.115f, x1, 0.395f, _rowBg);

        Image icon = NewImage("Icon", card.transform, 0.2f, 0.58f, 0.8f, 0.95f, null, _squareSprite);
        icon.color = PlaceholderGray;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        TMP_Text title = NewText("Title", card.transform, 0.05f, 0.42f, 0.95f, 0.58f, "Взять у Лиса", TextAlignmentOptions.Center);
        TMP_Text subtitle = NewText("Subtitle", card.transform, 0.05f, 0.25f, 0.95f, 0.42f,
            "10 печений — 10 монет", TextAlignmentOptions.Center, 12f);
        Button select = NewButton("SelectButton", card.transform, 0.1f, 0.04f, 0.9f, 0.22f, "Выбрать");

        var view = card.gameObject.AddComponent<ChooseOptionView>();
        var so = new SerializedObject(view);
        Set(so, "iconImage", icon);
        Set(so, "titleLabel", title);
        Set(so, "subtitleLabel", subtitle);
        Set(so, "selectButton", select);
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static StudyResultPage BuildResultPage(Transform window, Transform main)
    {
        GameObject page = NewPage("ResultPage", window, main);

        Image passedDecor = NewImage("PassedDecor", page.transform, 0.03f, 0.80f, 0.17f, 0.97f, null, _passedSprite);
        passedDecor.preserveAspect = true;
        passedDecor.raycastTarget = false;
        passedDecor.gameObject.SetActive(false);
        Image failedDecor = NewImage("FailedDecor", page.transform, 0.03f, 0.80f, 0.17f, 0.97f, null, _failedSprite);
        failedDecor.preserveAspect = true;
        failedDecor.raycastTarget = false;
        failedDecor.gameObject.SetActive(false);

        TMP_Text verdict = NewText("Verdict", page.transform, 0.20f, 0.82f, 0.97f, 0.96f, "Верно!", TextAlignmentOptions.Left);
        verdict.fontStyle = FontStyles.Bold;
        TMP_Text outcome = NewText("Outcome", page.transform, 0.03f, 0.63f, 0.97f, 0.80f,
            "Съедено 4, выкинуто 6…", TextAlignmentOptions.TopLeft, 14f, 60f);
        TMP_Text explanation = NewText("Explanation", page.transform, 0.03f, 0.44f, 0.97f, 0.62f,
            "За 2 дня Нёрп съест…", TextAlignmentOptions.TopLeft, 14f, 60f);
        TMP_Text lesson = NewText("Lesson", page.transform, 0.03f, 0.32f, 0.97f, 0.43f,
            "Вывод: …", TextAlignmentOptions.Left, 14f, 60f);
        lesson.fontStyle = FontStyles.Bold;

        Image promotion = NewImage("Promotion", page.transform, 0.03f, 0.18f, 0.97f, 0.30f, _panelBg);
        TMP_Text promotionText = NewText("Text", promotion.transform, 0.03f, 0.08f, 0.97f, 0.92f,
            "Повышение! …", TextAlignmentOptions.Center, 14f);

        Button retry = NewButton("RetryButton", page.transform, 0.03f, 0.03f, 0.48f, 0.15f, "Ещё раз");
        Button next = NewButton("NextButton", page.transform, 0.52f, 0.03f, 0.97f, 0.15f, "Дальше");

        var script = page.AddComponent<StudyResultPage>();
        var so = new SerializedObject(script);
        Set(so, "verdictLabel", verdict);
        Set(so, "passedDecor", passedDecor.gameObject);
        Set(so, "failedDecor", failedDecor.gameObject);
        Set(so, "outcomeLabel", outcome);
        Set(so, "explanationLabel", explanation);
        Set(so, "lessonLabel", lesson);
        Set(so, "promotionRoot", promotion.gameObject);
        Set(so, "promotionLabel", promotionText);
        Set(so, "nextButton", next);
        Set(so, "retryButton", retry);
        so.ApplyModifiedPropertiesWithoutUndo();

        page.SetActive(false);
        Report.Add("Собрана ResultPage (разбор), все поля назначены.");
        return script;
    }

    /// <summary>Speaker circle with the name under it + speech bubble to the right (Разложи and Реши share it).</summary>
    private static (Image speaker, TMP_Text name, TMP_Text speech) NewSpeaker(Transform page, string speakerName)
    {
        Image speaker = NewImage("SpeakerImage", page, 0.03f, 0.70f, 0.19f, 0.87f, null, _circleSprite);
        speaker.color = PlaceholderGray;
        speaker.preserveAspect = true;
        speaker.raycastTarget = false;
        TMP_Text nameLabel = NewText("SpeakerName", speaker.transform, -0.1f, -0.33f, 1.1f, 0f,
            speakerName, TextAlignmentOptions.Center, 12f);

        Image bubble = NewImage("SpeechBubble", page, 0.22f, 0.66f, 0.97f, 0.88f, _panelBg);
        TMP_Text speech = NewText("Speech", bubble.transform, 0.04f, 0.08f, 0.96f, 0.92f,
            "Реплика персонажа", TextAlignmentOptions.Left, 14f, 60f);
        return (speaker, nameLabel, speech);
    }

    // ───────────────────────── building blocks ─────────────────────────

    /// <summary>Full page in the same area as MainPage with the same background.</summary>
    private static GameObject NewPage(string name, Transform window, Transform main)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Build Study UI");
        go.layer = main.gameObject.layer;
        go.transform.SetParent(window, false);

        var rt = (RectTransform)go.transform;
        var mainRt = (RectTransform)main;
        rt.anchorMin = mainRt.anchorMin;
        rt.anchorMax = mainRt.anchorMax;
        rt.pivot = mainRt.pivot;
        rt.offsetMin = mainRt.offsetMin;
        rt.offsetMax = mainRt.offsetMax;

        Image bg = go.AddComponent<Image>();
        CopyImageStyle(_pageBg, bg);
        return go;
    }

    /// <summary>Copy of MainPage/Header_1 (back button + title) without the main page's OnClick.</summary>
    private static (Button back, TMP_Text title) NewHeader(Transform page, string title)
    {
        if (_header != null)
        {
            GameObject header = Object.Instantiate(_header, page);
            header.name = _header.name;
            Transform backT = header.transform.Find("BackButton");
            Button back = backT != null ? backT.GetComponent<Button>() : null;
            if (back != null)
            {
                back.onClick = new Button.ButtonClickedEvent();
            }

            Transform titleT = header.transform.Find("Title");
            TMP_Text titleLabel = titleT != null ? titleT.GetComponent<TMP_Text>() : null;
            if (titleLabel != null)
            {
                titleLabel.text = title;
            }

            if (back != null && titleLabel != null)
            {
                return (back, titleLabel);
            }

            Object.DestroyImmediate(header);
        }

        GameObject fallback = NewRect("Header_1", page, 0.013f, 0.896f, 1f, 0.991f);
        Button fallbackBack = NewButton("BackButton", fallback.transform, 0.02f, 0.12f, 0.10f, 0.88f, "‹");
        TMP_Text fallbackTitle = NewText("Title", fallback.transform, 0.234f, 0.06f, 0.766f, 0.94f, title, TextAlignmentOptions.Center);
        return (fallbackBack, fallbackTitle);
    }

    private static GameObject NewRect(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, x0, y0, x1, y1);
        return go;
    }

    private static Image NewImage(string name, Transform parent, float x0, float y0, float x1, float y1,
        Image style, Sprite sprite = null)
    {
        GameObject go = NewRect(name, parent, x0, y0, x1, y1);
        Image img = go.AddComponent<Image>();
        if (style != null)
        {
            CopyImageStyle(style, img);
        }
        else
        {
            img.sprite = sprite;
            if (sprite == null)
            {
                img.color = PlaceholderGray;
            }
        }

        return img;
    }

    private static void CopyImageStyle(Image src, Image dst)
    {
        if (src == null)
        {
            return;
        }

        dst.sprite = src.sprite;
        dst.type = src.type;
        dst.color = src.color;
        dst.pixelsPerUnitMultiplier = src.pixelsPerUnitMultiplier;
        dst.preserveAspect = src.preserveAspect;
    }

    /// <summary>Auto Size text (the rect decides the size), same font/colour as ThemeCard/Title.</summary>
    private static TMP_Text NewText(string name, Transform parent, float x0, float y0, float x1, float y1,
        string text, TextAlignmentOptions alignment, float minSize = 18f, float maxSize = 200f)
    {
        GameObject go = NewRect(name, parent, x0, y0, x1, y1);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (_textRef != null)
        {
            tmp.font = _textRef.font;
            tmp.fontSharedMaterial = _textRef.fontSharedMaterial;
            tmp.color = _textRef.color;
        }

        tmp.text = text;
        tmp.alignment = alignment;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = minSize;
        tmp.fontSizeMax = maxSize;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>Copy of TaskRow_1/StartButton (same look) with empty OnClick and a new label.</summary>
    private static Button NewButton(string name, Transform parent, float x0, float y0, float x1, float y1, string label)
    {
        Button button;
        if (_buttonTemplate != null)
        {
            GameObject go = Object.Instantiate(_buttonTemplate, parent);
            go.name = name;
            go.SetActive(true);
            SetRect((RectTransform)go.transform, x0, y0, x1, y1);
            button = go.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
        }
        else
        {
            Image img = NewImage(name, parent, x0, y0, x1, y1, null, _squareSprite);
            img.color = Color.white;
            button = img.gameObject.AddComponent<Button>();
            NewText("Text (TMP)", img.transform, 0f, 0.25f, 1f, 0.75f, label, TextAlignmentOptions.Center);
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }

        return button;
    }

    private static void SetRect(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"[StudyUIBuilder] {so.targetObject.GetType().Name} has no field '{field}'.");
            return;
        }

        prop.objectReferenceValue = value;
    }

    private static void SetArray(SerializedObject so, string field, Object[] values)
    {
        SerializedProperty prop = so.FindProperty(field);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
