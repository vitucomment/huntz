using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Imports aligned character sprite sheets and creates one sprite clip per direction.</summary>
public static class HuntzAnimationAssetBuilder
{
    const string AnimationRoot = "Assets/Huntz/Animations";
    const string RuntimeSpriteSetRoot = "Assets/Huntz/Resources/Huntz/Animations";

    static readonly string[] DirectionNames =
    {
        "South", "SouthWest", "West", "NorthWest",
        "North", "NorthEast", "East", "SouthEast"
    };

    static readonly SpriteSheetSpec[] InitialSheets =
    {
        new SpriteSheetSpec(
            "Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Idle_8D.png",
            "Hero", "Idle", 4, 8, 128f, 8f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Walk_8D.png", "Hero", "Walk", 8, 8, 128f, 14f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Attack_8D.png", "Hero", "Attack", 6, 8, 128f, 14f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Dodge_8D.png", "Hero", "Dodge", 4, 8, 128f, 24f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Hurt_8D.png", "Hero", "Hurt", 2, 8, 128f, 14f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Death_8D.png", "Hero", "Death", 8, 8, 128f, 12f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/NPC/Sheets/Ana_Idle_8D.png", "Ana", "Idle", 4, 8, 128f, 6f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/NPC/Sheets/Bruno_Idle_8D.png", "Bruno", "Idle", 4, 8, 128f, 6f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/AshSlime_Idle_4F.png", "AshSlime", "Idle", 4, 1, 64f, 6f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/AshSlime_Move_6F.png", "AshSlime", "Move", 6, 1, 64f, 12f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/AshSlime_Hurt_2F.png", "AshSlime", "Hurt", 2, 1, 64f, 14f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/AshSlime_Death_6F.png", "AshSlime", "Death", 6, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/SlimeKing_Idle_4F.png", "SlimeKing", "Idle", 4, 1, 64f, 6f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/SlimeKing_Move_6F.png", "SlimeKing", "Move", 6, 1, 64f, 10f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/SlimeKing_Hurt_2F.png", "SlimeKing", "Hurt", 2, 1, 64f, 14f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/SlimeKing_Death_6F.png", "SlimeKing", "Death", 6, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/RiftWarden_Idle_4F.png", "RiftWarden", "Idle", 4, 1, 64f, 6f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/RiftWarden_Charge_6F.png", "RiftWarden", "Charge", 6, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/RiftWarden_Pulse_6F.png", "RiftWarden", "Pulse", 6, 1, 64f, 10f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/RiftWarden_Recover_4F.png", "RiftWarden", "Recover", 4, 1, 64f, 8f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/RiftWarden_Hurt_2F.png", "RiftWarden", "Hurt", 2, 1, 64f, 14f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Characters/Creatures/Sheets/RiftWarden_Death_8F.png", "RiftWarden", "Death", 8, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Shared/Props/PortalHZ_Loop_4F.png", "PortalHZ", "Loop", 4, 1, 64f, 8f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Shared/Props/RiftCore_Idle_4F.png", "RiftCore", "Idle", 4, 1, 64f, 7f, true),
        new SpriteSheetSpec("Assets/Huntz/Art/Final/Shared/VFX/SwordArc_4F.png", "SwordArc", "Loop", 4, 1, 64f, 24f, false)
    };

    static readonly SpriteSheetSpec[] HeroAttackOverrides =
    {
        new SpriteSheetSpec(
            "Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Attack_8D_6F.png",
            "Hero", "Attack", 6, 8, 64f, 1000f / 140f, false, null, true),
        new SpriteSheetSpec(
            "Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Attack_East_12F.png",
            "Hero", "Attack", 12, 1, 64f, 14f, false, "East", true)
    };

    static IEnumerable<SpriteSheetSpec> AllSheets => InitialSheets.Concat(HeroAttackOverrides);

    [MenuItem("Huntz/Build Animation Assets")]
    public static void BuildAnimationAssets()
    {
        int clipCount = 0;

        foreach (SpriteSheetSpec spec in AllSheets)
        {
            SliceSheet(spec);
            clipCount += CreateDirectionalClips(spec);
        }

        int spriteClipCount = BuildRuntimeSpriteSets();
        AssetDatabase.SaveAssets();
        Debug.Log($"Huntz animation assets built: {clipCount} clips and {spriteClipCount} runtime sprite clips.");
    }

    [MenuItem("Huntz/Build Directional Sprite Sets")]
    public static void BuildDirectionalSpriteSetsMenu()
    {
        int clipCount = BuildRuntimeSpriteSets();
        AssetDatabase.SaveAssets();
        Debug.Log($"Huntz runtime sprite sets built: {clipCount} clips.");
    }

    [MenuItem("Huntz/Animation/Import Hero Walk")]
    public static void ImportHeroWalk()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before importing animation assets.");
        SpriteSheetSpec spec = InitialSheets.First(sheet => sheet.Character == "Hero" && sheet.Action == "Walk");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath);
        if (texture == null || texture.width != 1664 || texture.height != 1536)
            throw new InvalidOperationException("Hero Walk requires an eight by eight sheet of 208 x 192 frames.");
        SliceSheet(spec);
        CreateDirectionalClips(spec);
        BuildRuntimeSpriteSets();
        AssetDatabase.SaveAssets();
        Debug.Log("[Huntz] Hero Walk imported without rebuilding the scene. Review motion in Huntz/Animation/Preview.");
    }

    [MenuItem("Huntz/Animation/Import Hero Attack East")]
    public static void ImportHeroAttackEast()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before importing animation assets.");
        SpriteSheetSpec spec = HeroAttackOverrides.First(sheet => sheet.FixedDirection == "East");
        ImportAttackSheets(new[] { spec });
        Debug.Log("[Huntz] Hero Attack East imported without rebuilding the scene. Review all three phases in Huntz/Animation/Preview.");
    }

    [MenuItem("Huntz/Animation/Import Hero Attacks 8D")]
    public static void ImportHeroAttacks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before importing animation assets.");
        ImportAttackSheets(HeroAttackOverrides);
        Debug.Log("[Huntz] Hero attacks imported for all eight directions without rebuilding the scene.");
    }

    internal static void ImportAvailableHeroAttacks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SpriteSheetSpec[] available = HeroAttackOverrides
            .Where(spec => AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath) != null)
            .ToArray();
        if (available.Length > 0) ImportAttackSheets(available);
    }

    static void ImportAttackSheets(IEnumerable<SpriteSheetSpec> specs)
    {
        foreach (SpriteSheetSpec spec in specs)
        {
            TextureImporter importer = AssetImporter.GetAtPath(spec.AssetPath) as TextureImporter;
            if (importer == null)
                throw new FileNotFoundException($"Sprite sheet not found: {spec.AssetPath}");
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            if (sourceWidth != spec.Columns * 104 || sourceHeight != spec.Rows * 96)
                throw new InvalidOperationException(
                    $"{Path.GetFileName(spec.AssetPath)} requires {spec.Columns} x {spec.Rows} frames of 104 x 96.");
            SliceSheet(spec);
            CreateDirectionalClips(spec);
        }

        BuildHeroRuntimeSpriteSet();
        AssetDatabase.SaveAssets();
    }

    public static bool IsHeroAttackEastImported()
    {
        SpriteSheetSpec spec = HeroAttackOverrides.First(sheet => sheet.FixedDirection == "East");
        TextureImporter importer = AssetImporter.GetAtPath(spec.AssetPath) as TextureImporter;
        if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple ||
            importer.spritePixelsPerUnit != spec.PixelsPerUnit || importer.filterMode != FilterMode.Point ||
            importer.textureCompression != TextureImporterCompression.Uncompressed)
            return false;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(spec.AssetPath).OfType<Sprite>().ToArray();
        if (sprites.Length != spec.Columns ||
            !sprites.Any(sprite => sprite.name == "Hero_Attack_East_F11"))
            return false;

        string clipPath = $"{AnimationRoot}/Hero/Hero_Attack_East.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null) return false;
        EditorCurveBinding spriteBinding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
            .FirstOrDefault(binding => binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite");
        if (spriteBinding.type != typeof(SpriteRenderer) ||
            AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding).Length != spec.Columns)
            return false;

        HuntzDirectionalSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>(
            $"{RuntimeSpriteSetRoot}/Hero.asset");
        HuntzDirectionalSpriteClip runtimeClip = spriteSet != null
            ? spriteSet.Clips.FirstOrDefault(item => item.action == "Attack" && item.direction == "East") : null;
        return runtimeClip != null && runtimeClip.frames != null && runtimeClip.frames.Length == spec.Columns &&
               runtimeClip.usesEquippedWeaponOverlay;
    }

    public static bool IsHeroAttackSetImported()
    {
        HuntzDirectionalSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>(
            $"{RuntimeSpriteSetRoot}/Hero.asset");
        if (spriteSet == null) return false;

        foreach (string direction in DirectionNames)
        {
            HuntzDirectionalSpriteClip clip = spriteSet.Find("Attack", direction);
            int expectedFrames = direction == "East" ? 12 : 6;
            if (clip == null || clip.frames == null || clip.frames.Length != expectedFrames ||
                !clip.usesEquippedWeaponOverlay)
                return false;
        }

        return true;
    }

    static void SliceSheet(SpriteSheetSpec spec)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spec.AssetPath) as TextureImporter;
        if (importer == null)
            throw new FileNotFoundException($"Sprite sheet not found: {spec.AssetPath}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = spec.PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath);
        if (texture == null)
            throw new InvalidOperationException($"Could not load sprite sheet: {spec.AssetPath}");

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
            throw new InvalidOperationException($"No sprite data provider for: {spec.AssetPath}");

        provider.InitSpriteEditorDataProvider();
        Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
            .Where(rect => !string.IsNullOrEmpty(rect.name))
            .GroupBy(rect => rect.name)
            .ToDictionary(group => group.Key, group => group.First().spriteID);

        var spriteRects = new List<SpriteRect>(spec.Columns * spec.Rows);
        for (int row = 0; row < spec.Rows; row++)
        {
            for (int column = 0; column < spec.Columns; column++)
            {
                string direction = spec.FixedDirection ?? (spec.Rows == 8 ? DirectionNames[row] : null);
                string spriteName = GetSpriteName(spec, direction, column);

                int xMin = Mathf.RoundToInt(column * texture.width / (float)spec.Columns);
                int xMax = Mathf.RoundToInt((column + 1) * texture.width / (float)spec.Columns);
                int yMin = Mathf.RoundToInt((spec.Rows - row - 1) * texture.height / (float)spec.Rows);
                int yMax = Mathf.RoundToInt((spec.Rows - row) * texture.height / (float)spec.Rows);

                var spriteRect = new SpriteRect
                {
                    name = spriteName,
                    rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, spec.Character == "Hero" ? 0.035f : 0.04f),
                    spriteID = existingIds.TryGetValue(spriteName, out GUID existingId)
                        ? existingId
                        : GUID.Generate()
                };
                spriteRects.Add(spriteRect);
            }
        }

        provider.SetSpriteRects(spriteRects.ToArray());
        ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
        {
            var namePairs = spriteRects
                .Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID))
                .ToList();
            nameProvider.SetNameFileIdPairs(namePairs);
        }

        provider.Apply();
        importer.SaveAndReimport();
    }

    static int CreateDirectionalClips(SpriteSheetSpec spec)
    {
        string clipFolder = $"{AnimationRoot}/{spec.Character}";
        EnsureFolder(clipFolder);

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(spec.AssetPath)
            .OfType<Sprite>()
            .Where(sprite => sprite != null)
            .ToDictionary(sprite => sprite.name);

        int clipsCreated = 0;
        for (int row = 0; row < spec.Rows; row++)
        {
            string direction = spec.FixedDirection ?? (spec.Rows == 8 ? DirectionNames[row] : null);
            string clipName = direction == null ? $"{spec.Character}_{spec.Action}" : $"{spec.Character}_{spec.Action}_{direction}";
            string clipPath = $"{clipFolder}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(clipPath) };
                AssetDatabase.CreateAsset(clip, clipPath);
                clipsCreated++;
            }

            clip.frameRate = spec.FramesPerSecond;
            clip.legacy = true;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = spec.Loop;
            settings.stopTime = spec.Columns / spec.FramesPerSecond;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var keyframes = new ObjectReferenceKeyframe[spec.Columns];
            for (int frame = 0; frame < spec.Columns; frame++)
            {
                string spriteName = GetSpriteName(spec, direction, frame);
                if (!sprites.TryGetValue(spriteName, out Sprite sprite))
                    throw new InvalidOperationException($"Sprite '{spriteName}' was not imported from {spec.AssetPath}.");

                keyframes[frame] = new ObjectReferenceKeyframe
                {
                    time = frame / spec.FramesPerSecond,
                    value = sprite
                };
            }

            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
                string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            EditorUtility.SetDirty(clip);
        }

        return clipsCreated;
    }

    static int BuildRuntimeSpriteSets()
    {
        EnsureFolder(RuntimeSpriteSetRoot);
        int clipCount = 0;
        foreach (var character in AllSheets
                     .Where(spec => AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath) != null)
                     .GroupBy(spec => spec.Character))
            BuildRuntimeSpriteSet(character.Key, character.ToArray(), ref clipCount);
        return clipCount;
    }

    static int BuildHeroRuntimeSpriteSet()
    {
        EnsureFolder(RuntimeSpriteSetRoot);
        int clipCount = 0;
        SpriteSheetSpec[] available = AllSheets
            .Where(spec => spec.Character == "Hero" && AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath) != null)
            .ToArray();
        BuildRuntimeSpriteSet("Hero", available, ref clipCount);
        return clipCount;
    }

    static void BuildRuntimeSpriteSet(string character, SpriteSheetSpec[] sheets, ref int clipCount)
    {
        string assetPath = $"{RuntimeSpriteSetRoot}/{character}.asset";
        HuntzDirectionalSpriteSet spriteSet = AssetDatabase.LoadAssetAtPath<HuntzDirectionalSpriteSet>(assetPath);
        if (spriteSet == null)
        {
            spriteSet = ScriptableObject.CreateInstance<HuntzDirectionalSpriteSet>();
            spriteSet.name = $"{character} Directional Sprites";
            AssetDatabase.CreateAsset(spriteSet, assetPath);
        }

        var spriteClips = new List<HuntzDirectionalSpriteClip>();
        var uniqueClips = sheets
            .SelectMany(sheet => ResolveDirections(sheet).Select(direction => new { sheet, direction }))
            .GroupBy(pair => pair.sheet.Action + "_" + pair.direction)
            .Select(group => group.Last());
        foreach (var pair in uniqueClips)
        {
            SpriteSheetSpec sheet = pair.sheet;
            string direction = pair.direction;
            string action = sheet.Action;
            string clipName = $"{character}_{action}" + (direction == "None" ? "" : "_" + direction);
            string clipPath = $"{AnimationRoot}/{character}/{clipName}.anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) throw new FileNotFoundException($"Animation clip missing: {clipPath}");

            EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            EditorCurveBinding spriteBinding = bindings.FirstOrDefault(binding =>
                binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite");
            if (spriteBinding.type != typeof(SpriteRenderer))
                throw new InvalidOperationException($"SpriteRenderer.m_Sprite curve missing from {clipPath}.");

            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
            Sprite[] frames = keys.Select(key => key.value as Sprite).ToArray();
            if (frames.Length == 0 || frames.Any(frame => frame == null))
                throw new InvalidOperationException($"Sprite frames are incomplete in {clipPath}.");

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            spriteClips.Add(new HuntzDirectionalSpriteClip
            {
                action = action,
                direction = direction,
                frames = frames,
                framesPerSecond = clip.frameRate,
                loop = settings.loopTime,
                usesEquippedWeaponOverlay = sheet.UsesEquippedWeaponOverlay
            });
            clipCount++;
        }

        spriteSet.SetClips(spriteClips.ToArray());
        EditorUtility.SetDirty(spriteSet);
    }

    static string GetSpriteName(SpriteSheetSpec spec, string direction, int frame)
    {
        return direction == null
            ? $"{spec.Character}_{spec.Action}_F{frame:00}"
            : $"{spec.Character}_{spec.Action}_{direction}_F{frame:00}";
    }

    static string[] ResolveDirections(SpriteSheetSpec spec)
    {
        if (!string.IsNullOrEmpty(spec.FixedDirection)) return new[] { spec.FixedDirection };
        return spec.Rows == 8 ? DirectionNames : new[] { "None" };
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder))
            return;

        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    readonly struct SpriteSheetSpec
    {
        public readonly string AssetPath;
        public readonly string Character;
        public readonly string Action;
        public readonly int Columns;
        public readonly int Rows;
        public readonly float PixelsPerUnit;
        public readonly float FramesPerSecond;
        public readonly bool Loop;
        public readonly string FixedDirection;
        public readonly bool UsesEquippedWeaponOverlay;

        public SpriteSheetSpec(string assetPath, string character, string action, int columns, int rows,
            float pixelsPerUnit, float framesPerSecond, bool loop, string fixedDirection = null,
            bool usesEquippedWeaponOverlay = false)
        {
            AssetPath = assetPath;
            Character = character;
            Action = action;
            Columns = columns;
            Rows = rows;
            PixelsPerUnit = pixelsPerUnit;
            FramesPerSecond = framesPerSecond;
            Loop = loop;
            FixedDirection = fixedDirection;
            UsesEquippedWeaponOverlay = usesEquippedWeaponOverlay;
        }
    }
}

