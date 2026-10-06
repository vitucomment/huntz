from pathlib import Path

path = Path(__file__).parents[1] / "Assets/IsoRPG/Editor/HuntzIsoBuilder.cs"
source = path.read_text(encoding="utf-8")

def replace_region(text: str, start: str, end: str, body: str) -> str:
    a = text.index(start)
    b = text.index(end, a)
    body = body.replace("\n+", "\n")
    return text[:a] + body.rstrip() + "\n\n    " + text[b:]

source = source.replace(
    "    static Sprite tile, cube, roof, circle, square, capsule, blob;",
    "    static Sprite grass, darkGrass, path, plaza, water, sand, flower, questMarker;\n"
    "    static Sprite treeSprite, rockSprite, houseRed, houseBlue, wellSprite, chestSprite;\n"
    "    static Sprite shadowSprite, softFill, solidPixel, heroIdle, slimeIdle, kingIdle, slashSprite, panelSprite;\n"
    "    static Sprite heartIcon, potionIcon;"
)
source = source.replace(
    "        Directory.CreateDirectory(ArtDir);\n        CreateSprites();",
    "        Directory.CreateDirectory(ArtDir);\n"
    "        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);\n"
    "        HuntzAnimationAssetBuilder.BuildAnimationAssets();\n"
    "        CreateSprites();"
)
source = source.replace(
    "        new GameObject(\"IsoGame\").AddComponent<IsoGame>();",
    "        IsoGame hud = new GameObject(\"IsoGame\").AddComponent<IsoGame>();\n"
    "        hud.ConfigureArt(panelSprite.texture, heartIcon.texture, potionIcon.texture);\n"
    "        CreateReusablePrefabs(ground, props, characters);"
)
source = replace_region(source, "    static Color GroundColor(", "    static void BuildGround(", r'''    static Sprite GroundSprite(Ground ground) => ground switch
    {
        Ground.Grass => grass,
        Ground.DarkGrass => darkGrass,
        Ground.Path => path,
        Ground.Plaza => plaza,
        Ground.Water => water,
        _ => sand,
    };''')
source = replace_region(source, "    static void BuildGround(", "    static void BuildProps(", r'''    static void BuildGround(Transform root, Transform decor)
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
    }''')
source = replace_region(source, "    static void Tree(", "    static void Rock(", r'''    static void Tree(Transform root, Vector2 pos)
    {
        float scale = 0.92f + (float)rng.NextDouble() * 0.18f;
        GameObject go = Root("Tree", root, pos);
        SpriteObject("Tree Art", treeSprite, Vector2.zero, Vector2.one * scale, Color.white, go.transform, 1);
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.14f;
        collider.offset = new Vector2(0f, 0.05f);
        Finish(go, 0f, true);
    }''')
source = replace_region(source, "    static void Rock(", "    static void House(", r'''    static void Rock(Transform root, Vector2 pos, float s)
    {
        GameObject go = Root("Rock", root, pos);
        SpriteObject("Rock Art", rockSprite, Vector2.zero, Vector2.one * (s * 1.6f), Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(s, s * 0.8f, 0.2f * s);
        Finish(go, 0.2f * s, true);
    }''')
source = replace_region(source, "    static void House(", "    static void Well(", r'''    static void House(Transform root, Vector2 pos, float s, Color wall, Color roofColor)
    {
        GameObject go = Root("House", root, pos);
        Sprite sprite = roofColor.b > roofColor.r ? houseBlue : houseRed;
        SpriteObject("House Art", sprite, Vector2.zero, Vector2.one * (s * 0.9f), Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(s, s, 0.25f * s);
        Finish(go, 0.25f * s, true);
    }''')
source = replace_region(source, "    static void Well(", "    static void Chest(", r'''    static void Well(Transform root, Vector2 pos)
    {
        const float s = 0.7f;
        GameObject go = Root("Well", root, pos);
        SpriteObject("Well Art", wellSprite, Vector2.zero, Vector2.one * 0.9f, Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(s, s * 0.7f, 0.25f * s * 0.7f);
        Finish(go, 0.25f * s * 0.7f, true);
    }''')
source = replace_region(source, "    static void Chest(", "    // ---------- Characters ----------", r'''    static void Chest(Transform root, Vector2 pos, int potions)
    {
        GameObject go = Root("Chest", root, pos);
        Shadow(go.transform, new Vector2(0.55f, 0.25f), 0.1f);
        SpriteObject("Chest Sprite", chestSprite, Vector2.zero, Vector2.one * 0.72f, Color.white, go.transform, 1);
        go.AddComponent<PolygonCollider2D>().points = Diamond(0.5f, 0.42f, 0.105f);
        go.AddComponent<IsoChest>().Setup(potions);
        Finish(go, 0.1f, true);
    }''')
source = replace_region(source, "    static GameObject Character(", "    static void BuildNpcs(", r'''    static GameObject BuildPlayer(Transform root, Vector2 pos)
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
    }''')
source = replace_region(source, "    static void Npc(", "    static void BuildEnemies(", r'''    static void Npc(Transform root, Vector2 pos, string name, Color body, Color hair, string[] lines)
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
    }''')
source = replace_region(source, "    static GameObject Slime(", "    static void BuildCamera(", r'''    static GameObject Slime(Transform root, Vector2 pos, Color color, float size, int hp, int damage, int xp)
    {
        GameObject go = Root("Slime", root, pos);
        Shadow(go.transform, new Vector2(0.42f * size, 0.18f * size));
        Transform visual = Child("Visual", go.transform);
        visual.localScale = Vector3.one * size;
        bool king = size > 1.5f;
        string characterId = king ? "SlimeKing" : "AshSlime";
        string sheet = king ? "SlimeKing_Idle_4F.png" : "AshSlime_Idle_4F.png";
        Sprite idle = LoadSheetSprite(FinalArtDir + "/Characters/Creatures/Sheets/" + sheet,
            characterId + "_Idle_F00", 64f);
        GameObject sprite = SpriteObject("Slime Sprite", idle, Vector2.zero, Vector2.one, color, visual, 1);
        Animation animation = ConfigureAnimation(sprite);
        if (king)
            AddAnimationClips(animation, "SlimeKing_Idle");
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
        go.AddComponent<IsoEnemy>().Setup(hp, damage, xp);
        Finish(go, 0f, false);
        return go;
    }''')

source = replace_region(source, "    // ---------- Procedural sprites ----------", "    static void AddSceneToBuild(", r'''    // ---------- Imported pixel-art assets ----------

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
        foreach (IsoEnemy enemy in enemies)
        {
            if (enemy == null) continue;
            if (enemy.name == "Slime Rei") SavePrefab(enemy.gameObject, folder + "/SlimeKing.prefab");
            else SavePrefab(enemy.gameObject, folder + "/AshSlime.prefab");
            if (File.Exists(folder + "/AshSlime.prefab")) break;
        }
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
    }''')

path.write_text(source, encoding="utf-8", newline="\n")
