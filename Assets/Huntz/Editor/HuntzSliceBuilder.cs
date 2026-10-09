using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HuntzSliceBuilder
{
    const string Root = "Assets/Huntz";
    const string ArtDir = Root + "/Art";
    const string FinalArtDir = Root + "/Art/Final";
    const string SharedArtDir = FinalArtDir + "/Shared";
    const string ScenePath = Root + "/Scenes/HuntzVerticalSlice.unity";
    const int GridSize = 12;
    const int MainLandGridSize = 14;

    static Sprite tile;
    static Sprite landTile;
    static Sprite shadow;
    static Sprite softFill;
    static Sprite ring;
    static Sprite slash;
    static Sprite arenaTile;
    static Sprite solidPixel;
    static Sprite boundary;
    static Sprite portalFrame;
    static Sprite forge;
    static Sprite coreFrame;
    static Sprite heroIdle;
    static Sprite slimeIdle;
    static Sprite wardenIdle;
    static Sprite villageStone;
    static Sprite trailDirt;
    static Sprite houseBlue;
    static Sprite houseRed;
    static Sprite villageWell;
    static Sprite rock;
    static Sprite anaIdle;
    static Sprite brunoIdle;

    static Vector2 Iso(float x, float y) => new Vector2((x - y) * 0.5f, (x + y) * 0.25f - GridSize * 0.25f);

    [MenuItem("Huntz/Build MainLand + Hunt Loop")]
    public static void Build()
    {
        Directory.CreateDirectory(ArtDir);
        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        HuntzAnimationAssetBuilder.BuildAnimationAssets();
        CreateSprites();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("Huntz Vertical Slice").transform;
        BuildSpaceBackdrop(world);
        var land = Child("MainLand · Refúgio HZ", world);
        BuildMainLand(land, out Transform portal, out Transform preparation);

        var hunt = Child("Hunt Environment", world);
        var floor = Child("Arena Floor", hunt);
        var boundaries = Child("Arena Boundaries", hunt);
        var rooms = Child("Hunt Rooms", hunt);
        var gates = Child("Room Gates", hunt);
        var actors = Child("Combatants", world);

        BuildFloor(floor);
        BuildBoundaries(boundaries);
        HuntzSlicePlayer player = BuildPlayer(actors);
        BuildRooms(rooms);
        BuildCamera();
        HuntzSliceHud hud = new GameObject("Arena HUD").AddComponent<HuntzSliceHud>();
        hud.ConfigureArt(
            AssetDatabase.LoadAssetAtPath<Texture2D>(SharedArtDir + "/UI/Panel_Frame.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>(SharedArtDir + "/UI/Icon_Heart.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>(SharedArtDir + "/UI/Icon_RiftCore.png"),
            AssetDatabase.LoadAssetAtPath<Texture2D>(SharedArtDir + "/UI/Icon_Sword.png"));
        new GameObject("Hunt Controller").AddComponent<HuntzSliceHunt>()
            .Configure(rooms, gates, player, ring, softFill, 0, land, hunt, portal, preparation,
                new Vector2(0f, -1.45f));
        hunt.gameObject.SetActive(false);
        BuildLight();
        CreateReusablePrefabs(world);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[Huntz] Vertical slice room saved at " + ScenePath);
    }

    [MenuItem("Huntz/Build Vertical Slice Room")]
    static void BuildLegacyMenuAlias() => Build();

    static void BuildMainLand(Transform root, out Transform portal, out Transform preparation)
    {
        int landSize = MainLandGridSize;
        Vector2 LandIso(int x, int y) => new Vector2((x - y) * 0.5f, (x + y) * 0.25f - landSize * 0.25f);

        for (int x = 0; x <= landSize; x++)
        for (int y = 0; y <= landSize; y++)
        {
            bool edge = x == 0 || y == 0 || x == landSize || y == landSize;
            GameObject tileObject = SpriteObject($"MainLand Tile {x:00}-{y:00}", landTile, LandIso(x, y),
                Vector2.one, edge ? new Color(0.72f, 0.82f, 0.84f) : Color.white, root, -32000);
            tileObject.isStatic = true;
        }

        Vector2[] corners = { LandIso(0, 0), LandIso(landSize, 0), LandIso(landSize, landSize), LandIso(0, landSize) };
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 start = corners[i];
            Vector2 end = corners[(i + 1) % corners.Length];
            Vector2 delta = end - start;
            GameObject edge = new GameObject($"MainLand Edge {i + 1}");
            edge.transform.SetParent(root, false);
            edge.transform.position = (start + end) * 0.5f;
            edge.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            BoxCollider2D collider = edge.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(delta.magnitude + 0.18f, 0.2f);
            GameObject visual = new GameObject("Rim visual");
            visual.transform.SetParent(edge.transform, false);
            visual.transform.localScale = new Vector3(delta.magnitude + 0.18f, 0.2f, 1f);
            SpriteRenderer edgeVisual = visual.AddComponent<SpriteRenderer>();
            edgeVisual.sprite = boundary;
            edgeVisual.color = new Color(0.16f, 0.23f, 0.3f);
            edgeVisual.sortingOrder = -31900;
        }

        BuildAsteroidCrust(root, landSize, LandIso);

        portal = BuildPortal(root, new Vector2(0.1f, -0.5f));
        preparation = BuildPreparationPoint(root, new Vector2(-2.65f, -0.25f));

        BuildCrystal(root, new Vector2(-3.05f, -0.35f), new Color(0.34f, 0.82f, 0.95f));
        BuildCrystal(root, new Vector2(3.05f, 0.35f), new Color(0.72f, 0.44f, 0.96f));
        BuildCrystal(root, new Vector2(2.6f, -1.05f), new Color(0.38f, 0.72f, 0.9f));
        BuildVillage(root, LandIso);
    }

    static void BuildSpaceBackdrop(Transform parent)
    {
        var random = new System.Random(824197);
        Color[] palette =
        {
            new Color(0.72f, 0.84f, 1f, 0.72f),
            new Color(0.42f, 0.82f, 1f, 0.62f),
            new Color(0.85f, 0.58f, 1f, 0.62f)
        };

        for (int i = 0; i < 72; i++)
        {
            float x = (float)(random.NextDouble() * 24.0 - 12.0);
            float y = (float)(random.NextDouble() * 16.0 - 8.0);
            float size = (float)(random.NextDouble() * 0.022 + 0.014);
            Color color = palette[random.Next(palette.Length)];
            SpriteObject($"Espaço · estrela {i + 1:00}", solidPixel, new Vector2(x, y),
                new Vector2(size, size), color, parent, -33000);
        }
    }

    static void BuildAsteroidCrust(Transform parent, int size, System.Func<int, int, Vector2> gridToWorld)
    {
        int rockIndex = 0;
        for (int step = 2; step < size; step += 3)
        {
            Vector2[] edgePositions =
            {
                gridToWorld(step, 0) + new Vector2(0f, -0.22f),
                gridToWorld(step, size) + new Vector2(0f, 0.18f),
                gridToWorld(0, step) + new Vector2(-0.18f, 0f),
                gridToWorld(size, step) + new Vector2(0.18f, 0f)
            };

            foreach (Vector2 position in edgePositions)
            {
                float scale = rockIndex % 2 == 0 ? 0.42f : 0.32f;
                Color tint = rockIndex % 3 == 0
                    ? new Color(0.64f, 0.73f, 0.82f)
                    : new Color(0.42f, 0.51f, 0.61f);
                SpriteObject($"Asteroide · rocha {rockIndex + 1:00}", rock, position,
                    new Vector2(scale, scale * 0.78f), tint, parent, -31940);
                rockIndex++;
            }
        }
    }

    static void BuildVillage(Transform parent, System.Func<int, int, Vector2> gridToWorld)
    {
        Transform village = Child("Vila HZ", parent);

        for (int step = 9; step <= 11; step++)
        {
            SpriteObject($"Praça · pedra {step}", villageStone, gridToWorld(step, step),
                Vector2.one, Color.white, village, -31990);
        }

        for (int x = 8; x <= 12; x++)
        {
            SpriteObject($"Caminho da vila · {x}", trailDirt, gridToWorld(x, 9),
                Vector2.one, Color.white, village, -31989);
        }

        BuildHouse(village, "Mercado do Refúgio", houseBlue, new Vector2(-1.35f, 2.25f), "MERCADO");
        BuildHouse(village, "Oficina do Refúgio", houseRed, new Vector2(1.35f, 2.25f), "OFICINA");
        BuildVillageWell(village, new Vector2(0f, 1.95f));

        BuildVillager(village, "Ana · Guardiã do Refúgio", "Ana",
            new Vector2(-1.8f, 0.95f), anaIdle,
            "O Refúgio HZ é um asteroide à deriva entre instantes. O portal leva às hunts.");
        BuildVillager(village, "Bruno · Ferreiro", "Bruno",
            new Vector2(1.8f, 0.95f), brunoIdle,
            "Estou preparando a oficina. Em breve ela poderá vender e aprimorar equipamento.");
    }

    static void BuildHouse(Transform parent, string name, Sprite sprite, Vector2 position, string sign)
    {
        GameObject house = RootObject(name, parent, position);
        SpriteObject("Fachada", sprite, new Vector2(0f, 0.42f), new Vector2(1.28f, 1.28f),
            Color.white, house.transform, 10);
        BoxCollider2D collider = house.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.92f, 0.34f);
        collider.offset = new Vector2(0f, 0.16f);
        AddWorldLabel(house.transform, sign, new Vector3(0f, 1.15f, 0f), new Color(1f, 0.88f, 0.56f));
        house.AddComponent<HuntzSliceDepth>();
    }

    static void BuildVillageWell(Transform parent, Vector2 position)
    {
        GameObject well = RootObject("Poço da Vila", parent, position);
        Shadow(well.transform, new Vector2(0.72f, 0.24f));
        SpriteObject("Poço · estrutura", villageWell, new Vector2(0f, 0.25f),
            new Vector2(0.9f, 0.9f), Color.white, well.transform, 10);
        CircleCollider2D collider = well.AddComponent<CircleCollider2D>();
        collider.radius = 0.28f;
        collider.offset = new Vector2(0f, 0.16f);
        well.AddComponent<HuntzSliceDepth>();
    }

    static GameObject BuildVillager(Transform parent, string objectName, string character,
        Vector2 position, Sprite idleSprite, string greeting)
    {
        GameObject villager = RootObject(objectName, parent, position);
        Shadow(villager.transform, new Vector2(0.48f, 0.19f));
        Transform visual = Child("Visual", villager.transform);
        visual.localScale = Vector3.one * 0.62f;
        GameObject sprite = SpriteObject(character + " Sprite", idleSprite, Vector2.zero,
            Vector2.one, Color.white, visual, 10);

        HuntzDirectionalAnimator directional = villager.AddComponent<HuntzDirectionalAnimator>();
        directional.Configure(sprite.GetComponent<SpriteRenderer>(), character, LoadDirectionalSpriteSet(character));
        CircleCollider2D collider = villager.AddComponent<CircleCollider2D>();
        collider.radius = 0.17f;
        collider.offset = new Vector2(0f, 0.09f);
        villager.AddComponent<HuntzSliceVillager>().Configure(character, greeting, directional);
        villager.AddComponent<HuntzSliceDepth>();
        return villager;
    }

    static Transform BuildPortal(Transform parent, Vector2 position)
    {
        GameObject go = RootObject("Portal HZ", parent, position);
        Shadow(go.transform, new Vector2(1.85f, 0.58f));
        GameObject portalVisual = SpriteObject("Portal HZ · visual", portalFrame, Vector2.zero,
            Vector2.one, Color.white, go.transform, 16);
        AddSpriteAnimation(portalVisual, "PortalHZ", "Loop");
        AddWorldLabel(go.transform, "PORTAL HZ", new Vector3(0f, 1.78f, 0f), new Color(0.91f, 0.78f, 1f));
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.28f;
        collider.offset = new Vector2(0f, 0.18f);
        go.AddComponent<HuntzSliceDepth>();
        return go.transform;
    }

    static Transform BuildPreparationPoint(Transform parent, Vector2 position)
    {
        GameObject go = RootObject("Mesa de preparação", parent, position);
        Shadow(go.transform, new Vector2(1.18f, 0.38f));
        SpriteObject("Mesa e bigorna", forge, new Vector2(0f, 0.36f), new Vector2(1.2f, 0.9f),
            Color.white, go.transform, 12);
        SpriteObject("Núcleo da mesa", coreFrame, new Vector2(0f, 0.86f), Vector2.one * 0.46f,
            new Color(0.78f, 0.92f, 1f), go.transform, 14);
        AddWorldLabel(go.transform, "MESA", new Vector3(0f, 1.38f, 0f), new Color(0.73f, 0.9f, 0.94f));
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.88f, 0.3f);
        collider.offset = new Vector2(0f, 0.15f);
        go.AddComponent<HuntzSliceDepth>();
        return go.transform;
    }

    static void BuildCrystal(Transform parent, Vector2 position, Color color)
    {
        GameObject crystal = RootObject("Cristal da MainLand", parent, position);
        SpriteObject("Cristal · sombra", shadow, new Vector2(0f, 0.02f), new Vector2(0.72f, 0.22f),
            new Color(0f, 0f, 0f, 0.28f), crystal.transform, 0);
        SpriteObject("Cristal · fragmento", coreFrame, new Vector2(0f, 0.04f), new Vector2(0.72f, 0.95f),
            color, crystal.transform, 10);
        CircleCollider2D collider = crystal.AddComponent<CircleCollider2D>();
        collider.radius = 0.2f;
        collider.offset = new Vector2(0f, 0.12f);
        crystal.AddComponent<HuntzSliceDepth>();
    }

    static void AddWorldLabel(Transform parent, string value, Vector3 localPosition, Color color)
    {
        GameObject label = new GameObject(value);
        label.transform.SetParent(parent, false);
        label.transform.localPosition = localPosition;
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = value;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.075f;
        text.fontSize = 36;
        text.color = color;
        MeshRenderer renderer = label.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 60;
    }

    static void BuildFloor(Transform root)
    {
        for (int x = 0; x <= GridSize; x++)
        for (int y = 0; y <= GridSize; y++)
        {
            Sprite floorSprite = (x + y) % 4 == 0 ? arenaTile : tile;
            GameObject go = SpriteObject($"Floor {x:00}-{y:00}", floorSprite, Iso(x, y), Vector2.one,
                Color.white, root, -32000);
            go.isStatic = true;
        }
    }

    static void BuildBoundaries(Transform root)
    {
        Vector2[] corners = { Iso(0, 0), Iso(GridSize, 0), Iso(GridSize, GridSize), Iso(0, GridSize) };
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 start = corners[i];
            Vector2 end = corners[(i + 1) % corners.Length];
            Vector2 delta = end - start;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var wall = new GameObject($"Boundary {i + 1}");
            wall.transform.SetParent(root, false);
            wall.transform.position = (start + end) * 0.5f;
            wall.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(length + 0.16f, 0.18f);
            GameObject wallVisual = SpriteObject("Wall Visual", boundary, Vector2.zero, new Vector2(length + 0.16f, 0.18f),
                Color.white, wall.transform, -1800);
        }
    }

    static void BuildRooms(Transform roomLibrary)
    {
        BuildRoom(roomLibrary, 0, "Entrada", new Vector2Int(0, 0), new[] { 1 }, 0, 0, false,
            2, 2, new[] { new Vector2(3f, 6f), new Vector2(9f, 6f) });
        BuildRoom(roomLibrary, 1, "Bifurcação", new Vector2Int(1, 0), new[] { 0, 2, 3 }, 0, 0, false,
            0, 2, new Vector2[0]);
        BuildRoom(roomLibrary, 2, "Ninho de cinzas", new Vector2Int(1, 1), new[] { 1 }, 1, 0, false,
            3, 3, new[] { new Vector2(3.5f, 4f), new Vector2(6f, 6f), new Vector2(8.5f, 8f) });
        BuildRoom(roomLibrary, 3, "Trilha principal", new Vector2Int(2, 0), new[] { 1, 4 }, 0, 0, false,
            2, 2, new[] { new Vector2(3f, 8f), new Vector2(9f, 4f) });
        BuildRoom(roomLibrary, 4, "Santuário", new Vector2Int(2, 1), new[] { 3, 5 }, 0, 2, false,
            0, 2, new Vector2[0]);
        BuildRoom(roomLibrary, 5, "Câmara final", new Vector2Int(2, 2), new[] { 4 }, 0, 0, true,
            0, 2, new Vector2[0]);
    }

    static void BuildRoom(Transform roomLibrary, int id, string title, Vector2Int mapPosition,
        int[] connections, int riftCoreReward, int firstVisitHealing, bool finalRoom,
        int enemyCount, int enemyHealth, Vector2[] enemyGridPositions)
    {
        Transform roomRoot = Child($"Room {id:00} - {title}", roomLibrary);
        HuntzSliceRoom room = roomRoot.gameObject.AddComponent<HuntzSliceRoom>();
        room.Configure(id, mapPosition, title, connections, riftCoreReward, firstVisitHealing, finalRoom);

        Transform enemies = Child("Enemies", roomRoot);
        Transform telegraphs = Child("Telegraphs", roomRoot);
        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 gridPosition = enemyGridPositions[i];
            BuildEnemy(enemies, telegraphs, $"Ash Slime {id}-{i + 1}", Iso(gridPosition.x, gridPosition.y), enemyHealth);
        }

        if (finalRoom) BuildRiftWarden(enemies, telegraphs);

        roomRoot.gameObject.SetActive(id == 0);
    }

    static void BuildRiftWarden(Transform actors, Transform telegraphs)
    {
        GameObject go = RootObject("Boss - Guardião da Fenda", actors, new Vector2(0.9f, 0.45f));
        Shadow(go.transform, new Vector2(1.15f, 0.42f));
        Transform visual = Child("Visual", go.transform);
        GameObject sprite = SpriteObject("Rift Warden", wardenIdle, Vector2.zero, Vector2.one, Color.white, visual, 10);
        AddSpriteAnimation(sprite, "RiftWarden", "Idle");

        SpriteRenderer lane = SpriteObject("Aviso - corredor da investida", solidPixel, Vector2.zero,
            Vector2.one, new Color(1f, 0.28f, 0.08f, 0.48f), telegraphs, -11000).GetComponent<SpriteRenderer>();
        lane.gameObject.SetActive(false);
        SpriteRenderer endpoint = SpriteObject("Aviso - fim da investida", ring, Vector2.zero,
            Vector2.one * 0.58f, new Color(1f, 0.72f, 0.2f, 0.95f), telegraphs, -10999).GetComponent<SpriteRenderer>();
        endpoint.gameObject.SetActive(false);
        SpriteRenderer pulseFill = SpriteObject("Aviso - área do pulso", softFill, Vector2.zero,
            Vector2.one, new Color(1f, 0.22f, 0.05f, 0.2f), telegraphs, -11000).GetComponent<SpriteRenderer>();
        pulseFill.gameObject.SetActive(false);
        SpriteRenderer pulseRing = SpriteObject("Aviso - limite do pulso", ring, Vector2.zero,
            Vector2.one, new Color(1f, 0.7f, 0.16f, 1f), telegraphs, -10999).GetComponent<SpriteRenderer>();
        pulseRing.gameObject.SetActive(false);

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.52f;
        collider.offset = new Vector2(0f, 0.42f);

        HuntzSliceBoss boss = go.AddComponent<HuntzSliceBoss>();
        boss.Configure(18, visual, lane, endpoint, pulseFill, pulseRing);
        go.AddComponent<HuntzSliceDepth>();
    }

    static HuntzSlicePlayer BuildPlayer(Transform root)
    {
        GameObject go = RootObject("Slice Player", root, Vector2.zero);
        Shadow(go.transform, new Vector2(0.56f, 0.22f));
        Transform visual = Child("Visual", go.transform);
        visual.localScale = Vector3.one * 0.78f;
        GameObject sprite = SpriteObject("Warrior Sprite", heroIdle, Vector2.zero, Vector2.one, Color.white, visual, 10);
        HuntzDirectionalAnimator directional = go.AddComponent<HuntzDirectionalAnimator>();
        directional.Configure(sprite.GetComponent<SpriteRenderer>(), "Hero", LoadDirectionalSpriteSet("Hero"));

        GameObject slashObject = SpriteObject("Slash", slash, Vector2.zero, Vector2.one, Color.white, go.transform, 60);
        AddSpriteAnimation(slashObject, "SwordArc", null);
        slashObject.GetComponent<SpriteRenderer>().enabled = false;

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.19f;
        collider.offset = new Vector2(0f, 0.1f);

        HuntzSlicePlayer player = go.AddComponent<HuntzSlicePlayer>();
        go.AddComponent<HuntzSliceDepth>();
        return player;
    }

    static void BuildEnemy(Transform actors, Transform effects, string name, Vector2 position, int hitPoints = 2)
    {
        GameObject go = RootObject(name, actors, position);
        Shadow(go.transform, new Vector2(0.55f, 0.2f));
        Transform visual = Child("Visual", go.transform);
        GameObject sprite = SpriteObject("Ash Slime", slimeIdle, Vector2.zero, Vector2.one, Color.white, visual, 10);
        AddSpriteAnimation(sprite, "AshSlime", "Idle");

        GameObject marker = new GameObject(name + " attack warning");
        marker.transform.SetParent(effects, false);
        marker.transform.localScale = new Vector3(1.55f, 0.78f, 1f);
        GameObject fill = SpriteObject("Danger Area", softFill, Vector2.zero, Vector2.one, new Color(1f, 0.12f, 0.08f, 0.28f), marker.transform, -10000);
        GameObject outline = SpriteObject("Warning Ring", ring, Vector2.zero, Vector2.one, new Color(1f, 0.67f, 0.2f, 1f), marker.transform, -9999);
        marker.SetActive(false);

        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.21f;
        collider.offset = new Vector2(0f, 0.08f);

        HuntzSliceEnemy enemy = go.AddComponent<HuntzSliceEnemy>();
        enemy.SetTelegraph(marker.transform, fill.GetComponent<SpriteRenderer>(), outline.GetComponent<SpriteRenderer>());
        enemy.Configure(hitPoints, 1, 1.15f);
        go.AddComponent<HuntzSliceDepth>();
    }

    static void BuildCamera()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 4.7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.13f);
        go.AddComponent<AudioListener>();
    }

    static void BuildLight()
    {
        var go = new GameObject("Directional Light");
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.8f;
    }

    static void Shadow(Transform parent, Vector2 size) =>
        SpriteObject("Shadow", shadow, new Vector2(0f, 0.03f), size, new Color(0f, 0f, 0f, 0.32f), parent, 0);

    static Transform Child(string name, Transform parent)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }

    static GameObject RootObject(string name, Transform parent, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        return go;
    }

    static GameObject SpriteObject(string name, Sprite sprite, Vector2 position, Vector2 size, Color color, Transform parent, int order)
    {
        GameObject go = RootObject(name, parent, position);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    static void CreateSprites()
    {
        tile = LoadSprite(SharedArtDir + "/Terrain/Hunt_Arena.png", 64);
        arenaTile = LoadSprite(SharedArtDir + "/Terrain/Hunt_Ash.png", 64);
        landTile = LoadSprite(SharedArtDir + "/Terrain/Mainland_Grass.png", 64);
        boundary = LoadSprite(SharedArtDir + "/Props/HuntBoundary.png", 64);
        forge = LoadSprite(SharedArtDir + "/Props/Forge_Table.png", 64);
        shadow = LoadSprite(SharedArtDir + "/VFX/PixelShadow.png", 64);
        softFill = LoadSprite(SharedArtDir + "/VFX/Telegraph_Fill.png", 64);
        ring = LoadSprite(SharedArtDir + "/VFX/Telegraph_Ring.png", 64);
        solidPixel = LoadSprite(SharedArtDir + "/VFX/SolidPixel.png", 8);
        portalFrame = LoadSheetSprite(SharedArtDir + "/Props/PortalHZ_Loop_4F.png", "PortalHZ_Loop_F00", 64);
        coreFrame = LoadSheetSprite(SharedArtDir + "/Props/RiftCore_Idle_4F.png", "RiftCore_Idle_F00", 64);
        slash = LoadSheetSprite(SharedArtDir + "/VFX/SwordArc_4F.png", "SwordArc_Loop_F00", 64);
        heroIdle = LoadSheetSprite(FinalArtDir + "/Characters/Hero/Sheets/Hero_Idle_8D.png", "Hero_Idle_South_F00", 128);
        slimeIdle = LoadSheetSprite(FinalArtDir + "/Characters/Creatures/Sheets/AshSlime_Idle_4F.png", "AshSlime_Idle_F00", 64);
        wardenIdle = LoadSheetSprite(FinalArtDir + "/Characters/Creatures/Sheets/RiftWarden_Idle_4F.png", "RiftWarden_Idle_F00", 64);
        villageStone = LoadSprite(SharedArtDir + "/Terrain/Village_Stone.png", 64);
        trailDirt = LoadSprite(SharedArtDir + "/Terrain/Trail_Dirt.png", 64);
        houseBlue = LoadSprite(SharedArtDir + "/Props/House_BlueRoof.png", 64);
        houseRed = LoadSprite(SharedArtDir + "/Props/House_RedRoof.png", 64);
        villageWell = LoadSprite(SharedArtDir + "/Props/Village_Well.png", 64);
        rock = LoadSprite(SharedArtDir + "/Props/Rock.png", 64);
        anaIdle = LoadSheetSprite(FinalArtDir + "/Characters/NPC/Sheets/Ana_Idle_8D.png", "Ana_Idle_South_F00", 128);
        brunoIdle = LoadSheetSprite(FinalArtDir + "/Characters/NPC/Sheets/Bruno_Idle_8D.png", "Bruno_Idle_South_F00", 128);
        LoadSprite(SharedArtDir + "/UI/Panel_Frame.png", 64);
        LoadSprite(SharedArtDir + "/UI/Icon_Heart.png", 64);
        LoadSprite(SharedArtDir + "/UI/Icon_RiftCore.png", 64);
        LoadSprite(SharedArtDir + "/UI/Icon_Sword.png", 64);
    }

    static Sprite LoadSprite(string path, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Sprite asset missing: " + path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Sprite LoadSheetSprite(string path, string spriteName, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Sprite sheet missing: " + path);
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
            if (sprite.name == spriteName) return sprite;
        throw new InvalidOperationException("Sprite not found: " + spriteName + " in " + path);
    }

    static HuntzDirectionalSpriteSet LoadDirectionalSpriteSet(string character)
    {
        string path = $"{Root}/Resources/Huntz/Animations/{character}.asset";
        HuntzDirectionalSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>(path);
        if (spriteSet == null) throw new FileNotFoundException("Directional sprite set missing: " + path);
        return spriteSet;
    }

    static HuntzDirectionalAnimator AddSpriteAnimation(GameObject target, string character, string initialAction)
    {
        HuntzDirectionalAnimator animation = target.AddComponent<HuntzDirectionalAnimator>();
        animation.Configure(target.GetComponent<SpriteRenderer>(), character, LoadDirectionalSpriteSet(character), initialAction);
        return animation;
    }

    static void CreateReusablePrefabs(Transform world)
    {
        const string folder = Root + "/Prefabs";
        EnsureFolder(folder);
        SavePrefab(world.Find("MainLand · Refúgio HZ/Portal HZ")?.gameObject, folder + "/PortalHZ.prefab");
        SavePrefab(world.Find("MainLand · Refúgio HZ/Mesa de preparação")?.gameObject, folder + "/ForgeStation.prefab");
        SavePrefab(world.Find("Combatants/Slice Player")?.gameObject, folder + "/Warrior.prefab");
        HuntzSliceEnemy slime = world.GetComponentInChildren<HuntzSliceEnemy>(true);
        if (slime != null) SavePrefab(slime.gameObject, folder + "/AshSlime.prefab");
        HuntzSliceBoss boss = world.GetComponentInChildren<HuntzSliceBoss>(true);
        if (boss != null) SavePrefab(boss.gameObject, folder + "/RiftWarden.prefab");
        Transform land = world.Find("MainLand · Refúgio HZ");
        if (land != null)
        {
            int crystalIndex = 0;
            for (int i = 0; i < land.childCount; i++)
            {
                Transform child = land.GetChild(i);
                if (child.name == "Cristal da MainLand")
                {
                    SavePrefab(child.gameObject, folder + $"/RiftCrystal_{crystalIndex + 1:00}.prefab");
                    crystalIndex++;
                }
            }
        }
        SaveVillagePrefabs(world, folder);
    }

    static void SaveVillagePrefabs(Transform world, string folder)
    {
        foreach (HuntzSliceVillager villager in world.GetComponentsInChildren<HuntzSliceVillager>(true))
        {
            if (villager == null) continue;
            string fileName = villager.name.Contains("Ana") ? "Village_Ana.prefab" : "Village_Bruno.prefab";
            SavePrefab(villager.gameObject, folder + "/" + fileName);
        }

        foreach (Transform candidate in world.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "Mercado do Refúgio")
                SavePrefab(candidate.gameObject, folder + "/Village_Market.prefab");
            else if (candidate.name == "Oficina do Refúgio")
                SavePrefab(candidate.gameObject, folder + "/Village_Workshop.prefab");
            else if (candidate.name == "Poço da Vila")
                SavePrefab(candidate.gameObject, folder + "/Village_Well.prefab");
        }
    }

    static void SavePrefab(GameObject instance, string path)
    {
        if (instance == null) return;
        PrefabUtility.SaveAsPrefabAsset(instance, path);
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string name = Path.GetFileName(assetFolder);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static void AddSceneToBuild(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            if (scene.path != scenePath) scenes.Add(scene);
        scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }


}
