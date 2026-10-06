from pathlib import Path

path = Path(__file__).parents[1] / "Assets/IsoRPG/Scripts/IsoPlayer.cs"
source = path.read_text(encoding="utf-8")
source = source.replace("    Transform eyes;\n", "")
source = source.replace("    SpriteRenderer slash;", "    SpriteRenderer slash;\n    Animation slashAnimation;\n    HuntzDirectionalAnimator directionalAnimator;")
source = source.replace("    float invulnerableUntil;\n    Vector3 spawnPoint;", "    float invulnerableUntil;\n    float attackAnimationUntil;\n    float hurtAnimationUntil;\n    float deathReturnAt;\n    bool defeated;\n    Vector3 spawnPoint;")
source = source.replace("        eyes = visual != null ? visual.Find(\"Eyes\") : null;\n", "        directionalAnimator = GetComponent<HuntzDirectionalAnimator>();\n")
source = source.replace("            slash = slashT.GetComponent<SpriteRenderer>();\n            slash.enabled = false;", "            slash = slashT.GetComponent<SpriteRenderer>();\n            slashAnimation = slashT.GetComponent<Animation>();\n            slash.enabled = false;")

def replace_method(text: str, start: str, end: str, body: str) -> str:
    a = text.index(start)
    b = text.index(end, a)
    body = body.replace("\n+", "\n")
    return text[:a] + body.rstrip() + "\n\n    " + text[b:]

source = replace_method(source, "    void Update()", "void FixedUpdate()", r'''    void Update()
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
    }''')
source = source.replace("    void FixedUpdate() => rb.linearVelocity = move * moveSpeed;", "    void FixedUpdate() => rb.linearVelocity = defeated ? Vector2.zero : move * moveSpeed;")
source = source.replace("        lastAttackTime = Time.time;", "        lastAttackTime = Time.time;\n        attackAnimationUntil = Time.time + 0.43f;")
source = source.replace("        slash.enabled = true;", "        slash.enabled = true;\n        if (slashAnimation != null) slashAnimation.Play(\"SwordArc_Loop\");")
source = replace_method(source, "    void Animate()", "IsoInteractable FindInteractable(", r'''    void Animate()
    {
        string action = defeated ? "Death"
            : Time.time < attackAnimationUntil ? "Attack"
            : Time.time < hurtAnimationUntil ? "Hurt"
            : move.sqrMagnitude > 0.001f ? "Walk" : "Idle";
        if (directionalAnimator != null) directionalAnimator.PlayAction(action, facing);

        bool blink = Time.time < invulnerableUntil && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = blink ? new Color(1f, 0.35f, 0.35f, baseColors[i].a) : baseColors[i];
    }''')
source = replace_method(source, "    public void TakeDamage(", "public void AddXp(", r'''    public void TakeDamage(int amount, Vector2 knockDirection)
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
    }''')
path.write_text(source, encoding="utf-8", newline="\n")
