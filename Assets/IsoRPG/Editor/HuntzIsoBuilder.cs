using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the isometric RPG concept scene from code: Huntz > Build Isometric RPG Concept.
public static class HuntzIsoBuilder
{
    const string ArtDir = "Assets/IsoRPG/Art";
    const string ScenePath = "Assets/Scenes/HuntzIsoRPG.unity";
    const int MapSize = 26;
    const int GroundOrder = -32000;
    const int DecalOrder = -31000;

    enum Ground { Grass, DarkGrass, Path, Plaza, Water, Sand }

    static Sprite grass, darkGrass, path, plaza, water, sand, flower, questMarker;
    static Sprite treeSprite, rockSprite, houseRed, houseBlue, wellSprite, chestSprite;
    static Sprite shadowSprite, softFill, solidPixel, heroIdle, slimeIdle, kingIdle, slashSprite, panelSprite;
    static Sprite heartIcon, potionIcon;
    static System.Random rng;

    // Clear areas where props must not spawn: (gridX, gridY, radius).
    static readonly Vector3[] ClearZones =
    {
        new(8.5f, 8.5f, 5.5f),   // village
        new(19f, 19f, 3.2f),     // slime camp
        new(10.5f, 17.5f, 2f),   // slimes near the pond
    };

    static Vector2 Iso(float gx, float gy) => new Vector2((gx - gy) * 0.5f, (gx + gy) * 0.25f);

    [MenuItem("Huntz/Build Isometric RPG Concept")]
    public static void Build()
    {
        Directory.CreateDirectory(ArtDir);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        HuntzAnimationAssetBuilder.BuildAnimationAssets();
        CreateSprites();
        rng = new System.Random(7);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var world = new GameObject("World").transform;
        var ground = Child("Ground", world);
        var decor = Child("Decor", world);
        var props = Child("Props", world);
        var characters = Child("Characters", world);

        BuildGround(ground, decor);
        BuildProps(props);
        var player = BuildPlayer(characters, Iso(8.6f, 7.4f));
        BuildNpcs(characters);
        Chest(props, Iso(6.3f, 9.2f), 2);
        Chest(props, Iso(21.5f, 18.2f), 3);
        BuildEnemies(characters);
        BuildCamera(player.transform);
        IsoGame hud = new GameObject("IsoGame").AddComponent<IsoGame>();
        hud.ConfigureArt(panelSprite.texture, heartIcon.texture, potionIcon.texture);
        CreateReusablePrefabs(ground, props, characters);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuild(ScenePath);
        Debug.Log("[Huntz] Isometric RPG concept built at " + ScenePath);
    }

    // ---------- Map ----------

    static Ground Classify(int x, int y)
    {
        float pond = Dist(x, y, 6f, 17f);
        if (pond < 2.6f) return Ground.Water;
        if (pond < 3.4f) return Ground.Sand;
        if (x >= 6 && x <= 11 && y >= 6 && y <= 11) return Ground.Plaza;
        if (x >= 11 && y >= 11 && x <= 20 && y <= 20 && Mathf.Abs(x - y) <= 1) return Ground.Path;
        if (x >= 15 && y >= 15) return Ground.DarkGrass;
        return Ground.Grass;
    }

    static Sprite GroundSprite(Ground ground) => ground switch
    {
        Ground.Grass => grass,
        Ground.DarkGrass => darkGrass,
        Ground.Path => path,
        Ground.Plaza => plaza,
        Ground.Water => water,
        _ => sand,
    };

