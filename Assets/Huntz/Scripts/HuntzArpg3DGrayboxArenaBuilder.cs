using UnityEngine;

public enum HuntzArpg3DArenaLayoutKind
{
    Combat,
    Cache,
    Boss
}

public static class HuntzArpg3DGrayboxArenaBuilder
{
    const int FloorColumns = 5;
    const int FloorRows = 5;
    const float FloorStepX = 2.26f;
    const float FloorStepZ = 2.22f;

    public static Transform Build(Transform parent, Vector3 worldOrigin, HuntzArpg3DGrayboxKit kit,
        HuntzArpg3DArenaLayoutKind layout, int variant = 0)
    {
        if (parent == null || kit == null || !kit.IsComplete)
        {
            Debug.LogError("A complete graybox arena kit and runtime parent are required.");
            return null;
        }

        GameObject rootObject = new GameObject("Arena Graybox — " + LayoutName(layout));
        Transform root = rootObject.transform;
        root.SetParent(parent, true);
        root.position = worldOrigin;
        BuildFloor(root, kit, layout);

        switch (layout)
        {
            case HuntzArpg3DArenaLayoutKind.Combat:
                BuildCombat(root, kit, variant);
                break;
            case HuntzArpg3DArenaLayoutKind.Cache:
                BuildCache(root, kit, variant);
                break;
            case HuntzArpg3DArenaLayoutKind.Boss:
                BuildBoss(root, kit, variant);
                break;
        }

        return root;
    }

    static void BuildFloor(Transform root, HuntzArpg3DGrayboxKit kit, HuntzArpg3DArenaLayoutKind layout)
    {
        Color baseColor;
        Color alternateColor;
        switch (layout)
        {
            case HuntzArpg3DArenaLayoutKind.Cache:
                baseColor = new Color(0.29f, 0.32f, 0.39f);
                alternateColor = new Color(0.37f, 0.38f, 0.43f);
                break;
            case HuntzArpg3DArenaLayoutKind.Boss:
                baseColor = new Color(0.31f, 0.28f, 0.38f);
                alternateColor = new Color(0.42f, 0.35f, 0.48f);
                break;
            default:
                baseColor = new Color(0.3f, 0.36f, 0.32f);
                alternateColor = new Color(0.37f, 0.4f, 0.35f);
                break;
        }

        for (int row = 0; row < FloorRows; row++)
        {
            for (int column = 0; column < FloorColumns; column++)
            {
                float x = (column - (FloorColumns - 1) * 0.5f) * FloorStepX;
                float z = (row - (FloorRows - 1) * 0.5f) * FloorStepZ;
                bool alternate = ((row + column) & 1) == 0;
                Place(root, kit, HuntzArpg3DGrayboxModuleKind.Floor,
                    "Piso " + (row + 1) + "-" + (column + 1), new Vector3(x, 0f, z),
                    0f, alternate ? alternateColor : baseColor);
            }
        }
    }

    static void BuildCombat(Transform root, HuntzArpg3DGrayboxKit kit, int variant)
    {
        Color coverColor = new Color(0.43f, 0.31f, 0.22f);
        float rotation = (variant & 1) == 0 ? 28f : -28f;
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Cover, "Cobertura noroeste",
            new Vector3(-4f, 0f, 3f), rotation, coverColor);
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Cover, "Cobertura nordeste",
            new Vector3(4f, 0f, 3f), -rotation, coverColor * 1.05f);
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Cover, "Cobertura sudoeste",
            new Vector3(-4f, 0f, -3f), -rotation, coverColor * 0.92f);
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Cover, "Cobertura sudeste",
            new Vector3(4f, 0f, -3f), rotation, coverColor);
    }

    static void BuildCache(Transform root, HuntzArpg3DGrayboxKit kit, int variant)
    {
        Color wallColor = new Color(0.34f, 0.39f, 0.47f);
        float rotation = (variant & 1) == 0 ? 35f : -35f;
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Wall, "Mureta noroeste",
            new Vector3(-4f, 0f, 3f), rotation, wallColor);
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Wall, "Mureta nordeste",
            new Vector3(4f, 0f, 3f), -rotation, wallColor * 1.08f);
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Wall, "Mureta sudoeste",
            new Vector3(-4f, 0f, -3f), -rotation, wallColor * 0.94f);
        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Wall, "Mureta sudeste",
            new Vector3(4f, 0f, -3f), rotation, wallColor);

        Place(root, kit, HuntzArpg3DGrayboxModuleKind.Floor, "Pátio do baú",
            new Vector3(0f, 0.06f, 0.45f), 0f, new Color(0.48f, 0.39f, 0.27f));
    }

    static void BuildBoss(Transform root, HuntzArpg3DGrayboxKit kit, int variant)
    {
        Color platformColor = new Color(0.42f, 0.32f, 0.5f);
        Color pillarColor = new Color(0.36f, 0.38f, 0.5f);
        float rotation = (variant & 1) == 0 ? 0f : 45f;
        Vector3[] corners =
        {
            new Vector3(-4f, 0f, 3f),
            new Vector3(4f, 0f, 3f),
            new Vector3(-4f, 0f, -3f),
            new Vector3(4f, 0f, -3f)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 position = corners[i];
            Place(root, kit, HuntzArpg3DGrayboxModuleKind.Platform, "Plataforma do santuário " + (i + 1),
                position, rotation, platformColor * (i % 2 == 0 ? 1f : 1.08f));
            Place(root, kit, HuntzArpg3DGrayboxModuleKind.Pillar, "Pilar do santuário " + (i + 1),
                position + Vector3.up * 0.16f, -rotation, pillarColor);
        }
    }

    static GameObject Place(Transform parent, HuntzArpg3DGrayboxKit kit,
        HuntzArpg3DGrayboxModuleKind kind, string name,
        Vector3 localPosition, float yaw, Color color)
    {
        GameObject instance = Object.Instantiate(kit.GetPrefab(kind), parent, false);
        instance.name = name;
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Tint(instance, color);
        return instance;
    }

    static void Tint(GameObject instance, Color color)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    static string LayoutName(HuntzArpg3DArenaLayoutKind layout)
    {
        switch (layout)
        {
            case HuntzArpg3DArenaLayoutKind.Cache: return "saque";
            case HuntzArpg3DArenaLayoutKind.Boss: return "chefe";
            default: return "combate";
        }
    }
}
