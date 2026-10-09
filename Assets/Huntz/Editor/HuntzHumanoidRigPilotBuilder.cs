using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D.Animation;

public static class HuntzHumanoidRigPilotBuilder
{
    const string Root = "Assets/Huntz/Prototype/HumanoidRigPilot";
    const string ArtFolder = Root + "/Placeholder";
    const string AnimationFolder = Root + "/Animation";
    const string ClipFolder = AnimationFolder + "/Clips";
    const string ProfileFolder = Root + "/Appearance";
    const string PrefabFolder = Root + "/Prefabs";
    const string SceneFolder = Root + "/Scenes";
    const string PixelPath = ArtFolder + "/PlaceholderPixel.asset";
    const string DiamondPath = ArtFolder + "/GrayboxDiamond.asset";
    const string LibraryPath = ArtFolder + "/HumanoidRigPilotLibrary.asset";
    const string ControllerPath = AnimationFolder + "/HumanoidRigPilot.controller";
    const string BaseProfilePath = ProfileFolder + "/BaseWarm.asset";
    const string AlternateProfilePath = ProfileFolder + "/AlternateCool.asset";
    const string PrefabPath = PrefabFolder + "/HumanoidRigPilot.prefab";
    const string ScenePath = SceneFolder + "/HumanoidRigPilot.unity";

    static readonly string[] DirectionNames =
    {
        "South", "SouthWest", "West", "NorthWest",
        "North", "NorthEast", "East", "SouthEast"
    };

    static readonly HuntzHumanoidRigAction[] Actions =
    {
        HuntzHumanoidRigAction.Idle,
        HuntzHumanoidRigAction.Walk,
        HuntzHumanoidRigAction.Attack,
        HuntzHumanoidRigAction.Dodge,
        HuntzHumanoidRigAction.Hurt,
        HuntzHumanoidRigAction.Death
    };

    [MenuItem("Huntz/Prototype/Create Humanoid Rig Pilot")]
    public static void Build()
    {
        if (AssetDatabase.IsValidFolder(Root) || File.Exists(PrefabPath) || File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Humanoid Rig Pilot",
                "A pasta do piloto já existe. Os arquivos existentes foram preservados.", "OK");
            return;
        }

        CreateFolders();
        Sprite placeholder = CreatePlaceholderSprite();
        SpriteLibraryAsset libraryAsset = CreateSpriteLibrary(placeholder);
        HuntzHumanoidAppearanceProfile baseProfile = CreateProfile(
            BaseProfilePath, "Base quente", libraryAsset, "Base",
            new Color(0.96f, 0.69f, 0.47f), new Color(0.16f, 0.12f, 0.15f),
            new Color(0.16f, 0.42f, 0.48f), new Color(0.48f, 0.27f, 0.16f));
        HuntzHumanoidAppearanceProfile alternateProfile = CreateProfile(
            AlternateProfilePath, "Paleta fria", libraryAsset, "Alternate",
            new Color(0.70f, 0.78f, 0.91f), new Color(0.25f, 0.32f, 0.46f),
            new Color(0.31f, 0.35f, 0.58f), new Color(0.34f, 0.40f, 0.48f));
        AnimatorController controller = CreateController();
        AssetDatabase.SaveAssets();

        GameObject actor = BuildActor(placeholder, libraryAsset, baseProfile, alternateProfile, controller);
        PrefabUtility.SaveAsPrefabAsset(actor, PrefabPath);
        UnityEngine.Object.DestroyImmediate(actor);
        AssetDatabase.SaveAssets();

        BuildShowcaseScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[Huntz] Humanoid Rig Pilot created outside Build Settings: " + ScenePath);
        EditorUtility.RevealInFinder(ScenePath);
    }

    [MenuItem("Huntz/Prototype/Build Graybox Pilot Environment")]
    public static void BuildGrayboxEnvironment()
    {
        if (!AssetDatabase.IsValidFolder(Root) || !File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("Humanoid Rig Pilot",
                "Crie primeiro o Humanoid Rig Pilot. A cena atual e os arquivos existentes serão preservados.", "OK");
            return;
        }

        Sprite diamond = AssetDatabase.LoadAssetAtPath<Sprite>(DiamondPath);
        if (diamond == null) diamond = CreateDiamondSprite();
        Sprite pixel = AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
        UpdatePrefabBodyPart(pixel);
        AssetDatabase.SaveAssets();

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene pilotScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(pilotScene);
            RemoveGeneratedRoot(pilotScene, "Generated Graybox Environment");
            GameObject environment = new GameObject("Generated Graybox Environment");
            SceneManager.MoveGameObjectToScene(environment, pilotScene);

            BuildIsland(environment.transform, "Mainland Graybox", diamond, Vector2.zero, 10);
            BuildIsland(environment.transform, "Rift Hunt Graybox", diamond, new Vector2(16f, 0f), 8);
            BuildMainlandDetails(environment.transform, pilotScene, pixel);
            BuildHuntDetails(environment.transform, pixel);

            GameObject actor = FindRoot(pilotScene, "Humanoid Rig Pilot");
            GameObject cameraObject = FindRoot(pilotScene, "Rig Pilot Camera");
            RemoveGeneratedRoot(pilotScene, "Placeholder Ground");
            if (actor != null)
            {
                actor.transform.position = new Vector3(0f, 2.5f, 0f);
                HuntzRigPilotController player = actor.GetComponent<HuntzRigPilotController>();
                if (player != null) player.enabled = true;
            }

            if (cameraObject != null)
            {
                cameraObject.transform.position = new Vector3(0f, 2.5f, -10f);
                Camera camera = cameraObject.GetComponent<Camera>();
                if (camera != null) camera.orthographicSize = 3.4f;
                HuntzRigPilotCameraFollow follow = cameraObject.GetComponent<HuntzRigPilotCameraFollow>();
                if (follow == null) follow = cameraObject.AddComponent<HuntzRigPilotCameraFollow>();
                if (actor != null) follow.Configure(actor.transform);
            }

            EditorSceneManager.MarkSceneDirty(pilotScene);
            EditorSceneManager.SaveScene(pilotScene, ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(pilotScene, true);
            if (previousActiveScene.IsValid()) SceneManager.SetActiveScene(previousActiveScene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[Huntz] Graybox environment added to the isolated rig pilot; Build Settings and the vertical slice were not changed.");
    }

    static Sprite CreateDiamondSprite()
    {
        const int width = 64;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Graybox Isometric Diamond",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float dx = Mathf.Abs((x + 0.5f - width * 0.5f) / (width * 0.5f));
            float dy = Mathf.Abs((y + 0.5f - height * 0.5f) / (height * 0.5f));
            if (dx + dy <= 1f) pixels[y * width + x] = new Color32(255, 255, 255, 255);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, DiamondPath);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 64f,
            0, SpriteMeshType.FullRect);
        sprite.name = "Graybox Isometric Diamond";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        EditorUtility.SetDirty(texture);
        return sprite;
    }

    static void UpdatePrefabBodyPart(Sprite placeholder)
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform pelvis = prefabContents.transform.Find("Rig/Pelvis");
            if (pelvis == null || pelvis.Find("BodySprite") != null) return;
            AddPart(pelvis, "BodySprite", HuntzHumanoidPartSlot.Body, placeholder,
                new Vector2(10f, 9f), new Vector3(0f, 0.015f, 0.015f),
                new Color(0.96f, 0.69f, 0.47f), 0, 0);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    static void BuildIsland(Transform parent, string name, Sprite diamond, Vector2 offset, int maxGrid)
    {
        GameObject island = new GameObject(name);
        island.transform.SetParent(parent, false);

        float centerY = maxGrid * 0.25f;
        float halfWidth = (maxGrid + 1f) * 0.5f;
        float halfHeight = (maxGrid + 1f) * 0.25f;
        GameObject baseObject = new GameObject("Floating Rock Base");
        baseObject.transform.SetParent(island.transform, false);
        baseObject.transform.localPosition = new Vector3(offset.x, centerY - 0.16f, 0.65f);
        baseObject.transform.localScale = new Vector3(maxGrid + 1.25f, maxGrid + 1.25f, 1f);
        SpriteRenderer baseRenderer = baseObject.AddComponent<SpriteRenderer>();
        baseRenderer.sprite = diamond;
        baseRenderer.color = new Color(0.10f, 0.14f, 0.20f);
        baseRenderer.sortingOrder = -20;

        GameObject tileRoot = new GameObject("Isometric Walkable Tiles");
        tileRoot.transform.SetParent(island.transform, false);
        for (int gx = 0; gx <= maxGrid; gx++)
        for (int gy = 0; gy <= maxGrid; gy++)
        {
            GameObject tile = new GameObject($"Tile_{gx:00}_{gy:00}");
            tile.transform.SetParent(tileRoot.transform, false);
            tile.transform.localPosition = new Vector3(offset.x + (gx - gy) * 0.5f,
                (gx + gy) * 0.25f, 0.4f);
            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = diamond;
            bool alternate = ((gx + gy) & 1) == 0;
            renderer.color = alternate
                ? new Color(0.24f, 0.32f, 0.36f)
                : new Color(0.20f, 0.28f, 0.33f);
            renderer.sortingOrder = -15;
        }

        GameObject boundary = new GameObject("Walkable Area Boundary");
        boundary.transform.SetParent(island.transform, false);
        boundary.transform.localPosition = new Vector3(offset.x, centerY, 0f);
        Rigidbody2D body = boundary.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;
        EdgeCollider2D edge = boundary.AddComponent<EdgeCollider2D>();
        edge.edgeRadius = 0.035f;
        edge.points = new[]
        {
            new Vector2(0f, -halfHeight), new Vector2(halfWidth, 0f),
            new Vector2(0f, halfHeight), new Vector2(-halfWidth, 0f),
            new Vector2(0f, -halfHeight)
        };
    }

    static void BuildMainlandDetails(Transform parent, Scene scene, Sprite pixel)
    {
        AddSolidProp(parent, pixel, "Market Blockout", new Vector2(-0.8f, 1.2f),
            new Vector2(0.9f, 0.65f), new Color(0.48f, 0.36f, 0.28f));
        AddSolidProp(parent, pixel, "Workshop Blockout", new Vector2(1.55f, 3.25f),
            new Vector2(0.85f, 0.62f), new Color(0.34f, 0.39f, 0.46f));
        AddSolidProp(parent, pixel, "Rift Rock A", new Vector2(-2.0f, 3.25f),
            new Vector2(0.42f, 0.30f), new Color(0.30f, 0.34f, 0.38f));
        AddSolidProp(parent, pixel, "Rift Rock B", new Vector2(2.4f, 1.55f),
            new Vector2(0.38f, 0.26f), new Color(0.28f, 0.32f, 0.36f));

        SpawnPilotNpc(scene, "Ana — NPC Blockout", new Vector2(-1.7f, 1.85f),
            "Falar com Ana", "Ana: O portal leva à área de treino. Esta vila será expandida em etapas.", false);
        SpawnPilotNpc(scene, "Bruno — NPC Blockout", new Vector2(2.15f, 3.25f),
            "Falar com Bruno", "Bruno: As oficinas e lojas terão serviços próprios em um próximo incremento.", true);

        AddPortal(parent, pixel, "Rift Portal — Mainland", new Vector2(-1.45f, 3.20f),
            new Vector2(16f, 2f), "Entrar no portal de treino", "Área de treino: ataque com J; o impacto usa o evento da animação.");
        AddInteractable(parent, "Shop Marker — Market", new Vector2(-0.8f, 0.72f),
            "Ver mercado", "Mercado em graybox: colisão e ponto de interação prontos para receber a loja.", Vector2.zero, false);
        AddInteractable(parent, "Shop Marker — Workshop", new Vector2(1.55f, 2.77f),
            "Ver oficina", "Oficina em graybox: ponto de interação pronto para receber os serviços.", Vector2.zero, false);

        AddSpaceStars(parent, pixel, new Vector2(-5.8f, 2.5f), 14, 17);
        AddSpaceStars(parent, pixel, new Vector2(5.6f, 2.4f), 12, 29);
    }

    static void BuildHuntDetails(Transform parent, Sprite pixel)
    {
        AddPortal(parent, pixel, "Rift Portal — Hunt Return", new Vector2(15f, 1.5f),
            new Vector2(0f, 2.5f), "Retornar à Mainland", "Retornando à Mainland.");
        AddCombatTarget(parent, pixel, "Ash Slime — Graybox", new Vector2(17.2f, 2.75f),
            new Color(0.75f, 0.32f, 0.36f), 2, true);
        AddCombatTarget(parent, pixel, "Rift Warden — Graybox", new Vector2(18.45f, 1.85f),
            new Color(0.72f, 0.52f, 0.28f), 4);
        AddSolidProp(parent, pixel, "Hunt Barrier A", new Vector2(16.55f, 3.65f),
            new Vector2(0.42f, 0.75f), new Color(0.32f, 0.37f, 0.43f));
        AddSolidProp(parent, pixel, "Hunt Barrier B", new Vector2(19.1f, 1.15f),
            new Vector2(0.42f, 0.75f), new Color(0.32f, 0.37f, 0.43f));
    }

    static void SpawnPilotNpc(Scene scene, string name, Vector2 position, string prompt, string response,
        bool alternateAppearance)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return;
        GameObject npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        npc.name = name;
        npc.transform.position = position;
        HuntzRigPilotController controller = npc.GetComponent<HuntzRigPilotController>();
        if (controller != null) controller.enabled = false;
        Rigidbody2D body = npc.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
        }

        HuntzHumanoidAppearance appearance = npc.GetComponent<HuntzHumanoidAppearance>();
        if (alternateAppearance && appearance != null)
        {
            HuntzHumanoidAppearanceProfile profile = AssetDatabase.LoadAssetAtPath<HuntzHumanoidAppearanceProfile>(AlternateProfilePath);
            appearance.Apply(profile);
        }

        GameObject range = new GameObject("Interaction Range");
        range.transform.SetParent(npc.transform, false);
        CircleCollider2D trigger = range.AddComponent<CircleCollider2D>();
        trigger.radius = 0.72f;
        trigger.isTrigger = true;
        range.AddComponent<HuntzRigPilotInteractable>().Configure(prompt, response, Vector2.zero, false);
    }

    static void AddPortal(Transform parent, Sprite pixel, string name, Vector2 position,
        Vector2 destination, string prompt, string response)
    {
        GameObject portal = new GameObject(name);
        portal.transform.SetParent(parent, false);
        portal.transform.localPosition = position;
        CircleCollider2D trigger = portal.AddComponent<CircleCollider2D>();
        trigger.radius = 0.68f;
        trigger.isTrigger = true;
        portal.AddComponent<HuntzRigPilotInteractable>().Configure(prompt, response, destination, true);
        AddVisualChild(portal.transform, pixel, "Rift Frame", new Vector2(0.18f, 0.62f),
            new Color(0.42f, 0.31f, 0.78f), -3);
        AddVisualChild(portal.transform, pixel, "Rift Core", new Vector2(0.08f, 0.42f),
            new Color(0.30f, 0.72f, 0.84f), -2);
    }

    static void AddInteractable(Transform parent, string name, Vector2 position, string prompt,
        string response, Vector2 destination, bool teleports)
    {
        GameObject target = new GameObject(name);
        target.transform.SetParent(parent, false);
        target.transform.localPosition = position;
        CircleCollider2D trigger = target.AddComponent<CircleCollider2D>();
        trigger.radius = 0.58f;
        trigger.isTrigger = true;
        target.AddComponent<HuntzRigPilotInteractable>().Configure(prompt, response, destination, teleports);
    }

    static void AddCombatTarget(Transform parent, Sprite pixel, string name, Vector2 position,
        Color color, int health, bool contactHazard = false)
    {
        GameObject target = new GameObject(name);
        target.transform.SetParent(parent, false);
        target.transform.localPosition = position;
        target.transform.localScale = new Vector3(0.38f * 64f, 0.34f * 64f, 1f);
        Rigidbody2D body = target.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        SpriteRenderer renderer = target.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.color = color;
        renderer.sortingOrder = -1;
        BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f / 64f, 1f / 64f);
        target.AddComponent<HuntzRigPilotCombatTarget>().Configure(health, contactHazard);
    }

    static void AddSolidProp(Transform parent, Sprite pixel, string name, Vector2 position,
        Vector2 size, Color color)
    {
        GameObject prop = new GameObject(name);
        prop.transform.SetParent(parent, false);
        prop.transform.localPosition = position;
        prop.transform.localScale = new Vector3(size.x * 64f, size.y * 64f, 1f);
        SpriteRenderer renderer = prop.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.color = color;
        renderer.sortingOrder = -2;
        BoxCollider2D collider = prop.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f / 64f, 1f / 64f);
    }

    static void AddVisualChild(Transform parent, Sprite pixel, string name, Vector2 size,
        Color color, int sortingOrder)
    {
        GameObject visual = new GameObject(name);
        visual.transform.SetParent(parent, false);
        visual.transform.localScale = new Vector3(size.x * 64f, size.y * 64f, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = pixel;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    static void AddSpaceStars(Transform parent, Sprite pixel, Vector2 center, int count, int seed)
    {
        UnityEngine.Random.State previousState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(seed);
        for (int i = 0; i < count; i++)
        {
            GameObject star = new GameObject("Graybox Star");
            star.transform.SetParent(parent, false);
            star.transform.localPosition = new Vector3(center.x + UnityEngine.Random.Range(-2f, 2f),
                center.y + UnityEngine.Random.Range(-2f, 2f), 1f);
            float size = UnityEngine.Random.Range(2f, 4f);
            star.transform.localScale = new Vector3(size, size, 1f);
            SpriteRenderer renderer = star.AddComponent<SpriteRenderer>();
            renderer.sprite = pixel;
            renderer.color = new Color(0.48f, 0.57f, 0.70f, 0.75f);
            renderer.sortingOrder = -30;
        }
        UnityEngine.Random.state = previousState;
    }

    static GameObject FindRoot(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].name == objectName) return roots[i];
        return null;
    }

    static void RemoveGeneratedRoot(Scene scene, string objectName)
    {
        GameObject root = FindRoot(scene, objectName);
        if (root != null) UnityEngine.Object.DestroyImmediate(root);
    }

    static void CreateFolders()
    {
        EnsureFolder("Assets/Huntz", "Prototype");
        EnsureFolder("Assets/Huntz/Prototype", "HumanoidRigPilot");
        EnsureFolder(Root, "Placeholder");
        EnsureFolder(Root, "Animation");
        EnsureFolder(AnimationFolder, "Clips");
        EnsureFolder(Root, "Appearance");
        EnsureFolder(Root, "Prefabs");
        EnsureFolder(Root, "Scenes");
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }

    static Sprite CreatePlaceholderSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Rig Pilot White Pixel",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, PixelPath);

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 64f,
            0, SpriteMeshType.FullRect);
        sprite.name = "Rig Pilot White Pixel";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        EditorUtility.SetDirty(texture);
        return sprite;
    }

    static SpriteLibraryAsset CreateSpriteLibrary(Sprite placeholder)
    {
        SpriteLibraryAsset asset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
        asset.name = "Humanoid Rig Pilot Sprite Library";
        foreach (HuntzHumanoidPartSlot slot in Enum.GetValues(typeof(HuntzHumanoidPartSlot)))
        {
            asset.AddCategoryLabel(placeholder, slot.ToString(), "Base");
            asset.AddCategoryLabel(placeholder, slot.ToString(), "Alternate");
        }
        AssetDatabase.CreateAsset(asset, LibraryPath);
        EditorUtility.SetDirty(asset);
        return asset;
    }

    static HuntzHumanoidAppearanceProfile CreateProfile(string path, string displayName,
        SpriteLibraryAsset library, string label, Color skin, Color hair, Color clothing, Color leather)
    {
        HuntzHumanoidAppearanceProfile profile = ScriptableObject.CreateInstance<HuntzHumanoidAppearanceProfile>();
        profile.name = Path.GetFileNameWithoutExtension(path);
        var looks = new HuntzHumanoidPartLook[Enum.GetValues(typeof(HuntzHumanoidPartSlot)).Length];
        int index = 0;
        foreach (HuntzHumanoidPartSlot slot in Enum.GetValues(typeof(HuntzHumanoidPartSlot)))
        {
            Color tint = slot == HuntzHumanoidPartSlot.Head || slot == HuntzHumanoidPartSlot.Hand ||
                         slot == HuntzHumanoidPartSlot.Face
                ? skin
                : slot == HuntzHumanoidPartSlot.Hair
                    ? hair
                    : slot == HuntzHumanoidPartSlot.Foot || slot == HuntzHumanoidPartSlot.Accessory
                        ? leather
                        : clothing;
            looks[index++] = new HuntzHumanoidPartLook(slot, label, tint);
        }
        profile.Configure(displayName, library, looks);
        AssetDatabase.CreateAsset(profile, path);
        return profile;
    }

    static AnimatorController CreateController()
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState defaultState = null;

        for (int actionIndex = 0; actionIndex < Actions.Length; actionIndex++)
        for (int directionIndex = 0; directionIndex < DirectionNames.Length; directionIndex++)
        {
            HuntzHumanoidRigAction action = Actions[actionIndex];
            string clipName = action + "_" + DirectionNames[directionIndex];
            float duration = DurationFor(action);
            AnimationClip clip = CreateClip(clipName, action, directionIndex, duration);
            AssetDatabase.CreateAsset(clip, ClipFolder + "/" + clipName + ".anim");
            AnimatorState state = machine.AddState(clipName);
            state.motion = clip;
            if (action == HuntzHumanoidRigAction.Idle && directionIndex == 0)
                defaultState = state;
        }

        machine.defaultState = defaultState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static float DurationFor(HuntzHumanoidRigAction action)
    {
        switch (action)
        {
            case HuntzHumanoidRigAction.Idle: return 1.0f;
            case HuntzHumanoidRigAction.Walk: return 0.56f;
            case HuntzHumanoidRigAction.Attack: return 0.62f;
            case HuntzHumanoidRigAction.Dodge: return 0.24f;
            case HuntzHumanoidRigAction.Hurt: return 0.30f;
            default: return 0.82f;
        }
    }

    static AnimationClip CreateClip(string name, HuntzHumanoidRigAction action, int directionIndex, float duration)
    {
        AnimationClip clip = new AnimationClip { name = name, frameRate = 12f };
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = action == HuntzHumanoidRigAction.Idle || action == HuntzHumanoidRigAction.Walk;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        float side = directionIndex == 1 || directionIndex == 2 || directionIndex == 3 ? -1f : 1f;
        string pelvis = "Rig/Pelvis";
        string torso = "Rig/Pelvis/Torso";
        string rightUpper = torso + "/RightShoulder/UpperArm";
        string rightForearm = rightUpper + "/Forearm";
        string leftUpper = torso + "/LeftShoulder/UpperArm";
        string leftForearm = leftUpper + "/Forearm";
        string rightThigh = pelvis + "/RightHip/Thigh";
        string leftThigh = pelvis + "/LeftHip/Thigh";
        string rightShin = rightThigh + "/Shin";
        string leftShin = leftThigh + "/Shin";
        string weaponSocket = rightForearm + "/Hand/WeaponSocket";

        switch (action)
        {
            case HuntzHumanoidRigAction.Idle:
                SetFloatCurve(clip, pelvis, "m_LocalPosition.y", duration,
                    new[] { 0f, duration * 0.5f, duration }, new[] { 0.10f, 0.112f, 0.10f }, true);
                SetRotationCurve(clip, rightUpper, duration, new[] { 0f, duration * 0.5f, duration },
                    new[] { 3f * side, -3f * side, 3f * side }, true);
                SetRotationCurve(clip, leftUpper, duration, new[] { 0f, duration * 0.5f, duration },
                    new[] { -3f * side, 3f * side, -3f * side }, true);
                break;
            case HuntzHumanoidRigAction.Walk:
                SetFloatCurve(clip, pelvis, "m_LocalPosition.y", duration,
                    new[] { 0f, duration * 0.25f, duration * 0.5f, duration * 0.75f, duration },
                    new[] { 0.10f, 0.125f, 0.10f, 0.125f, 0.10f }, true);
                SetRotationCurve(clip, rightThigh, duration,
                    new[] { 0f, duration * 0.25f, duration * 0.5f, duration * 0.75f, duration },
                    new[] { -22f, 0f, 22f, 0f, -22f }, true);
                SetRotationCurve(clip, leftThigh, duration,
                    new[] { 0f, duration * 0.25f, duration * 0.5f, duration * 0.75f, duration },
                    new[] { 22f, 0f, -22f, 0f, 22f }, true);
                SetRotationCurve(clip, rightUpper, duration,
                    new[] { 0f, duration * 0.25f, duration * 0.5f, duration * 0.75f, duration },
                    new[] { 18f * side, 0f, -18f * side, 0f, 18f * side }, true);
                SetRotationCurve(clip, leftUpper, duration,
                    new[] { 0f, duration * 0.25f, duration * 0.5f, duration * 0.75f, duration },
                    new[] { -18f * side, 0f, 18f * side, 0f, -18f * side }, true);
                break;
            case HuntzHumanoidRigAction.Attack:
                SetRotationCurve(clip, torso, duration,
                    new[] { 0f, duration * 0.3f, duration * 0.5f, duration * 0.7f, duration },
                    new[] { 0f, -8f * side, 12f * side, 5f * side, 0f }, false);
                SetRotationCurve(clip, rightUpper, duration,
                    new[] { 0f, duration * 0.3f, duration * 0.5f, duration * 0.72f, duration },
                    new[] { 4f * side, -42f * side, 58f * side, 24f * side, 4f * side }, false);
                SetRotationCurve(clip, rightForearm, duration,
                    new[] { 0f, duration * 0.3f, duration * 0.5f, duration * 0.72f, duration },
                    new[] { 0f, -36f * side, -72f * side, -28f * side, 0f }, false);
                SetRotationCurve(clip, weaponSocket, duration,
                    new[] { 0f, duration * 0.3f, duration * 0.5f, duration * 0.72f, duration },
                    new[] { 0f, -20f * side, 38f * side, 18f * side, 0f }, false);
                SetRotationCurve(clip, leftUpper, duration,
                    new[] { 0f, duration * 0.3f, duration * 0.5f, duration },
                    new[] { 0f, 18f * side, -8f * side, 0f }, false);
                AddAnimationEvent(clip, duration * 0.5f, "AnimationEvent_WeaponImpact");
                break;
            case HuntzHumanoidRigAction.Dodge:
                SetRotationCurve(clip, torso, duration,
                    new[] { 0f, duration * 0.45f, duration }, new[] { 0f, -26f * side, 0f }, false);
                SetRotationCurve(clip, rightUpper, duration,
                    new[] { 0f, duration * 0.45f, duration }, new[] { 0f, -34f * side, 0f }, false);
                SetRotationCurve(clip, leftUpper, duration,
                    new[] { 0f, duration * 0.45f, duration }, new[] { 0f, 34f * side, 0f }, false);
                break;
            case HuntzHumanoidRigAction.Hurt:
                SetRotationCurve(clip, torso, duration,
                    new[] { 0f, duration * 0.28f, duration }, new[] { 0f, -20f * side, 0f }, false);
                SetRotationCurve(clip, rightUpper, duration,
                    new[] { 0f, duration * 0.28f, duration }, new[] { 0f, -24f * side, 0f }, false);
                break;
            case HuntzHumanoidRigAction.Death:
                SetRotationCurve(clip, torso, duration,
                    new[] { 0f, duration * 0.55f, duration }, new[] { 0f, 62f * side, 82f * side }, false);
                SetRotationCurve(clip, rightUpper, duration,
                    new[] { 0f, duration * 0.55f, duration }, new[] { 0f, 48f * side, 82f * side }, false);
                SetRotationCurve(clip, leftUpper, duration,
                    new[] { 0f, duration * 0.55f, duration }, new[] { 0f, -48f * side, -82f * side }, false);
                SetRotationCurve(clip, rightShin, duration,
                    new[] { 0f, duration * 0.55f, duration }, new[] { 0f, -28f, -48f }, false);
                SetRotationCurve(clip, leftShin, duration,
                    new[] { 0f, duration * 0.55f, duration }, new[] { 0f, -28f, -48f }, false);
                break;
        }

        if (action == HuntzHumanoidRigAction.Walk || action == HuntzHumanoidRigAction.Idle)
            SetFloatCurve(clip, rightShin, "m_LocalPosition.y", duration,
                new[] { 0f, duration }, new[] { 0f, 0f }, true);

        return clip;
    }

    static void SetFloatCurve(AnimationClip clip, string path, string property, float duration,
        float[] times, float[] values, bool loop)
    {
        Keyframe[] keys = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++) keys[i] = new Keyframe(times[i], values[i]);
        AnimationCurve curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        if (loop) curve.postWrapMode = WrapMode.Loop;
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    static void SetRotationCurve(AnimationClip clip, string path, float duration, float[] times,
        float[] angles, bool loop)
    {
        Keyframe[] x = new Keyframe[times.Length];
        Keyframe[] y = new Keyframe[times.Length];
        Keyframe[] z = new Keyframe[times.Length];
        Keyframe[] w = new Keyframe[times.Length];
        for (int i = 0; i < times.Length; i++)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angles[i]);
            x[i] = new Keyframe(times[i], rotation.x);
            y[i] = new Keyframe(times[i], rotation.y);
            z[i] = new Keyframe(times[i], rotation.z);
            w[i] = new Keyframe(times[i], rotation.w);
        }

        AddQuaternionCurve(clip, path, "m_LocalRotation.x", x, loop);
        AddQuaternionCurve(clip, path, "m_LocalRotation.y", y, loop);
        AddQuaternionCurve(clip, path, "m_LocalRotation.z", z, loop);
        AddQuaternionCurve(clip, path, "m_LocalRotation.w", w, loop);
    }

    static void AddQuaternionCurve(AnimationClip clip, string path, string property, Keyframe[] keys, bool loop)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
        if (loop) curve.postWrapMode = WrapMode.Loop;
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    static void AddAnimationEvent(AnimationClip clip, float time, string functionName)
    {
        AnimationEvent animationEvent = new AnimationEvent { time = time, functionName = functionName };
        AnimationUtility.SetAnimationEvents(clip, new[] { animationEvent });
    }

    static GameObject BuildActor(Sprite placeholder, SpriteLibraryAsset libraryAsset,
        HuntzHumanoidAppearanceProfile profile, HuntzHumanoidAppearanceProfile alternateProfile,
        AnimatorController controller)
    {
        GameObject actor = new GameObject("Humanoid Rig Pilot");
        Rigidbody2D body = actor.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D collider = actor.AddComponent<CircleCollider2D>();
        collider.radius = 0.12f;
        collider.offset = new Vector2(0f, 0.08f);

        Animator animator = actor.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        SpriteLibrary library = actor.AddComponent<SpriteLibrary>();
        library.spriteLibraryAsset = libraryAsset;

        Transform rigRoot = NewBone("Rig", actor.transform, Vector3.zero);
        Transform pelvis = NewBone("Pelvis", rigRoot, new Vector3(0f, 0.10f, 0f));
        AddPart(pelvis, "BodySprite", HuntzHumanoidPartSlot.Body, placeholder, new Vector2(10f, 9f),
            new Vector3(0f, 0.015f, 0.015f), new Color(0.96f, 0.69f, 0.47f), 0, 0);
        Transform torso = NewBone("Torso", pelvis, new Vector3(0f, 0.15f, 0f));
        Transform head = NewBone("Head", torso, new Vector3(0f, 0.20f, 0f));
        AddPart(head, "HeadSprite", HuntzHumanoidPartSlot.Head, placeholder, new Vector2(12f, 12f),
            new Vector3(0f, 0.015f, 0f), new Color(0.96f, 0.69f, 0.47f), 4, 4);
        AddPart(head, "HairSprite", HuntzHumanoidPartSlot.Hair, placeholder, new Vector2(14f, 6f),
            new Vector3(0f, 0.105f, 0f), new Color(0.16f, 0.12f, 0.15f), 6, 6);
        AddPart(head, "FaceSprite", HuntzHumanoidPartSlot.Face, placeholder, new Vector2(6f, 2f),
            new Vector3(0f, -0.01f, -0.01f), new Color(0.95f, 0.62f, 0.40f), 5, 5);
        AddPart(torso, "TorsoSprite", HuntzHumanoidPartSlot.Torso, placeholder, new Vector2(14f, 18f),
            new Vector3(0f, 0.075f, 0f), new Color(0.16f, 0.42f, 0.48f), 1, 1);
        AddPart(torso, "ArmorSprite", HuntzHumanoidPartSlot.Armor, placeholder, new Vector2(10f, 11f),
            new Vector3(0f, 0.08f, -0.01f), new Color(0.31f, 0.50f, 0.56f), 2, 2);

        BuildArm(torso, "LeftShoulder", -1f, placeholder, false);
        Transform rightHand = BuildArm(torso, "RightShoulder", 1f, placeholder, true);
        Transform weaponSocket = NewBone("WeaponSocket", rightHand, new Vector3(0f, -0.035f, 0f));
        weaponSocket.localRotation = Quaternion.identity;

        BuildLeg(pelvis, "LeftHip", -1f, placeholder);
        BuildLeg(pelvis, "RightHip", 1f, placeholder);

        HuntzHumanoidRig rig = actor.AddComponent<HuntzHumanoidRig>();
        rig.Configure(animator, library, weaponSocket);
        HuntzHumanoidAppearance appearance = actor.AddComponent<HuntzHumanoidAppearance>();
        appearance.Configure(library, profile);
        actor.AddComponent<HuntzEquippableWeapon>();
        HuntzRigPilotController pilotController = actor.AddComponent<HuntzRigPilotController>();
        pilotController.ConfigureAppearances(appearance, new[] { profile, alternateProfile });

        return actor;
    }

    static Transform BuildArm(Transform torso, string shoulderName, float side, Sprite placeholder, bool weaponHand)
    {
        Transform shoulder = NewBone(shoulderName, torso, new Vector3(0.105f * side, 0.045f, 0f));
        Transform upper = NewBone("UpperArm", shoulder, Vector3.zero);
        AddPart(upper, "UpperArmSprite", HuntzHumanoidPartSlot.UpperArm, placeholder,
            new Vector2(5f, 10f), new Vector3(0f, -0.06f, 0f), new Color(0.16f, 0.42f, 0.48f), 3, -1);
        Transform forearm = NewBone("Forearm", upper, new Vector3(0f, -0.12f, 0f));
        AddPart(forearm, "ForearmSprite", HuntzHumanoidPartSlot.Forearm, placeholder,
            new Vector2(4f, 9f), new Vector3(0f, -0.055f, 0f), new Color(0.48f, 0.27f, 0.16f), 3, -1);
        Transform hand = NewBone("Hand", forearm, new Vector3(0f, -0.11f, 0f));
        AddPart(hand, "HandSprite", HuntzHumanoidPartSlot.Hand, placeholder,
            new Vector2(5f, 5f), Vector3.zero, new Color(0.96f, 0.69f, 0.47f), 4, 0);
        return hand;
    }

    static void BuildLeg(Transform pelvis, string hipName, float side, Sprite placeholder)
    {
        Transform hip = NewBone(hipName, pelvis, new Vector3(0.045f * side, -0.015f, 0f));
        Transform thigh = NewBone("Thigh", hip, Vector3.zero);
        AddPart(thigh, "ThighSprite", HuntzHumanoidPartSlot.Thigh, placeholder,
            new Vector2(6f, 10f), new Vector3(0f, -0.075f, 0f), new Color(0.28f, 0.18f, 0.15f), 0, 0);
        Transform shin = NewBone("Shin", thigh, new Vector3(0f, -0.15f, 0f));
        AddPart(shin, "LowerLegSprite", HuntzHumanoidPartSlot.LowerLeg, placeholder,
            new Vector2(5f, 9f), new Vector3(0f, -0.06f, 0f), new Color(0.34f, 0.22f, 0.17f), 0, 0);
        Transform foot = NewBone("Foot", shin, new Vector3(0f, -0.13f, 0f));
        AddPart(foot, "FootSprite", HuntzHumanoidPartSlot.Foot, placeholder,
            new Vector2(7f, 4f), new Vector3(0f, -0.015f, 0f), new Color(0.20f, 0.14f, 0.12f), 1, 1);
    }

    static Transform NewBone(string name, Transform parent, Vector3 localPosition)
    {
        GameObject bone = new GameObject(name);
        bone.transform.SetParent(parent, false);
        bone.transform.localPosition = localPosition;
        bone.transform.localRotation = Quaternion.identity;
        bone.transform.localScale = Vector3.one;
        return bone.transform;
    }

    static void AddPart(Transform bone, string name, HuntzHumanoidPartSlot slot, Sprite sprite,
        Vector2 pixelSize, Vector3 localPosition, Color tint, int frontBias, int backBias)
    {
        GameObject partObject = new GameObject(name);
        partObject.transform.SetParent(bone, false);
        partObject.transform.localPosition = localPosition;
        partObject.transform.localScale = new Vector3(pixelSize.x, pixelSize.y, 1f);
        SpriteRenderer renderer = partObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tint;
        renderer.maskInteraction = SpriteMaskInteraction.None;
        SpriteResolver resolver = partObject.AddComponent<SpriteResolver>();
        resolver.SetCategoryAndLabel(slot.ToString(), "Base");
        partObject.AddComponent<HuntzHumanoidRigPart>().Configure(slot, renderer, frontBias, backBias);
    }

    static void BuildShowcaseScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            scene.name = "Humanoid Rig Pilot";
            GameObject cameraObject = new GameObject("Rig Pilot Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.10f);
            cameraObject.transform.position = new Vector3(0f, 0.60f, -10f);
            cameraObject.AddComponent<AudioListener>();

            GameObject ground = new GameObject("Placeholder Ground");
            SceneManager.MoveGameObjectToScene(ground, scene);
            SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PixelPath);
            groundRenderer.color = new Color(0.16f, 0.20f, 0.25f);
            groundRenderer.sortingOrder = -20;
            ground.transform.localScale = new Vector3(40f, 1f, 1f);
            ground.transform.position = new Vector3(0f, -0.24f, 1f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject actor = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            actor.name = "Humanoid Rig Pilot";
            actor.transform.position = Vector3.zero;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            if (activeScene.IsValid()) EditorSceneManager.SetActiveScene(activeScene);
        }
    }
}
