using UnityEditor;
using UnityEngine;

public static class HuntzArpg3DGrayboxKitBuilder
{
    const string EnvironmentFolder = "Assets/Huntz/Prototype/Isometric3D/Environment";
    const string KitFolder = EnvironmentFolder + "/GrayboxKit";
    const string KitPath = KitFolder + "/GrayboxArenaKit.asset";

    [MenuItem("Huntz/Prototype 3D/Build Graybox Arena Kit")]
    public static void BuildMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play Mode before building the graybox arena kit.");
            return;
        }

        HuntzArpg3DGrayboxKit kit = BuildKitAssets();
        if (kit != null) Debug.Log("Graybox arena kit generated with five reusable collision-aware prefabs.");
    }

    public static HuntzArpg3DGrayboxKit EnsureKitAssets()
    {
        HuntzArpg3DGrayboxKit kit = AssetDatabase.LoadAssetAtPath<HuntzArpg3DGrayboxKit>(KitPath);
        if (kit != null && kit.IsComplete) return kit;
        return BuildKitAssets();
    }

    public static HuntzArpg3DGrayboxKit BuildKitAssets()
    {
        EnsureFolder("Assets/Huntz/Prototype/Isometric3D", "Environment");
        EnsureFolder(EnvironmentFolder, "GrayboxKit");

        GameObject floor = BuildModulePrefab("FloorTile", HuntzArpg3DGrayboxModuleKind.Floor,
            new Vector3(2.26f, 0.06f, 2.22f), PrimitiveType.Cube);
        GameObject wall = BuildModulePrefab("WallSegment", HuntzArpg3DGrayboxModuleKind.Wall,
            new Vector3(1.35f, 0.88f, 0.22f), PrimitiveType.Cube);
        GameObject pillar = BuildModulePrefab("Pillar", HuntzArpg3DGrayboxModuleKind.Pillar,
            new Vector3(0.72f, 1.45f, 0.72f), PrimitiveType.Cylinder);
        GameObject cover = BuildModulePrefab("Cover", HuntzArpg3DGrayboxModuleKind.Cover,
            new Vector3(1.1f, 0.62f, 0.52f), PrimitiveType.Cube);
        GameObject platform = BuildModulePrefab("Platform", HuntzArpg3DGrayboxModuleKind.Platform,
            new Vector3(1.45f, 0.16f, 1.45f), PrimitiveType.Cube);

        HuntzArpg3DGrayboxKit kit = AssetDatabase.LoadAssetAtPath<HuntzArpg3DGrayboxKit>(KitPath);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<HuntzArpg3DGrayboxKit>();
            AssetDatabase.CreateAsset(kit, KitPath);
        }

        kit.Configure(floor, wall, pillar, cover, platform);
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return kit;
    }

    static GameObject BuildModulePrefab(string assetName, HuntzArpg3DGrayboxModuleKind kind,
        Vector3 dimensions, PrimitiveType visualType)
    {
        string path = KitFolder + "/" + assetName + ".prefab";
        GameObject root = new GameObject(assetName);
        try
        {
            HuntzArpg3DGrayboxModule module = root.AddComponent<HuntzArpg3DGrayboxModule>();

            GameObject presentation = GameObject.CreatePrimitive(visualType);
            presentation.name = "Presentation";
            presentation.transform.SetParent(root.transform, false);
            presentation.transform.localPosition = Vector3.up * (dimensions.y * 0.5f);
            presentation.transform.localScale = visualType == PrimitiveType.Cylinder
                ? new Vector3(dimensions.x, dimensions.y * 0.5f, dimensions.z)
                : dimensions;
            Collider presentationCollider = presentation.GetComponent<Collider>();
            if (presentationCollider != null) Object.DestroyImmediate(presentationCollider);

            GameObject collisionObject = new GameObject("Collision");
            collisionObject.transform.SetParent(root.transform, false);
            collisionObject.transform.localPosition = Vector3.up * (dimensions.y * 0.5f);
            BoxCollider solidCollider = collisionObject.AddComponent<BoxCollider>();
            solidCollider.size = dimensions;
            module.Configure(kind, dimensions, solidCollider);

            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
