using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HumanoidStandardPreviewBuilder
{
    const string CharacterFolder = "Assets/HuntzSlice/Art/Drafts/HumanoidStandard/PixelLab_Base";
    const string PreviewScenePath = "Assets/HuntzSlice/Scenes/HumanoidStandardPreview.unity";
    const string TilePath = "Assets/HuntzSlice/Art/ArenaTile.png";
    const float PixelsPerUnit = 64f;

    static readonly string[] Directions =
    {
        "south", "south-east", "east", "north-east",
        "north", "north-west", "west", "south-west"
    };

    static readonly string[] DirectionLabels =
    {
        "S", "SE", "E", "NE", "N", "NW", "W", "SW"
    };

    static readonly Vector2[] FootPositions =
    {
        new(0f, -1.15f),
        new(1.65f, -0.75f),
        new(2.9f, 0f),
        new(1.65f, 0.75f),
        new(0f, 1.15f),
        new(-1.65f, 0.75f),
        new(-2.9f, 0f),
        new(-1.65f, -0.75f)
    };

    static readonly Color[] TileColors =
    {
        new(0.24f, 0.38f, 0.28f),
        new(0.28f, 0.42f, 0.31f),
        new(0.22f, 0.35f, 0.27f),
        new(0.28f, 0.42f, 0.31f),
        new(0.24f, 0.38f, 0.28f),
        new(0.22f, 0.35f, 0.27f),
        new(0.28f, 0.42f, 0.31f),
        new(0.24f, 0.38f, 0.28f)
    };

    [MenuItem("Huntz/Build Humanoid Standard Preview")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ConfigureCharacterTextures();
        AssetDatabase.Refresh();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "HumanoidStandardPreview";

        Camera previewCamera = Camera.main;
        if (previewCamera == null)
        {
            GameObject cameraObject = new("Main Camera");
            previewCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 3.2f;
        previewCamera.transform.position = new Vector3(0f, 0f, -10f);
        previewCamera.transform.rotation = Quaternion.identity;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.055f, 0.085f, 0.08f, 1f);
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 100f;

        Sprite tile = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
        if (tile == null)
            throw new System.InvalidOperationException($"Could not load preview tile at {TilePath}.");

        for (int i = 0; i < Directions.Length; i++)
        {
            string direction = Directions[i];
            Vector2 feet = FootPositions[i];
            string spritePath = $"{CharacterFolder}/{direction}.png";
            Sprite character = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (character == null)
                throw new System.InvalidOperationException($"Could not load character sprite at {spritePath}.");

            GameObject tileObject = new($"Stage_{direction}");
            tileObject.transform.position = new Vector3(feet.x, feet.y + 0.25f, 0f);
            SpriteRenderer tileRenderer = tileObject.AddComponent<SpriteRenderer>();
            tileRenderer.sprite = tile;
            tileRenderer.color = TileColors[i];
            tileRenderer.sortingOrder = 0;

            GameObject characterObject = new($"Humanoid_{direction}");
            characterObject.transform.position = new Vector3(feet.x, feet.y, 0f);
            SpriteRenderer characterRenderer = characterObject.AddComponent<SpriteRenderer>();
            characterRenderer.sprite = character;
            characterRenderer.sortingOrder = 1;

            CreateDirectionLabel(direction, DirectionLabels[i], feet);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, PreviewScenePath))
            throw new System.InvalidOperationException($"Could not save preview scene at {PreviewScenePath}.");

        Debug.Log($"Humanoid Standard preview saved to {PreviewScenePath}.");
    }

    static void ConfigureCharacterTextures()
    {
        foreach (string direction in Directions)
        {
            string path = $"{CharacterFolder}/{direction}.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Could not configure texture importer for {path}.");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            importer.SetTextureSettings(settings);
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }
    }

    static void CreateDirectionLabel(string direction, string shortLabel, Vector2 feet)
    {
        GameObject labelObject = new($"Direction_{direction}");
        labelObject.transform.position = new Vector3(feet.x, feet.y + 1.62f, 0f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = shortLabel;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 32;
        label.characterSize = 0.075f;
        label.color = new Color(0.9f, 0.92f, 0.84f, 1f);
        label.fontStyle = FontStyle.Bold;

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sortingOrder = 2;
    }
}
