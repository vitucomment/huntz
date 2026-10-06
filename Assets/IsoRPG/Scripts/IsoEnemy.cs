using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IsoEnemy : MonoBehaviour
{
    public static readonly List<IsoEnemy> Active = new List<IsoEnemy>();

    [SerializeField] int maxHp = 3;
    [SerializeField] float speed = 1.4f;
    [SerializeField] float aggroRange = 3f;
    [SerializeField] float leashRange = 6f;
    [SerializeField] int contactDamage = 1;
    [SerializeField] int xpReward = 2;
    [SerializeField] string animationPrefix = "AshSlime";

    int hp;
    Rigidbody2D rb;
    Vector2 home;
    Vector2 wanderTarget;
    float nextWanderTime;
    float stunnedUntil;
    float hitFlashUntil;
    float phase;
    Transform visual;
    Vector3 visualBaseScale;
    SpriteRenderer[] renderers;
    Color[] baseColors;
    Animation animationPlayer;
    string currentAnimation;
    bool dying;
    float deathUntil;

    public void Setup(int hitPoints, int damage, int xp)
    {
        maxHp = hitPoints;
        contactDamage = damage;
        xpReward = xp;
    }

    public void SetAnimationPrefix(string prefix) => animationPrefix = prefix;

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hp = maxHp;
        home = transform.position;
        wanderTarget = home;
        phase = Random.value * 10f;
        visual = transform.Find("Visual");
        visualBaseScale = visual.localScale;
        renderers = visual.GetComponentsInChildren<SpriteRenderer>();
        animationPlayer = visual.GetComponentInChildren<Animation>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        PlayVisual("Idle", true);
    }

    void FixedUpdate()
    {
        if (dying)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < stunnedUntil)
        {
            rb.linearVelocity *= 0.85f;
            return;
        }

        Vector2 pos = rb.position;
        Vector2 velocity = Vector2.zero;
        var player = IsoPlayer.Instance;
        if (player != null)
        {
            Vector2 toPlayer = (Vector2)player.transform.position - pos;
            bool chase = toPlayer.magnitude < aggroRange && Vector2.Distance(home, pos) < leashRange;
            if (chase)
            {
                velocity = toPlayer.normalized * speed;
                if (toPlayer.magnitude < 0.45f + visualBaseScale.x * 0.1f) player.TakeDamage(contactDamage, toPlayer);
            }
        }

        if (velocity == Vector2.zero)
        {
            if (Time.time >= nextWanderTime)
            {
                wanderTarget = home + Random.insideUnitCircle * 1.5f;
                nextWanderTime = Time.time + Random.Range(2f, 4f);
            }
            Vector2 d = wanderTarget - pos;
            if (d.magnitude > 0.1f) velocity = d.normalized * speed * 0.35f;
        }

        rb.linearVelocity = velocity;
    }

    void Update()
    {
        if (dying)
        {
            if (Time.time >= deathUntil) Destroy(gameObject);
            PlayVisual("Death");
            return;
        }

        float s = Mathf.Sin(Time.time * 6f + phase) * 0.08f;
        visual.localScale = new Vector3(visualBaseScale.x * (1f + s), visualBaseScale.y * (1f - s), 1f);

        bool flash = Time.time < hitFlashUntil;
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = flash ? Color.white : baseColors[i];
        PlayVisual(flash ? "Hurt" : rb.linearVelocity.sqrMagnitude > 0.04f ? "Move" : "Idle");
    }

    public void TakeDamage(int amount, Vector2 direction)
    {
        if (dying || amount <= 0) return;
        hp -= amount;
        hitFlashUntil = Time.time + 0.12f;
        stunnedUntil = Time.time + 0.3f;
        rb.linearVelocity = direction.normalized * 4f;

        if (hp <= 0)
        {
            dying = true;
            deathUntil = Time.time + 0.5f;
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null) collider.enabled = false;
            rb.linearVelocity = Vector2.zero;
            PlayVisual("Death", true);
            if (IsoPlayer.Instance != null) IsoPlayer.Instance.AddXp(xpReward);
            IsoGame.Message($"+{xpReward} XP");
            return;
        }
    }

    void PlayVisual(string action, bool restart = false)
    {
        if (animationPlayer == null) return;
        string clipName = animationPrefix + "_" + action;
        AnimationState state = animationPlayer[clipName];
        if (state == null) return;
        state.wrapMode = action == "Idle" || action == "Move" ? WrapMode.Loop : WrapMode.Once;
        if (!restart && currentAnimation == clipName && animationPlayer.IsPlaying(clipName)) return;
        if (!restart && currentAnimation == clipName && state.wrapMode == WrapMode.Once) return;
        if (animationPlayer.isPlaying) animationPlayer.CrossFade(clipName, 0.04f);
        else animationPlayer.Play(clipName);
        currentAnimation = clipName;
    }
}
