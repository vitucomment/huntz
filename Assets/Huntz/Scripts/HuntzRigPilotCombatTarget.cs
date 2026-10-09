using UnityEngine;

[DisallowMultipleComponent]
public sealed class HuntzRigPilotCombatTarget : MonoBehaviour
{
    [SerializeField, Min(1)] int health = 2;
    [SerializeField] bool hurtsPlayerOnTouch;
    [SerializeField, Min(1)] int contactDamage = 1;
    [SerializeField, Min(0.1f)] float contactInterval = 0.9f;
    [SerializeField] SpriteRenderer[] renderers;
    [SerializeField] Collider2D[] colliders;

    bool defeated;
    float nextContactAt;

    public int Health => health;
    public bool IsDefeated => defeated;

    public void Configure(int startingHealth, bool contactHazard = false)
    {
        health = Mathf.Max(1, startingHealth);
        hurtsPlayerOnTouch = contactHazard;
        CacheVisuals();
    }

    void Awake()
    {
        CacheVisuals();
    }

    public void TakeDamage(int amount)
    {
        if (defeated || amount <= 0) return;
        health = Mathf.Max(0, health - amount);
        if (health > 0) return;

        defeated = true;
        if (colliders != null)
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = false;

        if (renderers != null)
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].color = new Color(0.28f, 0.30f, 0.33f, 0.6f);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (!hurtsPlayerOnTouch || defeated || Time.time < nextContactAt) return;
        HuntzRigPilotController player = collision.collider.GetComponentInParent<HuntzRigPilotController>();
        if (player == null || !player.ReceiveDamage(contactDamage, transform.position)) return;
        nextContactAt = Time.time + contactInterval;
    }

    void CacheVisuals()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider2D>(true);

    }
}
