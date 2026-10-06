using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class HuntzSlicePlayer : MonoBehaviour
{
    public static HuntzSlicePlayer Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeSingleton() => Instance = null;

    [SerializeField] float moveSpeed = 3.2f;
    [SerializeField] float dodgeSpeed = 8f;
    [SerializeField] float dodgeDuration = 0.16f;
    [SerializeField] float dodgeInvulnerability = 0.42f;
    [SerializeField] int maxHealth = 8;
    [SerializeField] int attackDamage = 1;
    [SerializeField] float attackReach = 0.72f;
    [SerializeField] float attackRadius = 0.5f;
    [SerializeField] float attackCooldown = 0.32f;
    [SerializeField] HuntzDirectionalAnimator directionalAnimator;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public int CurrentAttackDamage => attackDamage + damageBonus;

    Rigidbody2D body;
    Transform visual;
    SpriteRenderer slash;
    Animation slashAnimation;
    SpriteRenderer[] visualRenderers;
    Color[] baseColors;
    Vector2 movement;
    Vector2 facing = Vector2.down;
    Vector2 dodgeDirection = Vector2.down;
    int damageBonus;
    float dodgingUntil;
    float invulnerableUntil;
    float slashUntil;
    float attackAnimationUntil;
    float hurtAnimationUntil;
    float deathReturnAt;
    float nextAttackAt;
    bool showHitFlash;
    bool defeated;

    void Awake()
    {
        Instance = this;
        body = GetComponent<Rigidbody2D>();
        Health = maxHealth;

        visual = transform.Find("Visual");
        Transform slashTransform = transform.Find("Slash");
        if (slashTransform != null)
        {
            slash = slashTransform.GetComponent<SpriteRenderer>();
            slashAnimation = slashTransform.GetComponent<Animation>();
        }
        if (slash != null) slash.enabled = false;
        if (directionalAnimator == null)
            directionalAnimator = GetComponent<HuntzDirectionalAnimator>();

        visualRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        baseColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++) baseColors[i] = visualRenderers[i].color;
    }

    void OnEnable() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Instance != this) Instance = this;
        if (defeated)
        {
            movement = Vector2.zero;
            UpdateVisuals();
            if (deathReturnAt > 0f && Time.time >= deathReturnAt)
            {
                deathReturnAt = 0f;
                HuntzSliceHunt hunt = HuntzSliceHunt.Instance;
                if (hunt == null) hunt = FindAnyObjectByType<HuntzSliceHunt>();
                if (hunt != null) hunt.EndByPlayerDeath();
            }
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
        if (input.sqrMagnitude > 1f) input.Normalize();

        movement = ToIsometric(input);
        if (movement.sqrMagnitude > 0.001f) facing = movement;

        if (keyboard.spaceKey.wasPressedThisFrame) BeginDodge();
        bool attackPressed = keyboard.jKey.wasPressedThisFrame ||
                             (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (attackPressed && Time.time >= nextAttackAt) Attack();

        UpdateVisuals();
    }

    void FixedUpdate()
    {
        if (defeated)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < dodgingUntil) body.linearVelocity = dodgeDirection * dodgeSpeed;
        else body.linearVelocity = movement * moveSpeed;
    }

    static Vector2 ToIsometric(Vector2 input)
    {
        Vector2 projected = new Vector2((input.x - input.y) * 0.5f, (input.x + input.y) * 0.25f);
        return projected.sqrMagnitude > 0.001f ? projected.normalized : Vector2.zero;
    }

    void BeginDodge()
    {
        if (Time.time < dodgingUntil) return;
        dodgeDirection = movement.sqrMagnitude > 0.001f ? movement : facing;
        dodgingUntil = Time.time + dodgeDuration;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + dodgeInvulnerability);
    }

    void Attack()
    {
        if (defeated) return;
        nextAttackAt = Time.time + attackCooldown;
        attackAnimationUntil = Time.time + 0.43f;
        Vector2 center = body.position + facing * attackReach + Vector2.up * 0.2f;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, attackRadius))
        {
            HuntzSliceEnemy enemy = hit.GetComponentInParent<HuntzSliceEnemy>();
            if (enemy != null) enemy.TakeDamage(CurrentAttackDamage, facing);
            HuntzSliceBoss boss = hit.GetComponentInParent<HuntzSliceBoss>();
            if (boss != null) boss.TakeDamage(CurrentAttackDamage);
        }

        if (slash != null)
        {
            slash.transform.localPosition = new Vector3(facing.x * 0.5f, 0.4f + facing.y * 0.5f, 0f);
            slash.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            slash.color = new Color(1f, 0.94f, 0.62f, 0.9f);
            slash.enabled = true;
            if (slashAnimation != null) slashAnimation.Play("SwordArc_Loop");
            slashUntil = Time.time + 0.14f;
        }
    }

    public void TakeHit(Vector2 fromPosition, int damage)
    {
        if (defeated || Time.time < invulnerableUntil) return;

        Health = Mathf.Max(0, Health - damage);
        hurtAnimationUntil = Time.time + 0.18f;
        if (Health == 0)
        {
            defeated = true;
            movement = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            deathReturnAt = Time.time + 0.67f;
            HuntzSliceHud.ShowMessage("Você foi derrotado. A expedição terminou.");
            return;
        }

        Vector2 away = ((Vector2)transform.position - fromPosition).normalized;
        body.linearVelocity = away * 3.5f;
        invulnerableUntil = Time.time + 0.72f;
        showHitFlash = true;
        HuntzSliceHud.ShowMessage("Você sofreu um golpe!");
    }

    public void MoveToRoom(Vector2 position)
    {
        body.position = position;
        transform.position = position;
        body.linearVelocity = Vector2.zero;
        movement = Vector2.zero;
        dodgingUntil = 0f;
    }

    public void ResetForExpedition(Vector2 position, int bonusDamage)
    {
        defeated = false;
        Health = maxHealth;
        SetDamageBonus(bonusDamage);
        showHitFlash = false;
        invulnerableUntil = 0f;
        slashUntil = 0f;
        attackAnimationUntil = 0f;
        hurtAnimationUntil = 0f;
        deathReturnAt = 0f;
        nextAttackAt = 0f;
        if (slash != null) slash.enabled = false;
        MoveToRoom(position);
    }

    public void SetDamageBonus(int bonusDamage) => damageBonus = Mathf.Max(0, bonusDamage);

    public void Heal(int amount)
    {
        if (amount <= 0 || defeated) return;
        int previousHealth = Health;
        Health = Mathf.Min(maxHealth, Health + amount);
        if (Health > previousHealth)
            HuntzSliceHud.ShowMessage($"Santuário recuperou {Health - previousHealth} de vida.");
    }

    void UpdateVisuals()
    {
        if (slash != null && Time.time >= slashUntil) slash.enabled = false;
        bool invulnerable = Time.time < invulnerableUntil;
        bool visible = !invulnerable || Mathf.Repeat(Time.time * 14f, 1f) < 0.55f;
        bool dodging = Time.time < dodgingUntil;
        string action = defeated ? "Death"
            : dodging ? "Dodge"
            : Time.time < attackAnimationUntil ? "Attack"
            : Time.time < hurtAnimationUntil ? "Hurt"
            : movement.sqrMagnitude > 0.001f ? "Walk" : "Idle";
        if (directionalAnimator != null) directionalAnimator.PlayAction(action, facing);

        for (int i = 0; i < visualRenderers.Length; i++)
        {
            Color color = baseColors[i];
            if (showHitFlash && visible) color = Color.white;
            else if (dodging && visible) color = Color.Lerp(color, new Color(0.35f, 0.95f, 1f, color.a), 0.8f);
            color.a *= visible ? 1f : 0.2f;
            visualRenderers[i].color = color;
        }

        showHitFlash = false;
    }
}