        static void BuildGround(Transform root, Transform decor)
    {
        Color[] flowerColors = { new(1f, 1f, 0.95f), new(1f, 0.85f, 0.3f), new(1f, 0.55f, 0.7f), new(0.7f, 0.6f, 1f) };
        for (int gx = 0; gx < MapSize; gx++)
        for (int gy = 0; gy < MapSize; gy++)
        {
            Ground type = Classify(gx, gy);
            GameObject go = SpriteObject($"Tile_{gx}_{gy}", GroundSprite(type), Iso(gx, gy), Vector2.one,
                Color.white, root, GroundOrder);
            go.isStatic = true;
            if (type == Ground.Water) go.AddComponent<PolygonCollider2D>().points = Diamond(1f, 1f, 0f);

            if (type == Ground.Grass && rng.NextDouble() < 0.12)
            {
                Vector2 offset = new Vector2((float)(rng.NextDouble() - 0.5) * 0.5f, (float)(rng.NextDouble() - 0.5) * 0.2f);
                Color color = flowerColors[rng.Next(flowerColors.Length)];
                SpriteObject("Flower", flower, Iso(gx, gy) + offset, Vector2.one * 0.32f, color, decor, DecalOrder);
            }
        }
    }

        static void BuildProps(Transform root)
    {
        // A ring of trees keeps the player inside the map.
        for (int i = 0; i < MapSize; i++)
        {
            Tree(root, Iso(i, 0));
            Tree(root, Iso(i, MapSize - 1));
            if (i > 0 && i < MapSize - 1)
            {
                Tree(root, Iso(0, i));
                Tree(root, Iso(MapSize - 1, i));
            }
        }

        for (int gx = 2; gx < MapSize - 2; gx++)
        for (int gy = 2; gy < MapSize - 2; gy++)
        {
            var t = Classify(gx, gy);
            if (t != Ground.Grass && t != Ground.DarkGrass) continue;
            if (gx >= 10 && gy >= 10 && Mathf.Abs(gx - gy) <= 2) continue; // keep the trail open
            if (ClearZones.Any(z => Dist(gx, gy, z.x, z.y) < z.z)) continue;

            double r = rng.NextDouble();
            var jitter = new Vector2((float)(rng.NextDouble() - 0.5) * 0.3f, (float)(rng.NextDouble() - 0.5) * 0.15f);
            if (r < (t == Ground.Grass ? 0.11 : 0.06)) Tree(root, Iso(gx, gy) + jitter);
            else if (r < 0.15) Rock(root, Iso(gx, gy) + jitter, 0.3f + (float)rng.NextDouble() * 0.2f);
        }

        House(root, Iso(6.4f, 12f), 1.8f, new Color(0.9f, 0.82f, 0.66f), new Color(0.75f, 0.3f, 0.25f));
        House(root, Iso(12f, 6.4f), 1.6f, new Color(0.82f, 0.78f, 0.72f), new Color(0.3f, 0.42f, 0.7f));
        Well(root, Iso(8.2f, 8.2f));
    }

    // ---------- Props ----------

    static void Tree(Transform root, Vector2 pos)
    {
        float scale = 0.92f + (float)rng.NextDouble() * 0.18f;
        GameObject go = Root("Tree", root, pos);
        SpriteObject("Tree Art", treeSprite, Vector2.zero, Vector2.one * scale, Color.white, go.transform, 1);
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.14f;
        collider.offset = new Vector2(0f, 0.05f);
        Finish(go, 0f, true);
    }

