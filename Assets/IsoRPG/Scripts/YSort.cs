using UnityEngine;

// Isometric depth: things lower on screen draw in front. Child renderers keep
// their relative order on top of the order derived from the Y position.
public class YSort : MonoBehaviour
{
    const int Precision = 200;

    [SerializeField] float yOffset;
    [SerializeField] bool isStatic = true;
    [SerializeField] int[] baseOrders;

    SpriteRenderer[] renderers;

    public void Init(float offset, bool staticObject)
    {
        yOffset = offset;
        isStatic = staticObject;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseOrders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseOrders[i] = renderers[i].sortingOrder;
        Apply();
    }

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (baseOrders == null || baseOrders.Length != renderers.Length) Init(yOffset, isStatic);
        Apply();
        if (isStatic) enabled = false;
    }

    void LateUpdate() => Apply();

    void Apply()
    {
        int order = Mathf.RoundToInt(-(transform.position.y + yOffset) * Precision);
        for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = order + baseOrders[i];
    }
}
