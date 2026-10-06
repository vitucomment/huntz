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
    const string AnimationRoot = "Assets/HuntzSlice/Animations";

    static readonly string[] DirectionNames =
    {
        "South", "SouthWest", "West", "NorthWest",
        "North", "NorthEast", "East", "SouthEast"
    };

    static readonly SpriteSheetSpec[] InitialSheets =
    {
        new SpriteSheetSpec(
            "Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/Hero_Idle_8D.png",
            "Hero", "Idle", 4, 8, 128f, 8f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/Hero_Walk_8D.png", "Hero", "Walk", 6, 8, 128f, 10f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/Hero_Attack_8D.png", "Hero", "Attack", 6, 8, 128f, 14f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/Hero_Dodge_8D.png", "Hero", "Dodge", 4, 8, 128f, 24f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/Hero_Hurt_8D.png", "Hero", "Hurt", 2, 8, 128f, 14f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Hero/Sheets/Hero_Death_8D.png", "Hero", "Death", 8, 8, 128f, 12f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/NPC/Sheets/Ana_Idle_8D.png", "Ana", "Idle", 4, 8, 128f, 6f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/NPC/Sheets/Bruno_Idle_8D.png", "Bruno", "Idle", 4, 8, 128f, 6f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/AshSlime_Idle_4F.png", "AshSlime", "Idle", 4, 1, 64f, 6f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/AshSlime_Move_6F.png", "AshSlime", "Move", 6, 1, 64f, 12f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/AshSlime_Hurt_2F.png", "AshSlime", "Hurt", 2, 1, 64f, 14f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/AshSlime_Death_6F.png", "AshSlime", "Death", 6, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/SlimeKing_Idle_4F.png", "SlimeKing", "Idle", 4, 1, 64f, 6f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/SlimeKing_Move_6F.png", "SlimeKing", "Move", 6, 1, 64f, 10f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/SlimeKing_Hurt_2F.png", "SlimeKing", "Hurt", 2, 1, 64f, 14f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/SlimeKing_Death_6F.png", "SlimeKing", "Death", 6, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/RiftWarden_Idle_4F.png", "RiftWarden", "Idle", 4, 1, 64f, 6f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/RiftWarden_Charge_6F.png", "RiftWarden", "Charge", 6, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/RiftWarden_Pulse_6F.png", "RiftWarden", "Pulse", 6, 1, 64f, 10f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/RiftWarden_Recover_4F.png", "RiftWarden", "Recover", 4, 1, 64f, 8f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/RiftWarden_Hurt_2F.png", "RiftWarden", "Hurt", 2, 1, 64f, 14f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Characters/Creatures/Sheets/RiftWarden_Death_8F.png", "RiftWarden", "Death", 8, 1, 64f, 12f, false),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Shared/Props/PortalHZ_Loop_4F.png", "PortalHZ", "Loop", 4, 1, 64f, 8f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Shared/Props/RiftCore_Idle_4F.png", "RiftCore", "Idle", 4, 1, 64f, 7f, true),
        new SpriteSheetSpec("Assets/HuntzSlice/Art/Final/Shared/VFX/SwordArc_4F.png", "SwordArc", "Loop", 4, 1, 64f, 24f, false)
    };

    [MenuItem("Huntz/Build Animation Assets")]
    public static void BuildAnimationAssets()
    {
        int clipCount = 0;

        foreach (SpriteSheetSpec spec in InitialSheets)
        {
            SliceSheet(spec);
            clipCount += CreateDirectionalClips(spec);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Huntz animation assets built: {clipCount} directional clips.");
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
                string direction = spec.Rows == 8 ? DirectionNames[row] : null;
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
                    pivot = new Vector2(0.5f, spec.Rows == 8 ? 0.035f : 0.04f),
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
            string direction = spec.Rows == 8 ? DirectionNames[row] : null;
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

    static string GetSpriteName(SpriteSheetSpec spec, string direction, int frame)
    {
        return direction == null
            ? $"{spec.Character}_{spec.Action}_F{frame:00}"
            : $"{spec.Character}_{spec.Action}_{direction}_F{frame:00}";
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

        public SpriteSheetSpec(string assetPath, string character, string action, int columns, int rows,
            float pixelsPerUnit, float framesPerSecond, bool loop)
        {
            AssetPath = assetPath;
            Character = character;
            Action = action;
            Columns = columns;
            Rows = rows;
            PixelsPerUnit = pixelsPerUnit;
            FramesPerSecond = framesPerSecond;
            Loop = loop;
        }
    }
}
