using System;
using UnityEngine;

public sealed class HuntzArpg3DInteractable : MonoBehaviour
{
    [SerializeField] string displayName = "Interagir";
    [SerializeField, TextArea] string message = "Ainda em construção.";
    [SerializeField] Transform destination;
    [SerializeField] string destinationName;
    Action<HuntzArpg3DPlayerController> interactionOverride;

    public string DisplayName => displayName;

    public void SetInteractionOverride(Action<HuntzArpg3DPlayerController> handler)
    {
        interactionOverride = handler;
    }

    public void Configure(string title, string body, Transform teleportTarget = null, string regionName = null)
    {
        displayName = title;
        message = body;
        destination = teleportTarget;
        destinationName = regionName;
    }

    public void Interact(HuntzArpg3DPlayerController player)
    {
        if (player == null) return;

        if (interactionOverride != null)
        {
            interactionOverride(player);
            return;
        }

        if (destination != null)
        {
            player.TeleportTo(destination.position, destinationName);
            return;
        }

        player.ShowFeedback(displayName + ": " + message);
    }
}
