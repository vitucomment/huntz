using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(HuntzHumanoidRig))]
public sealed class HuntzRigPilotController : MonoBehaviour
{
    [SerializeField, Min(0f)] float moveSpeed = 3.2f;
    [SerializeField, Min(0f)] float dodgeSpeed = 8f;
    [SerializeField, Min(0.01f)] float dodgeDuration = 0.24f;
    [SerializeField, Min(0.01f)] float attackDuration = 0.62f;
    [SerializeField, Min(0.01f)] float attackCooldown = 0.32f;
    [SerializeField, Min(1)] int maxHealth = 5;
    [SerializeField] HuntzHumanoidRig rig;
    [SerializeField] HuntzHumanoidAppearance appearance;
    [SerializeField] HuntzHumanoidAppearanceProfile[] appearanceProfiles;

    Rigidbody2D body;
    InputAction moveAction;
    InputAction dodgeAction;
    InputAction attackAction;
    InputAction appearanceAction;
    InputAction interactAction;
    InputAction restartAction;
    Vector2 movement;
    Vector2 facing = Vector2.down;
    Vector2 dodgeDirection = Vector2.down;
    Vector2 lockedActionDirection = Vector2.down;
    Vector2 startPosition;
    float actionLockedUntil;
    float dodgingUntil;
    float nextAttackAt;
    float nextDamageAt;
    int appearanceIndex;
    int health;
    HuntzHumanoidRigAction lockedAction = HuntzHumanoidRigAction.Idle;
    bool isDead;
    HuntzRigPilotInteractable nearbyInteractable;
    string feedback;
    float feedbackUntil;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (rig == null) rig = GetComponent<HuntzHumanoidRig>();
        if (appearance == null) appearance = GetComponent<HuntzHumanoidAppearance>();
        startPosition = transform.position;
        health = maxHealth;
        CreateInputActions();
    }

    void OnEnable()
    {
        moveAction?.Enable();
        dodgeAction?.Enable();
        attackAction?.Enable();
        appearanceAction?.Enable();
        interactAction?.Enable();
        restartAction?.Enable();
        if (rig != null) rig.WeaponImpact += ResolveWeaponImpact;
    }

    void OnDisable()
    {
        moveAction?.Disable();
        dodgeAction?.Disable();
        attackAction?.Disable();
        appearanceAction?.Disable();
        interactAction?.Disable();
        restartAction?.Disable();
        if (rig != null) rig.WeaponImpact -= ResolveWeaponImpact;
    }

    void OnDestroy()
    {
        moveAction?.Dispose();
        dodgeAction?.Dispose();
        attackAction?.Dispose();
        appearanceAction?.Dispose();
        interactAction?.Dispose();
        restartAction?.Dispose();
    }

    void Update()
    {
        if (restartAction != null && restartAction.WasPressedThisFrame() && isDead)
            RestartPilot();
        if (isDead) return;

        Vector2 gridInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        float inputMagnitude = Mathf.Clamp01(gridInput.magnitude);
        movement = GridToWorld(gridInput, inputMagnitude);
        if (movement.sqrMagnitude > 0.001f && Time.time >= actionLockedUntil) facing = movement;

        if (appearanceAction != null && appearanceAction.WasPressedThisFrame() &&
            appearance != null && appearanceProfiles != null && appearanceProfiles.Length > 0)
        {
            appearanceIndex = (appearanceIndex + 1) % appearanceProfiles.Length;
            appearance.Apply(appearanceProfiles[appearanceIndex]);
        }

        if (dodgeAction != null && dodgeAction.WasPressedThisFrame() && Time.time >= actionLockedUntil)
            BeginDodge();

        if (attackAction != null && attackAction.WasPressedThisFrame() && Time.time >= nextAttackAt &&
            Time.time >= actionLockedUntil)
            BeginAttack();

        if (interactAction != null && interactAction.WasPressedThisFrame() &&
            Time.time >= actionLockedUntil && nearbyInteractable != null)
            nearbyInteractable.Interact(this);

        if (Time.time < actionLockedUntil)
        {
            rig.Play(lockedAction, lockedActionDirection);
            return;
        }

        rig.Play(movement.sqrMagnitude > 0.001f ? HuntzHumanoidRigAction.Walk : HuntzHumanoidRigAction.Idle,
            movement.sqrMagnitude > 0.001f ? movement : facing);
    }

    void FixedUpdate()
    {
        if (isDead)
            body.linearVelocity = Vector2.zero;
        else if (Time.time < dodgingUntil)
            body.linearVelocity = dodgeDirection * dodgeSpeed;
        else if (Time.time < actionLockedUntil)
            body.linearVelocity = Vector2.zero;
        else
            body.linearVelocity = movement * moveSpeed;
    }

    void BeginAttack()
    {
        nextAttackAt = Time.time + attackCooldown;
        actionLockedUntil = Time.time + attackDuration;
        lockedAction = HuntzHumanoidRigAction.Attack;
        lockedActionDirection = facing;
        rig.Play(HuntzHumanoidRigAction.Attack, lockedActionDirection, true);
    }

    void BeginDodge()
    {
        dodgeDirection = movement.sqrMagnitude > 0.001f ? movement.normalized : facing.normalized;
        lockedActionDirection = dodgeDirection;
        lockedAction = HuntzHumanoidRigAction.Dodge;
        dodgingUntil = Time.time + dodgeDuration;
        actionLockedUntil = dodgingUntil;
        rig.Play(HuntzHumanoidRigAction.Dodge, dodgeDirection, true);
    }

    void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");
        moveAction.AddBinding("<Gamepad>/dpad");

        dodgeAction = new InputAction("Dodge", InputActionType.Button);
        dodgeAction.AddBinding("<Keyboard>/space");
        dodgeAction.AddBinding("<Gamepad>/rightShoulder");

        attackAction = new InputAction("Attack", InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/j");
        attackAction.AddBinding("<Mouse>/leftButton");
        attackAction.AddBinding("<Gamepad>/buttonWest");

        appearanceAction = new InputAction("Next Appearance", InputActionType.Button);
        appearanceAction.AddBinding("<Keyboard>/c");

        interactAction = new InputAction("Interact", InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonSouth");

        restartAction = new InputAction("Restart Pilot", InputActionType.Button);
        restartAction.AddBinding("<Keyboard>/r");
    }

    public void ConfigureAppearances(HuntzHumanoidAppearance targetAppearance,
        HuntzHumanoidAppearanceProfile[] profiles)
    {
        appearance = targetAppearance;
        appearanceProfiles = profiles ?? new HuntzHumanoidAppearanceProfile[0];
        appearanceIndex = 0;
    }

    public void SetNearbyInteractable(HuntzRigPilotInteractable interactable)
    {
        nearbyInteractable = interactable;
    }

    public void TeleportTo(Vector2 destination)
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        movement = Vector2.zero;
        body.linearVelocity = Vector2.zero;
        body.position = destination;
        transform.position = destination;
        facing = Vector2.down;
        actionLockedUntil = 0f;
        dodgingUntil = 0f;
        nearbyInteractable = null;
        rig.Play(HuntzHumanoidRigAction.Idle, facing, true);
    }

    public void ShowFeedback(string message)
    {
        feedback = message;
        feedbackUntil = Time.time + 3.5f;
    }

    public bool ReceiveDamage(int amount, Vector2 sourcePosition)
    {
        if (isDead || amount <= 0 || Time.time < dodgingUntil || Time.time < nextDamageAt)
            return false;

        nextDamageAt = Time.time + 0.45f;
        health = Mathf.Max(0, health - amount);
        Vector2 hitDirection = sourcePosition - (Vector2)transform.position;
        if (hitDirection.sqrMagnitude > 0.001f) facing = hitDirection.normalized;
        lockedActionDirection = facing;

        if (health == 0)
        {
            isDead = true;
            lockedAction = HuntzHumanoidRigAction.Death;
            actionLockedUntil = float.PositiveInfinity;
            body.linearVelocity = Vector2.zero;
            rig.Play(lockedAction, lockedActionDirection, true);
            ShowFeedback("Você caiu. Pressione R para reiniciar o piloto.");
            return true;
        }

        lockedAction = HuntzHumanoidRigAction.Hurt;
        actionLockedUntil = Time.time + 0.30f;
        rig.Play(lockedAction, lockedActionDirection, true);
        ShowFeedback("Dano recebido — vida: " + health + "/" + maxHealth);
        return true;
    }

    void RestartPilot()
    {
        isDead = false;
        health = maxHealth;
        movement = Vector2.zero;
        facing = Vector2.down;
        lockedActionDirection = facing;
        lockedAction = HuntzHumanoidRigAction.Idle;
        actionLockedUntil = 0f;
        dodgingUntil = 0f;
        nextDamageAt = 0f;
        body.linearVelocity = Vector2.zero;
        body.position = startPosition;
        transform.position = startPosition;
        rig.Play(HuntzHumanoidRigAction.Idle, facing, true);
        ShowFeedback("Piloto reiniciado.");
    }

    void ResolveWeaponImpact()
    {
        Vector2 direction = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector2.down;
        Vector2 center = (Vector2)transform.position + direction * 0.52f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, 0.55f);
        for (int i = 0; i < hits.Length; i++)
        {
            HuntzRigPilotCombatTarget target = hits[i].GetComponentInParent<HuntzRigPilotCombatTarget>();
            if (target != null) target.TakeDamage(1);
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(12f, 12f, 370f, 132f), "HUNTZ — PILOTO DE RIG");
        GUI.Label(new Rect(24f, 38f, 310f, 22f), "WASD mover   J atacar   Espaço esquivar");
        GUI.Label(new Rect(24f, 60f, 310f, 22f), "C trocar paleta   E interagir com portais e NPCs");
        GUI.Label(new Rect(24f, 82f, 330f, 22f), "Vida: " + health + "/" + maxHealth + "   Estado: " + rig.CurrentAction);
        GUI.Label(new Rect(24f, 104f, 330f, 22f), isDead ? "R reinicia o piloto" : "Direção: " + rig.CurrentDirection);

        if (nearbyInteractable != null)
            GUI.Box(new Rect(12f, Screen.height - 58f, 420f, 34f), "E — " + nearbyInteractable.Prompt);

        if (!string.IsNullOrEmpty(feedback) && Time.time < feedbackUntil)
            GUI.Box(new Rect(Screen.width * 0.5f - 230f, 18f, 460f, 40f), feedback);
    }

    static Vector2 GridToWorld(Vector2 gridInput, float inputMagnitude)
    {
        Vector2 projected = new Vector2(
            (gridInput.x - gridInput.y) * 0.5f,
            (gridInput.x + gridInput.y) * 0.25f);
        return projected.sqrMagnitude > 0.001f ? projected.normalized * inputMagnitude : Vector2.zero;
    }
}
