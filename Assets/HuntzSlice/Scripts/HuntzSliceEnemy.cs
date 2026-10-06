using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class HuntzSliceEnemy : MonoBehaviour
{
    public static readonly List<HuntzSliceEnemy> Active = new List<HuntzSliceEnemy>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActiveEnemies() => Active.Clear();

    [SerializeField] int health = 2;
    [SerializeField] int damage = 1;
    [SerializeField] float moveSpeed = 1.15f;
    [SerializeField] float detectionRange = 6f;
    [SerializeField] float attackRange = 0.82f;
    [SerializeField] float attackRadius = 0.72f;
    [SerializeField] float telegraphDuration = 0.78f;
    [SerializeField] float attackCooldown = 0.9f;
    [SerializeField] Transform telegraph;
    [SerializeField] SpriteRenderer telegraphFill;
    [SerializeField] SpriteRenderer telegraphRing;

    Rigidbody2D body;
    int maxHealth = 2;
    Collider2D hitCollider;
    Vector2 spawnPosition;
    SpriteRenderer[] visualRenderers;
    Color[] baseColors;
    Animation visualAnimation;
    string currentAnimation;
    float hurtUntil;
    float deactivateAt;
    bool dying;
    Vector2 targetPoint;
    bool windingUp;
    bool hurtFlash;
    float strikeAt;
    float nextAttackAt;
    float stunUntil;
    float impactUntil;
    Vector3 telegraphBaseScale;

    void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }
    void OnDisable() => Active.Remove(this);

    public void Configure(int hitPoints, int contactDamage, float speed)
    {
        maxHealth = Mathf.Max(1, hitPoints);
        health = maxHealth;
        damage = Mathf.Max(0, contactDamage);
        moveSpeed = Mathf.Max(0f, speed);
    }

    public void SetTelegraph(Transform marker, SpriteRenderer fill, SpriteRenderer outline)
    {
        telegraph = marker;
        telegraphFill = fill;
        telegraphRing = outline;
        if (telegraph != null)
        {
            telegraphBaseScale = telegraph.localScale;
            telegraph.gameObject.SetActive(false);
        }
    }

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();
        spawnPosition = body.position;
        Transform visual = transform.Find("Visual");
        visualRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        visualAnimation = visual != null ? visual.GetComponentInChildren<Animation>(true) : null;
        PlayVisual("AshSlime_Idle", true);
        baseColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++) baseColors[i] = visualRenderers[i].color;
        if (telegraph != null)
        {
            telegraphBaseScale = telegraph.localScale;
            telegraph.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (windingUp || Time.time < stunUntil)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        HuntzSlicePlayer player = HuntzSlicePlayer.Instance;
        if (player == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.transform.position - body.position;
        if (toPlayer.magnitude > detectionRange)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (toPlayer.magnitude <= attackRange && Time.time >= nextAttackAt)
        {
            BeginTelegraph(player.transform.position);
            body.linearVelocity = Vector2.zero;
        }
        else if (toPlayer.magnitude > attackRange * 0.78f)
        {
            body.linearVelocity = toPlayer.normalized * moveSpeed;
        }
        else
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        if (dying)
        {
            if (Time.time >= deactivateAt) gameObject.SetActive(false);
            return;
        }

        if (windingUp)
        {
            float pulse = 0.9f + Mathf.PingPong(Time.time * 1.8f, 0.25f);
            telegraph.localScale = telegraphBaseScale * pulse;
            if (telegraphFill != null) telegraphFill.color = new Color(1f, 0.12f, 0.08f, 0.2f + pulse * 0.12f);
            if (Time.time >= strikeAt) ResolveTelegraph();
        }
        else if (impactUntil > 0f && Time.time >= impactUntil)
        {
            if (telegraph != null) telegraph.gameObject.SetActive(false);
            impactUntil = 0f;
        }

        if (Time.time < hurtUntil) PlayVisual("AshSlime_Hurt");
        else PlayVisual(body != null && body.linearVelocity.sqrMagnitude > 0.04f ? "AshSlime_Move" : "AshSlime_Idle");

        for (int i = 0; i < visualRenderers.Length; i++)
            visualRenderers[i].color = hurtFlash ? Color.white : baseColors[i];
        hurtFlash = false;
    }

    void BeginTelegraph(Vector2 point)
    {
        windingUp = true;
        targetPoint = point;
        strikeAt = Time.time + telegraphDuration;
        if (telegraph != null)
        {
            telegraph.position = new Vector3(point.x, point.y + 0.03f, 0f);
            telegraph.localScale = telegraphBaseScale;
            telegraph.gameObject.SetActive(true);
        }
    }

    void ResolveTelegraph()
    {
        windingUp = false;
        nextAttackAt = Time.time + attackCooldown;
        HuntzSlicePlayer player = HuntzSlicePlayer.Instance;
        if (player != null && Vector2.Distance(player.transform.position, targetPoint) <= attackRadius)
            player.TakeHit(transform.position, damage);

        if (telegraphFill != null) telegraphFill.color = new Color(1f, 0.08f, 0.05f, 0.48f);
        if (telegraphRing != null) telegraphRing.color = new Color(1f, 0.72f, 0.28f, 1f);
        if (telegraph != null) telegraph.localScale = telegraphBaseScale * 1.08f;
        impactUntil = Time.time + 0.12f;
    }

    public void TakeDamage(int amount, Vector2 direction)
    {
        if (!isActiveAndEnabled || amount <= 0 || health <= 0 || dying) return;
        health -= amount;
        hurtFlash = true;
        hurtUntil = Time.time + 0.18f;
        stunUntil = Time.time + 0.18f;
        body.linearVelocity = direction.normalized * 2.7f;
        if (windingUp)
        {
            windingUp = false;
            if (telegraph != null) telegraph.gameObject.SetActive(false);
        }

        if (health <= 0)
        {
            dying = true;
            deactivateAt = Time.time + 0.5f;
            if (hitCollider != null) hitCollider.enabled = false;
            if (telegraph != null) telegraph.gameObject.SetActive(false);
            body.linearVelocity = Vector2.zero;
            PlayVisual("AshSlime_Death", true);
        }
    }


    void PlayVisual(string clipName, bool restart = false)
    {
        if (visualAnimation == null || visualAnimation[clipName] == null) return;
        AnimationState state = visualAnimation[clipName];
        state.wrapMode = clipName == "AshSlime_Idle" || clipName == "AshSlime_Move"
            ? WrapMode.Loop : WrapMode.Once;
        if (!restart && currentAnimation == clipName && visualAnimation.IsPlaying(clipName)) return;
        if (!restart && currentAnimation == clipName && state.wrapMode == WrapMode.Once) return;
        if (visualAnimation.isPlaying) visualAnimation.CrossFade(clipName, 0.04f);
        else visualAnimation.Play(clipName);
        currentAnimation = clipName;
    }

    public void ResetForExpedition()
    {
        gameObject.SetActive(true);
        health = maxHealth;
        dying = false;
        deactivateAt = 0f;
        hurtUntil = 0f;
        currentAnimation = null;
        PlayVisual("AshSlime_Idle", true);
        windingUp = false;
        hurtFlash = false;
        strikeAt = 0f;
        stunUntil = 0f;
        impactUntil = 0f;
        nextAttackAt = Time.time + 0.65f;
        if (body != null)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
        }
        transform.position = spawnPosition;
        if (hitCollider != null) hitCollider.enabled = true;
        if (telegraph != null) telegraph.gameObject.SetActive(false);
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null) continue;
            visualRenderers[i].enabled = true;
            visualRenderers[i].color = baseColors[i];
        }
    }
}
