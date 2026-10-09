using UnityEngine;
using UnityEngine.U2D.Animation;

public enum HuntzHumanoidPartSlot
{
    Body,
    Head,
    Hair,
    Face,
    Torso,
    UpperArm,
    Forearm,
    Hand,
    Thigh,
    LowerLeg,
    Foot,
    Armor,
    Accessory
}

[DisallowMultipleComponent]
public sealed class HuntzHumanoidRigPart : MonoBehaviour
{
    [SerializeField] HuntzHumanoidPartSlot slot;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteResolver spriteResolver;
    [SerializeField] int frontSortingBias;
    [SerializeField] int backSortingBias;

    public HuntzHumanoidPartSlot Slot => slot;
    public SpriteRenderer Renderer => spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
    public int FrontSortingBias => frontSortingBias;
    public int BackSortingBias => backSortingBias;

    public void Configure(HuntzHumanoidPartSlot partSlot, SpriteRenderer partRenderer,
        int frontBias = 0, int backBias = 0)
    {
        slot = partSlot;
        spriteRenderer = partRenderer;
        spriteResolver = partRenderer != null ? partRenderer.GetComponent<SpriteResolver>() : null;
        frontSortingBias = frontBias;
        backSortingBias = backBias;
    }

    public void Apply(string label, Color tint, SpriteLibrary library)
    {
        SpriteRenderer target = Renderer;
        if (target != null) target.color = tint;

        if (library == null || string.IsNullOrWhiteSpace(label)) return;

        if (spriteResolver == null && target != null)
            spriteResolver = target.GetComponent<SpriteResolver>();
        if (spriteResolver == null && target != null)
            spriteResolver = target.gameObject.AddComponent<SpriteResolver>();
        if (spriteResolver == null) return;

        if (!spriteResolver.SetCategoryAndLabel(slot.ToString(), label))
            Debug.LogWarning($"[Huntz] Sprite Library entry '{slot}/{label}' is missing on '{name}'.", this);
    }
}
