using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Maintains explicit character bindings without regenerating the level.</summary>
public static class HuntzAnimationTools
{
    const string SetRoot = "Assets/Huntz/Resources/Huntz/Animations/";
    static readonly string[] Directions = { "South", "SouthWest", "West", "NorthWest", "North", "NorthEast", "East", "SouthEast" };
    static readonly string[] HeroActions = { "Idle", "Walk", "Attack", "Dodge", "Hurt", "Death" };

    [MenuItem("Huntz/Animation/Bind Active Scene and Character Prefabs")]
    public static void BindActiveSceneAndPrefabs()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before binding animation assets.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Huntz/Scenes/HuntzVerticalSlice.unity")
            throw new InvalidOperationException("Open HuntzVerticalSlice before binding characters.");

        foreach (GameObject root in scene.GetRootGameObjects())
            BindTree(root, true);
        EditorSceneManager.MarkSceneDirty(scene);

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Huntz/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (BindTree(prefab, false)) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Huntz] Character animation bindings updated. Save the active scene to persist its changes.");
    }

    static bool BindTree(GameObject root, bool undo)
    {
        bool changed = false;
        foreach (HuntzDirectionalAnimator animator in root.GetComponentsInChildren<HuntzDirectionalAnimator>(true))
        {
            Bind(animator, undo);
            changed = true;
        }
        foreach (Animation legacy in root.GetComponentsInChildren<Animation>(true))
        {
            SpriteRenderer renderer = legacy.GetComponent<SpriteRenderer>();
            AnimationClip firstClip = legacy.clip;
            if (firstClip == null)
                foreach (AnimationState state in legacy) { firstClip = state.clip; break; }
            if (renderer == null || firstClip == null) continue;
            string[] nameParts = firstClip.name.Split(new[] { '_' }, 2);
            if (nameParts.Length != 2) continue;
            HuntzDirectionalSpriteSet set = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>(SetRoot + nameParts[0] + ".asset");
            if (set == null) throw new InvalidOperationException("Missing sprite set for " + firstClip.name);
            HuntzDirectionalAnimator animator = undo ? Undo.AddComponent<HuntzDirectionalAnimator>(legacy.gameObject)
                : legacy.gameObject.AddComponent<HuntzDirectionalAnimator>();
            animator.Configure(renderer, nameParts[0], set, nameParts[0] == "SwordArc" ? "" : nameParts[1]);
            if (undo) Undo.DestroyObjectImmediate(legacy);
            else UnityEngine.Object.DestroyImmediate(legacy);
            EditorUtility.SetDirty(animator);
            changed = true;
        }
        return changed;
    }

    static void Bind(HuntzDirectionalAnimator animator, bool undo)
    {
        HuntzDirectionalSpriteSet set = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>(SetRoot + animator.CharacterId + ".asset");
        if (set == null) throw new InvalidOperationException("Missing sprite set for " + animator.CharacterId);
        SpriteRenderer renderer = animator.TargetRenderer != null ? animator.TargetRenderer : animator.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            Transform visual = animator.transform.Find("Visual");
            SpriteRenderer[] renderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : Array.Empty<SpriteRenderer>();
            if (renderers.Length != 1)
                throw new InvalidOperationException(animator.name + " needs exactly one body SpriteRenderer under Visual.");
            renderer = renderers[0];
        }

        if (undo) Undo.RecordObject(animator, "Bind Huntz animation");
        animator.Configure(renderer, animator.CharacterId, set);
        Animation legacy = renderer.GetComponent<Animation>();
        if (legacy != null)
        {
            if (undo) Undo.DestroyObjectImmediate(legacy);
            else UnityEngine.Object.DestroyImmediate(legacy);
        }
        EditorUtility.SetDirty(animator);
        if (undo && PrefabUtility.IsPartOfPrefabInstance(animator))
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
    }

    public static List<string> ValidateSet(HuntzDirectionalSpriteSet set, string characterId)
    {
        var errors = new List<string>();
        if (set == null) { errors.Add("Sprite set is not assigned."); return errors; }
        if (set.Clips == null || set.Clips.Count == 0) { errors.Add("Sprite set has no clips."); return errors; }
        var keys = new HashSet<string>();
        foreach (HuntzDirectionalSpriteClip clip in set.Clips)
        {
            if (clip == null) { errors.Add("Null clip."); continue; }
            string key = clip.action + "/" + clip.direction;
            if (!keys.Add(key)) errors.Add("Duplicate clip " + key);
            if (clip.direction != "None" && !Directions.Contains(clip.direction)) errors.Add("Unknown direction in " + key);
            if (clip.framesPerSecond <= 0f || float.IsNaN(clip.framesPerSecond) || float.IsInfinity(clip.framesPerSecond)) errors.Add("Invalid frame rate in " + key);
            if (clip.frames == null || clip.frames.Length < 2) { errors.Add("Fewer than two frames in " + key); continue; }
            if (clip.frames.Any(frame => frame == null)) { errors.Add("Missing sprite in " + key); continue; }
            if (clip.frames.Distinct().Count() < 2) errors.Add("Every frame references the same sprite in " + key);
            Sprite first = clip.frames[0];
            foreach (Sprite frame in clip.frames)
            {
                if (frame.rect.size != first.rect.size || frame.pivot != first.pivot || frame.pixelsPerUnit != first.pixelsPerUnit)
                    errors.Add("Inconsistent canvas, pivot or PPU in " + key);
                if (frame.texture.filterMode != FilterMode.Point) errors.Add("Non-Point filtering in " + key);
            }
        }
        IEnumerable<string> actions = characterId == "Hero" ? HeroActions : set.Clips.Where(c => c != null).Select(c => c.action).Distinct();
        foreach (string action in actions)
        {
            if (characterId != "Hero" && keys.Contains(action + "/None"))
            {
                if (Directions.Any(direction => keys.Contains(action + "/" + direction)))
                    errors.Add("Mixed directional and direction-independent clips for " + action);
                continue;
            }
            foreach (string direction in Directions)
                if (!keys.Contains(action + "/" + direction)) errors.Add("Missing " + action + "/" + direction);
        }
        return errors.Distinct().ToList();
    }

    public static List<string> ValidateScene(Scene scene)
    {
        var errors = new List<string>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
        foreach (Animation legacy in root.GetComponentsInChildren<Animation>(true))
            if (legacy.GetComponent<SpriteRenderer>() != null) errors.Add(legacy.name + ": sprite still uses Legacy Animation.");
        foreach (HuntzDirectionalAnimator animator in root.GetComponentsInChildren<HuntzDirectionalAnimator>(true))
        {
            if (animator.TargetRenderer == null) errors.Add(animator.name + ": body renderer is not explicitly assigned.");
            else if (animator.TargetRenderer.GetComponent<Animation>() != null || animator.TargetRenderer.GetComponent<Animator>() != null)
                errors.Add(animator.name + ": another animation component can write the body sprite.");
            errors.AddRange(ValidateSet(animator.SpriteSet, animator.CharacterId).Select(error => animator.name + ": " + error));
        }
        }
        return errors;
    }

    [MenuItem("Huntz/Animation/Validate Active Scene")]
    public static void ValidateActiveScene()
    {
        List<string> errors = ValidateScene(SceneManager.GetActiveScene());
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        Debug.Log("[Huntz] Character animation references, directions, frames, pivots and filtering are valid. Review motion visually in Animation Preview.");
    }
}

/// <summary>Rejects incomplete character animation bindings in the actual build scene.</summary>
public sealed class HuntzAnimationBuildCheck : IProcessSceneWithReport
{
    public int callbackOrder => 0;
    public void OnProcessScene(Scene scene, BuildReport report)
    {
        if (report == null) return;
        List<string> errors = HuntzAnimationTools.ValidateScene(scene);
        if (errors.Count > 0) throw new BuildFailedException("Huntz animation validation failed:\n" + string.Join("\n", errors));
    }
}
