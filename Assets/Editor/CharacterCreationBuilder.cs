using System.Collections.Generic;
using Creation;
using Feeding;
using Home;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools → Character → Build Character Creation. Two parts, both safe to run again (only missing things are added):
/// 1. Models: the three character FBX go under the character root (the object with CharacterMotor), each with its own
///    Animator using the root's controller, colour textures a/b/c_1..3, the bow on the neck bone. The old placeholder
///    model is switched off, and CharacterMotor / CharacterFeeding / MicrophoneMimic / Boombox get CharacterAppearance.
/// 2. UI: Window_2 (colour) and Window_3 (accessory) are made as copies of Window_1, option cards get
///    CreationOption + a "Selected" tint, CharacterCreationFlow is created and plugged into IntroCutscene.
/// One Ctrl+Z undoes the whole run.
/// </summary>
public static class CharacterCreationBuilder
{
    private const string ModelsFolder = "Assets/Models/Chars/";
    private const string BowPath = ModelsFolder + "babochka.fbx";

    private static readonly (string title, string objectName, string fbx, string texturePrefix)[] Models =
    {
        ("Аксолотль", "Model_Aksolotl", "_aksolotl_Lp.fbx", "a"),
        ("Медведь", "Model_Medved", "_medved_lp.fbx", "b"),
        ("Скуф", "Model_Skuf", "_skufprivet.fbx", "c"),
    };

    private static readonly string[] AccessoryTitles = { "В горошек", "Синяя", "Розовая", "Без бабочки" };
    private static readonly Color SelectedTint = new Color(1f, 0.85f, 0.3f, 0.35f);

    private static readonly List<string> Report = new List<string>();

    [MenuItem("Tools/Character/Build Character Creation")]
    private static void Build()
    {
        Report.Clear();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Build Character Creation");

        CharacterAppearance appearance = SetupModels();
        bool uiBuilt = BuildUI(appearance);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Report.Add(uiBuilt && appearance != null
            ? "Готово. Сохрани сцену (Ctrl+S). Откатить всё — Ctrl+Z."
            : "Сделано не всё — см. ошибки выше. Откатить — Ctrl+Z.");
        Debug.Log("[CharacterCreationBuilder]\n• " + string.Join("\n• ", Report));
    }

    // ───────────────────────── 1. models ─────────────────────────

    private static CharacterAppearance SetupModels()
    {
        CharacterMotor motor = FindFirst<CharacterMotor>();
        if (motor == null)
        {
            Debug.LogError("[CharacterCreationBuilder] No CharacterMotor in the scene — can't find the character.");
            return null;
        }

        GameObject root = motor.gameObject;
        CharacterAppearance appearance = root.GetComponent<CharacterAppearance>();
        if (appearance == null)
        {
            appearance = Undo.AddComponent<CharacterAppearance>(root);
            Report.Add($"На персонажа '{root.name}' добавлен CharacterAppearance.");
        }

        var so = new SerializedObject(appearance);
        SerializedProperty models = so.FindProperty("models");
        if (models.arraySize == 0)
        {
            AddModels(root, models, so.FindProperty("hideObjects"));
        }
        else
        {
            Report.Add("CharacterAppearance → Models уже заполнены — модели не трогаю.");
        }

        so.ApplyModifiedProperties();

        WireAppearance<CharacterMotor>("appearance", appearance);
        WireAppearance<CharacterFeeding>("appearance", appearance);
        WireAppearance<MicrophoneMimic>("characterAppearance", appearance);
        WireAppearance<Boombox>("playerAppearance", appearance);
        return appearance;
    }

    private static void AddModels(GameObject root, SerializedProperty models, SerializedProperty hideObjects)
    {
        // Everything already under the root is the old placeholder model.
        var oldParts = new List<GameObject>();
        foreach (Transform child in root.transform)
        {
            oldParts.Add(child.gameObject);
        }

        Animator rootAnimator = root.GetComponent<Animator>();
        RuntimeAnimatorController controller = rootAnimator != null ? rootAnimator.runtimeAnimatorController : null;
        var bowAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BowPath);

