using UnityEngine;

public class IsoNPC : IsoInteractable
{
    [SerializeField] string displayName = "Aldeão";
    [SerializeField, TextArea] string[] lines;

    public void Setup(string npcName, string[] dialogue)
    {
        displayName = npcName;
        lines = dialogue;
    }

    public override string Prompt => $"Falar com {displayName}";

    public override void Interact(IsoPlayer player)
    {
        if (IsoGame.Instance != null) IsoGame.Instance.StartDialogue(displayName, lines);
    }
}
