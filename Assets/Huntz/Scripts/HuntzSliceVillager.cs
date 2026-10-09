using UnityEngine;

[DisallowMultipleComponent]
public sealed class HuntzSliceVillager : MonoBehaviour
{
    [SerializeField] string displayName = "Morador";
    [SerializeField, TextArea] string greeting = "Bem-vindo ao refúgio.";
    [SerializeField] HuntzDirectionalAnimator directionalAnimator;
    [SerializeField, Min(0f)] float lookDistance = 3.5f;

    public string InteractionHint => $"[E] Conversar com {displayName}";

    public void Configure(string characterName, string openingLine, HuntzDirectionalAnimator animator)
    {
        displayName = characterName;
        greeting = openingLine;
        directionalAnimator = animator;
    }

    void Awake()
    {
        if (directionalAnimator == null)
            directionalAnimator = GetComponent<HuntzDirectionalAnimator>();
    }

    void Update()
    {
        if (directionalAnimator == null) return;
        HuntzSlicePlayer player = HuntzSlicePlayer.Instance;
        if (player == null) return;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > lookDistance * lookDistance) return;
        directionalAnimator.PlayAction("Idle", toPlayer);
    }

    public void Interact()
    {
        HuntzSliceHud.ShowMessage($"{displayName}: {greeting}", 3.5f);
    }
}
