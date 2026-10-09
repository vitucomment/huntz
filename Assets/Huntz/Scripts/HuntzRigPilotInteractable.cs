using UnityEngine;

public sealed class HuntzRigPilotInteractable : MonoBehaviour
{
    [SerializeField] string prompt = "interagir";
    [SerializeField, TextArea] string response = "";
    [SerializeField] Vector2 destination;
    [SerializeField] bool teleports;

    public string Prompt => prompt;

    public void Configure(string interactionPrompt, string interactionResponse,
        Vector2 targetPosition, bool isPortal)
    {
        prompt = interactionPrompt;
        response = interactionResponse;
        destination = targetPosition;
        teleports = isPortal;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HuntzRigPilotController player = other.GetComponentInParent<HuntzRigPilotController>();
        if (player != null) player.SetNearbyInteractable(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        HuntzRigPilotController player = other.GetComponentInParent<HuntzRigPilotController>();
        if (player != null) player.SetNearbyInteractable(null);
    }

    public void Interact(HuntzRigPilotController player)
    {
        if (player == null) return;
        if (teleports) player.TeleportTo(destination);
        if (!string.IsNullOrWhiteSpace(response)) player.ShowFeedback(response);
    }
}
