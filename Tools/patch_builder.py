from pathlib import Path

path = Path(__file__).parents[1] / "Assets/HuntzSlice/Editor/HuntzSliceBuilder.cs"
source = path.read_text(encoding="utf-8")

def replace_method(text: str, signature: str, next_signature: str, body: str) -> str:
    start = text.index(signature)
    end = text.index(next_signature, start)
    body = body.replace("\n+", "\n")
    return text[:start] + body.rstrip() + "\n\n" + text[end:]

source = source.replace(".Configure(rooms, gates, player, ring, circle,", ".Configure(rooms, gates, player, ring, softFill,")
source = source.replace("            var sr = wallVisual.GetComponent<SpriteRenderer>();\n            sr.sprite = square;\n", "")

source = replace_method(source, "    static void BuildRiftWarden(", "    static HuntzSlicePlayer BuildPlayer(", r'''    static void BuildRiftWarden(Transform actors, Transform telegraphs)
    {
        GameObject go = RootObject("Boss - Guardião da Fenda", actors, new Vector2(0.9f, 0.45f));
        Shadow(go.transform, new Vector2(1.15f, 0.42f));
        Transform visual = Child("Visual", go.transform);
        GameObject sprite = SpriteObject("Rift Warden", wardenIdle, Vector2.zero, Vector2.one, Color.white, visual, 10);
        Animation animation = AddAnimation(sprite, "RiftWarden_Idle");
        AddAnimationClip(animation, "RiftWarden_Idle", "RiftWarden_Charge", "RiftWarden_Pulse",
            "RiftWarden_Recover", "RiftWarden_Hurt", "RiftWarden_Death");
        animation.Play("RiftWarden_Idle");

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
    }''')

source = replace_method(source, "    static HuntzSlicePlayer BuildPlayer(", "    static void BuildEnemy(", r'''    static HuntzSlicePlayer BuildPlayer(Transform root)
    {
        GameObject go = RootObject("Slice Player", root, Vector2.zero);
        Shadow(go.transform, new Vector2(0.56f, 0.22f));
        Transform visual = Child("Visual", go.transform);
        visual.localScale = Vector3.one * 0.78f;
        GameObject sprite = SpriteObject("Warrior Sprite", heroIdle, Vector2.zero, Vector2.one, Color.white, visual, 10);
        Animation animation = AddAnimation(sprite, "Hero_Idle_South");
        AddAnimationClip(animation, GetDirectionalClipNames("Hero", "Idle"));
        AddAnimationClip(animation, GetDirectionalClipNames("Hero", "Walk"));
        AddAnimationClip(animation, GetDirectionalClipNames("Hero", "Attack"));
        AddAnimationClip(animation, GetDirectionalClipNames("Hero", "Dodge"));
        AddAnimationClip(animation, GetDirectionalClipNames("Hero", "Hurt"));
        AddAnimationClip(animation, GetDirectionalClipNames("Hero", "Death"));
        animation.Play("Hero_Idle_South");
        HuntzDirectionalAnimator directional = go.AddComponent<HuntzDirectionalAnimator>();
        directional.Configure(animation, "Hero");

        GameObject slashObject = SpriteObject("Slash", slash, Vector2.zero, Vector2.one, Color.white, go.transform, 60);
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
    }''')

source = replace_method(source, "    static void BuildEnemy(", "    static void BuildCamera(", r'''    static void BuildEnemy(Transform actors, Transform effects, string name, Vector2 position, int hitPoints = 2)
    {
        GameObject go = RootObject(name, actors, position);
        Shadow(go.transform, new Vector2(0.55f, 0.2f));
        Transform visual = Child("Visual", go.transform);
        GameObject sprite = SpriteObject("Ash Slime", slimeIdle, Vector2.zero, Vector2.one, Color.white, visual, 10);
        Animation animation = AddAnimation(sprite, "AshSlime_Idle");
        AddAnimationClip(animation, "AshSlime_Idle", "AshSlime_Move", "AshSlime_Hurt", "AshSlime_Death");
        animation.Play("AshSlime_Idle");

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
    }''')

source = source.replace("SpriteObject(\"Shadow\", circle,", "SpriteObject(\"Shadow\", shadow,")
source = replace_method(source, "    static void CreateSprites(", "\n}", r'''    static void CreateSprites()
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

    static string[] GetDirectionalClipNames(string character, string action)
    {
        string[] directions = { "South", "SouthWest", "West", "NorthWest", "North", "NorthEast", "East", "SouthEast" };
        string[] names = new string[directions.Length];
        for (int i = 0; i < directions.Length; i++) names[i] = character + "_" + action + "_" + directions[i];
        return names;
    }

    static Animation AddAnimation(GameObject target, string defaultClip)
    {
        Animation animation = target.AddComponent<Animation>();
        animation.playAutomatically = false;
        animation.wrapMode = WrapMode.Loop;
        return animation;
    }

    static void AddAnimationClip(Animation animation, params string[] clipNames)
    {
        foreach (string clipName in clipNames)
        {
            string path = FindAnimationPath(clipName);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) throw new FileNotFoundException("Animation clip missing: " + path);
            animation.AddClip(clip, clipName);
        }
    }

    static string FindAnimationPath(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets(clipName + " t:AnimationClip", new[] { "Assets/HuntzSlice/Animations" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == clipName) return path;
        }
        throw new FileNotFoundException("Animation clip asset missing: " + clipName);
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
            for (int i = 0; i < land.childCount; i++)
            {
                Transform child = land.GetChild(i);
                if (child.name == "Cristal da MainLand") SavePrefab(child.gameObject, folder + "/RiftCrystal.prefab");
            }
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
    }''')

source = source.replace(".Configure(rooms, gates, player, ring, circle,", ".Configure(rooms, gates, player, ring, softFill,")
source = source.replace("circle,", "softFill,")
source = source.replace("square,", "solidPixel,")
path.write_text(source, encoding="utf-8", newline="\n")
