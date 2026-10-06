using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class IsoPlayer : MonoBehaviour
{
    public static IsoPlayer Instance { get; private set; }

    [SerializeField] float moveSpeed = 3.2f;
    [SerializeField] int maxHp = 10;
    [SerializeField] int attackDamage = 1;
    [SerializeField] float attackReach = 0.45f;
    [SerializeField] float attackRadius = 0.5f;
    [SerializeField] float attackCooldown = 0.35f;
    [SerializeField] float interactRadius = 0.9f;
    [SerializeField] int potionHeal = 5;

    public int Hp { get; private set; }
    public int MaxHp => maxHp;
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpToNext => Level * 4;
    public int Potions { get; private set; }
    public IsoInteractable NearbyInteractable { get; private set; }

    Rigidbody2D rb;
    Transform visual;
    SpriteRenderer slash;
    Animation slashAnimation;
    HuntzDirectionalAnimator directionalAnimator;
    SpriteRenderer[] renderers;
    Color[] baseColors;
    Vector2 move;
    Vector2 facing = Vector2.down;
    float lastAttackTime = -10f;
    float invulnerableUntil;
    float attackAnimationUntil;
    float hurtAnimationUntil;
    float deathReturnAt;
    bool defeated;
    Vector3 spawnPoint;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        Hp = maxHp;
        spawnPoint = transform.position;

        visual = transform.Find("Visual");
        directionalAnimator = GetComponent<HuntzDirectionalAnimator>();
        var slashT = transform.Find("Slash");
        if (slashT != null)
        {
            slash = slashT.GetComponent<SpriteRenderer>();
            slashAnimation = slashT.GetComponent<Animation>();
            slash.enabled = false;
        }

        renderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0];
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        bool interactPressed = kb != null && kb.eKey.wasPressedThisFrame;
        if (defeated)
        {
            move = Vector2.zero;
            Animate();
            if (deathReturnAt > 0f && Time.time >= deathReturnAt)
            {
                deathReturnAt = 0f;
                Hp = maxHp;
                rb.position = spawnPoint;
                transform.position = spawnPoint;
                defeated = false;
                invulnerableUntil = Time.time + 0.8f;
                IsoGame.Message("Você desmaiou... e acordou na vila.");
            }
            return;
        }

        if (IsoGame.Instance != null && IsoGame.Instance.InDialogue)
        {
            move = Vector2.zero;
            if (interactPressed || (kb != null && kb.spaceKey.wasPressedThisFrame)) IsoGame.Instance.AdvanceDialogue();
            Animate();
            return;
        }

        move = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
        }
        if (move.sqrMagnitude > 0f)
        {
            move.Normalize();
            facing = move;
        }

        bool attackPressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame))
                             || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (attackPressed && Time.time - lastAttackTime >= attackCooldown) Attack();
        if (kb != null && kb.qKey.wasPressedThisFrame) UsePotion();

        NearbyInteractable = FindInteractable();
        if (interactPressed && NearbyInteractable != null) NearbyInteractable.Interact(this);
        Animate();
    }

    void FixedUpdate() => rb.linearVelocity = defeated ? Vector2.zero : move * moveSpeed;

    void Attack()
    {
        lastAttackTime = Time.time;
        attackAnimationUntil = Time.time + 0.43f;
        Vector2 center = (Vector2)transform.position + new Vector2(0f, 0.25f) + facing * attackReach;
        foreach (var hit in Physics2D.OverlapCircleAll(center, attackRadius))
        {
            var enemy = hit.GetComponentInParent<IsoEnemy>();
            if (enemy != null) enemy.TakeDamage(attackDamage, facing);
        }
        if (slash != null) StartCoroutine(ShowSlash(center));
    }

    IEnumerator ShowSlash(Vector2 center)
    {
        slash.transform.position = center;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
        slash.enabled = true;
        if (slashAnimation != null) slashAnimation.Play("SwordArc_Loop");
        const float duration = 0.12f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            var c = slash.color;
            c.a = 0.8f * (1f - t / duration);
            slash.color = c;
            yield return null;
        }
        slash.enabled = false;
    }

    void Animate()
    {
        string action = defeated ? "Death"
            : Time.time < attackAnimationUntil ? "Attack"
            : Time.time < hurtAnimationUntil ? "Hurt"
            : move.sqrMagnitude > 0.001f ? "Walk" : "Idle";
        if (directionalAnimator != null) directionalAnimator.PlayAction(action, facing);

        bool blink = Time.time < invulnerableUntil && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = blink ? new Color(1f, 0.35f, 0.35f, baseColors[i].a) : baseColors[i];
    }

    IsoInteractable FindInteractable()
    {
        IsoInteractable best = null;
        float bestDist = float.MaxValue;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, interactRadius))
        {
            var it = hit.GetComponentInParent<IsoInteractable>();
            if (it == null || string.IsNullOrEmpty(it.Prompt)) continue;
            float d = Vector2.Distance(transform.position, it.transform.position);
            if (d < bestDist)
            {
                best = it;
                bestDist = d;
            }
        }
        return best;
    }

    public void TakeDamage(int amount, Vector2 knockDirection)
    {
        if (defeated || Time.time < invulnerableUntil) return;
        Hp = Mathf.Max(0, Hp - amount);
        invulnerableUntil = Time.time + 1f;
        hurtAnimationUntil = Time.time + 0.18f;
        rb.position += knockDirection.normalized * 0.25f;

        if (Hp == 0)
        {
            defeated = true;
            move = Vector2.zero;
            deathReturnAt = Time.time + 0.67f;
        }
    }

    public void AddXp(int amount)
    {
        Xp += amount;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            maxHp += 2;
            Hp = maxHp;
            if (Level % 2 == 0) attackDamage++;
            IsoGame.Message($"Subiu para o nível {Level}!");
        }
    }

    public void AddPotions(int amount) => Potions += amount;

    void UsePotion()
    {
        if (Potions == 0)
        {
            IsoGame.Message("Sem poções.");
            return;
        }
        if (Hp == maxHp) return;
        Potions--;
        Hp = Mathf.Min(maxHp, Hp + potionHeal);
        IsoGame.Message($"+{potionHeal} HP");
    }
}
