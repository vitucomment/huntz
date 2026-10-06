using UnityEngine;

// Keeps an actor's child sprites together while sorting actors by their feet.
[DisallowMultipleComponent]
public sealed class HuntzSliceDepth : MonoBehaviour
{
    [SerializeField] int sortingOffset;

    SpriteRenderer[] renderers;
    int[] localOrders;

    void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        localOrders = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) localOrders[i] = renderers[i].sortingOrder;
    }

    void LateUpdate()
    {
        if (renderers == null) return;
        int depth = sortingOffset - Mathf.RoundToInt(transform.position.y * 100f);
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].sortingOrder = depth + localOrders[i];
    }
}
