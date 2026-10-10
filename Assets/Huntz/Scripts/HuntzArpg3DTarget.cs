using System;
using UnityEngine;

public sealed class HuntzArpg3DTarget : MonoBehaviour
{
    [SerializeField] string targetName = "Alvo";
    [SerializeField, Min(1)] int maxHealth = 3;
    [SerializeField, Min(0f)] float aggroRange = 0f;
    [SerializeField, Min(0f)] float moveSpeed = 2f;
    [SerializeField, Min(0f)] float contactRange = 1.2f;
    [SerializeField, Min(0)] int contactDamage = 1;
    [SerializeField] Renderer[] renderers;

    int health;
    float nextContactDamageTime;
    float verticalVelocity;
    CharacterController characterController;
    HuntzArpg3DPlayerController player;
    Color[] originalColors;
    bool defeated;

    public event Action<HuntzArpg3DTarget> Defeated;

    public string TargetName => targetName;
    public int Health => health;
    public bool IsDefeated => defeated;

    public void Configure(string displayName, int startingHealth, float chaseDistance, float chaseSpeed,
        float touchDistance, int damageOnTouch, Renderer[] visibleRenderers)
    {
        targetName = displayName;
        maxHealth = Mathf.Max(1, startingHealth);
        aggroRange = Mathf.Max(0f, chaseDistance);
        moveSpeed = Mathf.Max(0f, chaseSpeed);
        contactRange = Mathf.Max(0f, touchDistance);
        contactDamage = Mathf.Max(0, damageOnTouch);
        renderers = visibleRenderers;
        health = maxHealth;
    }

    void Awake()
    {
        health = maxHealth;
        characterController = GetComponent<CharacterController>();
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
            {
                originalColors[i] = Color.white;
                continue;
            }

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            if (block.HasColor("_BaseColor"))
            {
                originalColors[i] = block.GetColor("_BaseColor");
                continue;
            }

            Material material = targetRenderer.sharedMaterial;
            originalColors[i] = material != null
                ? material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material.color
                : Color.white;
        }
    }

    void Update()
    {
        if (defeated || aggroRange <= 0f) return;
        if (player == null) player = FindAnyObjectByType<HuntzArpg3DPlayerController>();
        if (player == null || player.IsDead) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance > aggroRange) return;

        if (distance > contactRange * 0.72f)
        {
            Vector3 direction = toPlayer.normalized;
            if (characterController != null) direction = FindOpenSteeringDirection(direction);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 velocity = direction * moveSpeed;
            if (characterController != null)
            {
                if (characterController.isGrounded) verticalVelocity = -2f;
                verticalVelocity += Physics.gravity.y * Time.deltaTime;
                characterController.Move((velocity + Vector3.up * verticalVelocity) * Time.deltaTime);
            }
            else
            {
                transform.position += velocity * Time.deltaTime;
            }
        }

        if (contactDamage > 0 && distance <= contactRange && Time.time >= nextContactDamageTime)
        {
            nextContactDamageTime = Time.time + 1.1f;
            player.ReceiveDamage(contactDamage, transform.position);
        }
    }

    Vector3 FindOpenSteeringDirection(Vector3 desiredDirection)
    {
        Vector3 center = transform.TransformPoint(characterController.center);
        float segment = Mathf.Max(0f, characterController.height * 0.5f - characterController.radius);
        Vector3 bottom = center - Vector3.up * segment;
        Vector3 top = center + Vector3.up * segment;
        float probeDistance = Mathf.Clamp(moveSpeed * Time.deltaTime * 5f, 0.55f, 1.15f);
        RaycastHit blockingHit = default;
        bool blocked = false;

        RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, characterController.radius * 0.9f,
            desiredDirection, probeDistance, ~0, QueryTriggerInteraction.Ignore);
        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (!IsArenaBlocker(hit.collider) || hit.distance >= closestDistance) continue;
            blockingHit = hit;
            closestDistance = hit.distance;
            blocked = true;
        }

        if (!blocked) return desiredDirection;

        Vector3 tangent = Vector3.Cross(Vector3.up, blockingHit.normal);
        tangent.y = 0f;
        if (tangent.sqrMagnitude < 0.01f) return desiredDirection;
        tangent.Normalize();
        Vector3 left = (desiredDirection + tangent * 0.9f).normalized;
        Vector3 right = (desiredDirection - tangent * 0.9f).normalized;
        bool leftOpen = !IsArenaDirectionBlocked(bottom, top, left, 0.8f);
        bool rightOpen = !IsArenaDirectionBlocked(bottom, top, right, 0.8f);

        if (leftOpen && rightOpen)
            return Vector3.Dot(left, desiredDirection) >= Vector3.Dot(right, desiredDirection) ? left : right;
        if (leftOpen) return left;
        if (rightOpen) return right;
        return Vector3.Dot(tangent, desiredDirection) >= 0f ? tangent : -tangent;
    }

    bool IsArenaDirectionBlocked(Vector3 bottom, Vector3 top, Vector3 direction, float distance)
    {
        RaycastHit[] hits = Physics.CapsuleCastAll(bottom, top, characterController.radius * 0.9f,
            direction, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (IsArenaBlocker(hit.collider)) return true;
        }
        return false;
    }

    static bool IsArenaBlocker(Collider candidate)
    {
        HuntzArpg3DGrayboxModule module = candidate != null
            ? candidate.GetComponentInParent<HuntzArpg3DGrayboxModule>()
            : null;
        if (module == null || module.Kind == HuntzArpg3DGrayboxModuleKind.Floor
            || module.Kind == HuntzArpg3DGrayboxModuleKind.Platform) return false;
        return true;
    }

    public void TakeHit(int amount)
    {
        if (defeated || amount <= 0) return;
        health = Mathf.Max(0, health - amount);
        StopCoroutine(nameof(FlashHit));
        StartCoroutine(nameof(FlashHit));
        if (health == 0) Defeat();
    }

    System.Collections.IEnumerator FlashHit()
    {
        SetColor(Color.white);
        yield return new WaitForSeconds(0.08f);
        RestoreColors();
    }

    void Defeat()
    {
        defeated = true;
        Defeated?.Invoke(this);
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider targetCollider in colliders) targetCollider.enabled = false;
        transform.localScale *= 0.78f;
        SetColor(new Color(0.16f, 0.16f, 0.18f));
        Destroy(gameObject, 0.7f);
    }

    void SetColor(Color color)
    {
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null) continue;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            targetRenderer.SetPropertyBlock(block);
        }
    }

    void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderers[i].GetPropertyBlock(block);
            block.SetColor("_BaseColor", originalColors[i]);
            block.SetColor("_Color", originalColors[i]);
            renderers[i].SetPropertyBlock(block);
        }
    }
}