        static void Rock(Transform root, Vector2 pos, float s)
    {
        GameObject go = Root("Rock", root, pos);
        SpriteObject("Rock Art", rockSprite, Vector2.zero, Vector2.one * (s * 1.6f), Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(s, s * 0.8f, 0.2f * s);
        Finish(go, 0.2f * s, true);
    }

        static void House(Transform root, Vector2 pos, float s, Color wall, Color roofColor)
    {
        GameObject go = Root("House", root, pos);
        Sprite sprite = roofColor.b > roofColor.r ? houseBlue : houseRed;
        SpriteObject("House Art", sprite, Vector2.zero, Vector2.one * (s * 0.9f), Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(s, s, 0.25f * s);
        Finish(go, 0.25f * s, true);
    }

        static void Well(Transform root, Vector2 pos)
    {
        const float s = 0.7f;
        GameObject go = Root("Well", root, pos);
        SpriteObject("Well Art", wellSprite, Vector2.zero, Vector2.one * 0.9f, Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(s, s * 0.7f, 0.25f * s * 0.7f);
        Finish(go, 0.25f * s * 0.7f, true);
    }

        static void Chest(Transform root, Vector2 pos, int potions)
    {
        GameObject go = Root("Chest", root, pos);
        Shadow(go.transform, new Vector2(0.55f, 0.25f), 0.1f);
        SpriteObject("Chest Sprite", chestSprite, Vector2.zero, Vector2.one * 0.72f, Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(0.5f, 0.42f, 0.105f);
        go.AddComponent<IsoChest>().Setup(potions);
        Finish(go, 0.1f, true);
    }

        // ---------- Characters ----------

    static GameObject BuildPlayer(Transform root, Vector2 pos)
    {
        GameObject go = Root("Player", root, pos);
        Shadow(go.transform, new Vector2(0.42f, 0.2f));
        Transform visual = Child("Visual", go.transform);
        visual.localScale = Vector3.one * 0.78f;
        GameObject sprite = SpriteObject("Warrior Sprite", heroIdle, Vector2.zero, Vector2.one, Color.white, visual, 1);
        Animation animation = ConfigureAnimation(sprite);
        string[] clipNames = new List<string>()
            .Concat(DirectionalClipNames("Hero", "Idle"))
            .Concat(DirectionalClipNames("Hero", "Walk"))
            .Concat(DirectionalClipNames("Hero", "Attack"))
            .Concat(DirectionalClipNames("Hero", "Dodge"))
            .Concat(DirectionalClipNames("Hero", "Hurt"))
            .Concat(DirectionalClipNames("Hero", "Death"))
            .ToArray();
        AddAnimationClips(animation, clipNames);
        animation.Play("Hero_Idle_South");
        HuntzDirectionalAnimator directional = go.AddComponent<HuntzDirectionalAnimator>();
        directional.Configure(animation, "Hero");

        GameObject slash = SpriteObject("Slash", slashSprite, Vector2.zero, Vector2.one, Color.white, go.transform, 10);
        Animation slashAnimation = ConfigureAnimation(slash);
        AddAnimationClips(slashAnimation, "SwordArc_Loop");
        slash.GetComponent<SpriteRenderer>().enabled = false;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.18f;
        col.offset = new Vector2(0f, 0.1f);
        go.AddComponent<IsoPlayer>();
        Finish(go, 0f, false);
        return go;
    }

        static void BuildNpcs(Transform root)
    {
        Npc(root, Iso(7.6f, 10.9f), "Anciã Ana", new Color(0.55f, 0.3f, 0.6f), new Color(0.92f, 0.92f, 0.95f), new[]
        {
            "Ah, um viajante! Bem-vindo à vila de Huntz.",
            "Slimes tomaram os campos a nordeste. Siga a trilha de terra para encontrá-los.",
            "Dizem que um Slime Rei os comanda. Derrote todos e a vila estará a salvo.",
            "Há um baú aqui perto com poções. Pegue o que precisar.",
        });
        Npc(root, Iso(10.9f, 7.6f), "Ferreiro Bruno", new Color(0.45f, 0.35f, 0.28f), new Color(0.15f, 0.12f, 0.1f), new[]
        {
            "Minha forja anda fria desde que os slimes chegaram.",
            "Dica de ferreiro: golpeie e recue. Slimes ficam atordoados quando apanham.",
            "E cuidado com os que rondam o lago a oeste.",
        });
    }

    static void Npc(Transform root, Vector2 pos, string name, Color body, Color hair, string[] lines)
    {
        GameObject go = Root(name, root, pos);
        Shadow(go.transform, new Vector2(0.42f, 0.2f));
        Transform visual = Child("Visual", go.transform);
        visual.localScale = Vector3.one * 0.72f;
        string characterId = name.Contains("Ana") ? "Ana" : "Bruno";
        Sprite idle = LoadSheetSprite(FinalArtDir + "/Characters/NPC/Sheets/" + characterId + "_Idle_8D.png",
            characterId + "_Idle_South_F00", 128f);
        GameObject sprite = SpriteObject("Character Sprite", idle, Vector2.zero, Vector2.one, Color.white, visual, 1);
        Animation animation = ConfigureAnimation(sprite);
        AddAnimationClips(animation, DirectionalClipNames(characterId, "Idle"));
        animation.Play(characterId + "_Idle_South");
        HuntzDirectionalAnimator directional = go.AddComponent<HuntzDirectionalAnimator>();
        directional.Configure(animation, characterId);
        directional.PlayAction("Idle", Vector2.down);

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;
        col.offset = new Vector2(0f, 0.1f);
        go.AddComponent<IsoNPC>().Setup(name, lines);
        SpriteObject("Talk Marker", questMarker, new Vector2(0f, 1.05f), Vector2.one * 0.65f, Color.white, go.transform, 6);
        Finish(go, 0f, true);
    }

        static void BuildEnemies(Transform root)
    {
        var green = new Color(0.45f, 0.85f, 0.4f);
        var teal = new Color(0.35f, 0.75f, 0.8f);
        foreach (var p in new[] { new Vector2(18f, 18.5f), new Vector2(20.5f, 18f), new Vector2(19f, 21f), new Vector2(21.5f, 20.5f), new Vector2(17f, 20f) })
            Slime(root, Iso(p.x, p.y), green, 1f, 3, 1, 2);
        foreach (var p in new[] { new Vector2(10.5f, 17f), new Vector2(10f, 18.5f) })
            Slime(root, Iso(p.x, p.y), teal, 0.85f, 2, 1, 1);
        Slime(root, Iso(20f, 20f), new Color(0.75f, 0.35f, 0.85f), 1.7f, 10, 2, 8).name = "Slime Rei";
    }

    static GameObject Slime(Transform root, Vector2 pos, Color color, float size, int hp, int damage, int xp)
    {
        GameObject go = Root("Slime", root, pos);
        Shadow(go.transform, new Vector2(0.42f * size, 0.18f * size));
        Transform visual = Child("Visual", go.transform);
        visual.localScale = Vector3.one * size;
        bool king = size > 1.5f;
        string characterId = king ? "SlimeKing" : "AshSlime";
        Sprite idle = king ? kingIdle : slimeIdle;
        GameObject sprite = SpriteObject("Slime Sprite", idle, Vector2.zero, Vector2.one, color, visual, 1);
        Animation animation = ConfigureAnimation(sprite);
        if (king)
            AddAnimationClips(animation, "SlimeKing_Idle", "SlimeKing_Move", "SlimeKing_Hurt", "SlimeKing_Death");
        else
            AddAnimationClips(animation, "AshSlime_Idle", "AshSlime_Move", "AshSlime_Hurt", "AshSlime_Death");
        animation.Play(characterId + "_Idle");

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.18f * size;
        col.offset = new Vector2(0f, 0.08f * size);
        IsoEnemy enemy = go.AddComponent<IsoEnemy>();
        enemy.Setup(hp, damage, xp);
        enemy.SetAnimationPrefix(characterId);
        Finish(go, 0f, false);
        return go;
    }

        static void BuildCamera(Transform target)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(target.position.x, target.position.y + 0.4f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4.2f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.13f, 0.11f);
        camGo.AddComponent<AudioListener>();

        var so = new SerializedObject(camGo.AddComponent<CameraFollow2D>());
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("offset").vector2Value = new Vector2(0f, 0.4f);
        so.FindProperty("smoothTime").floatValue = 0.15f;
        so.FindProperty("minY").floatValue = -1000f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Helpers ----------

    static float Dist(float x, float y, float cx, float cy) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));

    static Color Shade(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a);

    // Iso footprint diamond of width w; h scales its height, yCenter lifts it off the pivot.
    static Vector2[] Diamond(float w, float h, float yCenter) => new[]
    {
        new Vector2(0f, yCenter - 0.25f * h), new Vector2(0.5f * w, yCenter),
        new Vector2(0f, yCenter + 0.25f * h), new Vector2(-0.5f * w, yCenter),
    };

    static Transform Child(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static GameObject Root(string name, Transform parent, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        return go;
    }

    static void Shadow(Transform parent, Vector2 size, float y = 0f) =>
        SpriteObject("Shadow", shadowSprite, new Vector2(0f, y), size, new Color(0f, 0f, 0f, 0.5f), parent, 0);

    static void Finish(GameObject go, float yOffset, bool isStatic)
    {
        go.AddComponent<YSort>().Init(yOffset, isStatic);
        if (isStatic) go.isStatic = true;
    }

    static GameObject SpriteObject(string name, Sprite sprite, Vector2 localPos, Vector2 scale, Color color, Transform parent, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return go;
    }

    // ---------- Imported pixel-art assets ----------

    const string FinalArtDir = "Assets/HuntzSlice/Art/Final";
    const string SharedArtDir = FinalArtDir + "/Shared";

    static void CreateSprites()
    {
        grass = LoadSprite(SharedArtDir + "/Terrain/Grass.png", 64f);
        darkGrass = LoadSprite(SharedArtDir + "/Terrain/Grass_Dark.png", 64f);
        path = LoadSprite(SharedArtDir + "/Terrain/Trail_Dirt.png", 64f);
        plaza = LoadSprite(SharedArtDir + "/Terrain/Village_Stone.png", 64f);
        water = LoadSprite(SharedArtDir + "/Terrain/Pond_Water.png", 64f);
        sand = LoadSprite(SharedArtDir + "/Terrain/Shore_Sand.png", 64f);
        flower = LoadSprite(SharedArtDir + "/Props/Meadow_Flower.png", 64f);
        questMarker = LoadSprite(SharedArtDir + "/Props/Quest_Marker.png", 64f);
        treeSprite = LoadSprite(SharedArtDir + "/Props/Tree.png", 64f);
        rockSprite = LoadSprite(SharedArtDir + "/Props/Rock.png", 64f);
        houseRed = LoadSprite(SharedArtDir + "/Props/House_RedRoof.png", 64f);
        houseBlue = LoadSprite(SharedArtDir + "/Props/House_BlueRoof.png", 64f);
        wellSprite = LoadSprite(SharedArtDir + "/Props/Village_Well.png", 64f);
        chestSprite = LoadSprite(SharedArtDir + "/Props/Treasure_Chest.png", 64f);
        shadowSprite = LoadSprite(SharedArtDir + "/VFX/PixelShadow.png", 64f);
        softFill = LoadSprite(SharedArtDir + "/VFX/Telegraph_Fill.png", 64f);
        solidPixel = LoadSprite(SharedArtDir + "/VFX/SolidPixel.png", 8f);
        slashSprite = LoadSheetSprite(SharedArtDir + "/VFX/SwordArc_4F.png", "SwordArc_Loop_F00", 64f);
        heroIdle = LoadSheetSprite(FinalArtDir + "/Characters/Hero/Sheets/Hero_Idle_8D.png", "Hero_Idle_South_F00", 128f);
        slimeIdle = LoadSheetSprite(FinalArtDir + "/Characters/Creatures/Sheets/AshSlime_Idle_4F.png", "AshSlime_Idle_F00", 64f);
        kingIdle = LoadSheetSprite(FinalArtDir + "/Characters/Creatures/Sheets/SlimeKing_Idle_4F.png", "SlimeKing_Idle_F00", 64f);
        panelSprite = LoadSprite(SharedArtDir + "/UI/Panel_Frame.png", 64f);
        heartIcon = LoadSprite(SharedArtDir + "/UI/Icon_Heart.png", 64f);
        potionIcon = LoadSprite(SharedArtDir + "/UI/Icon_Potion.png", 64f);
    }

    static Sprite LoadSprite(string assetPath, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Sprite asset missing: " + assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static Sprite LoadSheetSprite(string assetPath, string spriteName, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) throw new FileNotFoundException("Sprite sheet missing: " + assetPath);
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>())
            if (sprite.name == spriteName) return sprite;
        throw new InvalidOperationException("Sprite not found: " + spriteName + " in " + assetPath);
    }

    static Animation ConfigureAnimation(GameObject target)
    {
        Animation animation = target.AddComponent<Animation>();
        animation.playAutomatically = false;
        animation.wrapMode = WrapMode.Loop;
        return animation;
    }

    static string[] DirectionalClipNames(string character, string action)
    {
        string[] directions = { "South", "SouthWest", "West", "NorthWest", "North", "NorthEast", "East", "SouthEast" };
        return directions.Select(direction => character + "_" + action + "_" + direction).ToArray();
    }

    static void AddAnimationClips(Animation animation, params string[] clipNames)
    {
        foreach (string clipName in clipNames)
        {
            string guid = AssetDatabase.FindAssets(clipName + " t:AnimationClip", new[] { "Assets/HuntzSlice/Animations" }).FirstOrDefault();
            if (string.IsNullOrEmpty(guid)) throw new FileNotFoundException("Animation clip missing: " + clipName);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            animation.AddClip(clip, clipName);
        }
    }

    static void CreateReusablePrefabs(Transform ground, Transform props, Transform characters)
    {
        const string folder = "Assets/IsoRPG/Prefabs";
        EnsureFolder(folder);
        SavePrefab(ground.childCount > 0 ? ground.GetChild(0).gameObject : null, folder + "/GrassTile.prefab");
        SavePrefab(FindChild(props, "Tree")?.gameObject, folder + "/Tree.prefab");
        SavePrefab(FindChild(props, "Rock")?.gameObject, folder + "/Rock.prefab");
        SavePrefab(FindChild(props, "Well")?.gameObject, folder + "/VillageWell.prefab");
        SavePrefab(FindChild(props, "Chest")?.gameObject, folder + "/TreasureChest.prefab");
        Transform[] houses = Enumerable.Range(0, props.childCount).Select(props.GetChild).Where(child => child.name == "House").ToArray();
        if (houses.Length > 0) SavePrefab(houses[0].gameObject, folder + "/VillageHouse_Red.prefab");
        if (houses.Length > 1) SavePrefab(houses[1].gameObject, folder + "/VillageHouse_Blue.prefab");
        SavePrefab(FindChild(characters, "Player")?.gameObject, folder + "/Warrior.prefab");
        IsoNPC[] npcs = characters.GetComponentsInChildren<IsoNPC>(true);
        foreach (IsoNPC npc in npcs)
            if (npc != null) SavePrefab(npc.gameObject, folder + "/" + (npc.name.Contains("Ana") ? "VillageElderAna" : "BlacksmithBruno") + ".prefab");
        IsoEnemy[] enemies = characters.GetComponentsInChildren<IsoEnemy>(true);
        IsoEnemy regularSlime = enemies.FirstOrDefault(enemy => enemy != null && enemy.name != "Slime Rei");
        IsoEnemy slimeKing = enemies.FirstOrDefault(enemy => enemy != null && enemy.name == "Slime Rei");
        if (regularSlime != null) SavePrefab(regularSlime.gameObject, folder + "/AshSlime.prefab");
        if (slimeKing != null) SavePrefab(slimeKing.gameObject, folder + "/SlimeKing.prefab");
        AssetDatabase.SaveAssets();
    }

    static Transform FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
            if (parent.GetChild(i).name == name) return parent.GetChild(i);
        return null;
    }

    static void SavePrefab(GameObject instance, string assetPath)
    {
        if (instance != null) PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(assetFolder));
    }

    static void AddSceneToBuild(string path)
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
        var entry = new EditorBuildSettingsScene(path, true);
        if (path == "Assets/Scenes/HuntzIsoRPG.unity")
        {
            int verticalSliceIndex = scenes.FindIndex(scene => scene.path == "Assets/HuntzSlice/Scenes/HuntzVerticalSlice.unity");
            scenes.Insert(verticalSliceIndex >= 0 ? verticalSliceIndex + 1 : 0, entry);
        }
        else scenes.Insert(0, entry);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
