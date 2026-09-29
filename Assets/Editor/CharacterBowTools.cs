using System.Collections.Generic;
using System.Linq;
using Home;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Puts each model's bow on a real skeleton bone — one of the SkinnedMeshRenderer's bones, which the animation
/// actually moves (rig helpers like "QuickRigCharacter_Ctrl_Neck" or the model root don't follow the clips).
/// • Tools → Character → Reattach Bows: auto — neck, else head, else the upper-most spine/chest bone. Logs every
///   model's bone list, so a wrong pick is easy to fix by hand.
/// • Select a bone in the Hierarchy → Tools → Character → Move Bow To Selected Bone: that model's bow goes there.
/// The bow lands on the bone's origin with its world scale kept; fit the position/rotation by hand afterwards.
/// </summary>
public static class CharacterBowTools
{
    [MenuItem("Tools/Character/Reattach Bows")]
    private static void ReattachBows()
    {
        var report = new List<string>();
        foreach ((GameObject root, GameObject bow) in Slots())
        {
            Transform[] bones = DeformBones(root);
            report.Add($"{root.name}: кости — {string.Join(", ", bones.Select(b => b.name))}");

            if (bow == null)
            {
                report.Add($"{root.name}: бабочки нет в слоте — пропускаю.");
                continue;
            }

            Transform target = PickBone(bones);
            if (target == null)
            {
                report.Add($"{root.name}: не нашёл кость шеи/головы/спины — выбери кость руками (Move Bow To Selected Bone).");
                continue;
            }

            report.Add(MoveBow(bow, target));
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[CharacterBowTools]\n• " + string.Join("\n• ", report));
    }

    [MenuItem("Tools/Character/Move Bow To Selected Bone")]
    private static void MoveBowToSelected()
    {
        Transform bone = Selection.activeTransform;
        foreach ((GameObject root, GameObject bow) in Slots())
        {
            if (bone == null || !bone.IsChildOf(root.transform))
            {
                continue;
            }

            if (bow == null)
            {
                Debug.LogWarning($"[CharacterBowTools] {root.name}: в слоте нет бабочки.");
                return;
            }

            if (bone == bow.transform || bone.IsChildOf(bow.transform))
            {
                Debug.LogWarning("[CharacterBowTools] Выделена сама бабочка — выдели кость персонажа.");
                return;
            }

            Debug.Log("[CharacterBowTools] " + MoveBow(bow, bone));
            EditorSceneManager.MarkSceneDirty(bone.gameObject.scene);
            Selection.activeGameObject = bow;
            return;
        }

        Debug.LogWarning("[CharacterBowTools] Выдели в Hierarchy кость внутри Model_Aksolotl / Model_Medved / Model_Skuf.");
    }

    [MenuItem("Tools/Character/Move Bow To Selected Bone", true)]
    private static bool MoveBowToSelectedValidate() => Selection.activeTransform != null;

    private static string MoveBow(GameObject bow, Transform bone)
    {
        if (bow.transform.parent == bone)
        {
            return $"{bow.name} уже на '{bone.name}'.";
        }

        Vector3 worldScale = bow.transform.lossyScale;
        Undo.SetTransformParent(bow.transform, bone, "Move Bow");
        Undo.RecordObject(bow.transform, "Move Bow");
        bow.transform.localPosition = Vector3.zero;
        bow.transform.localRotation = Quaternion.identity;
        Vector3 parentScale = bone.lossyScale;
        bow.transform.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
        return $"{bow.name} → кость '{bone.name}' ({PathFrom(bone)}). Подгони положение руками.";
    }

    /// <summary>(model root, bow) for every slot of the scene's CharacterAppearance.</summary>
    private static IEnumerable<(GameObject root, GameObject bow)> Slots()
    {
        var appearance = Object.FindFirstObjectByType<CharacterAppearance>(FindObjectsInactive.Include);
        if (appearance == null)
        {
            Debug.LogError("[CharacterBowTools] В сцене нет CharacterAppearance — сначала Build Character Creation.");
            yield break;
        }

        SerializedProperty models = new SerializedObject(appearance).FindProperty("models");
        for (int i = 0; i < models.arraySize; i++)
        {
            SerializedProperty slot = models.GetArrayElementAtIndex(i);
            var root = slot.FindPropertyRelative("root").objectReferenceValue as GameObject;
            var bow = slot.FindPropertyRelative("bow").objectReferenceValue as GameObject;
            if (root != null)
            {
                yield return (root, bow);
            }
        }
    }

    /// <summary>Bones the model's skinned meshes are bound to, in hierarchy order.</summary>
    private static Transform[] DeformBones(GameObject root)
    {
        var used = new HashSet<Transform>(root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .SelectMany(r => r.bones)
            .Where(b => b != null));
        return root.GetComponentsInChildren<Transform>(true).Where(used.Contains).ToArray();
    }

    private static Transform PickBone(Transform[] bones)
    {
        Transform Named(string part) => bones.FirstOrDefault(b => b.name.ToLowerInvariant().Contains(part));

        // Spine chains go spine → spine1 → spine2…: the deepest one is closest to the neck.
        Transform upperSpine = bones
            .Where(b => b.name.ToLowerInvariant().Contains("spine") || b.name.ToLowerInvariant().Contains("chest"))
            .OrderByDescending(Depth)
            .FirstOrDefault();

        return Named("neck") ?? Named("head") ?? upperSpine;
    }

    private static int Depth(Transform t)
    {
        int depth = 0;
        for (Transform p = t.parent; p != null; p = p.parent)
        {
            depth++;
        }

        return depth;
    }

    private static string PathFrom(Transform bone)
    {
        var names = new List<string>();
        for (Transform t = bone; t != null && t.GetComponent<Animator>() == null; t = t.parent)
        {
            names.Add(t.name);
        }

        names.Reverse();
        return string.Join("/", names);
    }

    private static float SafeDivide(float a, float b) => Mathf.Approximately(b, 0f) ? a : a / b;
}
