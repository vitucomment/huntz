using UnityEngine;

// Runtime marker used to navigate to an adjacent room after combat is cleared.
[DisallowMultipleComponent]
public sealed class HuntzSliceGate : MonoBehaviour
{
    [SerializeField] int destinationRoomId;

    public int DestinationRoomId => destinationRoomId;

    public void Configure(int roomId) => destinationRoomId = roomId;
}
