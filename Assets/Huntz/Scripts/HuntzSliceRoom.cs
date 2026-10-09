using UnityEngine;

// A fixed node in the prototype's hand-authored 3x3 hunt map.
[DisallowMultipleComponent]
public sealed class HuntzSliceRoom : MonoBehaviour
{
    [SerializeField] int roomId;
    [SerializeField] Vector2Int mapPosition;
    [SerializeField] string roomTitle;
    [SerializeField] int[] connectedRoomIds = new int[0];
    [SerializeField] int riftCoreReward;
    [SerializeField] int firstVisitHealing;
    [SerializeField] bool isFinalRoom;

    public int Id => roomId;
    public Vector2Int MapPosition => mapPosition;
    public string Title => roomTitle;
    public int[] Connections => connectedRoomIds;
    public int RiftCoreReward => riftCoreReward;
    public int FirstVisitHealing => firstVisitHealing;
    public bool IsFinalRoom => isFinalRoom;
    public GameObject Content => gameObject;

    public void Configure(int id, Vector2Int position, string title, int[] connections,
        int coreReward, int healing, bool finalRoom)
    {
        roomId = id;
        mapPosition = position;
        roomTitle = title;
        connectedRoomIds = connections ?? new int[0];
        riftCoreReward = Mathf.Max(0, coreReward);
        firstVisitHealing = Mathf.Max(0, healing);
        isFinalRoom = finalRoom;
    }
}