[InitializeOnLoad]
static class HuntzAttackArtImportBootstrap
{
    static HuntzAttackArtImportBootstrap()
    {
        EditorApplication.playModeStateChanged += HuntzAttackArtImportPostprocessor.OnPlayModeStateChanged;
        HuntzAttackArtImportPostprocessor.QueueImport();
    }
}

sealed class HuntzAttackArtImportPostprocessor : AssetPostprocessor
{
    const string HeroAttack8DPath = "Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Attack_8D_6F.png";
    const string HeroAttackEastPath = "Assets/Huntz/Art/Final/Characters/Hero/Sheets/Hero_Attack_East_12F.png";

    static bool queued;
    static bool processing;

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (importedAssets.Contains(HeroAttack8DPath) || importedAssets.Contains(HeroAttackEastPath)) QueueImport();
    }

    internal static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) QueueImport();
    }

    internal static void QueueImport()
    {
        if (queued || processing) return;
        queued = true;
        EditorApplication.delayCall += () =>
        {
            queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                (!File.Exists(HeroAttack8DPath) && !File.Exists(HeroAttackEastPath)) ||
                HuntzAnimationAssetBuilder.IsHeroAttackSetImported())
                return;

            processing = true;
            try
            {
                HuntzAnimationAssetBuilder.ImportAvailableHeroAttacks();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                processing = false;
            }
        };
    }
}
