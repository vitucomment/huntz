using UnityEngine;

[DisallowMultipleComponent]
public sealed class HuntzArpg3DWeaponGeometry : MonoBehaviour
{
    [SerializeField] Transform bladeBase;
    [SerializeField] Transform bladeTip;
    [SerializeField, Range(0.05f, 0.5f)] float hitRadius = 0.18f;

    public Transform BladeBase => bladeBase;
    public Transform BladeTip => bladeTip;
    public float HitRadius => hitRadius;
    public bool IsConfigured => bladeBase != null && bladeTip != null && bladeBase != bladeTip;

    public void Configure(Transform basePoint, Transform tipPoint, float radius)
    {
        bladeBase = basePoint;
        bladeTip = tipPoint;
        hitRadius = Mathf.Clamp(radius, 0.05f, 0.5f);
    }

    public bool TryGetHitCapsule(out Vector3 start, out Vector3 end, out float radius)
    {
        start = Vector3.zero;
        end = Vector3.zero;
        radius = 0f;
        if (!isActiveAndEnabled || !IsConfigured) return false;

        start = bladeBase.position;
        end = bladeTip.position;
        Vector3 scale = transform.lossyScale;
        float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        radius = hitRadius * Mathf.Max(0.01f, largestScale);
        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!IsConfigured) return;

        Vector3 start = bladeBase.position;
        Vector3 end = bladeTip.position;
        float radius = hitRadius * Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            Mathf.Abs(transform.lossyScale.z));
        Vector3 axis = (end - start).normalized;
        Vector3 side = Vector3.Cross(axis, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.001f) side = Vector3.right;
        Vector3 up = Vector3.Cross(axis, side).normalized;

        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(start, radius);
        Gizmos.DrawWireSphere(end, radius);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Vector3 offset = (side * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(start + offset, end + offset);
        }
    }
}
