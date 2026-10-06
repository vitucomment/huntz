using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class HuntzSliceBoss : MonoBehaviour
{
    enum Phase
    {
        Observing,
        TelegraphingCharge,
        Charging,
        TelegraphingPulse,
        Recovering,
        Defeated
    }

    [SerializeField] int maxHealth = 18;
    [SerializeField] int attackDamage = 2;
    [SerializeField] float chargeTellDuration = 1.1f;
    [SerializeField] float pulseTellDuration = 1.2f;
    [SerializeField] float recoveryDuration = 0.9f;
    [SerializeField] float chargeSpeed = 7f;
    [SerializeField] float chargeMaximumDistance = 3.8f;
    [SerializeField] float pulseRadius = 2f;
    [SerializeField] float hitRadius = 0.62f;
    [SerializeField] Transform visualRoot;
    [SerializeField] SpriteRenderer chargeLane;
    [SerializeField] SpriteRenderer chargeEndpoint;
    [SerializeField] SpriteRenderer pulseFill;
    [SerializeField] SpriteRenderer pulseRing;

    Rigidbody2D body;
    CircleCollider2D hitCollider;
    SpriteRenderer[] visualRenderers;
    Color[] baseColors;
    Animation visualAnimation;
    string currentVisualClip;
    bool defeatCompletionSent;
    float defeatCompletesAt;
    HuntzSlicePlayer player;
    Vector2 spawnPosition;
    Phase phase = Phase.Observing;
    bool nextAttackIsCharge = true;
    bool hitDuringCharge;
    float phaseEndsAt;
    float nextAttackAt;
    float chargeDistance;
    Vector2 chargeDirection;
    Vector2 chargeEnd;

    public string BossName => "Guardião da Fenda";
    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDefeated => phase == Phase.Defeated;

    public string StateLabel => phase switch
    {
        Phase.Observing => "Observando",
        Phase.TelegraphingCharge => "AVISO: INVESTIDA — saia do corredor!",
        Phase.Charging => "INVESTIDA",
        Phase.TelegraphingPulse => "AVISO: PULSO — saia do círculo!",
        Phase.Recovering => "Recuperando — janela para atacar",
        Phase.Defeated => "Derrotado",
        _ => "Preparando ataque"
    };

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<CircleCollider2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        spawnPosition = body.position;
        Health = maxHealth;

        visualRenderers = visualRoot != null
            ? visualRoot.GetComponentsInChildren<SpriteRenderer>(true)
            : new SpriteRenderer[0];
        visualAnimation = visualRoot != null ? visualRoot.GetComponentInChildren<Animation>(true) : null;
        baseColors = new Color[visualRenderers.Length];
        for (int i = 0; i < visualRenderers.Length; i++)
            baseColors[i] = visualRenderers[i].color;

        SetTelegraphsActive(false);
        PlayVisual("RiftWarden_Idle", true);
    }

    void Start()
    {
        player = ResolvePlayer();
        nextAttackAt = Time.time + 1f;
    }

    void Update()
    {
        if (IsDefeated)
        {
            if (!defeatCompletionSent && Time.time >= defeatCompletesAt) CompleteDefeat();
            return;
        }
        if (player == null) player = ResolvePlayer();

        switch (phase)
        {
            case Phase.Observing:
                if (Time.time >= nextAttackAt)
                {
                    if (nextAttackIsCharge) BeginChargeTelegraph();
                    else BeginPulseTelegraph();
                    nextAttackIsCharge = !nextAttackIsCharge;
                }
                break;

            case Phase.TelegraphingCharge:
                if (Time.time >= phaseEndsAt) BeginCharge();
                break;

            case Phase.Charging:
                if (Time.time >= phaseEndsAt) BeginRecovery();
                break;

            case Phase.TelegraphingPulse:
                UpdatePulseTelegraph();
                if (Time.time >= phaseEndsAt) ResolvePulse();
                break;

            case Phase.Recovering:
                if (Time.time >= phaseEndsAt)
                {
                    phase = Phase.Observing;
                    PlayVisual("RiftWarden_Idle");
                    nextAttackAt = Time.time + 0.35f;
                }
                break;
        }

        UpdateHurtFlash();
    }

    void FixedUpdate()
    {
        if (phase != Phase.Charging)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 previousPosition = body.position;
        Vector2 nextPosition = previousPosition + chargeDirection * chargeSpeed * Time.fixedDeltaTime;
        body.linearVelocity = chargeDirection * chargeSpeed;

        if (!hitDuringCharge && player != null &&
            DistanceToSegment(player.transform.position, previousPosition, nextPosition) <= hitRadius)
        {
            hitDuringCharge = true;
            player.TakeHit(transform.position, attackDamage);
        }
    }

    public void Configure(int health, Transform visuals, SpriteRenderer lane, SpriteRenderer endpoint,
        SpriteRenderer radialFill, SpriteRenderer radialRing)
    {
        maxHealth = Mathf.Max(1, health);
        visualRoot = visuals;
        chargeLane = lane;
        chargeEndpoint = endpoint;
        pulseFill = radialFill;
        pulseRing = radialRing;
        Health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDefeated || amount <= 0) return;

        Health = Mathf.Max(0, Health - amount);
        for (int i = 0; i < visualRenderers.Length; i++)
            if (visualRenderers[i] != null) visualRenderers[i].color = Color.white;

        PlayVisual("RiftWarden_Hurt", true);
        if (Health == 0) Defeat();
    }

    public void ResetForExpedition()
    {
        phase = Phase.Observing;
        defeatCompletionSent = false;
        defeatCompletesAt = 0f;
        PlayVisual("RiftWarden_Idle", true);
        nextAttackIsCharge = true;
        hitDuringCharge = false;
        phaseEndsAt = 0f;
        nextAttackAt = Time.time + 1f;
        chargeDistance = 0f;
        chargeDirection = Vector2.down;
        chargeEnd = Vector2.zero;
        Health = maxHealth;
        if (body != null)
        {
            body.position = spawnPosition;
            body.linearVelocity = Vector2.zero;
        }
        transform.position = spawnPosition;
        if (hitCollider != null) hitCollider.enabled = true;
        SetTelegraphsActive(false);
        for (int i = 0; i < visualRenderers.Length; i++)
        {
            if (visualRenderers[i] == null) continue;
            visualRenderers[i].enabled = true;
            visualRenderers[i].color = baseColors[i];
        }
    }

    void BeginChargeTelegraph()
    {
        phase = Phase.TelegraphingCharge;
        PlayVisual("RiftWarden_Charge");
        PlayVisual("RiftWarden_Charge");
        Vector2 target = player != null ? (Vector2)player.transform.position : body.position + Vector2.down;
        chargeDirection = (target - body.position).normalized;
        if (chargeDirection.sqrMagnitude < 0.001f) chargeDirection = Vector2.down;
        chargeDistance = Mathf.Clamp(Vector2.Distance(body.position, target) + 0.8f, 2.4f, chargeMaximumDistance);
        chargeEnd = body.position + chargeDirection * chargeDistance;
        phaseEndsAt = Time.time + chargeTellDuration;

        if (chargeLane != null)
        {
            chargeLane.transform.position = (body.position + chargeEnd) * 0.5f;
            chargeLane.transform.rotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(chargeDirection.y, chargeDirection.x) * Mathf.Rad2Deg);
            chargeLane.transform.localScale = new Vector3(chargeDistance, 0.26f, 1f);
            chargeLane.color = new Color(1f, 0.28f, 0.08f, 0.48f);
            chargeLane.gameObject.SetActive(true);
        }

        if (chargeEndpoint != null)
        {
            chargeEndpoint.transform.position = chargeEnd;
            chargeEndpoint.color = new Color(1f, 0.72f, 0.2f, 0.95f);
            chargeEndpoint.gameObject.SetActive(true);
        }
    }

    void BeginCharge()
    {
        phase = Phase.Charging;
        PlayVisual("RiftWarden_Charge");
        PlayVisual("RiftWarden_Charge");
        hitDuringCharge = false;
        phaseEndsAt = Time.time + chargeDistance / chargeSpeed;
        if (chargeLane != null) chargeLane.color = new Color(1f, 0.05f, 0.03f, 0.82f);
        if (chargeEndpoint != null) chargeEndpoint.color = new Color(1f, 0.14f, 0.06f, 1f);
    }

    void BeginPulseTelegraph()
    {
        phase = Phase.TelegraphingPulse;
        PlayVisual("RiftWarden_Pulse");
        PlayVisual("RiftWarden_Pulse");
        phaseEndsAt = Time.time + pulseTellDuration;
        if (pulseFill != null)
        {
            pulseFill.transform.position = body.position;
            pulseFill.transform.localScale = Vector3.one * (pulseRadius * 2f);
            pulseFill.color = new Color(1f, 0.22f, 0.05f, 0.2f);
            pulseFill.gameObject.SetActive(true);
        }

        if (pulseRing != null)
        {
            pulseRing.transform.position = body.position;
            pulseRing.transform.localScale = Vector3.one * (pulseRadius * 2f);
            pulseRing.color = new Color(1f, 0.7f, 0.16f, 1f);
            pulseRing.gameObject.SetActive(true);
        }
    }

    void UpdatePulseTelegraph()
    {
        float pulse = 0.92f + Mathf.PingPong(Time.time * 1.6f, 0.12f);
        Vector3 scale = Vector3.one * (pulseRadius * 2f * pulse);
        if (pulseFill != null) pulseFill.transform.localScale = scale;
        if (pulseRing != null) pulseRing.transform.localScale = scale;
    }

    void ResolvePulse()
    {
        if (player != null && Vector2.Distance(body.position, player.transform.position) <= pulseRadius)
            player.TakeHit(transform.position, attackDamage);
        BeginRecovery();
    }

    void BeginRecovery()
    {
        phase = Phase.Recovering;
        PlayVisual("RiftWarden_Recover");
        PlayVisual("RiftWarden_Recover");
        phaseEndsAt = Time.time + recoveryDuration;
        body.linearVelocity = Vector2.zero;
        SetTelegraphsActive(false);
    }

    void Defeat()
    {
        phase = Phase.Defeated;
        body.linearVelocity = Vector2.zero;
        if (hitCollider != null) hitCollider.enabled = false;
        SetTelegraphsActive(false);
        defeatCompletesAt = Time.time + 0.67f;
        PlayVisual("RiftWarden_Death", true);
    }

    void CompleteDefeat()
    {
        defeatCompletionSent = true;
        HuntzSliceHunt hunt = HuntzSliceHunt.Instance;
        if (hunt == null) hunt = FindAnyObjectByType<HuntzSliceHunt>();
        if (hunt != null) hunt.CompleteFinalBoss();
    }

    void PlayVisual(string clipName, bool restart = false)
    {
        if (visualAnimation == null || visualAnimation[clipName] == null) return;
        AnimationState state = visualAnimation[clipName];
        state.wrapMode = clipName == "RiftWarden_Idle" ? WrapMode.Loop : WrapMode.Once;
        if (!restart && currentVisualClip == clipName && visualAnimation.IsPlaying(clipName)) return;
        if (!restart && currentVisualClip == clipName && state.wrapMode == WrapMode.Once) return;
        if (visualAnimation.isPlaying) visualAnimation.CrossFade(clipName, 0.04f);
        else visualAnimation.Play(clipName);
        currentVisualClip = clipName;
    }

    void UpdateHurtFlash()
    {
        // Keep a short, bright hit response without changing the attack phase.
        for (int i = 0; i < visualRenderers.Length; i++)
            if (visualRenderers[i] != null && visualRenderers[i].enabled)
                visualRenderers[i].color = Color.Lerp(visualRenderers[i].color, baseColors[i], Time.deltaTime * 12f);
    }

    void SetTelegraphsActive(bool isActive)
    {
        if (chargeLane != null) chargeLane.gameObject.SetActive(isActive);
        if (chargeEndpoint != null) chargeEndpoint.gameObject.SetActive(isActive);
        if (pulseFill != null) pulseFill.gameObject.SetActive(isActive);
        if (pulseRing != null) pulseRing.gameObject.SetActive(isActive);
    }

    HuntzSlicePlayer ResolvePlayer()
    {
        HuntzSlicePlayer resolved = HuntzSlicePlayer.Instance;
        return resolved != null ? resolved : FindAnyObjectByType<HuntzSlicePlayer>();
    }

    static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.0001f) return Vector2.Distance(point, start);
        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + segment * t);
    }
}
