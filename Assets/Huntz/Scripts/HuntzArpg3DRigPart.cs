using UnityEngine;

public enum HuntzArpg3DPartSlot
{
    Skin,
    Cloth,
    Leather,
    Metal,
    Hair,
    Accent
}

[DisallowMultipleComponent]
public sealed class HuntzArpg3DRigPart : MonoBehaviour
{
    [SerializeField] HuntzArpg3DPartSlot slot;
    Renderer partRenderer;
    MaterialPropertyBlock propertyBlock;

    public HuntzArpg3DPartSlot Slot => slot;

    void Awake()
    {
        partRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    public void ApplyColor(Color color)
    {
        if (partRenderer == null) partRenderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        if (partRenderer == null) return;

        partRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        partRenderer.SetPropertyBlock(propertyBlock);
    }

    public void Configure(HuntzArpg3DPartSlot partSlot, Renderer targetRenderer)
    {
        slot = partSlot;
        partRenderer = targetRenderer;
        propertyBlock = new MaterialPropertyBlock();
    }
}
