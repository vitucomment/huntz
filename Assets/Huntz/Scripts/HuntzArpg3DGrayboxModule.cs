using UnityEngine;

public enum HuntzArpg3DGrayboxModuleKind
{
    Floor,
    Wall,
    Pillar,
    Cover,
    Platform
}

[DisallowMultipleComponent]
public sealed class HuntzArpg3DGrayboxModule : MonoBehaviour
{
    [SerializeField] HuntzArpg3DGrayboxModuleKind kind;
    [SerializeField] Vector3 dimensions = Vector3.one;
    [SerializeField] Collider solidCollider;

    public HuntzArpg3DGrayboxModuleKind Kind => kind;
    public Vector3 Dimensions => dimensions;
    public Collider SolidCollider => solidCollider;
    public bool IsConfigured => solidCollider != null && dimensions.x > 0f && dimensions.y > 0f && dimensions.z > 0f;

    public void Configure(HuntzArpg3DGrayboxModuleKind moduleKind, Vector3 size, Collider collision)
    {
        kind = moduleKind;
        dimensions = size;
        solidCollider = collision;
    }

    void OnDrawGizmosSelected()
    {
        if (solidCollider == null) return;
        Gizmos.color = new Color(1f, 0.55f, 0.12f, 0.9f);
        Gizmos.DrawWireCube(solidCollider.bounds.center, solidCollider.bounds.size);
    }
}
