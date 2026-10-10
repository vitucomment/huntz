using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HuntzIsometric3DPilotBuilder
{
    const string Root = "Assets/Huntz/Prototype/Isometric3D";
    const string MaterialFolder = Root + "/Materials";
    const string MeshFolder = Root + "/Meshes";
    const string AnimationFolder = Root + "/Animation";
    const string AppearanceFolder = Root + "/Appearance";
    const string PrefabFolder = Root + "/Prefabs";
    const string SceneFolder = Root + "/Scenes";
    const string IslandCollisionName = "Collision";
    const string AvatarPath = AnimationFolder + "/HuntzHumanoid3D.asset";
    const string TransformCurveAvatarPath = AnimationFolder + "/HuntzHumanoid3D_TransformCurves.asset";
    const string PilotScenePath = SceneFolder + "/Isometric3DARPGPilot.unity";
    const string RigPrefabPath = PrefabFolder + "/HumanoidRig.prefab";
    const string PlayerPrefabPath = PrefabFolder + "/Player.prefab";
    const string ControllerPath = AnimationFolder + "/HuntzHumanoid3D.controller";
    const float AttackClipDuration = 0.68f;
    const float AttackWindupNormalizedTime = 0.16f;
    const float AttackImpactNormalizedTime = 0.31f;
    const float AttackRecoveryNormalizedTime = 0.46f;

    static readonly string[] ClipNames = { "Idle", "Walk", "Run", "Attack", "Dodge", "Hurt", "Death" };

    sealed class Palette
    {
        public Material skin;
        public Material cloth;
        public Material leather;
        public Material metal;
        public Material hair;
        public Material accent;
        public Material rock;
        public Material wood;
        public Material leaf;
        public Material glass;
        public Material path;
        public Material crystal;
    }

    sealed class BuiltRig
    {
        public GameObject root;
        public HuntzArpg3DRig rig;
        public HuntzArpg3DAppearance appearance;
        public Transform weaponSocket;
    }

    [MenuItem("Huntz/Prototype 3D/Build Isometric ARPG Pilot")]
    public static void BuildPilot()
    {
        if (File.Exists(PilotScenePath) && !EditorUtility.DisplayDialog(
                "Rebuild the 3D pilot?",
                "This rebuild replaces only the generated Isometric3DARPGPilot scene and assets inside " + Root + ". The 2D slice and its Build Settings entry remain untouched.",
                "Rebuild pilot", "Cancel"))
            return;

        if (EditorSceneManager.GetActiveScene().path == PilotScenePath && EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorUtility.DisplayDialog("3D pilot has unsaved edits",
                "Save or discard the current pilot edits before rebuilding it.", "OK");
            return;
        }

        EnsureFolders();
        HuntzArpg3DGrayboxKit arenaKit = HuntzArpg3DGrayboxKitBuilder.EnsureKitAssets();
        Palette palette = CreatePaletteMaterials();
        HuntzArpg3DAppearanceProfile baseAppearance = CreateAppearanceProfile("BaseSteel", palette,
            new Color(0.18f, 0.36f, 0.52f), new Color(0.14f, 0.09f, 0.06f), new Color(0.9f, 0.54f, 0.12f));
        HuntzArpg3DAppearanceProfile alternateAppearance = CreateAppearanceProfile("GuildEmber", palette,
            new Color(0.56f, 0.19f, 0.16f), new Color(0.24f, 0.12f, 0.07f), new Color(0.91f, 0.68f, 0.24f));
        AnimationClip[] clips = CreateAnimationClips();
        AnimatorController controller = CreateAnimatorController(clips);
        Mesh mainlandMesh = CreateIslandMesh("MainlandIsland", 14f, 10f, 3.2f);
        Mesh huntMesh = CreateIslandMesh("HuntIsland", 11f, 8.5f, 3f);
        GameObject rigPrefab = CreateHumanoidRigPrefab(controller, baseAppearance, alternateAppearance, palette);
        GameObject playerPrefab = CreatePlayerPrefab(rigPrefab, controller, baseAppearance, alternateAppearance);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Isometric3DARPGPilot";
        BuildLightingAndCamera(out HuntzArpg3DCameraFollow cameraFollow);

        GameObject environment = new GameObject("Generated 3D ARPG Graybox");
        Transform mainland = CreateIsland(environment.transform, "MainLand — Asteroide", mainlandMesh,
            Vector3.zero, palette);
        Transform hunt = CreateIsland(environment.transform, "Hunt — Campo de Treino", huntMesh,
            new Vector3(38f, 0f, 0f), palette);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.name = "Jogador — Guerreiro 3D";
        player.transform.position = new Vector3(-2.5f, 0.05f, -0.8f);
        HuntzArpg3DPlayerController playerController = player.GetComponent<HuntzArpg3DPlayerController>();
        HuntzArpg3DRig playerRig = player.GetComponentInChildren<HuntzArpg3DRig>();
        HuntzArpg3DAppearance playerAppearance = player.GetComponentInChildren<HuntzArpg3DAppearance>();
        playerController.Configure(playerRig, playerAppearance, cameraFollow);
        cameraFollow.SetTarget(player.transform, true);

        Transform mainArrival = CreateArrivalMarker(environment.transform, "Chegada da MainLand", new Vector3(-2.5f, 0.05f, -0.8f));
        Transform huntArrival = CreateArrivalMarker(environment.transform, "Chegada da Hunt", new Vector3(38f, 0.05f, -3.2f));
        BuildMainland(environment.transform, mainland, rigPrefab, baseAppearance, alternateAppearance,
            palette, player, mainArrival, huntArrival);
        BuildTrainingHunt(environment.transform, hunt, rigPrefab, baseAppearance, alternateAppearance,
            palette, player, mainArrival, huntArrival);
        HuntzArpg3DHuntDirector huntDirector = environment.AddComponent<HuntzArpg3DHuntDirector>();
        huntDirector.Configure(playerController, arenaKit);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, PilotScenePath))
            throw new InvalidOperationException("Unity could not save the generated 3D pilot scene.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeGameObject = player;
        Debug.Log("Huntz 3D ARPG pilot generated. MainLand and training hunt are graybox scenes; the 2D Build Settings scene was not changed.");
    }

    [MenuItem("Huntz/Prototype 3D/Refresh Island Collision")]
    public static void RefreshIslandCollision()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != PilotScenePath)
        {
            EditorUtility.DisplayDialog("3D pilot required",
                "Open Isometric3DARPGPilot in Edit Mode before refreshing island collision.", "OK");
            return;
        }

        MeshFilter[] filters = UnityEngine.Object.FindObjectsByType<MeshFilter>();
        int updated = 0;
        foreach (MeshFilter filter in filters)
        {
            if (filter == null || filter.gameObject.scene != scene || filter.sharedMesh == null)
                continue;
            if (filter.sharedMesh.name != "MainlandIsland" && filter.sharedMesh.name != "HuntIsland")
                continue;

            Mesh collisionMesh = CreateIslandCollisionMesh(filter.sharedMesh);
            AttachIslandCollision(filter.transform, collisionMesh, true);
            MeshCollider legacyCollider = filter.GetComponent<MeshCollider>();
            if (legacyCollider != null) Undo.DestroyObjectImmediate(legacyCollider);
            updated++;
        }

        if (updated == 0)
        {
            EditorUtility.DisplayDialog("Island meshes not found",
                "No generated MainLand or Hunt island mesh was found in the active pilot scene.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Refreshed separate collision meshes for " + updated + " pilot islands.");
    }

    [MenuItem("Huntz/Prototype 3D/Install Hunt Vertical Slice")]
    public static void InstallHuntVerticalSlice()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before installing the Hunt loop.");
            return;
        }

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != PilotScenePath)
        {
            Debug.LogError("Open Isometric3DARPGPilot before installing its Hunt loop.");
            return;
        }

        if (scene.isDirty)
        {
            Debug.LogWarning("Save the current 3D pilot edits before installing the Hunt loop.");
            return;
        }

        GameObject environment = GameObject.Find("Generated 3D ARPG Graybox");
        HuntzArpg3DPlayerController player = UnityEngine.Object.FindAnyObjectByType<HuntzArpg3DPlayerController>();
        if (environment == null || player == null)
        {
            Debug.LogError("The generated environment or the 3D player is missing from the active pilot scene.");
            return;
        }

        HuntzArpg3DHuntDirector director = environment.GetComponent<HuntzArpg3DHuntDirector>();
        if (director == null) director = Undo.AddComponent<HuntzArpg3DHuntDirector>(environment);
        Undo.RecordObject(director, "Install Hunt vertical slice");
        HuntzArpg3DGrayboxKit arenaKit = HuntzArpg3DGrayboxKitBuilder.EnsureKitAssets();
        director.Configure(player, arenaKit);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, PilotScenePath))
        {
            Debug.LogError("Unity could not save the Hunt vertical slice into the 3D pilot scene.");
            return;
        }

        Debug.Log("Hunt vertical slice installed: the Portal HZ now starts the 3×3 Easy Hunt in the active 3D pilot.");
    }

    [MenuItem("Huntz/Prototype 3D/Refresh Locomotion Animations")]
    public static void RefreshLocomotionAnimations()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before refreshing the humanoid locomotion assets.");
            return;
        }

        AnimationClip[] clips = CreateAnimationClips();
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError("The 3D humanoid Animator Controller is missing. Rebuild the pilot to create it.");
            return;
        }

        bool hasGaitRate = false;
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == "GaitRate" && parameter.type == AnimatorControllerParameterType.Float)
                hasGaitRate = true;
        }
        if (!hasGaitRate) controller.AddParameter("GaitRate", AnimatorControllerParameterType.Float);

        AnimatorState locomotion = null;
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states)
        {
            if (child.state.name == "Locomotion")
            {
                locomotion = child.state;
                break;
            }
        }

        if (locomotion == null)
        {
            Debug.LogError("The 3D humanoid Animator Controller has no Locomotion state.");
            return;
        }

        locomotion.motion = CreateLocomotionBlendTree(controller, clips[0], clips[1], clips[2], locomotion.motion as BlendTree);
        locomotion.speedParameterActive = true;
        locomotion.speedParameter = "GaitRate";
        EditorUtility.SetDirty(locomotion);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Huntz locomotion clips refreshed: looping Idle, articulated Walk/Run, and velocity-synced playback.");
    }

    [MenuItem("Huntz/Prototype 3D/Refresh Humanoid Rig Prefab")]
    public static void RefreshHumanoidRigPrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before refreshing the humanoid rig prefab.");
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path == PilotScenePath && activeScene.isDirty)
        {
            EditorUtility.DisplayDialog("3D pilot has unsaved edits",
                "Save the pilot scene before refreshing its shared humanoid rig prefab.", "OK");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        HuntzArpg3DAppearanceProfile baseProfile = AssetDatabase.LoadAssetAtPath<HuntzArpg3DAppearanceProfile>(AppearanceFolder + "/BaseSteel.asset");
        HuntzArpg3DAppearanceProfile alternateProfile = AssetDatabase.LoadAssetAtPath<HuntzArpg3DAppearanceProfile>(AppearanceFolder + "/GuildEmber.asset");
        Palette palette = LoadRigPalette();
        if (controller == null || baseProfile == null || alternateProfile == null ||
            palette.skin == null || palette.cloth == null || palette.leather == null ||
            palette.metal == null || palette.hair == null || palette.accent == null)
        {
            Debug.LogError("Cannot refresh the humanoid rig: its Animator Controller, appearance profiles, or materials are missing.");
            return;
        }

        GameObject rigPrefab = CreateHumanoidRigPrefab(controller, baseProfile, alternateProfile, palette);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = rigPrefab;
        Debug.Log("Humanoid rig prefab refreshed with a retargetable Humanoid Avatar and articulated toe joints. The pilot scene was not rebuilt.");
    }

    static Palette LoadRigPalette()
    {
        return new Palette
        {
            skin = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Graybox_Skin.mat"),
            cloth = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Graybox_Cloth.mat"),
            leather = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Graybox_Leather.mat"),
            metal = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Graybox_Metal.mat"),
            hair = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Graybox_Hair.mat"),
            accent = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Graybox_Accent.mat")
        };
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Huntz/Prototype");
        EnsureFolder(Root);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
        EnsureFolder(AnimationFolder);
        EnsureFolder(AppearanceFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(SceneFolder);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || !AssetDatabase.IsValidFolder(parent))
            throw new DirectoryNotFoundException("Unity asset folder parent is missing: " + parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static Palette CreatePaletteMaterials()
    {
        return new Palette
        {
            skin = CreateMaterial("Graybox_Skin", new Color(0.82f, 0.57f, 0.39f)),
            cloth = CreateMaterial("Graybox_Cloth", new Color(0.18f, 0.36f, 0.52f)),
            leather = CreateMaterial("Graybox_Leather", new Color(0.2f, 0.12f, 0.08f)),
            metal = CreateMaterial("Graybox_Metal", new Color(0.62f, 0.72f, 0.78f), 0.42f),
            hair = CreateMaterial("Graybox_Hair", new Color(0.12f, 0.08f, 0.06f)),
            accent = CreateMaterial("Graybox_Accent", new Color(0.9f, 0.54f, 0.12f)),
            rock = CreateMaterial("Graybox_Rock", new Color(0.22f, 0.2f, 0.23f)),
            wood = CreateMaterial("Graybox_Wood", new Color(0.32f, 0.2f, 0.12f)),
            leaf = CreateMaterial("Graybox_Leaf", new Color(0.22f, 0.42f, 0.27f)),
            glass = CreateEmissiveMaterial("Graybox_Glass", new Color(0.24f, 0.67f, 0.82f), 0.45f),
            path = CreateMaterial("Graybox_Path", new Color(0.35f, 0.39f, 0.44f), 0.08f),
            crystal = CreateEmissiveMaterial("Graybox_Crystal", new Color(0.38f, 0.31f, 0.92f), 1.6f),
        };
    }

    static Material CreateMaterial(string name, Color color, float metallic = 0f)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) throw new InvalidOperationException("Unity did not expose a compatible lit shader for the graybox materials.");
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.24f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material CreateEmissiveMaterial(string name, Color color, float intensity)
    {
        Material material = CreateMaterial(name, color, 0.2f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    static HuntzArpg3DAppearanceProfile CreateAppearanceProfile(string name, Palette palette,
        Color clothColor, Color leatherColor, Color accentColor)
    {
        string path = AppearanceFolder + "/" + name + ".asset";
        HuntzArpg3DAppearanceProfile profile = AssetDatabase.LoadAssetAtPath<HuntzArpg3DAppearanceProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<HuntzArpg3DAppearanceProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }

        profile.name = name;
        profile.skin = new Color(0.82f, 0.57f, 0.39f);
        profile.cloth = clothColor;
        profile.leather = leatherColor;
        profile.metal = new Color(0.62f, 0.72f, 0.78f);
        profile.hair = new Color(0.12f, 0.08f, 0.06f);
        profile.accent = accentColor;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static AnimationClip[] CreateAnimationClips()
    {
        AnimationClip[] clips = new AnimationClip[ClipNames.Length];
        clips[0] = CreateClip("Idle", 0.9f, true, (clip, times) =>
        {
            AddPositionCurve(clip, "Armature/Hips/Spine", "m_LocalPosition.y", times,
                new[] { 0.17f, 0.19f, 0.17f, 0.15f, 0.17f });
        });
        clips[1] = CreateClip("Walk", 0.58f, true,
            (clip, times) => AuthorLocomotionCycle(clip, times, 36f, 35f, 18f, 0.018f, 4f, 0.625f));
        clips[2] = CreateClip("Run", 0.46f, true,
            (clip, times) => AuthorLocomotionCycle(clip, times, 56f, 58f, 26f, 0.04f, 8f, 0.5f));
        float[] attackKeyTimes =
        {
            0f,
            AttackClipDuration * AttackWindupNormalizedTime,
            AttackClipDuration * AttackImpactNormalizedTime,
            AttackClipDuration * AttackRecoveryNormalizedTime,
            AttackClipDuration
        };
        clips[3] = CreateClip("Attack", AttackClipDuration, false, (clip, times) =>
        {
            string arm = "Armature/Hips/Spine/Chest/RightShoulder/RightUpperArm";
            string elbow = arm + "/RightLowerArm";
            AddRotationCurve(clip, arm, attackKeyTimes, t => AttackArmPose(t));
            AddRotationCurve(clip, elbow, attackKeyTimes, t => AttackElbowPose(t));
            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent
                {
                    functionName = nameof(HuntzArpg3DRig.OnWeaponImpact),
                    time = AttackClipDuration * AttackImpactNormalizedTime
                }
            });
        });
        clips[4] = CreateClip("Dodge", 0.32f, false, (clip, times) =>
        {
            AddRotationCurve(clip, "Armature/Hips", times,
                t => new Vector3(Mathf.Sin(t * Mathf.PI) * 22f, 0f, Mathf.Sin(t * Mathf.PI) * -9f));
            AddPositionCurve(clip, "Armature/Hips", "m_LocalPosition.y", times,
                new[] { 0.94f, 0.83f, 0.83f, 0.9f, 0.94f });
        });
        clips[5] = CreateClip("Hurt", 0.3f, false, (clip, times) =>
        {
            AddRotationCurve(clip, "Armature/Hips/Spine", times,
                t => new Vector3(0f, 0f, Mathf.Sin(t * Mathf.PI) * 13f));
            AddRotationCurve(clip, "Armature/Hips/Spine/Chest", times,
                t => new Vector3(-Mathf.Sin(t * Mathf.PI) * 18f, 0f, 0f));
        });
        clips[6] = CreateClip("Death", 0.95f, false, (clip, times) =>
        {
            AddRotationCurve(clip, "Armature/Hips", new[] { 0f, 0.32f, 0.62f, 0.95f },
                t => new Vector3(0f, 0f, Mathf.Lerp(0f, 78f, Mathf.Clamp01(t / 0.62f))));
            AddPositionCurve(clip, "Armature/Hips", "m_LocalPosition.y", new[] { 0f, 0.32f, 0.62f, 0.95f },
                new[] { 0.94f, 0.76f, 0.22f, 0.12f });
        });
        AssetDatabase.SaveAssets();
        return clips;
    }

    static void AuthorLocomotionCycle(AnimationClip clip, float[] defaultTimes, float strideAngle,
        float kneeFlex, float armSwing, float hipBob, float forwardLean, float stanceFraction)
    {
        const string hips = "Armature/Hips";
        const string leftUpperLeg = hips + "/LeftUpperLeg";
        const string rightUpperLeg = hips + "/RightUpperLeg";
        const string leftLowerLeg = leftUpperLeg + "/LeftLowerLeg";
        const string rightLowerLeg = rightUpperLeg + "/RightLowerLeg";
        const string leftFoot = leftLowerLeg + "/LeftFoot";
        const string rightFoot = rightLowerLeg + "/RightFoot";
        const string leftToeBase = leftFoot + "/LeftToeBase";
        const string rightToeBase = rightFoot + "/RightToeBase";
        const string leftArm = "Armature/Hips/Spine/Chest/LeftShoulder/LeftUpperArm";
        const string rightArm = "Armature/Hips/Spine/Chest/RightShoulder/RightUpperArm";
        const string leftElbow = leftArm + "/LeftLowerArm";
        const string rightElbow = rightArm + "/RightLowerArm";

        const int curveIntervals = 32;
        float duration = defaultTimes[defaultTimes.Length - 1];
        float[] curveTimes = new float[curveIntervals + 1];
        float[] hipHeights = new float[curveIntervals + 1];
        for (int i = 0; i <= curveIntervals; i++)
        {
            curveTimes[i] = duration * i / curveIntervals;
            float phase = (float)i / curveIntervals;
            hipHeights[i] = 0.93f + Mathf.Abs(Mathf.Sin(phase * Mathf.PI * 2f)) * hipBob;
        }

        AddPositionCurve(clip, hips, "m_LocalPosition.y", curveTimes, hipHeights);
        AddRotationCurve(clip, hips, curveTimes,
            t => new Vector3(-forwardLean * 0.15f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 1.5f));
        AddRotationCurve(clip, "Armature/Hips/Spine", curveTimes,
            t => new Vector3(-forwardLean * 0.45f, 0f, -Mathf.Sin(t * Mathf.PI * 2f) * 0.65f));
        AddRotationCurve(clip, "Armature/Hips/Spine/Chest", curveTimes,
            t => new Vector3(-forwardLean * 0.4f, -Mathf.Sin(t * Mathf.PI * 2f) * 1.25f, 0f));

        AddRotationCurve(clip, leftUpperLeg, curveTimes,
            t => new Vector3(EvaluateThighPitch(t, strideAngle, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, rightUpperLeg, curveTimes,
            t => new Vector3(EvaluateThighPitch(t + 0.5f, strideAngle, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, leftLowerLeg, curveTimes,
            t => new Vector3(EvaluateKneeFlex(t, kneeFlex, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, rightLowerLeg, curveTimes,
            t => new Vector3(EvaluateKneeFlex(t + 0.5f, kneeFlex, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, leftFoot, curveTimes,
            t => new Vector3(EvaluateFootPitch(t, strideAngle, kneeFlex, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, rightFoot, curveTimes,
            t => new Vector3(EvaluateFootPitch(t + 0.5f, strideAngle, kneeFlex, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, leftToeBase, curveTimes,
            t => new Vector3(EvaluateToePitch(t, stanceFraction), 0f, 0f));
        AddRotationCurve(clip, rightToeBase, curveTimes,
            t => new Vector3(EvaluateToePitch(t + 0.5f, stanceFraction), 0f, 0f));

        AddRotationCurve(clip, leftArm, curveTimes,
            t => new Vector3(-EvaluateThighPitch(t, strideAngle, stanceFraction) * armSwing / strideAngle, 0f, 0f));
        AddRotationCurve(clip, rightArm, curveTimes,
            t => new Vector3(-EvaluateThighPitch(t + 0.5f, strideAngle, stanceFraction) * armSwing * 0.28f / strideAngle, 0f, 0f));
        AddRotationCurve(clip, leftElbow, curveTimes,
            t => new Vector3(12f + EvaluateKneeFlex(t + 0.5f, kneeFlex, stanceFraction) * 0.08f, 0f, 0f));
        AddRotationCurve(clip, rightElbow, curveTimes,
            t => new Vector3(18f + EvaluateKneeFlex(t, kneeFlex, stanceFraction) * 0.04f, 0f, 0f));
    }

    static float EvaluateThighPitch(float phase, float strideAngle, float stanceFraction)
    {
        phase = Mathf.Repeat(phase, 1f);
        float forwardAngle = -strideAngle;
        float backAngle = strideAngle * 0.9f;
        if (phase <= stanceFraction)
            return Mathf.Lerp(forwardAngle, backAngle, phase / stanceFraction);

        float swingPhase = (phase - stanceFraction) / (1f - stanceFraction);
        float easedSwing = swingPhase * swingPhase * (3f - 2f * swingPhase);
        return Mathf.Lerp(backAngle, forwardAngle, easedSwing);
    }

    static float EvaluateKneeFlex(float phase, float kneeFlex, float stanceFraction)
    {
        phase = Mathf.Repeat(phase, 1f);
        if (phase <= stanceFraction) return 2f;

        float swingPhase = (phase - stanceFraction) / (1f - stanceFraction);
        return 2f + Mathf.Sin(swingPhase * Mathf.PI) * kneeFlex;
    }

    static float EvaluateFootPitch(float phase, float strideAngle, float kneeFlex, float stanceFraction)
    {
        float thigh = EvaluateThighPitch(phase, strideAngle, stanceFraction);
        float knee = EvaluateKneeFlex(phase, kneeFlex, stanceFraction);
        phase = Mathf.Repeat(phase, 1f);
        float toeLift = 0f;
        if (phase > stanceFraction)
        {
            float swingPhase = (phase - stanceFraction) / (1f - stanceFraction);
            toeLift = Mathf.Sin(swingPhase * Mathf.PI) * 8f;
        }
        return -thigh - knee - toeLift;
    }

    static float EvaluateToePitch(float phase, float stanceFraction)
    {
        phase = Mathf.Repeat(phase, 1f);
        if (phase <= stanceFraction)
        {
            float pushOff = Mathf.InverseLerp(stanceFraction * 0.72f, stanceFraction, phase);
            return Mathf.SmoothStep(0f, 16f, pushOff);
        }

        float swingPhase = (phase - stanceFraction) / (1f - stanceFraction);
        if (swingPhase < 0.22f)
            return Mathf.Lerp(16f, -6f, Mathf.SmoothStep(0f, 1f, swingPhase / 0.22f));
        if (swingPhase > 0.82f)
            return Mathf.Lerp(-6f, 0f, Mathf.SmoothStep(0f, 1f, (swingPhase - 0.82f) / 0.18f));
        return -6f;
    }

    delegate void ClipAuthoring(AnimationClip clip, float[] defaultTimes);

    static AnimationClip CreateClip(string name, float duration, bool looping, ClipAuthoring authoring)
    {
        string path = AnimationFolder + "/" + name + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = name;
        clip.frameRate = 30f;
        clip.wrapMode = looping ? WrapMode.Loop : WrapMode.Once;
        AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = looping;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        clip.ClearCurves();
        AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
        float[] times = { 0f, duration * 0.25f, duration * 0.5f, duration * 0.75f, duration };
        authoring?.Invoke(clip, times);
        clip.EnsureQuaternionContinuity();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static Vector3 AttackArmPose(float time)
    {
        if (time < AttackWindupNormalizedTime)
            return Vector3.Lerp(Vector3.zero, new Vector3(55f, -12f, -18f), time / AttackWindupNormalizedTime);
        if (time < AttackImpactNormalizedTime)
        {
            float phase = (time - AttackWindupNormalizedTime) /
                          (AttackImpactNormalizedTime - AttackWindupNormalizedTime);
            return Vector3.Lerp(new Vector3(55f, -12f, -18f), new Vector3(-24f, 0f, 0f), phase);
        }
        if (time < AttackRecoveryNormalizedTime)
        {
            float phase = (time - AttackImpactNormalizedTime) /
                          (AttackRecoveryNormalizedTime - AttackImpactNormalizedTime);
            return Vector3.Lerp(new Vector3(-24f, 0f, 0f), new Vector3(-8f, 4f, 3f), phase);
        }

        float recovery = (time - AttackRecoveryNormalizedTime) / (1f - AttackRecoveryNormalizedTime);
        return Vector3.Lerp(new Vector3(-8f, 4f, 3f), Vector3.zero, recovery);
    }

    static Vector3 AttackElbowPose(float time)
    {
        if (time < AttackWindupNormalizedTime)
            return Vector3.Lerp(Vector3.zero, new Vector3(52f, 0f, 0f), time / AttackWindupNormalizedTime);
        if (time < AttackImpactNormalizedTime)
        {
            float phase = (time - AttackWindupNormalizedTime) /
                          (AttackImpactNormalizedTime - AttackWindupNormalizedTime);
            return Vector3.Lerp(new Vector3(52f, 0f, 0f), new Vector3(-14f, 0f, 0f), phase);
        }
        if (time < AttackRecoveryNormalizedTime)
        {
            float phase = (time - AttackImpactNormalizedTime) /
                          (AttackRecoveryNormalizedTime - AttackImpactNormalizedTime);
            return Vector3.Lerp(new Vector3(-14f, 0f, 0f), Vector3.zero, phase);
        }
        return Vector3.zero;
    }

    static void AddRotationCurve(AnimationClip clip, string path, float[] times, Func<float, Vector3> pose)
    {
        List<Keyframe> x = new List<Keyframe>();
        List<Keyframe> y = new List<Keyframe>();
        List<Keyframe> z = new List<Keyframe>();
        List<Keyframe> w = new List<Keyframe>();
        float duration = times[times.Length - 1];
        foreach (float time in times)
        {
            Quaternion rotation = Quaternion.Euler(pose(duration <= 0f ? 0f : time / duration));
            x.Add(new Keyframe(time, rotation.x));
            y.Add(new Keyframe(time, rotation.y));
            z.Add(new Keyframe(time, rotation.z));
            w.Add(new Keyframe(time, rotation.w));
        }

        SetCurve(clip, path, "m_LocalRotation.x", x);
        SetCurve(clip, path, "m_LocalRotation.y", y);
        SetCurve(clip, path, "m_LocalRotation.z", z);
        SetCurve(clip, path, "m_LocalRotation.w", w);
    }

    static void AddPositionCurve(AnimationClip clip, string path, string property, float[] times, float[] values)
    {
        List<Keyframe> keys = new List<Keyframe>();
        for (int i = 0; i < times.Length; i++) keys.Add(new Keyframe(times[i], values[Mathf.Min(i, values.Length - 1)]));
        SetCurve(clip, path, property, keys);
    }

    static void SetCurve(AnimationClip clip, string path, string property, List<Keyframe> keys)
    {
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
        AnimationCurve curve = new AnimationCurve(keys.ToArray());
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    static AnimatorController CreateAnimatorController(AnimationClip[] clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("GaitRate", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Death", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = machine.AddState("Locomotion", new Vector3(240f, 80f, 0f));
        idle.motion = CreateLocomotionBlendTree(controller, clips[0], clips[1], clips[2]);
        idle.speedParameterActive = true;
        idle.speedParameter = "GaitRate";
        machine.defaultState = idle;

        string[] actionStates = { "Attack", "Dodge", "Hurt", "Death" };
        string[] triggerNames = { "Attack", "Dodge", "Hurt", "Death" };
        int[] clipIndices = { 3, 4, 5, 6 };
        for (int i = 0; i < actionStates.Length; i++)
        {
            AnimatorState state = machine.AddState(actionStates[i], new Vector3(500f, 60f + i * 110f, 0f));
            state.motion = clips[clipIndices[i]];
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.hasFixedDuration = true;
            enter.duration = 0.04f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, triggerNames[i]);
            if (actionStates[i] == "Death") continue;

            AnimatorStateTransition exit = state.AddTransition(idle);
            exit.hasExitTime = true;
            exit.exitTime = 1f;
            exit.hasFixedDuration = true;
            exit.duration = 0.06f;
        }

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static BlendTree CreateLocomotionBlendTree(AnimatorController controller, AnimationClip idle,
        AnimationClip walk, AnimationClip run, BlendTree existing = null)
    {
        BlendTree blend = existing;
        if (blend == null)
        {
            blend = new BlendTree();
            AssetDatabase.AddObjectToAsset(blend, controller);
        }
        else
        {
            blend.children = Array.Empty<ChildMotion>();
        }

        blend.name = "Locomotion 1D";
        blend.blendType = BlendTreeType.Simple1D;
        blend.blendParameter = "MoveSpeed";
        blend.useAutomaticThresholds = false;
        blend.AddChild(idle, 0f);
        blend.AddChild(walk, 1f);
        blend.AddChild(run, 1.55f);
        EditorUtility.SetDirty(blend);
        return blend;
    }

    static Mesh CreateIslandMesh(string assetName, float radiusX, float radiusZ, float depth)
    {
        string path = MeshFolder + "/" + assetName + ".asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = assetName };
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            mesh.Clear();
        }

        const int segments = 48;
        Vector3[] vertices = new Vector3[1 + segments * 2 + 1];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float wobble = 1f + 0.035f * Mathf.Sin(angle * 3f) + 0.02f * Mathf.Cos(angle * 5f);
            vertices[1 + i] = new Vector3(Mathf.Cos(angle) * radiusX * wobble, 0f, Mathf.Sin(angle) * radiusZ * wobble);
            vertices[1 + segments + i] = new Vector3(Mathf.Cos(angle) * radiusX * 0.72f * wobble,
                -depth, Mathf.Sin(angle) * radiusZ * 0.72f * wobble);
        }
        int bottomCenter = vertices.Length - 1;
        vertices[bottomCenter] = new Vector3(0f, -depth, 0f);

        List<int> topTriangles = new List<int>(segments * 3);
        List<int> sideTriangles = new List<int>(segments * 6 + segments * 3);
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int topA = 1 + i;
            int topB = 1 + next;
            int bottomA = 1 + segments + i;
            int bottomB = 1 + segments + next;
            topTriangles.Add(0); topTriangles.Add(topB); topTriangles.Add(topA);
            sideTriangles.Add(topA); sideTriangles.Add(topB); sideTriangles.Add(bottomA);
            sideTriangles.Add(topB); sideTriangles.Add(bottomB); sideTriangles.Add(bottomA);
            sideTriangles.Add(bottomCenter); sideTriangles.Add(bottomA); sideTriangles.Add(bottomB);
        }

        mesh.subMeshCount = 2;
        mesh.vertices = vertices;
        mesh.SetTriangles(topTriangles, 0);
        mesh.SetTriangles(sideTriangles, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    static Transform CreateIsland(Transform parent, string name, Mesh mesh, Vector3 position, Palette palette)
    {
        GameObject island = new GameObject(name);
        island.transform.SetParent(parent, false);
        island.transform.position = position;
        MeshFilter filter = island.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = island.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new[]
        {
            CreateMaterial("Graybox_IslandTop", new Color(0.27f, 0.43f, 0.33f)),
            CreateMaterial("Graybox_IslandRock", new Color(0.2f, 0.24f, 0.32f))
        };
        AttachIslandCollision(island.transform, CreateIslandCollisionMesh(mesh));
        return island.transform;
    }

    static Mesh CreateIslandCollisionMesh(Mesh visualMesh)
    {
        string assetName = visualMesh.name + "_Collision";
        string path = MeshFolder + "/" + assetName + ".asset";
        Mesh collisionMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (collisionMesh == null)
        {
            collisionMesh = new Mesh { name = assetName };
            AssetDatabase.CreateAsset(collisionMesh, path);
        }
        else
        {
            collisionMesh.Clear();
        }

        collisionMesh.vertices = visualMesh.vertices;
        collisionMesh.subMeshCount = visualMesh.subMeshCount;
        for (int subMesh = 0; subMesh < visualMesh.subMeshCount; subMesh++)
            collisionMesh.SetTriangles(visualMesh.GetTriangles(subMesh), subMesh, false);
        collisionMesh.RecalculateNormals();
        collisionMesh.RecalculateBounds();
        EditorUtility.SetDirty(collisionMesh);
        return collisionMesh;
    }

    static void AttachIslandCollision(Transform island, Mesh collisionMesh, bool recordUndo = false)
    {
        Transform collision = island.Find(IslandCollisionName);
        if (collision == null)
        {
            GameObject collisionObject = new GameObject(IslandCollisionName);
            collision = collisionObject.transform;
            collision.SetParent(island, false);
            if (recordUndo) Undo.RegisterCreatedObjectUndo(collisionObject, "Create island collision object");
        }

        if (recordUndo) Undo.RecordObject(collision, "Align island collision object");
        collision.localPosition = Vector3.zero;
        collision.localRotation = Quaternion.identity;
        collision.localScale = Vector3.one;
        MeshCollider collider = collision.GetComponent<MeshCollider>();
        if (collider == null)
        {
            collider = recordUndo
                ? Undo.AddComponent<MeshCollider>(collision.gameObject)
                : collision.gameObject.AddComponent<MeshCollider>();
        }
        if (recordUndo) Undo.RecordObject(collider, "Assign island collision mesh");
        if (collider.sharedMesh != collisionMesh)
        {
            collider.sharedMesh = null;
            collider.sharedMesh = collisionMesh;
        }
        collider.convex = false;
    }

    static GameObject CreateHumanoidRigPrefab(AnimatorController controller,
        HuntzArpg3DAppearanceProfile baseProfile, HuntzArpg3DAppearanceProfile alternateProfile, Palette palette)
    {
        GameObject rigRoot = new GameObject("HumanoidRig");
        Animator animator = rigRoot.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        HuntzArpg3DRig rig = rigRoot.AddComponent<HuntzArpg3DRig>();
        HuntzArpg3DAppearance appearance = rigRoot.AddComponent<HuntzArpg3DAppearance>();
        Transform armature = NewBone("Armature", rigRoot.transform, Vector3.zero);
        Transform hips = NewBone("Hips", armature, new Vector3(0f, 0.93f, 0f));
        CreatePart("Pelvis", PrimitiveType.Cube, hips, Vector3.zero, new Vector3(0.38f, 0.24f, 0.25f), palette.leather, HuntzArpg3DPartSlot.Leather);
        Transform spine = NewBone("Spine", hips, new Vector3(0f, 0.16f, 0f));
        Transform chest = NewBone("Chest", spine, new Vector3(0f, 0.26f, 0f));
        CreatePart("Chestplate", PrimitiveType.Capsule, chest, new Vector3(0f, 0.12f, 0f), new Vector3(0.34f, 0.34f, 0.24f), palette.cloth, HuntzArpg3DPartSlot.Cloth);
        Transform neck = NewBone("Neck", chest, new Vector3(0f, 0.35f, 0f));
        CreatePart("NeckMesh", PrimitiveType.Cylinder, neck, Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), palette.skin, HuntzArpg3DPartSlot.Skin);
        Transform head = NewBone("Head", neck, new Vector3(0f, 0.14f, 0f));
        CreatePart("HeadMesh", PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.29f, 0.32f, 0.27f), palette.skin, HuntzArpg3DPartSlot.Skin);
        CreatePart("HairCap", PrimitiveType.Sphere, head, new Vector3(0f, 0.09f, -0.015f), new Vector3(0.30f, 0.20f, 0.28f), palette.hair, HuntzArpg3DPartSlot.Hair);
        CreatePart("ShoulderCloth", PrimitiveType.Cube, chest, new Vector3(0f, 0.17f, 0f), new Vector3(0.69f, 0.12f, 0.31f), palette.accent, HuntzArpg3DPartSlot.Accent);

        CreateArm(hips, chest, "Left", -1f, palette);
        Transform rightHand = CreateArm(hips, chest, "Right", 1f, palette);
        Transform weaponSocket = NewBone("WeaponSocket", rightHand, new Vector3(0f, -0.08f, 0.18f));
        CreateSword(weaponSocket, palette);
        CreateLeg(hips, "Left", -1f, palette);
        CreateLeg(hips, "Right", 1f, palette);

        Avatar retargetAvatar = AvatarBuilder.BuildHumanAvatar(rigRoot, CreateHumanDescription());
        if (retargetAvatar != null && retargetAvatar.isValid && retargetAvatar.isHuman)
        {
            retargetAvatar = SaveAvatarAsset(retargetAvatar, AvatarPath, "HuntzHumanoid3D_Retarget");
        }
        else
        {
            if (retargetAvatar != null) UnityEngine.Object.DestroyImmediate(retargetAvatar);
            Debug.LogError("Unity could not create a valid Humanoid Avatar mapping for the graybox rig.");
            retargetAvatar = null;
        }

        Avatar transformCurveAvatar = AvatarBuilder.BuildGenericAvatar(rigRoot, string.Empty);
        if (transformCurveAvatar != null && transformCurveAvatar.isValid)
        {
            transformCurveAvatar = SaveAvatarAsset(transformCurveAvatar, TransformCurveAvatarPath,
                "HuntzHumanoid3D_TransformCurves");
            animator.avatar = transformCurveAvatar;
        }
        else
        {
            if (transformCurveAvatar != null) UnityEngine.Object.DestroyImmediate(transformCurveAvatar);
            Debug.LogError("Unity could not create the Generic Avatar required by the current transform-curve animations.");
        }

        rig.Configure(animator, weaponSocket, retargetAvatar, 0.68f, 0.32f, 0.3f);
        if (!rig.ValidateRig(out string rigIssue))
            Debug.LogError("Generated humanoid rig failed validation: " + rigIssue, rigRoot);
        appearance.Configure(new[] { baseProfile, alternateProfile });
        PrefabUtility.SaveAsPrefabAsset(rigRoot, RigPrefabPath);
        UnityEngine.Object.DestroyImmediate(rigRoot);
        return AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
    }

    static Avatar SaveAvatarAsset(Avatar avatar, string path, string assetName)
    {
        avatar.name = assetName;
        Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if (existing != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(avatar, path);
        return AssetDatabase.LoadAssetAtPath<Avatar>(path);
    }

    static GameObject CreatePlayerPrefab(GameObject rigPrefab, AnimatorController controller,
        HuntzArpg3DAppearanceProfile baseProfile, HuntzArpg3DAppearanceProfile alternateProfile)
    {
        GameObject player = new GameObject("Player");
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.9f;
        characterController.radius = 0.34f;
        characterController.center = new Vector3(0f, 0.95f, 0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 48f;
        characterController.skinWidth = 0.035f;

        GameObject rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rigInstance.transform.SetParent(player.transform, false);
        Animator rigAnimator = rigInstance.GetComponent<Animator>();
        if (rigAnimator != null) rigAnimator.runtimeAnimatorController = controller;
        HuntzArpg3DRig rig = rigInstance.GetComponent<HuntzArpg3DRig>();
        HuntzArpg3DAppearance appearance = rigInstance.GetComponent<HuntzArpg3DAppearance>();
        appearance.Configure(new[] { baseProfile, alternateProfile });
        player.AddComponent<HuntzArpg3DPlayerController>().Configure(rig, appearance, null);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        UnityEngine.Object.DestroyImmediate(player);
        return saved;
    }

    static Transform CreateArm(Transform hips, Transform chest, string side, float sign, Palette palette)
    {
        Transform shoulder = NewBone(side + "Shoulder", chest, new Vector3(sign * 0.31f, 0.17f, 0f));
        Transform upperArm = NewBone(side + "UpperArm", shoulder, new Vector3(sign * 0.11f, -0.04f, 0f));
        CreatePart(side + "UpperArmMesh", PrimitiveType.Capsule, upperArm, new Vector3(0f, -0.15f, 0f),
            new Vector3(0.15f, 0.16f, 0.15f), palette.cloth, HuntzArpg3DPartSlot.Cloth);
        Transform lowerArm = NewBone(side + "LowerArm", upperArm, new Vector3(0f, -0.3f, 0f));
        CreatePart(side + "ForearmMesh", PrimitiveType.Capsule, lowerArm, new Vector3(0f, -0.15f, 0f),
            new Vector3(0.13f, 0.16f, 0.13f), palette.leather, HuntzArpg3DPartSlot.Leather);
        Transform hand = NewBone(side + "Hand", lowerArm, new Vector3(0f, -0.29f, 0f));
        CreatePart(side + "HandMesh", PrimitiveType.Sphere, hand, Vector3.zero,
            new Vector3(0.14f, 0.12f, 0.14f), palette.skin, HuntzArpg3DPartSlot.Skin);
        return hand;
    }

    static void CreateLeg(Transform hips, string side, float sign, Palette palette)
    {
        Transform upperLeg = NewBone(side + "UpperLeg", hips, new Vector3(sign * 0.12f, -0.05f, 0f));
        CreatePart(side + "ThighMesh", PrimitiveType.Capsule, upperLeg, new Vector3(0f, -0.23f, 0f),
            new Vector3(0.18f, 0.25f, 0.18f), palette.cloth, HuntzArpg3DPartSlot.Cloth);
        Transform lowerLeg = NewBone(side + "LowerLeg", upperLeg, new Vector3(0f, -0.43f, 0f));
        CreatePart(side + "ShinMesh", PrimitiveType.Capsule, lowerLeg, new Vector3(0f, -0.2f, 0f),
            new Vector3(0.15f, 0.23f, 0.15f), palette.leather, HuntzArpg3DPartSlot.Leather);
        Transform foot = NewBone(side + "Foot", lowerLeg, new Vector3(0f, -0.42f, 0.07f));
        CreatePart(side + "HeelBootMesh", PrimitiveType.Cube, foot, new Vector3(0f, -0.04f, 0.015f),
            new Vector3(0.2f, 0.1f, 0.2f), palette.leather, HuntzArpg3DPartSlot.Leather);
        Transform toeBase = NewBone(side + "ToeBase", foot, new Vector3(0f, -0.02f, 0.12f));
        CreatePart(side + "ForefootBootMesh", PrimitiveType.Cube, toeBase, new Vector3(0f, -0.02f, 0.065f),
            new Vector3(0.2f, 0.085f, 0.17f), palette.leather, HuntzArpg3DPartSlot.Leather);
    }

    static HumanDescription CreateHumanDescription()
    {
        return new HumanDescription
        {
            human = new[]
            {
                CreateHumanBone(HumanBodyBones.Hips, "Hips"),
                CreateHumanBone(HumanBodyBones.LeftUpperLeg, "LeftUpperLeg"),
                CreateHumanBone(HumanBodyBones.RightUpperLeg, "RightUpperLeg"),
                CreateHumanBone(HumanBodyBones.LeftLowerLeg, "LeftLowerLeg"),
                CreateHumanBone(HumanBodyBones.RightLowerLeg, "RightLowerLeg"),
                CreateHumanBone(HumanBodyBones.LeftFoot, "LeftFoot"),
                CreateHumanBone(HumanBodyBones.RightFoot, "RightFoot"),
                CreateHumanBone(HumanBodyBones.LeftToes, "LeftToeBase"),
                CreateHumanBone(HumanBodyBones.RightToes, "RightToeBase"),
                CreateHumanBone(HumanBodyBones.Spine, "Spine"),
                CreateHumanBone(HumanBodyBones.Chest, "Chest"),
                CreateHumanBone(HumanBodyBones.Neck, "Neck"),
                CreateHumanBone(HumanBodyBones.Head, "Head"),
                CreateHumanBone(HumanBodyBones.LeftShoulder, "LeftShoulder"),
                CreateHumanBone(HumanBodyBones.RightShoulder, "RightShoulder"),
                CreateHumanBone(HumanBodyBones.LeftUpperArm, "LeftUpperArm"),
                CreateHumanBone(HumanBodyBones.RightUpperArm, "RightUpperArm"),
                CreateHumanBone(HumanBodyBones.LeftLowerArm, "LeftLowerArm"),
                CreateHumanBone(HumanBodyBones.RightLowerArm, "RightLowerArm"),
                CreateHumanBone(HumanBodyBones.LeftHand, "LeftHand"),
                CreateHumanBone(HumanBodyBones.RightHand, "RightHand")
            },
            skeleton = new SkeletonBone[0],
            armStretch = 0.05f,
            legStretch = 0.05f,
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            feetSpacing = 0.1f,
            hasTranslationDoF = false,
            rootMotionBoneName = string.Empty
        };
    }

    static HumanBone CreateHumanBone(HumanBodyBones humanBone, string transformName)
    {
        return new HumanBone
        {
            humanName = HumanTrait.BoneName[(int)humanBone],
            boneName = transformName,
            limit = new HumanLimit { useDefaultValues = true }
        };
    }

    static void CreateSword(Transform socket, Palette palette)
    {
        GameObject sword = new GameObject("Starter Sword");
        sword.transform.SetParent(socket, false);
        sword.transform.localRotation = Quaternion.Euler(125f, 0f, 0f);

        CreatePart("Blade", PrimitiveType.Cube, sword.transform, new Vector3(0f, 0.36f, 0f),
            new Vector3(0.16f, 0.56f, 0.05f), palette.metal, HuntzArpg3DPartSlot.Metal);
        CreatePart("BladeShoulder", PrimitiveType.Cube, sword.transform, new Vector3(0f, 0.62f, 0f),
            new Vector3(0.13f, 0.14f, 0.045f), palette.metal, HuntzArpg3DPartSlot.Metal);
        GameObject bladePoint = CreatePart("BladePoint", PrimitiveType.Cube, sword.transform,
            new Vector3(0f, 0.735f, 0f), new Vector3(0.13f, 0.13f, 0.04f),
            palette.metal, HuntzArpg3DPartSlot.Metal);
        bladePoint.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        CreatePart("GuardCore", PrimitiveType.Cube, sword.transform, new Vector3(0f, 0.015f, 0f),
            new Vector3(0.15f, 0.065f, 0.11f), palette.accent, HuntzArpg3DPartSlot.Accent);
        GameObject leftGuard = CreatePart("GuardLeft", PrimitiveType.Cube, sword.transform,
            new Vector3(-0.12f, 0.035f, 0f), new Vector3(0.15f, 0.06f, 0.1f),
            palette.accent, HuntzArpg3DPartSlot.Accent);
        leftGuard.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject rightGuard = CreatePart("GuardRight", PrimitiveType.Cube, sword.transform,
            new Vector3(0.12f, 0.035f, 0f), new Vector3(0.15f, 0.06f, 0.1f),
            palette.accent, HuntzArpg3DPartSlot.Accent);
        rightGuard.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

        CreatePart("Grip", PrimitiveType.Cylinder, sword.transform, new Vector3(0f, -0.105f, 0f),
            new Vector3(0.065f, 0.12f, 0.065f), palette.leather, HuntzArpg3DPartSlot.Leather);
        CreatePart("GripBandUpper", PrimitiveType.Cylinder, sword.transform, new Vector3(0f, -0.045f, 0f),
            new Vector3(0.07f, 0.012f, 0.07f), palette.metal, HuntzArpg3DPartSlot.Metal);
        CreatePart("GripBandLower", PrimitiveType.Cylinder, sword.transform, new Vector3(0f, -0.165f, 0f),
            new Vector3(0.07f, 0.012f, 0.07f), palette.metal, HuntzArpg3DPartSlot.Metal);
        CreatePart("Pommel", PrimitiveType.Sphere, sword.transform, new Vector3(0f, -0.245f, 0f),
            new Vector3(0.11f, 0.11f, 0.11f), palette.accent, HuntzArpg3DPartSlot.Accent);

        Transform bladeBase = NewBone("BladeBase", sword.transform, new Vector3(0f, 0.065f, 0f));
        Transform bladeTip = NewBone("BladeTip", sword.transform, new Vector3(0f, 0.82f, 0f));
        sword.AddComponent<HuntzArpg3DWeaponGeometry>().Configure(bladeBase, bladeTip, 0.2f);
    }

    static Transform NewBone(string name, Transform parent, Vector3 localPosition)
    {
        GameObject bone = new GameObject(name);
        bone.transform.SetParent(parent, false);
        bone.transform.localPosition = localPosition;
        return bone.transform;
    }

    static GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material, HuntzArpg3DPartSlot slot)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        Renderer renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        part.AddComponent<HuntzArpg3DRigPart>().Configure(slot, renderer);
        return part;
    }

    static void BuildLightingAndCamera(out HuntzArpg3DCameraFollow cameraFollow)
    {
        RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.52f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.color = new Color(1f, 0.91f, 0.78f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        GameObject cameraObject = new GameObject("Isometric ARPG Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.075f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.orthographic = true;
        camera.orthographicSize = 10f;
        cameraObject.AddComponent<AudioListener>();
        cameraFollow = cameraObject.AddComponent<HuntzArpg3DCameraFollow>();
    }

    static Transform CreateArrivalMarker(Transform parent, string name, Vector3 position)
    {
        GameObject marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        marker.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        return marker.transform;
    }

    static void BuildMainland(Transform root, Transform island, GameObject rigPrefab,
        HuntzArpg3DAppearanceProfile baseProfile, HuntzArpg3DAppearanceProfile alternateProfile,
        Palette palette, GameObject player, Transform mainArrival, Transform huntArrival)
    {
        CreateHouse(island, "Casa do Mercado", new Vector3(-7f, 0f, -4f), palette,
            new Color(0.28f, 0.36f, 0.47f));
        CreateHouse(island, "Oficina", new Vector3(-7f, 0f, 0.3f), palette,
            new Color(0.47f, 0.31f, 0.2f));
        CreateTree(island, new Vector3(7.5f, 0f, -3.2f), palette);
        CreateTree(island, new Vector3(7.8f, 0f, 1.6f), palette);
        CreateWell(island, new Vector3(1.5f, 0f, -4f), palette);
        CreateRockCluster(island, new Vector3(8.5f, 0f, -6f), palette);

        CreateVillager(island, rigPrefab, "Ana — Mercadora", new Vector3(-4.6f, 0f, -4f),
            alternateProfile, "Serviços da Guilda", "A loja ainda está em construção no piloto.");
        CreateVillager(island, rigPrefab, "Bruno — Ferreiro", new Vector3(-4.5f, 0f, 0.4f),
            baseProfile, "Oficina", "Equipamentos e melhorias serão conectados a este NPC.");

        CreatePortal(island, "Portal HZ — Hunt de Treino", new Vector3(9.5f, 0f, 5.1f), huntArrival,
            "Hunt de Treino", palette, new Color(0.22f, 0.69f, 0.95f));
        CreatePlayerMarker(island, player.transform.position, "Ponto inicial da MainLand", palette);
        CreateVillageRoute(island, player.transform.position, new Vector3(9.5f, 0f, 5.1f), palette);
        CreateTownBeacon(island, new Vector3(3.8f, 0f, 4.1f), palette);
        BuildMainlandObstacles(island, palette);
    }

    static void BuildTrainingHunt(Transform root, Transform island, GameObject rigPrefab,
        HuntzArpg3DAppearanceProfile baseProfile, HuntzArpg3DAppearanceProfile alternateProfile,
        Palette palette, GameObject player, Transform mainArrival, Transform huntArrival)
    {
        Vector3 center = island.position;
        CreateHouse(island, "Acampamento de Caça", new Vector3(-6.5f, 0f, -4.4f), palette,
            new Color(0.26f, 0.31f, 0.34f));
        CreatePortal(island, "Portal de Retorno", new Vector3(7.3f, 0f, 5.1f), mainArrival,
            "MainLand", palette, new Color(0.78f, 0.42f, 0.9f));
        CreateVillager(island, rigPrefab, "Vigia da Guilda", new Vector3(-4.6f, 0f, -4.3f),
            alternateProfile, "Vigia", "A área é um campo de treino para combate e leitura de espaço.");

        CreateCombatTarget(island, "Ash Slime — Treino", new Vector3(2.5f, 0f, 1.2f),
            new Color(0.71f, 0.22f, 0.23f), 3, 7f, 1.7f, 1);
        CreateCombatTarget(island, "Ash Slime — Treino", new Vector3(6.1f, 0f, -1.3f),
            new Color(0.75f, 0.27f, 0.3f), 3, 7f, 1.7f, 1);
        CreateCombatTarget(island, "Guardião de Treino", new Vector3(-0.4f, 0f, 4.1f),
            new Color(0.44f, 0.37f, 0.63f), 8, 0f, 0f, 0);

        CreateTrainingObstacle(island, "Pilar de Pedra", new Vector3(-1.3f, 0f, -0.1f), new Vector3(1.2f, 2.7f, 1.2f), palette.rock);
        CreateTrainingObstacle(island, "Barreira de Madeira A", new Vector3(4f, 0f, 3.4f), new Vector3(2.6f, 0.9f, 0.35f), palette.wood);
        CreateTrainingObstacle(island, "Barreira de Madeira B", new Vector3(7.1f, 0f, 2.2f), new Vector3(0.35f, 0.9f, 2.2f), palette.wood);
        CreateGroundMarker(island, new Vector3(0f, 0.02f, 0f), new Color(0.84f, 0.26f, 0.21f));
    }

    static void BuildMainlandObstacles(Transform island, Palette palette)
    {
        CreateTrainingObstacle(island, "Caixa do Mercado", new Vector3(-9f, 0f, -1.5f), new Vector3(1f, 1f, 0.8f), palette.wood);
        CreateTrainingObstacle(island, "Pedra da Borda", new Vector3(3.8f, 0f, 7.2f), new Vector3(1.5f, 1.2f, 1.1f), palette.rock);
        CreateTrainingObstacle(island, "Pedra da Borda 2", new Vector3(-1f, 0f, 7.6f), new Vector3(1.1f, 0.8f, 0.9f), palette.rock);
    }

    static void CreateHouse(Transform parent, string name, Vector3 position, Palette palette, Color color)
    {
        Material wall = CreateMaterial("Graybox_" + Sanitize(name) + "_Wall", color);
        GameObject house = new GameObject(name);
        house.transform.SetParent(parent, false);
        house.transform.localPosition = position;
        CreateSolidPrimitive("Body", PrimitiveType.Cube, house.transform, new Vector3(0f, 0.95f, 0f),
            new Vector3(3.3f, 1.9f, 2.4f), wall, true);
        CreateSolidPrimitive("Telhado lado norte", PrimitiveType.Cube, house.transform, new Vector3(0f, 2.05f, -0.62f),
            new Vector3(3.75f, 0.24f, 1.62f), palette.rock, false).transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
        CreateSolidPrimitive("Telhado lado sul", PrimitiveType.Cube, house.transform, new Vector3(0f, 2.05f, 0.62f),
            new Vector3(3.75f, 0.24f, 1.62f), palette.rock, false).transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
        CreateSolidPrimitive("Cumeeira", PrimitiveType.Cube, house.transform, new Vector3(0f, 2.4f, 0f),
            new Vector3(3.85f, 0.18f, 0.2f), palette.wood, false);
        CreateSolidPrimitive("Fundacao", PrimitiveType.Cube, house.transform, new Vector3(0f, 0.13f, 0f),
            new Vector3(3.48f, 0.26f, 2.55f), palette.rock, false);
        CreateSolidPrimitive("Porta", PrimitiveType.Cube, house.transform, new Vector3(0f, 0.62f, 1.22f),
            new Vector3(0.7f, 1.25f, 0.08f), palette.wood, false);
        CreateSolidPrimitive("Janela esquerda", PrimitiveType.Cube, house.transform, new Vector3(-1.05f, 1.15f, 1.23f),
            new Vector3(0.56f, 0.54f, 0.07f), palette.glass, false);
        CreateSolidPrimitive("Janela direita", PrimitiveType.Cube, house.transform, new Vector3(1.05f, 1.15f, 1.23f),
            new Vector3(0.56f, 0.54f, 0.07f), palette.glass, false);
        CreateSolidPrimitive("Placa da loja", PrimitiveType.Cube, house.transform, new Vector3(0f, 1.76f, 1.27f),
            new Vector3(1.05f, 0.34f, 0.12f), palette.accent, false);
    }

    static void CreateVillageRoute(Transform parent, Vector3 start, Vector3 end, Palette palette)
    {
        GameObject plaza = CreateSolidPrimitive("Praca central", PrimitiveType.Cylinder, parent,
            new Vector3(start.x, 0.035f, start.z), new Vector3(2.9f, 0.045f, 2.35f), palette.path, false);
        plaza.transform.localRotation = Quaternion.Euler(0f, -12f, 0f);

        Vector3 direction = end - start;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        int stoneCount = Mathf.CeilToInt(length / 0.9f);
        for (int i = 0; i <= stoneCount; i++)
        {
            Vector3 position = Vector3.Lerp(start, end, i / (float)stoneCount);
            float stagger = (i % 2 == 0 ? -0.12f : 0.12f);
            position += Quaternion.Euler(0f, angle, 0f) * new Vector3(stagger, 0f, 0f);
            GameObject stone = CreateSolidPrimitive("Lajota da trilha", PrimitiveType.Cube, parent,
                new Vector3(position.x, 0.04f, position.z), new Vector3(0.98f, 0.08f, 0.72f), palette.path, false);
            stone.transform.localRotation = Quaternion.Euler(0f, angle + (i % 3 - 1) * 4f, 0f);
        }
    }

    static void CreateTownBeacon(Transform parent, Vector3 position, Palette palette)
    {
        GameObject beacon = new GameObject("Marco astral da vila");
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = position;
        CreateSolidPrimitive("Base de pedra", PrimitiveType.Cylinder, beacon.transform,
            new Vector3(0f, 0.18f, 0f), new Vector3(1.35f, 0.18f, 1.35f), palette.rock, true);
        CreateSolidPrimitive("Pedestal", PrimitiveType.Cube, beacon.transform,
            new Vector3(0f, 0.72f, 0f), new Vector3(0.72f, 0.95f, 0.72f), palette.metal, true);
        GameObject core = CreateSolidPrimitive("Cristal guia", PrimitiveType.Cube, beacon.transform,
            new Vector3(0f, 1.65f, 0f), new Vector3(0.72f, 1.05f, 0.72f), palette.crystal, false);
        core.transform.localRotation = Quaternion.Euler(45f, 18f, 45f);
        CreateSolidPrimitive("Anel do pedestal", PrimitiveType.Cylinder, beacon.transform,
            new Vector3(0f, 1.05f, 0f), new Vector3(0.8f, 0.07f, 0.8f), palette.accent, false);
    }

    static void CreateTree(Transform parent, Vector3 position, Palette palette)
    {
        GameObject tree = new GameObject("Árvore de Graybox");
        tree.transform.SetParent(parent, false);
        tree.transform.localPosition = position;
        CreateSolidPrimitive("Tronco", PrimitiveType.Cylinder, tree.transform, new Vector3(0f, 0.72f, 0f),
            new Vector3(0.32f, 0.72f, 0.32f), palette.wood, true);
        CreateSolidPrimitive("Copa", PrimitiveType.Sphere, tree.transform, new Vector3(0f, 2f, 0f),
            new Vector3(1.45f, 1.75f, 1.45f), palette.leaf, false);
    }

    static void CreateWell(Transform parent, Vector3 position, Palette palette)
    {
        GameObject well = new GameObject("Poço — Graybox");
        well.transform.SetParent(parent, false);
        well.transform.localPosition = position;
        CreateSolidPrimitive("Base", PrimitiveType.Cylinder, well.transform, new Vector3(0f, 0.42f, 0f),
            new Vector3(1.45f, 0.42f, 1.45f), palette.rock, true);
        CreateSolidPrimitive("Pilar A", PrimitiveType.Cube, well.transform, new Vector3(-0.48f, 1.25f, 0f),
            new Vector3(0.18f, 1.55f, 0.18f), palette.wood, false);
        CreateSolidPrimitive("Pilar B", PrimitiveType.Cube, well.transform, new Vector3(0.48f, 1.25f, 0f),
            new Vector3(0.18f, 1.55f, 0.18f), palette.wood, false);
        CreateSolidPrimitive("Travessa", PrimitiveType.Cube, well.transform, new Vector3(0f, 2.06f, 0f),
            new Vector3(1.25f, 0.18f, 0.18f), palette.wood, false);
    }

    static void CreateRockCluster(Transform parent, Vector3 position, Palette palette)
    {
        GameObject cluster = new GameObject("Rochas — Graybox");
        cluster.transform.SetParent(parent, false);
        cluster.transform.localPosition = position;
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2) * 0.7f;
            float z = (i / 2) * 0.6f;
            GameObject rock = CreateSolidPrimitive("Rocha " + (i + 1), PrimitiveType.Sphere, cluster.transform,
                new Vector3(x, 0.4f + 0.08f * i, z), new Vector3(0.7f, 0.8f + 0.12f * i, 0.66f), palette.rock, true);
            rock.transform.localRotation = Quaternion.Euler(i * 9f, i * 27f, i * 13f);
        }
    }

    static void CreateVillager(Transform parent, GameObject rigPrefab, string title, Vector3 localPosition,
        HuntzArpg3DAppearanceProfile profile, string interactionTitle, string interactionText)
    {
        GameObject villager = new GameObject(title);
        villager.transform.SetParent(parent, false);
        villager.transform.localPosition = localPosition;
        CapsuleCollider collider = villager.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 0.92f, 0f);
        collider.height = 1.85f;
        collider.radius = 0.4f;

        GameObject rigInstance = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
        rigInstance.name = "Rig — " + title;
        rigInstance.transform.SetParent(villager.transform, false);
        HuntzArpg3DAppearance appearance = rigInstance.GetComponent<HuntzArpg3DAppearance>();
        appearance.Configure(new[] { profile });
        Transform socket = rigInstance.GetComponent<HuntzArpg3DRig>().WeaponSocket;
        if (socket != null && socket.Find("Starter Sword") != null)
            socket.Find("Starter Sword").gameObject.SetActive(false);

        SphereCollider interactionRange = villager.AddComponent<SphereCollider>();
        interactionRange.isTrigger = true;
        interactionRange.center = new Vector3(0f, 0.9f, 0f);
        interactionRange.radius = 2.6f;
        villager.AddComponent<HuntzArpg3DInteractable>().Configure(interactionTitle, interactionText);
    }

    static void CreatePortal(Transform parent, string name, Vector3 localPosition, Transform destination,
        string destinationName, Palette palette, Color portalColor)
    {
        Material glow = CreateMaterial("Graybox_" + Sanitize(name) + "_Portal", portalColor, 0.15f);
        GameObject portal = new GameObject(name);
        portal.transform.SetParent(parent, false);
        portal.transform.localPosition = localPosition;
        CreateSolidPrimitive("Pilar Esquerdo", PrimitiveType.Cube, portal.transform,
            new Vector3(-0.85f, 1.2f, 0f), new Vector3(0.3f, 2.4f, 0.4f), palette.metal, false);
        CreateSolidPrimitive("Pilar Direito", PrimitiveType.Cube, portal.transform,
            new Vector3(0.85f, 1.2f, 0f), new Vector3(0.3f, 2.4f, 0.4f), palette.metal, false);
        CreateSolidPrimitive("Superficie do portal", PrimitiveType.Sphere, portal.transform,
            new Vector3(0f, 1.22f, -0.08f), new Vector3(1.34f, 2.12f, 0.12f),
            CreateEmissiveMaterial("Graybox_" + Sanitize(name) + "_RiftSurface", portalColor, 0.9f), false);
        CreateSolidPrimitive("Topo", PrimitiveType.Cube, portal.transform,
            new Vector3(0f, 2.35f, 0f), new Vector3(2f, 0.32f, 0.4f), palette.accent, false);
        CreateSolidPrimitive("Limiar", PrimitiveType.Cube, portal.transform,
            new Vector3(0f, 0.05f, 0f), new Vector3(1.5f, 0.08f, 1f), glow, false);
        SphereCollider range = portal.AddComponent<SphereCollider>();
        range.center = new Vector3(0f, 1f, 0f);
        range.radius = 2.4f;
        range.isTrigger = true;
        portal.AddComponent<HuntzArpg3DInteractable>().Configure(name, "Pressione E para atravessar.", destination, destinationName);
    }

    static void CreateCombatTarget(Transform parent, string name, Vector3 localPosition, Color color,
        int health, float aggroRange, float chaseSpeed, int contactDamage)
    {
        GameObject target = new GameObject(name);
        target.transform.SetParent(parent, false);
        target.transform.localPosition = localPosition;
        CharacterController controller = null;
        if (aggroRange > 0f)
        {
            controller = target.AddComponent<CharacterController>();
            controller.height = 1.2f;
            controller.radius = 0.47f;
            controller.center = new Vector3(0f, 0.58f, 0f);
            controller.stepOffset = 0.18f;
            controller.skinWidth = 0.03f;
        }
        else
        {
            CapsuleCollider capsule = target.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.48f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
        }

        GameObject body = CreateSolidPrimitive("Corpo", aggroRange > 0f ? PrimitiveType.Sphere : PrimitiveType.Capsule,
            target.transform, new Vector3(0f, aggroRange > 0f ? 0.58f : 0.9f, 0f),
            aggroRange > 0f ? new Vector3(0.98f, 0.85f, 0.92f) : new Vector3(0.9f, 0.9f, 0.8f),
            CreateMaterial("Graybox_" + Sanitize(name), color), false);
        if (aggroRange > 0f)
        {
            CreateSolidPrimitive("Núcleo", PrimitiveType.Sphere, target.transform,
                new Vector3(0f, 0.7f, 0.32f), new Vector3(0.22f, 0.2f, 0.12f),
                CreateMaterial("Graybox_SlimeCore", new Color(0.96f, 0.7f, 0.38f)), false);
        }

        HuntzArpg3DTarget combat = target.AddComponent<HuntzArpg3DTarget>();
        combat.Configure(name, health, aggroRange, chaseSpeed, 1.25f, contactDamage,
            target.GetComponentsInChildren<Renderer>());
    }

    static void CreateTrainingObstacle(Transform parent, string name, Vector3 localPosition,
        Vector3 scale, Material material)
    {
        CreateSolidPrimitive(name, PrimitiveType.Cube, parent, localPosition + new Vector3(0f, scale.y * 0.5f, 0f),
            scale, material, true);
    }

    static void CreateGroundMarker(Transform parent, Vector3 localPosition, Color color)
    {
        Material marker = CreateMaterial("Graybox_TargetMarker", color);
        GameObject ring = CreateSolidPrimitive("Marca de Combate", PrimitiveType.Cylinder, parent,
            localPosition + new Vector3(0f, 0.025f, 0f), new Vector3(0.85f, 0.025f, 0.85f), marker, false);
    }

    static void CreatePlayerMarker(Transform parent, Vector3 position, string name, Palette palette)
    {
        GameObject marker = CreateSolidPrimitive(name, PrimitiveType.Cylinder, parent,
            position + new Vector3(0f, 0.015f, 0f), new Vector3(0.9f, 0.015f, 0.9f), palette.accent, false);
    }

    static GameObject CreateSolidPrimitive(string name, PrimitiveType type, Transform parent,
        Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        primitive.name = name;
        if (!keepCollider)
        {
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        }
        primitive.transform.SetParent(parent, false);
        primitive.transform.localPosition = localPosition;
        primitive.transform.localScale = localScale;
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return primitive;
    }

    static string Sanitize(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c.ToString(), string.Empty);
        return value.Replace(' ', '_').Replace('—', '_');
    }
}
