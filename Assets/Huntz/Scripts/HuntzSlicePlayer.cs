using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
[RequireComponent(typeof(HuntzDirectionalAnimator))]
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
    [SerializeField] float attackLength = 1.05f;
    [SerializeField] float attackWidth = 0.74f;
    [SerializeField] float attackCooldown = 0.32f;
    [SerializeField] HuntzDirectionalAnimator directionalAnimator;
    [SerializeField] HuntzEquippableWeapon weaponEquipment;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public int CurrentAttackDamage => attackDamage + damageBonus + (weaponEquipment != null ? weaponEquipment.DamageBonus : 0);
    float CurrentAttackReach => attackReach + (weaponEquipment != null ? weaponEquipment.ReachBonus : 0f);
    float CurrentAttackWidth => attackWidth + (weaponEquipment != null ? weaponEquipment.WidthBonus : 0f);

    Rigidbody2D body;
    Transform visual;
    SpriteRenderer slash;
    HuntzDirectionalAnimator slashAnimation;
    SpriteRenderer[] visualRenderers;
    Color[] baseColors;
    InputAction moveAction;
    InputAction dodgeAction;
    InputAction attackAction;
    readonly HashSet<HuntzSliceEnemy> attackHitEnemies = new HashSet<HuntzSliceEnemy>();
    readonly HashSet<HuntzSliceBoss> attackHitBosses = new HashSet<HuntzSliceBoss>();
    Vector2 movement;
    Vector2 facing = Vector2.down;
    Vector2 dodgeDirection = Vector2.down;
    Vector2 attackDirection = Vector2.down;
    int damageBonus;
    float dodgingUntil;
    float invulnerableUntil;
    float slashUntil;
    float attackStartedAt;
    float attackImpactAt;
    float attackDuration;
    float attackAnimationUntil;
    float hurtAnimationUntil;
    float deathReturnAt;
    float nextAttackAt;
    bool showHitFlash;
    bool defeated;
    bool attackActive;
    bool attackImpactResolved;
    int attackImpactFrame;
    bool restartAttackAnimation;
    bool restartDodgeAnimation;
    bool restartHurtAnimation;

    void Awake()
    {
        Instance = this;
        body = GetComponent<Rigidbody2D>();
        Health = maxHealth;
        CreateInputActions();

        visual = transform.Find("Visual");
        Transform slashTransform = transform.Find("Slash");
        if (slashTransform != null)
        {
            slash = slashTransform.GetComponent<SpriteRenderer>();
            slashAnimation = slashTransform.GetComponent<HuntzDirectionalAnimator>();
        }
        if (slash != null) slash.enabled = false;
        if (directionalAnimator == null)
            directionalAnimator = GetComponent<HuntzDirectionalAnimator>();

        SpriteRenderer characterRenderer = directionalAnimator != null ? directionalAnimator.TargetRenderer : null;
        if (characterRenderer == null && visual != null)
            characterRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        weaponEquipment = weaponEquipment != null ? weaponEquipment : GetComponent<HuntzEquippableWeapon>();
        if (weaponEquipment == null) weaponEquipment = gameObject.AddComponent<HuntzEquippableWeapon>();
        weaponEquipment.Initialize(characterRenderer, visual, directionalAnimator);
        if (directionalAnimator != null) directionalAnimator.FrameChanged += OnAnimationFrameChanged;

        visualRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        baseColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++) baseColors[i] = visualRenderers[i].color;
    }

    void OnEnable()
    {
        Instance = this;
        moveAction?.Enable();
        dodgeAction?.Enable();
        attackAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
        dodgeAction?.Disable();
        attackAction?.Disable();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (directionalAnimator != null) directionalAnimator.FrameChanged -= OnAnimationFrameChanged;
        moveAction?.Dispose();
        dodgeAction?.Dispose();
        attackAction?.Dispose();
    }

    void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        dodgeAction = new InputAction("Dodge", InputActionType.Button);
        dodgeAction.AddBinding("<Keyboard>/space");
        dodgeAction.AddBinding("<Gamepad>/rightShoulder");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/j");
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Gamepad>/buttonWest");
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

        Vector2 screenInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        float inputMagnitude = Mathf.Clamp01(screenInput.magnitude);

        movement = GridToWorld(ScreenDirectionToGrid(screenInput), inputMagnitude);
        if (movement.sqrMagnitude > 0.001f && Time.time >= dodgingUntil) facing = movement;

        if (dodgeAction != null && dodgeAction.WasPressedThisFrame()) BeginDodge();
        bool attackPressed = attackAction != null && attackAction.WasPressedThisFrame();
        if (attackPressed && Time.time >= nextAttackAt && Time.time >= dodgingUntil && Time.time >= hurtAnimationUntil)
            Attack();

        UpdateAttack();
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

    static Vector2 GridToWorld(Vector2 gridInput, float inputMagnitude)
    {
        // Keyboard directions are logical grid axes; project them into the isometric world.
        Vector2 projected = new Vector2(
            (gridInput.x - gridInput.y) * 0.5f,
            (gridInput.x + gridInput.y) * 0.25f);
        return projected.sqrMagnitude > 0.001f ? projected.normalized * inputMagnitude : Vector2.zero;
    }

    static Vector2 ScreenDirectionToGrid(Vector2 screenDirection)
    {
        return new Vector2(
            screenDirection.x + 2f * screenDirection.y,
            -screenDirection.x + 2f * screenDirection.y);
    }

    void BeginDodge()
    {
        if (Time.time < dodgingUntil) return;
        CancelAttack();
        dodgeDirection = movement.sqrMagnitude > 0.001f ? movement : facing;
        float visualDuration = directionalAnimator != null
            ? directionalAnimator.GetActionDuration("Dodge", dodgeDirection, dodgeDuration) : dodgeDuration;
        dodgingUntil = Time.time + Mathf.Max(dodgeDuration, visualDuration);
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + dodgeInvulnerability);
        restartDodgeAnimation = true;
    }

    void Attack()
    {
        if (defeated || attackActive || Time.time < dodgingUntil) return;
        attackDirection = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector2.down;
        attackDuration = directionalAnimator != null
            ? directionalAnimator.GetActionDuration("Attack", attackDirection, 0.43f) : 0.43f;
        int frameCount = directionalAnimator != null
            ? directionalAnimator.GetActionFrameCount("Attack", attackDirection, 6) : 6;
        float framesPerSecond = directionalAnimator != null
            ? directionalAnimator.GetActionFramesPerSecond("Attack", attackDirection, 14f) : 14f;
        attackImpactFrame = Mathf.Clamp(Mathf.CeilToInt(frameCount / 3f), 1, Mathf.Max(1, frameCount - 1));
        attackStartedAt = Time.time;
        attackImpactAt = attackStartedAt + attackImpactFrame / Mathf.Max(1f, framesPerSecond);
        attackAnimationUntil = attackStartedAt + attackDuration;
        nextAttackAt = attackAnimationUntil + attackCooldown;
        attackActive = true;
        attackImpactResolved = false;
        restartAttackAnimation = true;
        attackHitEnemies.Clear();
        attackHitBosses.Clear();
    }

    void UpdateAttack()
    {
        if (!attackActive) return;
        if (!attackImpactResolved && directionalAnimator == null && Time.time >= attackImpactAt)
            ResolveAttackImpact();
        if (Time.time < attackAnimationUntil) return;
        attackActive = false;
        attackAnimationUntil = 0f;
        if (weaponEquipment != null) weaponEquipment.SetPose("Idle", facing, 0f);
    }

    void OnAnimationFrameChanged(string action, int previousFrame, int frameIndex)
    {
        if (!attackActive || attackImpactResolved || action != "Attack" ||
            previousFrame >= attackImpactFrame || frameIndex < attackImpactFrame)
            return;

        ResolveAttackImpact();
    }

    void ResolveAttackImpact()
    {
        if (!attackActive || attackImpactResolved) return;
        attackImpactResolved = true;
        if (weaponEquipment != null) weaponEquipment.SetImpactPose(attackDirection, attackDuration * 0.16f);

        float reach = CurrentAttackReach;
        float width = CurrentAttackWidth;
        Vector2 center = body.position + attackDirection * reach;
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg - 90f;
        Vector2 areaSize = new Vector2(width, attackLength);
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, areaSize, angle))
        {
            HuntzSliceEnemy enemy = hit.GetComponentInParent<HuntzSliceEnemy>();
            if (enemy != null && attackHitEnemies.Add(enemy))
                enemy.TakeDamage(CurrentAttackDamage, attackDirection);
            HuntzSliceBoss boss = hit.GetComponentInParent<HuntzSliceBoss>();
            if (boss != null && attackHitBosses.Add(boss))
                boss.TakeDamage(CurrentAttackDamage);
        }

        if (slash != null)
        {
            slash.transform.position = center;
            slash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg);
            slash.color = new Color(1f, 0.94f, 0.62f, 0.9f);
            slash.enabled = true;
            if (slashAnimation != null) slashAnimation.PlayClip("SwordArc_Loop", true);
            slashUntil = Time.time + (slashAnimation != null
                ? slashAnimation.GetActionDuration("Loop", Vector2.down, 0.17f) : 0.17f);
        }
    }

    void CancelAttack()
    {
        attackActive = false;
        attackImpactResolved = false;
        attackAnimationUntil = 0f;
        restartAttackAnimation = false;
        slashUntil = 0f;
        if (slash != null) slash.enabled = false;
        if (weaponEquipment != null) weaponEquipment.SetPose("Idle", facing, 0f);
    }

    public void TakeHit(Vector2 fromPosition, int damage)
    {
        if (defeated || Time.time < invulnerableUntil) return;

        Health = Mathf.Max(0, Health - damage);
        CancelAttack();
        hurtAnimationUntil = Time.time + (directionalAnimator != null
            ? directionalAnimator.GetActionDuration("Hurt", facing, 0.18f) : 0.18f);
        restartHurtAnimation = true;
        if (Health == 0)
        {
            defeated = true;
            movement = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            deathReturnAt = Time.time + (directionalAnimator != null
                ? directionalAnimator.GetActionDuration("Death", facing, 0.67f) : 0.67f);
            HuntzSliceHud.ShowMessage("VocÃª foi derrotado. A expediÃ§Ã£o terminou.");
            return;
        }

        Vector2 away = ((Vector2)transform.position - fromPosition).normalized;
        body.linearVelocity = away * 3.5f;
        invulnerableUntil = Time.time + 0.72f;
        showHitFlash = true;
        HuntzSliceHud.ShowMessage("VocÃª sofreu um golpe!");
    }

    public void MoveToRoom(Vector2 position)
    {
        CancelAttack();
        body.position = position;
        transform.position = position;
        body.linearVelocity = Vector2.zero;
        movement = Vector2.zero;
        dodgingUntil = 0f;
        restartDodgeAnimation = false;
    }

    public void ResetForExpedition(Vector2 position, int bonusDamage)
    {
        CancelAttack();
        defeated = false;
        Health = maxHealth;
        SetDamageBonus(bonusDamage);
        showHitFlash = false;
        invulnerableUntil = 0f;
        hurtAnimationUntil = 0f;
        deathReturnAt = 0f;
        nextAttackAt = 0f;
        restartDodgeAnimation = false;
        restartHurtAnimation = false;
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
            HuntzSliceHud.ShowMessage($"SantuÃ¡rio recuperou {Health - previousHealth} de vida.");
    }

    void UpdateVisuals()
    {
        if (slash != null && Time.time >= slashUntil) slash.enabled = false;
        bool invulnerable = Time.time < invulnerableUntil;
        bool visible = !invulnerable || Mathf.Repeat(Time.time * 14f, 1f) < 0.55f;
        bool dodging = Time.time < dodgingUntil;
        string action = defeated ? "Death"
            : dodging ? "Dodge"
            : Time.time < hurtAnimationUntil ? "Hurt"
            : attackActive ? "Attack"
            : body != null && body.linearVelocity.sqrMagnitude > 0.0025f ? "Walk" : "Idle";
        if (directionalAnimator != null)
        {
            bool restart = (action == "Attack" && restartAttackAnimation) ||
                          (action == "Dodge" && restartDodgeAnimation) ||
                          (action == "Hurt" && restartHurtAnimation);
            Vector2 actionFacing = action == "Dodge" ? dodgeDirection
                : action == "Attack" ? attackDirection : facing;
            directionalAnimator.PlaybackSpeed = action == "Walk" && body != null
                ? Mathf.Clamp(body.linearVelocity.magnitude / Mathf.Max(0.01f, moveSpeed), 0.1f, 1.5f) : 1f;
            directionalAnimator.PlayAction(action, actionFacing, restart);
            if (action == "Attack") restartAttackAnimation = false;
            if (action == "Dodge") restartDodgeAnimation = false;
            if (action == "Hurt") restartHurtAnimation = false;
        }

        if (weaponEquipment != null)
        {
            float attackProgress = attackActive
                ? Mathf.Clamp01((Time.time - attackStartedAt) / Mathf.Max(0.001f, attackDuration))
                : 0f;
            weaponEquipment.SetPose(action, action == "Attack" ? attackDirection : facing, attackProgress);
        }

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

    void OnDrawGizmosSelected()
    {
        Vector2 attackDirection = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector2.down;
        Vector2 center = (Vector2)transform.position + attackDirection * CurrentAttackReach;
        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg - 90f;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, angle), Vector3.one);
        Gizmos.color = new Color(1f, 0.25f, 0.12f, 0.75f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(CurrentAttackWidth, attackLength, 0f));
        Gizmos.matrix = previousMatrix;
    }
}
