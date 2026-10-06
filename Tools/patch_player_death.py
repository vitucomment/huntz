from pathlib import Path

path = Path(__file__).parents[1] / "Assets/HuntzSlice/Scripts/HuntzSlicePlayer.cs"
text = path.read_text(encoding="utf-8")

def replace_region(source: str, start: str, end: str, body: str) -> str:
    a = source.index(start)
    b = source.index(end, a)
    body = body.replace("\n+", "\n")
    return source[:a] + body.rstrip() + "\n\n    " + source[b:]

text = text.replace("    float hurtAnimationUntil;", "    float hurtAnimationUntil;\n    float deathReturnAt;")
text = replace_region(text, "    void Update()", "void FixedUpdate()", r'''    void Update()
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
    }''')

text = replace_region(text, "    public void TakeHit(", "public void MoveToRoom(", r'''    public void TakeHit(Vector2 fromPosition, int damage)
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
    }''')
text = text.replace("        hurtAnimationUntil = 0f;", "        hurtAnimationUntil = 0f;\n        deathReturnAt = 0f;")
path.write_text(text, encoding="utf-8", newline="\n")
