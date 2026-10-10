using UnityEngine;

[CreateAssetMenu(fileName = "GrayboxArenaKit", menuName = "Huntz/3D Graybox Arena Kit")]
public sealed class HuntzArpg3DGrayboxKit : ScriptableObject
{
    [SerializeField] GameObject floorTile;
    [SerializeField] GameObject wallSegment;
    [SerializeField] GameObject pillar;
    [SerializeField] GameObject cover;
    [SerializeField] GameObject platform;

    public bool IsComplete => floorTile != null && wallSegment != null && pillar != null
        && cover != null && platform != null;

    public GameObject GetPrefab(HuntzArpg3DGrayboxModuleKind kind)
    {
        switch (kind)
        {
            case HuntzArpg3DGrayboxModuleKind.Floor: return floorTile;
            case HuntzArpg3DGrayboxModuleKind.Wall: return wallSegment;
            case HuntzArpg3DGrayboxModuleKind.Pillar: return pillar;
            case HuntzArpg3DGrayboxModuleKind.Cover: return cover;
            case HuntzArpg3DGrayboxModuleKind.Platform: return platform;
            default: return null;
        }
    }

    public void Configure(GameObject floor, GameObject wall, GameObject pillarPrefab,
        GameObject coverPrefab, GameObject platformPrefab)
    {
        floorTile = floor;
        wallSegment = wall;
        pillar = pillarPrefab;
        cover = coverPrefab;
        platform = platformPrefab;
    }
}
