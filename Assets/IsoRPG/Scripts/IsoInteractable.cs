using UnityEngine;

public abstract class IsoInteractable : MonoBehaviour
{
    // Empty prompt means "nothing to do here right now".
    public abstract string Prompt { get; }
    public abstract void Interact(IsoPlayer player);
}
