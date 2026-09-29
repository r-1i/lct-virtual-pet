using System.Collections.Generic;
using System.Linq;
using Feeding;
using Home;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools → Character → Create Base Animator. Builds Character_Base.controller (Assets/Animators, or wherever it was moved) (states, parameters,
/// transitions — see CharacterAnimParams), one empty placeholder clip per state (an Override Controller swaps clips
/// by their original, so every state needs its own), and an Override Controller per character, assigned to the
/// Model_* Animators under CharacterAppearance, filled with the character's clips from CharacterClips (Loop Time set).
/// Existing assets are kept — delete one to rebuild it. Safe to run again: only empty override slots are filled.
/// </summary>
public static class CharacterAnimatorBuilder
{
    private const string DefaultFolder = "Assets/Animators";
    private const string ControllerName = "Character_Base";

    /// <summary>Wherever Character_Base.controller lives now (the folder may be moved), else DefaultFolder.</summary>
    private static string _folder = DefaultFolder;
    private static string BaseFolder => _folder + "/Base";
    private static string ControllerPath => $"{_folder}/{ControllerName}.controller";

    // Same order as CharacterAppearance → Models (built by CharacterCreationBuilder).
    private static readonly string[] Characters = { "Aksolotl", "Medved", "Skuf" };

    /// <summary>
    /// Clips the artist sent per character: folder + state → FBX file name. The clip inside each FBX goes into the
    /// character's Override Controller (only into empty slots) and gets Loop Time set for looping states.
    /// Add a character here when its animations arrive.
    /// </summary>
    private static readonly Dictionary<string, (string folder, (string state, string fbx)[] clips)> CharacterClips =
        new Dictionary<string, (string, (string, string)[])>
        {
            ["Skuf"] = ("Assets/Models/Chars/SKUF_ANIMATIONS", new[]
            {
                ("Idle", "happy_idle"),
                ("Greet", "privet"),
                ("StomachAche", "bobo"),
                ("Joy", "salituha"),
                ("Play", "igrivost"),
                ("Talk", "slushat"),
                ("Eat", "eda"),
                ("Dance", "tanec"),
                ("Work", "Working"),
            }),
        };

    private static readonly HashSet<string> LoopingStates = new HashSet<string> { "Idle", "StomachAche", "Talk", "Dance", "Work" };

    private const float LoopBlend = 0.2f;
    private const float OneShotIn = 0.1f;
    private const float OneShotExitTime = 0.95f;

    private static readonly List<string> Report = new List<string>();

    [MenuItem("Tools/Character/Create Base Animator")]
    private static void Build()
    {
        Report.Clear();

        string[] existing = AssetDatabase.FindAssets($"{ControllerName} t:AnimatorController")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => System.IO.Path.GetFileNameWithoutExtension(path) == ControllerName)
            .ToArray();
        if (existing.Length > 1)
        {
            Debug.LogError("[CharacterAnimatorBuilder] Найдено несколько Character_Base.controller — удали лишнюю папку " +
                           "целиком (вместе с её override-контроллерами и Base) и запусти ещё раз:\n" + string.Join("\n", existing));
            return;
        }

        // Asset paths always use '/', so the folder is everything before the last one.
        _folder = existing.Length == 1 ? existing[0].Substring(0, existing[0].LastIndexOf('/')) : DefaultFolder;
        EnsureFolder(_folder);
        EnsureFolder(BaseFolder);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = CreateController();
            Report.Add($"Создан {ControllerPath}: 9 состояний, 8 параметров, заглушки клипов в {BaseFolder}.");
        }
        else
        {
            Report.Add($"{ControllerPath} уже есть — не трогаю (удали его, чтобы пересобрать).");
        }

        if (!controller.animationClips.Any(c => c != null && c.name.StartsWith("Base_")))
        {
            Debug.LogError($"[CharacterAnimatorBuilder] {ControllerPath} сделан не этим инструментом: у его состояний нет " +
                           "заглушек Base_*, поэтому Override-контроллерам нечего подменять. Удали его (и override-контроллеры " +
                           "рядом с ним) и запусти ещё раз — инструмент соберёт правильный.");
            return;
        }

        var overrides = new AnimatorOverrideController[Characters.Length];
        for (int i = 0; i < Characters.Length; i++)
        {
            string path = $"{_folder}/{Characters[i]}.overrideController";
            overrides[i] = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (overrides[i] == null)
            {
                overrides[i] = new AnimatorOverrideController(controller) { name = Characters[i] };
                AssetDatabase.CreateAsset(overrides[i], path);
                Report.Add($"Создан {path} — разложи в него клипы персонажа.");
            }
        }

        for (int i = 0; i < Characters.Length; i++)
        {
            FillOverrides(Characters[i], overrides[i]);
        }

        AssetDatabase.SaveAssets();
        AssignToModels(overrides);

        Debug.Log("[CharacterAnimatorBuilder]\n• " + string.Join("\n• ", Report));
    }

    private static AnimatorController CreateController()
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(CharacterAnimParams.Dancing, AnimatorControllerParameterType.Bool);
        controller.AddParameter(CharacterAnimParams.Talking, AnimatorControllerParameterType.Bool);
        controller.AddParameter(CharacterAnimParams.Working, AnimatorControllerParameterType.Bool);
        controller.AddParameter(CharacterAnimParams.StomachAche, AnimatorControllerParameterType.Bool);
        controller.AddParameter(CharacterAnimParams.Greet, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(CharacterAnimParams.Joy, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(CharacterAnimParams.Play, AnimatorControllerParameterType.Trigger);
        controller.AddParameter(CharacterAnimParams.Eat, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        sm.anyStatePosition = new Vector3(20f, 380f);
        sm.entryPosition = new Vector3(20f, 120f);
        sm.exitPosition = new Vector3(800f, 120f);

        AnimatorState idle = AddState(sm, "Idle", new Vector3(300f, 120f));
        sm.defaultState = idle;

        // Looping states: Idle ⇄ state while the bool is on.
        AddLoop(sm, idle, "Dance", CharacterAnimParams.Dancing, new Vector3(560f, -60f));
        AddLoop(sm, idle, "Talk", CharacterAnimParams.Talking, new Vector3(620f, 40f));
        AddLoop(sm, idle, "Work", CharacterAnimParams.Working, new Vector3(620f, 200f));
        AddLoop(sm, idle, "StomachAche", CharacterAnimParams.StomachAche, new Vector3(560f, 300f));

        // One-shots: from Any State on the trigger, back to Idle at the end of the clip.
        AddOneShot(sm, idle, "Greet", CharacterAnimParams.Greet, new Vector3(250f, 380f));
        AddOneShot(sm, idle, "Joy", CharacterAnimParams.Joy, new Vector3(250f, 460f));
        AddOneShot(sm, idle, "Play", CharacterAnimParams.Play, new Vector3(250f, 540f));
        AddOneShot(sm, idle, "Eat", CharacterAnimParams.Eat, new Vector3(250f, 620f));

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, Vector3 position)
    {
        AnimatorState state = sm.AddState(name, position);
        state.motion = PlaceholderClip(name);
        return state;
    }

    private static void AddLoop(AnimatorStateMachine sm, AnimatorState idle, string name, string parameter, Vector3 position)
    {
        AnimatorState state = AddState(sm, name, position);

        AnimatorStateTransition enter = idle.AddTransition(state);
        enter.hasExitTime = false;
        enter.duration = LoopBlend;
        enter.AddCondition(AnimatorConditionMode.If, 0f, parameter);

        AnimatorStateTransition exit = state.AddTransition(idle);
        exit.hasExitTime = false;
        exit.duration = LoopBlend;
        exit.AddCondition(AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddOneShot(AnimatorStateMachine sm, AnimatorState idle, string name, string trigger, Vector3 position)
    {
        AnimatorState state = AddState(sm, name, position);

        AnimatorStateTransition enter = sm.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = OneShotIn;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);

        AnimatorStateTransition exit = state.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = OneShotExitTime;
        exit.duration = LoopBlend;
    }

    /// <summary>Empty clip, one per state — only a key for the Override Controllers' table.</summary>
    private static AnimationClip PlaceholderClip(string state)
    {
        string path = $"{BaseFolder}/Base_{state}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = $"Base_{state}" };
            AssetDatabase.CreateAsset(clip, path);
        }

        return clip;
    }

    private static void FillOverrides(string character, AnimatorOverrideController overrideController)
    {
        if (!CharacterClips.TryGetValue(character, out var set))
        {
            Report.Add($"{character}: клипов в таблице CharacterClips нет — разложи их в {character}.overrideController сам.");
            return;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        int filled = 0;

        foreach ((string state, string fbx) in set.clips)
        {
            string path = $"{set.folder}/{fbx}.fbx";
            AnimationClip clip = LoadClip(path, LoopingStates.Contains(state));
            if (clip == null)
            {
                Report.Add($"{character}: в {path} нет клипа — слот {state} пустой.");
                continue;
            }

            int index = overrides.FindIndex(pair => pair.Key != null && pair.Key.name == $"Base_{state}");
            if (index < 0 || overrides[index].Value != null)
            {
                continue;
            }

            overrides[index] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[index].Key, clip);
            filled++;

            if (state == "Eat")
            {
                SetEatDuration(clip);
            }
        }

        overrideController.ApplyOverrides(overrides);
        EditorUtility.SetDirty(overrideController);
        Report.Add($"{character}: разложено клипов — {filled} (занятые слоты не трогаю).");
    }

    /// <summary>The (first) clip inside an FBX, with Loop Time set on its importer if it differs.</summary>
    private static AnimationClip LoadClip(string fbxPath, bool loop)
    {
        if (AssetImporter.GetAtPath(fbxPath) is ModelImporter importer)
        {
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;

            if (clips.Length > 0 && clips.Any(c => c.loopTime != loop))
            {
                foreach (ModelImporterClipAnimation c in clips)
                {
                    c.loopTime = loop;
                }

                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    /// <summary>CharacterFeeding keeps the character busy for this long — match the eating clip.</summary>
    private static void SetEatDuration(AnimationClip clip)
    {
        foreach (CharacterFeeding feeding in Object.FindObjectsByType<CharacterFeeding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var so = new SerializedObject(feeding);
            SerializedProperty duration = so.FindProperty("eatActionDuration");
            if (!Mathf.Approximately(duration.floatValue, clip.length))
            {
                duration.floatValue = clip.length;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(feeding.gameObject.scene);
                Report.Add($"CharacterFeeding → Eat Action Duration = {clip.length:0.##} с (длина клипа {clip.name}).");
            }
        }
    }

    /// <summary>Puts each character's override on its model's Animator (replacing whatever was there).</summary>
    private static void AssignToModels(AnimatorOverrideController[] overrides)
    {
        var appearance = Object.FindFirstObjectByType<CharacterAppearance>(FindObjectsInactive.Include);
        if (appearance == null)
        {
            Report.Add("CharacterAppearance в сцене нет — назначь Override Controller'ы в Animator моделей сам.");
            return;
        }

        SerializedProperty models = new SerializedObject(appearance).FindProperty("models");
        for (int i = 0; i < models.arraySize && i < overrides.Length; i++)
        {
            var animator = models.GetArrayElementAtIndex(i).FindPropertyRelative("animator").objectReferenceValue as Animator;
            if (animator == null)
            {
                Report.Add($"Models [{i}]: нет Animator — пропускаю.");
                continue;
            }

            if (animator.runtimeAnimatorController == overrides[i])
            {
                continue;
            }

            Undo.RecordObject(animator, "Assign Character Animator");
            animator.runtimeAnimatorController = overrides[i];
            Report.Add($"{animator.name} → Controller = {overrides[i].name}.");
        }

        EditorSceneManager.MarkSceneDirty(appearance.gameObject.scene);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
