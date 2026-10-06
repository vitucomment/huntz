using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds a small 2D platformer demo scene from code: Huntz > Build 2D Demo Scene.
public static class HuntzDemoBuilder
{
    const string ArtDir = "Assets/Art";
    const string ScenePath = "Assets/Scenes/Huntz2DDemo.unity";

    static Sprite square;
    static Sprite circle;

    [MenuItem("Huntz/Build 2D Demo Scene")]
    public static void Build()
    {
        Directory.CreateDirectory(ArtDir);
        square = MakeSprite("Square", 4, 4, (x, y) => 1f, FilterMode.Point);
        circle = MakeSprite("Circle", 64, 64, (x, y) =>
        {
            float dx = x - 31.5f, dy = y - 31.5f;
            return Mathf.Clamp01(32f - Mathf.Sqrt(dx * dx + dy * dy));
        }, FilterMode.Bilinear);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        BuildBackground();
        var level = new GameObject("Level").transform;
        BuildLevel(level);
        var player = BuildPlayer(new Vector2(-4f, 1f));
        BuildCamera(player.transform);
        new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        Debug.Log("[Huntz] Demo scene built at " + ScenePath);
    }

    static void BuildBackground()
    {
        var bg = new GameObject("Background").transform;
        var rng = new System.Random(42);

        // Distant hills.
        for (int i = 0; i < 12; i++)
        {
            float x = -12f + i * 4.5f;
            float h = 3f + (float)rng.NextDouble() * 4f;
            Block("Hill", new Vector2(x, -10f + (h + 8f) / 2f), new Vector2(5f, h + 8f), new Color(0.16f, 0.2f, 0.36f), bg, -20, false);
        }

        // Stars.
        for (int i = 0; i < 60; i++)
        {
            var pos = new Vector2(-14f + (float)rng.NextDouble() * 60f, 4f + (float)rng.NextDouble() * 9f);
            float s = 0.06f + (float)rng.NextDouble() * 0.1f;
            var star = Sprite2D("Star", circle, pos, new Vector2(s, s), new Color(1f, 1f, 0.9f, 0.4f + (float)rng.NextDouble() * 0.5f), bg, -30);
            star.isStatic = true;
        }

        Sprite2D("Moon", circle, new Vector2(20f, 10f), new Vector2(2.5f, 2.5f), new Color(1f, 0.95f, 0.8f), bg, -25);
    }

    static void BuildLevel(Transform level)
    {
        var ground = new Color(0.36f, 0.25f, 0.2f);
        var grass = new Color(0.35f, 0.75f, 0.35f);
        var platform = new Color(0.55f, 0.45f, 0.75f);

        void Ground(float xMin, float xMax)
        {
            float w = xMax - xMin;
            Block("Ground", new Vector2(xMin + w / 2f, -2f), new Vector2(w, 2f), ground, level, 0, true);
            Block("Grass", new Vector2(xMin + w / 2f, -0.9f), new Vector2(w, 0.2f), grass, level, 1, false);
        }

        Ground(-8f, 6f);
        Ground(9f, 15f);
        Ground(25f, 38f);

        Block("Platform", new Vector2(4f, 2f), new Vector2(3f, 0.4f), platform, level, 0, true);
        Block("Platform", new Vector2(7.5f, 3.5f), new Vector2(2.5f, 0.4f), platform, level, 0, true);
        Block("Platform", new Vector2(13f, 2.5f), new Vector2(2.5f, 0.4f), platform, level, 0, true);
        Block("Platform", new Vector2(17.5f, 1.5f), new Vector2(2.5f, 0.4f), platform, level, 0, true);
        Block("Platform", new Vector2(21.5f, 3f), new Vector2(2.5f, 0.4f), platform, level, 0, true);
        Block("Platform", new Vector2(30f, 3f), new Vector2(3f, 0.4f), platform, level, 0, true);

        var coins = new GameObject("Coins").transform;
        coins.SetParent(level);
        Vector2[] coinPositions =
        {
            new(0f, 0f), new(1.5f, 0f), new(4f, 3f), new(7.5f, 4.5f), new(10.5f, 0f),
            new(13f, 3.5f), new(17.5f, 2.5f), new(19.5f, 3.5f), new(21.5f, 4f), new(28f, 0f),
            new(30f, 4f), new(32f, 0f),
        };
        foreach (var p in coinPositions)
        {
            var coin = Sprite2D("Coin", circle, p, new Vector2(0.6f, 0.6f), new Color(1f, 0.82f, 0.15f), coins, 5);
            coin.AddComponent<CircleCollider2D>().isTrigger = true;
            coin.AddComponent<Coin>();
        }

        // Goal flag.
        var goal = new GameObject("Goal");
        goal.transform.SetParent(level);
        goal.transform.position = new Vector3(35f, 0.5f, 0f);
        Block("Pole", new Vector2(35f, 1f), new Vector2(0.15f, 4f), new Color(0.85f, 0.85f, 0.9f), goal.transform, 2, false);
        Block("Flag", new Vector2(35.6f, 2.6f), new Vector2(1.1f, 0.7f), new Color(0.95f, 0.3f, 0.35f), goal.transform, 2, false);
        var trigger = goal.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.5f, 4f);
        trigger.offset = new Vector2(0f, 0.5f);
        goal.AddComponent<Goal>();
    }

    static GameObject BuildPlayer(Vector2 pos)
    {
        var player = Sprite2D("Player", square, pos, new Vector2(0.9f, 0.9f), new Color(0.3f, 0.85f, 0.95f), null, 10);
        var rb = player.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        player.AddComponent<BoxCollider2D>();
        player.AddComponent<PlayerController2D>();

        var eyes = new GameObject("Eyes").transform;
        eyes.SetParent(player.transform, false);
        eyes.localPosition = new Vector3(0.12f, 0.15f, 0f);
        foreach (float x in new[] { -0.15f, 0.15f })
        {
            var eye = Sprite2D("Eye", square, Vector2.zero, Vector2.one, new Color(0.08f, 0.1f, 0.15f), eyes, 11);
            eye.transform.localPosition = new Vector3(x, 0f, 0f);
            eye.transform.localScale = new Vector3(0.12f, 0.22f, 1f);
        }
        return player;
    }

    static void BuildCamera(Transform target)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(-2f, 2.5f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.16f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraFollow2D>().SetTarget(target);
        // SetTarget only sets a runtime field; persist it through serialization too.
        var so = new SerializedObject(camGo.GetComponent<CameraFollow2D>());
        so.FindProperty("target").objectReferenceValue = target;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject Block(string name, Vector2 pos, Vector2 size, Color color, Transform parent, int order, bool solid)
    {
        var go = Sprite2D(name, square, pos, size, color, parent, order);
        if (solid)
        {
            go.AddComponent<BoxCollider2D>();
            go.isStatic = true;
        }
        return go;
    }

    static GameObject Sprite2D(string name, Sprite sprite, Vector2 pos, Vector2 size, Color color, Transform parent, int order)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    static Sprite MakeSprite(string name, int width, int ppu, Func<int, int, float> alpha, FilterMode filter)
    {
        string path = $"{ArtDir}/{name}.png";
        var tex = new Texture2D(width, width, TextureFormat.RGBA32, false);
        for (int y = 0; y < width; y++)
            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y)));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = filter;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
