using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class HuntzWeaponAssetBuilder
{
    const string SwordSpritePath = "Assets/Huntz/Art/Final/Shared/Weapons/StarterSword.png";
    const string StarterSwordDefinitionPath = "Assets/Huntz/Resources/Huntz/Weapons/StarterSword.asset";
    const string StarterSwordResourcePath = "Huntz/Weapons/StarterSword";

    [MenuItem("Huntz/Combat/Import Starter Sword")]
    public static void ImportStarterSword()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before importing weapon assets.");
        EnsureStarterSword();
    }

    public static bool IsStarterSwordImported()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        HuntzWeaponDefinition definition = AssetDatabase.LoadAssetAtPath<HuntzWeaponDefinition>(StarterSwordDefinitionPath);
        return sprite != null && definition != null && definition.Sprite == sprite;
    }

    internal static void EnsureStarterSword()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SwordSpritePath) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (importer.spritePixelsPerUnit != 64f)
        {
            importer.spritePixelsPerUnit = 64f;
            changed = true;
        }
        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            changed = true;
        }
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        bool textureSettingsChanged = false;
        if (textureSettings.spriteAlignment != (int)SpriteAlignment.Custom)
        {
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            textureSettingsChanged = true;
        }
        if (textureSettings.spritePivot != new Vector2(0.16f, 0.18f))
        {
            textureSettings.spritePivot = new Vector2(0.16f, 0.18f);
            textureSettingsChanged = true;
        }
        if (textureSettingsChanged)
        {
            importer.SetTextureSettings(textureSettings);
            changed = true;
        }
        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (changed) importer.SaveAndReimport();

        Sprite sword = AssetDatabase.LoadAssetAtPath<Sprite>(SwordSpritePath);
        if (sword == null) throw new InvalidOperationException($"Could not import the sword sprite at {SwordSpritePath}.");

        EnsureFolder(Path.GetDirectoryName(StarterSwordDefinitionPath).Replace('\\', '/'));
        HuntzWeaponDefinition definition = AssetDatabase.LoadAssetAtPath<HuntzWeaponDefinition>(StarterSwordDefinitionPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<HuntzWeaponDefinition>();
            definition.name = "StarterSword";
            definition.Configure("Espada de treino", sword);
            AssetDatabase.CreateAsset(definition, StarterSwordDefinitionPath);
        }
        else if (definition.Sprite != sword)
        {
            definition.Configure(definition.DisplayName, sword, definition.DamageBonus,
                definition.ReachBonus, definition.WidthBonus);
            EditorUtility.SetDirty(definition);
        }

        AssetDatabase.SaveAssets();
    }

    static void EnsureFolder(string assetFolder)
    {
        if (AssetDatabase.IsValidFolder(assetFolder)) return;
        string parent = Path.GetDirectoryName(assetFolder)?.Replace('\\', '/');
        string folderName = Path.GetFileName(assetFolder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(assetFolder)) AssetDatabase.CreateFolder(parent, folderName);
    }
}

[InitializeOnLoad]
static class HuntzWeaponAssetBootstrap
{
    static HuntzWeaponAssetBootstrap()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        HuntzWeaponAssetPostprocessor.QueueImport();
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) HuntzWeaponAssetPostprocessor.QueueImport();
    }
}

sealed class HuntzWeaponAssetPostprocessor : AssetPostprocessor
{
    const string SwordSpritePath = "Assets/Huntz/Art/Final/Shared/Weapons/StarterSword.png";

    static bool queued;
    static bool processing;

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
        string[] movedAssets, string[] movedFromAssetPaths)
    {
        if (importedAssets.Contains(SwordSpritePath)) QueueImport();
    }

    internal static void QueueImport()
    {
        if (queued || processing) return;
        queued = true;
        EditorApplication.delayCall += () =>
        {
            queued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(SwordSpritePath) ||
                HuntzWeaponAssetBuilder.IsStarterSwordImported())
                return;

            processing = true;
            try
            {
                HuntzWeaponAssetBuilder.EnsureStarterSword();
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
