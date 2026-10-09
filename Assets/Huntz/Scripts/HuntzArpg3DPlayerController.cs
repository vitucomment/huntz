using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class HuntzArpg3DPlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] float moveSpeed = 4.8f;
    [SerializeField, Min(1f)] float sprintMultiplier = 1.55f;
    [SerializeField, Min(1f)] float acceleration = 34f;
    [SerializeField, Min(1f)] float deceleration = 42f;
    [SerializeField, Range(0.1f, 1f)] float attackMoveMultiplier = 0.62f;
    [SerializeField, Min(0f)] float dodgeSpeed = 12.5f;
    [SerializeField, Min(0f)] float dodgeCooldown = 0.4f;
    [SerializeField, Min(1f)] float maxStamina = 100f;
    [SerializeField, Min(0f)] float sprintStaminaPerSecond = 22f;
    [SerializeField, Min(0f)] float dodgeStaminaCost = 28f;
    [SerializeField, Min(0f)] float staminaRegenPerSecond = 34f;
    [SerializeField, Min(0f)] float staminaRegenDelay = 0.45f;
    [SerializeField, Min(0f)] float minimumStaminaToSprint = 12f;
    [SerializeField, Range(0f, 0.3f)] float attackInputBuffer = 0.16f;
    [SerializeField, Min(0f)] float dodgeInvulnerabilityTail = 0.08f;
    [SerializeField, Min(1f)] float facingTurnSpeed = 1080f;
    [SerializeField, Min(0.1f)] float attackRange = 1.65f;
    [SerializeField, Min(0.1f)] float interactionRange = 2.5f;
    [SerializeField, Min(1)] int attackDamage = 1;
    [SerializeField, Min(1)] int maxHealth = 5;
    [SerializeField] HuntzArpg3DRig rig;
    [SerializeField] HuntzArpg3DAppearance appearance;
    [SerializeField] HuntzArpg3DCameraFollow cameraFollow;

    CharacterController motor;
    Camera aimCamera;
    InputAction moveAction;
    InputAction attackAction;
    InputAction dodgeAction;
    InputAction sprintAction;
    InputAction interactAction;
    InputAction appearanceAction;
    InputAction restartAction;
    readonly HashSet<HuntzArpg3DTarget> hitTargets = new HashSet<HuntzArpg3DTarget>();
    Vector3 facing = Vector3.forward;
    Vector3 dodgeDirection = Vector3.forward;
    Vector3 planarVelocity;
    Vector3 spawnPosition;
    float verticalVelocity;
    float hurtControlLockUntil;
    float dodgeEndsAt;
    float nextDodgeAt;
    float invulnerableUntil;
    float attackEndsAt;
    float nextAttackAt;
    float bufferedAttackUntil = float.NegativeInfinity;
    float lastStaminaSpendAt = float.NegativeInfinity;
    float stamina;
    int health;
    int riftFragments;
    bool isDead;
    string regionName = "MainLand";
    string feedback = "Explore a MainLand e atravesse o portal para a hunt de treino.";
    float feedbackUntil;

    public bool IsDead => isDead;
    public int Health => health;
    public int MaxHealth => maxHealth;
    public float Stamina => stamina;
    public float MaxStamina => maxStamina;
    public int RiftFragments => riftFragments;
    public event Action ResetToSpawn;

    public void Configure(HuntzArpg3DRig characterRig, HuntzArpg3DAppearance characterAppearance, HuntzArpg3DCameraFollow followCamera)
    {
        rig = characterRig;
        appearance = characterAppearance;
        cameraFollow = followCamera;
    }

    void Awake()
    {
        motor = GetComponent<CharacterController>();
        if (rig == null) rig = GetComponentInChildren<HuntzArpg3DRig>();
        if (appearance == null) appearance = GetComponentInChildren<HuntzArpg3DAppearance>();
        if (cameraFollow == null) cameraFollow = FindAnyObjectByType<HuntzArpg3DCameraFollow>();
        aimCamera = cameraFollow != null ? cameraFollow.GetComponent<Camera>() : Camera.main;
        spawnPosition = transform.position;
        health = maxHealth;
        stamina = maxStamina;
        CreateInputActions();
    }

    void OnEnable()
    {
        EnableActions(true);
        if (rig != null) rig.WeaponImpact += ResolveWeaponImpact;
    }

    void OnDisable()
    {
        EnableActions(false);
        if (rig != null) rig.WeaponImpact -= ResolveWeaponImpact;
    }

    void OnDestroy()
    {
        moveAction?.Dispose();
        attackAction?.Dispose();
        dodgeAction?.Dispose();
        sprintAction?.Dispose();
        interactAction?.Dispose();
        appearanceAction?.Dispose();
        restartAction?.Dispose();
    }

    void Update()
    {
        if (restartAction.WasPressedThisFrame())
        {
            ResetPlayer();
            return;
        }
        if (isDead) return;

        if (appearanceAction.WasPressedThisFrame() && appearance != null)
        {
            appearance.CycleProfile();
            ShowFeedback("Visual do personagem: " + appearance.ActiveProfileName);
        }

        if (interactAction.WasPressedThisFrame()) TryInteract();
        if (attackAction.WasPressedThisFrame()) bufferedAttackUntil = Time.time + attackInputBuffer;
        if (attackAction.IsPressed() || Time.time <= bufferedAttackUntil) TryAttack();
        if (dodgeAction.WasPressedThisFrame()) TryDodge();

        Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
        Vector3 desiredMove = CameraRelativeMove(input);
        bool dodging = Time.time < dodgeEndsAt;
        bool hurtLocked = Time.time < hurtControlLockUntil;
        bool attacking = Time.time < attackEndsAt;
        bool sprinting = sprintAction.IsPressed() && desiredMove.sqrMagnitude > 0.01f
            && !hurtLocked && !dodging && stamina >= minimumStaminaToSprint;
        UpdateStamina(sprinting);

        Vector3 targetVelocity = desiredMove * moveSpeed * (sprinting ? sprintMultiplier : 1f);
        if (attacking) targetVelocity *= attackMoveMultiplier;
        if (hurtLocked) targetVelocity = Vector3.zero;

        if (dodging)
            planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, deceleration * 0.18f * Time.deltaTime);
        else
        {
            float response = targetVelocity.sqrMagnitude > 0.001f ? acceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, response * Time.deltaTime);
        }

        Vector3 horizontalVelocity = dodging ? dodgeDirection * dodgeSpeed : planarVelocity;

        if (motor.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        motor.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (!hurtLocked && !dodging && !attacking && desiredMove.sqrMagnitude > 0.001f)
            SetFacing(desiredMove, false);

        if (rig != null)
        {
            Vector3 actualPlanarVelocity = motor.velocity;
            actualPlanarVelocity.y = 0f;
            float actualSpeed = actualPlanarVelocity.magnitude;
            float speedInWalkUnits = actualSpeed / Mathf.Max(0.01f, moveSpeed);
            float gaitRate = 1f;
            if (actualSpeed > 0.01f)
            {
                if (actualSpeed <= moveSpeed)
                    gaitRate = Mathf.Lerp(0.75f, 1.3f, Mathf.Clamp01(actualSpeed / Mathf.Max(0.01f, moveSpeed)));
                else
                {
                    float sprintRange = Mathf.Max(0.01f, moveSpeed * (sprintMultiplier - 1f));
                    gaitRate = Mathf.Lerp(1.3f, 1.2f, Mathf.Clamp01((actualSpeed - moveSpeed) / sprintRange));
                }
            }

            rig.SetMoveSpeed(!hurtLocked && !dodging
                ? Mathf.Clamp(speedInWalkUnits, 0f, sprintMultiplier)
                : 0f, gaitRate);
        }
        if (transform.position.y < -8f) ResetPlayer();
    }

    void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Keyboard>/j");
        dodgeAction = new InputAction("Dodge", InputActionType.Button, "<Keyboard>/space");
        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Keyboard>/rightShift");
        interactAction = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        appearanceAction = new InputAction("Appearance", InputActionType.Button, "<Keyboard>/c");
        restartAction = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");
    }

    void EnableActions(bool enabled)
    {
        SetActionEnabled(moveAction, enabled);
        SetActionEnabled(attackAction, enabled);
        SetActionEnabled(dodgeAction, enabled);
        SetActionEnabled(sprintAction, enabled);
        SetActionEnabled(interactAction, enabled);
        SetActionEnabled(appearanceAction, enabled);
        SetActionEnabled(restartAction, enabled);
    }

    static void SetActionEnabled(InputAction action, bool enabled)
    {
        if (action == null) return;
        if (enabled && !action.enabled) action.Enable();
        if (!enabled && action.enabled) action.Disable();
    }

    Vector3 CameraRelativeMove(Vector2 input)
    {
        Transform cameraTransform = aimCamera != null ? aimCamera.transform : transform;
        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
        Vector3 right = cameraTransform.right;
        right.y = 0f;
        right = right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
        return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
    }

    void AimAtPointer()
    {
        if (Mouse.current == null || aimCamera == null) return;
        Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float distance)) return;
        Vector3 point = ray.GetPoint(distance);
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.02f) SetFacing(direction, true);
    }

    void SetFacing(Vector3 direction, bool snap)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;
        facing = direction.normalized;
        Quaternion targetRotation = Quaternion.LookRotation(facing, Vector3.up);
        transform.rotation = snap
            ? targetRotation
            : Quaternion.RotateTowards(transform.rotation, targetRotation, facingTurnSpeed * Time.deltaTime);
        facing = transform.forward;
        facing.y = 0f;
        facing.Normalize();
    }

    void UpdateStamina(bool sprinting)
    {
        if (sprinting)
        {
            stamina = Mathf.Max(0f, stamina - sprintStaminaPerSecond * Time.deltaTime);
            lastStaminaSpendAt = Time.time;
            return;
        }

        if (Time.time < lastStaminaSpendAt + staminaRegenDelay) return;
        stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.deltaTime);
    }

    void TryAttack()
    {
        if (isDead || Time.time < hurtControlLockUntil || Time.time < dodgeEndsAt
            || Time.time < attackEndsAt || Time.time < nextAttackAt || rig == null) return;
        AimAtPointer();
        attackEndsAt = Time.time + rig.AttackDuration;
        nextAttackAt = attackEndsAt + 0.08f;
        bufferedAttackUntil = float.NegativeInfinity;
        hitTargets.Clear();
        rig.PlayAttack();
    }

    void TryDodge()
    {
        if (isDead || Time.time < hurtControlLockUntil || Time.time < dodgeEndsAt
            || Time.time < nextDodgeAt || rig == null) return;
        if (stamina < dodgeStaminaCost)
        {
            ShowFeedback("Vigor insuficiente para esquivar.");
            return;
        }

        Vector3 direction = CameraRelativeMove(moveAction.ReadValue<Vector2>());
        if (direction.sqrMagnitude <= 0.01f)
            direction = planarVelocity.sqrMagnitude > 0.04f ? planarVelocity : facing;
        dodgeDirection = direction.normalized;
        SetFacing(dodgeDirection, true);
        stamina = Mathf.Max(0f, stamina - dodgeStaminaCost);
        lastStaminaSpendAt = Time.time;
        bufferedAttackUntil = float.NegativeInfinity;
        attackEndsAt = 0f;
        dodgeEndsAt = Time.time + rig.DodgeDuration;
        nextDodgeAt = dodgeEndsAt + dodgeCooldown;
        invulnerableUntil = dodgeEndsAt + dodgeInvulnerabilityTail;
        planarVelocity = dodgeDirection * dodgeSpeed;
        rig.PlayDodge();
    }

    void TryInteract()
    {
        HuntzArpg3DInteractable nearest = null;
        float nearestDistance = interactionRange;
        Collider[] overlaps = Physics.OverlapSphere(transform.position + Vector3.up * 0.7f,
            interactionRange, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider overlap in overlaps)
        {
            HuntzArpg3DInteractable candidate = overlap.GetComponentInParent<HuntzArpg3DInteractable>();
            if (candidate == null) continue;
            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance >= nearestDistance) continue;
            nearest = candidate;
            nearestDistance = distance;
        }

        if (nearest != null) nearest.Interact(this);
        else ShowFeedback("Não há nada interativo por perto.");
    }

    void ResolveWeaponImpact()
    {
        Vector3 center = transform.position + Vector3.up * 0.85f + facing * attackRange;
        Collider[] overlaps = Physics.OverlapSphere(center, 1.15f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider overlap in overlaps)
        {
            HuntzArpg3DTarget target = overlap.GetComponentInParent<HuntzArpg3DTarget>();
            if (target == null || !hitTargets.Add(target)) continue;
            target.TakeHit(attackDamage);
        }
    }

    public void ReceiveDamage(int amount, Vector3 sourcePosition)
    {
        if (isDead || amount <= 0 || Time.time < invulnerableUntil) return;
        health = Mathf.Max(0, health - amount);
        Vector3 away = transform.position - sourcePosition;
        away.y = 0f;
        if (away.sqrMagnitude > 0.01f) SetFacing(away, true);
        ShowFeedback("Você recebeu dano. Vida: " + health + "/" + maxHealth);
        attackEndsAt = 0f;
        bufferedAttackUntil = float.NegativeInfinity;
        planarVelocity = Vector3.zero;

        if (health == 0)
        {
            isDead = true;
            hurtControlLockUntil = float.PositiveInfinity;
            if (rig != null) rig.PlayDeath();
            return;
        }

        hurtControlLockUntil = Time.time + (rig != null ? rig.HurtDuration : 0.25f);
        if (rig != null) rig.PlayHurt();
    }

    public void TeleportTo(Vector3 position, string destinationRegion)
    {
        motor.enabled = false;
        transform.position = position;
        motor.enabled = true;
        verticalVelocity = 0f;
        planarVelocity = Vector3.zero;
        dodgeEndsAt = 0f;
        nextDodgeAt = 0f;
        invulnerableUntil = 0f;
        attackEndsAt = 0f;
        bufferedAttackUntil = float.NegativeInfinity;
        regionName = string.IsNullOrWhiteSpace(destinationRegion) ? "Região de treino" : destinationRegion;
        if (cameraFollow != null) cameraFollow.SetTarget(transform, true);
        ShowFeedback("Você chegou a " + regionName + ".");
    }

    public void FaceDirection(Vector3 direction)
    {
        SetFacing(direction, true);
    }

    public void ReturnToMainlandAfterDeath(Vector3 position, string message)
    {
        motor.enabled = false;
        transform.position = position;
        motor.enabled = true;
        verticalVelocity = 0f;
        planarVelocity = Vector3.zero;
        dodgeEndsAt = 0f;
        nextDodgeAt = 0f;
        invulnerableUntil = 0f;
        attackEndsAt = 0f;
        bufferedAttackUntil = float.NegativeInfinity;
        health = maxHealth;
        stamina = maxStamina;
        isDead = false;
        hurtControlLockUntil = 0f;
        nextAttackAt = 0f;
        lastStaminaSpendAt = float.NegativeInfinity;
        regionName = "MainLand";
        if (rig != null) rig.ResetRig();
        if (cameraFollow != null) cameraFollow.SetTarget(transform, true);
        ShowFeedback(message);
    }

    public void AddRiftFragments(int amount)
    {
        if (amount <= 0) return;
        riftFragments = Mathf.Max(0, riftFragments + amount);
    }

    public void ShowFeedback(string message)
    {
        feedback = message;
        feedbackUntil = Time.time + 4f;
    }

    void ResetPlayer()
    {
        motor.enabled = false;
        transform.position = spawnPosition;
        transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        facing = Vector3.forward;
        dodgeDirection = Vector3.forward;
        motor.enabled = true;
        verticalVelocity = 0f;
        planarVelocity = Vector3.zero;
        health = maxHealth;
        stamina = maxStamina;
        isDead = false;
        hurtControlLockUntil = 0f;
        dodgeEndsAt = 0f;
        nextDodgeAt = 0f;
        invulnerableUntil = 0f;
        attackEndsAt = 0f;
        nextAttackAt = 0f;
        bufferedAttackUntil = float.NegativeInfinity;
        lastStaminaSpendAt = float.NegativeInfinity;
        regionName = "MainLand";
        if (rig != null) rig.ResetRig();
        if (cameraFollow != null) cameraFollow.SetTarget(transform, true);
        ShowFeedback("Você voltou para a MainLand.");
        ResetToSpawn?.Invoke();
    }

    void OnGUI()
    {
        GUI.Box(new Rect(14f, 14f, 500f, 250f), "HUNTZ — ARPG ISOMÉTRICO 3D");
        GUI.Label(new Rect(28f, 43f, 470f, 22f), "Região: " + regionName);
        GUI.Label(new Rect(28f, 65f, 470f, 20f), "Vida  " + health + "/" + maxHealth);
        DrawMeter(new Rect(28f, 85f, 470f, 12f), maxHealth > 0 ? (float)health / maxHealth : 0f,
            new Color(0.82f, 0.18f, 0.16f));
        GUI.Label(new Rect(28f, 101f, 470f, 20f), "Vigor  " + Mathf.CeilToInt(stamina) + "/" + Mathf.CeilToInt(maxStamina));
        DrawMeter(new Rect(28f, 121f, 470f, 12f), maxStamina > 0f ? stamina / maxStamina : 0f,
            new Color(0.95f, 0.68f, 0.16f));
        GUI.Label(new Rect(28f, 140f, 470f, 20f), "Fragmentos de fenda  " + riftFragments);
        GUI.Label(new Rect(28f, 162f, 470f, 20f), "WASD mover  •  Shift correr  •  Mouse/J atacar  •  Espaço esquivar");
        GUI.Label(new Rect(28f, 182f, 470f, 20f), "E interagir  •  C aparência  •  R reiniciar");
        GUI.Label(new Rect(28f, 202f, 470f, 20f), "Roda do mouse: ajustar zoom");
        string visibleFeedback = Time.time <= feedbackUntil ? feedback : "Aproxime-se de um ponto de interação e pressione E.";
        GUI.Label(new Rect(28f, 222f, 470f, 24f), visibleFeedback);
    }

    static void DrawMeter(Rect rect, float value, Color fillColor)
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = fillColor;
        float fillWidth = Mathf.Max(0f, rect.width - 2f) * Mathf.Clamp01(value);
        GUI.DrawTexture(new Rect(rect.x + 1f, rect.y + 1f, fillWidth, Mathf.Max(0f, rect.height - 2f)),
            Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