        foreach ((string title, string objectName, string fbx, string prefix) in Models)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelsFolder + fbx);
            if (asset == null)
            {
                Debug.LogError($"[CharacterCreationBuilder] Model not found: {ModelsFolder + fbx}");
                continue;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
            Undo.RegisterCreatedObjectUndo(model, "Build Character Creation");
            model.name = objectName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            SetLayerRecursively(model, root.layer);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            GameObject bow = null;
            if (bowAsset != null)
            {
                Transform bone = FindBone(model.transform, "neck") ?? FindBone(model.transform, "head") ?? model.transform;
                bow = (GameObject)PrefabUtility.InstantiatePrefab(bowAsset, bone);
                bow.name = "Bow";
                bow.transform.localPosition = Vector3.zero;
                bow.transform.localRotation = Quaternion.identity;
                SetLayerRecursively(bow, root.layer);
                bow.SetActive(false);
                Report.Add($"{objectName}: бабочка прицеплена к кости '{bone.name}' — подвинь её по месту.");
            }

            int i = models.arraySize;
            models.arraySize++;
            SerializedProperty slot = models.GetArrayElementAtIndex(i);
            slot.FindPropertyRelative("title").stringValue = title;
            slot.FindPropertyRelative("root").objectReferenceValue = model;
            slot.FindPropertyRelative("animator").objectReferenceValue = animator;
            slot.FindPropertyRelative("bow").objectReferenceValue = bow;
            slot.FindPropertyRelative("bodyRenderers").arraySize = 0;
            SerializedProperty colors = slot.FindPropertyRelative("colorTextures");
            colors.arraySize = 3;
            for (int c = 0; c < 3; c++)
            {
                string texPath = $"{ModelsFolder}{prefix}_{c + 1}.png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (texture == null)
                {
                    Debug.LogWarning($"[CharacterCreationBuilder] Texture not found: {texPath}");
                }

                colors.GetArrayElementAtIndex(c).objectReferenceValue = texture;
            }

            if (i > 0)
            {
                model.SetActive(false);
            }

            Report.Add($"Добавлена модель {objectName} ({title}): Animator с '{(controller != null ? controller.name : "—")}', окрасы {prefix}_1..3.");
        }

        hideObjects.arraySize = oldParts.Count;
        for (int i = 0; i < oldParts.Count; i++)
        {
            hideObjects.GetArrayElementAtIndex(i).objectReferenceValue = oldParts[i];
            Undo.RecordObject(oldParts[i], "Build Character Creation");
            oldParts[i].SetActive(false);
        }

        if (oldParts.Count > 0)
        {
            Report.Add($"Старая модель ({oldParts.Count} дочерних объектов) выключена и занесена в Hide Objects.");
        }

        if (rootAnimator != null && rootAnimator.enabled)
        {
            Undo.RecordObject(rootAnimator, "Build Character Creation");
            rootAnimator.enabled = false;
            Report.Add("Animator на корне персонажа выключен — теперь у каждой модели свой.");
        }
    }

    /// <summary>Only bones the skinned meshes are bound to — rig helpers (e.g. "QuickRigCharacter_Ctrl_Neck") aren't moved by the clips. See also Tools → Character → Reattach Bows.</summary>
    private static Transform FindBone(Transform model, string part)
    {
        var deform = new HashSet<Transform>();
        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            foreach (Transform bone in smr.bones)
            {
                if (bone != null)
                {
                    deform.Add(bone);
                }
            }
        }

        foreach (Transform t in model.GetComponentsInChildren<Transform>(true))
        {
            if (t != model && deform.Contains(t) && t.name.ToLowerInvariant().Contains(part))
            {
                return t;
            }
        }

        return null;
    }

    private static void WireAppearance<T>(string field, CharacterAppearance appearance) where T : Object
    {
        foreach (T target in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(target);
            SetIfEmpty(so, field, appearance);
            so.ApplyModifiedProperties();
        }
    }

    // ───────────────────────── 2. UI ─────────────────────────

    private static bool BuildUI(CharacterAppearance appearance)
    {
        Transform welcome = FindByName("Window_0");
        Transform profile = FindByName("Window_1");
        if (welcome == null || profile == null)
        {
            Debug.LogError("[CharacterCreationBuilder] Window_0 / Window_1 not found in the scene.");
            return false;
        }

        Transform panel = profile.parent;

        // Copies first, while Window_1 is still untouched.
        Transform color = panel.Find("Window_2");
        if (color == null)
        {
            color = MakeStepWindow(profile, "Window_2", "Выбери окрас", "Далее", 1);
            SetupColorCards(color);
        }
        else
        {
            Report.Add("Window_2 уже есть — не трогаю.");
        }

        Transform accessory = panel.Find("Window_3");
        if (accessory == null)
        {
            accessory = MakeStepWindow(profile, "Window_3", "Выбери аксессуар", "Готово", 2);
            SetupAccessoryCards(accessory);
        }
        else
        {
            Report.Add("Window_3 уже есть — не трогаю.");
        }

        CreationOption[] modelOptions = MakeOptions(profile);
        CreationOption[] colorOptions = MakeOptions(color);
        CreationOption[] accessoryOptions = MakeOptions(accessory);

        Transform flowT = panel.Find("CharacterCreationFlow");
        CharacterCreationFlow flow = flowT != null ? flowT.GetComponent<CharacterCreationFlow>() : null;
        if (flow == null)
        {
            var go = new GameObject("CharacterCreationFlow", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Build Character Creation");
            go.layer = panel.gameObject.layer;
            go.transform.SetParent(panel, false);
            flow = go.AddComponent<CharacterCreationFlow>();
            Report.Add($"Создан объект CharacterCreationFlow в '{panel.name}'.");
        }

        var so = new SerializedObject(flow);
        SetIfEmpty(so, "appearance", appearance);
        Transform home = panel.Find("Window_Home");
        if (home != null && so.FindProperty("gameWindows").arraySize == 0)
        {
            SetArray(so, "gameWindows", new Object[] { home.gameObject });
            Report.Add("CharacterCreationFlow → Game Windows = Window_Home.");
        }

        SetIfEmpty(so, "welcomeWindow", welcome.gameObject);
        SetIfEmpty(so, "welcomeNextButton", FindButton(welcome, "Label_0/Play_Button"));
        SetIfEmpty(so, "profileWindow", profile.gameObject);
        Transform input = profile.Find("Label_0/InputField (TMP)");
        SetIfEmpty(so, "nameInput", input != null ? input.GetComponent<TMP_InputField>() : null);
        SetArrayIfEmpty(so, "modelOptions", modelOptions);
        SetIfEmpty(so, "profileNextButton", FindButton(profile, "Label_0/Play_Button"));
        SetIfEmpty(so, "colorWindow", color.gameObject);
        SetArrayIfEmpty(so, "colorOptions", colorOptions);
        SetIfEmpty(so, "colorBackButton", FindButton(color, "Label_0/BackButton"));
        SetIfEmpty(so, "colorNextButton", FindButton(color, "Label_0/Play_Button"));
        SetIfEmpty(so, "accessoryWindow", accessory.gameObject);
        SetArrayIfEmpty(so, "accessoryOptions", accessoryOptions);
        SetIfEmpty(so, "accessoryBackButton", FindButton(accessory, "Label_0/BackButton"));
        SetIfEmpty(so, "accessoryDoneButton", FindButton(accessory, "Label_0/Play_Button"));
        so.ApplyModifiedProperties();

        IntroCutscene intro = FindFirst<IntroCutscene>();
        if (intro != null)
        {
            var introSo = new SerializedObject(intro);
            SetIfEmpty(introSo, "characterCreation", flow);
            introSo.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("[CharacterCreationBuilder] No IntroCutscene in the scene — nothing will start the creation.");
        }

        return true;
    }

    /// <summary>Copy of Window_1 without the name field, with a new header, a back button and the next button relabelled.</summary>
    private static Transform MakeStepWindow(Transform profile, string name, string header, string nextLabel, int siblingOffset)
    {
        GameObject copy = Object.Instantiate(profile.gameObject, profile.parent);
        Undo.RegisterCreatedObjectUndo(copy, "Build Character Creation");
        copy.name = name;
        copy.SetActive(false);
        copy.transform.SetSiblingIndex(profile.GetSiblingIndex() + siblingOffset);

        Transform label = copy.transform.Find("Label_0");
        Transform input = label.Find("InputField (TMP)");
        if (input != null)
        {
            Object.DestroyImmediate(input.gameObject);
        }

        Transform headerT = label.Find("Header");
        if (headerT != null && headerT.TryGetComponent(out TMP_Text headerText))
        {
            headerText.text = header;
        }

        Transform next = label.Find("Play_Button");
        if (next != null)
        {
            // The copy carries Window_1's OnClick (Window_1.SetActive) — the flow drives the windows now.
            var nextButton = next.GetComponent<Button>();
            nextButton.onClick = new Button.ButtonClickedEvent();
            SetButtonLabel(next, nextLabel);

            var nextRt = (RectTransform)next;
            GameObject back = Object.Instantiate(next.gameObject, label);
            back.name = "BackButton";
            var backRt = (RectTransform)back.transform;
            backRt.anchorMin = nextRt.anchorMin;
            backRt.anchorMax = new Vector2(Mathf.Lerp(nextRt.anchorMin.x, nextRt.anchorMax.x, 0.3f), nextRt.anchorMax.y);
            backRt.offsetMin = Vector2.zero;
            backRt.offsetMax = Vector2.zero;
            SetButtonLabel(back.transform, "Назад");
            Transform arrow = back.transform.Find("Image");
            if (arrow != null)
            {
                arrow.gameObject.SetActive(false);
            }

            nextRt.anchorMin = new Vector2(Mathf.Lerp(nextRt.anchorMin.x, nextRt.anchorMax.x, 0.33f), nextRt.anchorMin.y);
            nextRt.offsetMin = Vector2.zero;
            nextRt.offsetMax = Vector2.zero;
        }

        Report.Add($"Собрано {name} «{header}» (копия Window_1): без поля имени, кнопки «Назад» и «{nextLabel}».");
        return copy.transform;
    }

    private static void SetupColorCards(Transform window)
    {
        TMP_Text style = FindText(window, "Label_0/Header");
        int n = 0;
        foreach (Transform card in Cards(window))
        {
            n++;
            Transform icon = card.Find("Icon");
            if (icon != null)
            {
                icon.gameObject.SetActive(false);
            }

            NewLabel(card, "Label", n.ToString(), style, 0.1f, 0.1f, 0.9f, 0.9f);
        }
    }

    private static void SetupAccessoryCards(Transform window)
    {
        TMP_Text style = FindText(window, "Label_0/Header");
        int n = 0;
        foreach (Transform card in Cards(window))
        {
            Transform icon = card.Find("Icon");
            if (icon != null)
            {
                var rt = (RectTransform)icon;
                rt.anchorMin = new Vector2(0.15f, 0.3f);
                rt.anchorMax = new Vector2(0.85f, 0.95f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            string title = n < AccessoryTitles.Length ? AccessoryTitles[n] : "";
            NewLabel(card, "Label", title, style, 0.05f, 0.04f, 0.95f, 0.28f);
            n++;
        }
    }

    /// <summary>Every card in the window's HorizontalGrid gets a Button, a "Selected" tint and CreationOption.</summary>
    private static CreationOption[] MakeOptions(Transform window)
    {
        var result = new List<CreationOption>();
        foreach (Transform card in Cards(window))
        {
            CreationOption option = card.GetComponent<CreationOption>();
            if (option == null)
            {
                Button button = card.GetComponent<Button>();
                if (button == null)
                {
                    button = Undo.AddComponent<Button>(card.gameObject);
                    button.targetGraphic = card.GetComponent<Image>();
                }

                Transform selected = card.Find("Selected");
                if (selected == null)
                {
                    var go = new GameObject("Selected", typeof(RectTransform));
                    Undo.RegisterCreatedObjectUndo(go, "Build Character Creation");
                    go.layer = card.gameObject.layer;
                    go.transform.SetParent(card, false);
                    Stretch((RectTransform)go.transform, 0f, 0f, 1f, 1f);
                    var tint = go.AddComponent<Image>();
                    var cardImage = card.GetComponent<Image>();
                    if (cardImage != null)
                    {
                        tint.sprite = cardImage.sprite;
                        tint.type = cardImage.type;
                        tint.pixelsPerUnitMultiplier = cardImage.pixelsPerUnitMultiplier;
                    }

                    tint.color = SelectedTint;
                    tint.raycastTarget = false;
                    go.SetActive(false);
                    selected = go.transform;
                }

                option = Undo.AddComponent<CreationOption>(card.gameObject);
                var so = new SerializedObject(option);
                so.FindProperty("button").objectReferenceValue = button;
                so.FindProperty("selectedMark").objectReferenceValue = selected.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            result.Add(option);
        }

        if (result.Count != 3)
        {
            Debug.LogWarning($"[CharacterCreationBuilder] {window.name}: {result.Count} cards in HorizontalGrid, expected 3.");
        }
        else
        {
            Report.Add($"{window.name}: 3 карточки выбора (Button + Selected + CreationOption).");
        }

        return result.ToArray();
    }

    private static IEnumerable<Transform> Cards(Transform window)
    {
        Transform grid = window.Find("Label_0/HorizontalGrid");
        if (grid == null)
        {
            yield break;
        }

        foreach (Transform card in grid)
        {
            yield return card;
        }
    }

    // ───────────────────────── helpers ─────────────────────────

    private static T FindFirst<T>() where T : Object =>
        Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

    private static Transform FindByName(string name)
    {
        foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
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

    private static Button FindButton(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static TMP_Text FindText(Transform root, string path)
    {
        Transform t = root.Find(path);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static void SetButtonLabel(Transform button, string text)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = text;
        }
    }

    private static void NewLabel(Transform parent, string name, string text, TMP_Text style,
        float x0, float y0, float x1, float y1)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        Stretch((RectTransform)go.transform, x0, y0, x1, y1);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (style != null)
        {
            tmp.font = style.font;
            tmp.fontSharedMaterial = style.fontSharedMaterial;
            tmp.color = style.color;
        }

        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 18f;
        tmp.fontSizeMax = 200f;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
    }

    private static void Stretch(RectTransform rt, float x0, float y0, float x1, float y1)
    {
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
        {
            t.gameObject.layer = layer;
        }
    }

    private static void SetIfEmpty(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"[CharacterCreationBuilder] {so.targetObject.GetType().Name} has no field '{field}'.");
            return;
        }

        if (value == null || prop.objectReferenceValue != null)
        {
            return;
        }

        prop.objectReferenceValue = value;
        Report.Add($"{so.targetObject.GetType().Name} ('{so.targetObject.name}') → {field} = {value.name}");
    }

    private static void SetArrayIfEmpty(SerializedObject so, string field, Object[] values)
    {
        if (so.FindProperty(field).arraySize == 0)
        {
            SetArray(so, field, values);
        }
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
