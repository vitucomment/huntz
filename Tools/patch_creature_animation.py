from pathlib import Path

root = Path(__file__).parents[1]

def replace_region(path: Path, start: str, end: str, body: str) -> None:
    text = path.read_text(encoding="utf-8")
    a = text.index(start)
    b = text.index(end, a)
    body = body.replace("\n+", "\n")
    text = text[:a] + body.rstrip() + "\n\n    " + text[b:]
    path.write_text(text, encoding="utf-8", newline="\n")

enemy_path = root / "Assets/HuntzSlice/Scripts/HuntzSliceEnemy.cs"
enemy = enemy_path.read_text(encoding="utf-8")
enemy = enemy.replace("    Color[] baseColors;", "    Color[] baseColors;\n    Animation visualAnimation;\n    string currentAnimation;\n    float hurtUntil;\n    float deactivateAt;\n    bool dying;")
enemy = enemy.replace("        visualRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];", '''        visualRenderers = visual != null ? visual.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        visualAnimation = visual != null ? visual.GetComponentInChildren<Animation>(true) : null;
        PlayVisual("AshSlime_Idle", true);''')
enemy_path.write_text(enemy, encoding="utf-8", newline="\n")
replace_region(enemy_path, "    void Update()", "void BeginTelegraph(", r'''    void Update()
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
    }''')
replace_region(enemy_path, "    public void TakeDamage(", "public void ResetForExpedition(", r'''    public void TakeDamage(int amount, Vector2 direction)
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
    }''')
enemy = enemy_path.read_text(encoding="utf-8")
enemy = enemy.replace("        health = maxHealth;\n        windingUp = false;", "        health = maxHealth;\n        dying = false;\n        deactivateAt = 0f;\n        hurtUntil = 0f;\n        currentAnimation = null;\n        PlayVisual(\"AshSlime_Idle\", true);\n        windingUp = false;")
insert = '''
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
'''
enemy = enemy.replace("    public void ResetForExpedition()", insert + "\n    public void ResetForExpedition()")
enemy_path.write_text(enemy, encoding="utf-8", newline="\n")

boss_path = root / "Assets/HuntzSlice/Scripts/HuntzSliceBoss.cs"
boss = boss_path.read_text(encoding="utf-8")
boss = boss.replace("    Color[] baseColors;", "    Color[] baseColors;\n    Animation visualAnimation;\n    string currentVisualClip;\n    bool defeatCompletionSent;\n    float defeatCompletesAt;")
boss = boss.replace("        baseColors = new Color[visualRenderers.Length];", "        visualAnimation = visualRoot != null ? visualRoot.GetComponentInChildren<Animation>(true) : null;\n        baseColors = new Color[visualRenderers.Length];")
boss = boss.replace("        SetTelegraphsActive(false);\n    }\n\n    void Start()", "        SetTelegraphsActive(false);\n        PlayVisual(\"RiftWarden_Idle\", true);\n    }\n\n    void Start()")
boss = boss.replace("        if (IsDefeated) return;\n        if (player == null)", "        if (IsDefeated)\n        {\n            if (!defeatCompletionSent && Time.time >= defeatCompletesAt) CompleteDefeat();\n            return;\n        }\n        if (player == null)")
boss = boss.replace("                    phase = Phase.Observing;\n                    nextAttackAt", "                    phase = Phase.Observing;\n                    PlayVisual(\"RiftWarden_Idle\");\n                    nextAttackAt")
boss = boss.replace("        if (Health == 0) Defeat();", "        PlayVisual(\"RiftWarden_Hurt\", true);\n        if (Health == 0) Defeat();")
boss = boss.replace("        phase = Phase.Observing;\n        nextAttackIsCharge", "        phase = Phase.Observing;\n        defeatCompletionSent = false;\n        defeatCompletesAt = 0f;\n        PlayVisual(\"RiftWarden_Idle\", true);\n        nextAttackIsCharge")
boss = boss.replace("        phase = Phase.TelegraphingCharge;", "        phase = Phase.TelegraphingCharge;\n        PlayVisual(\"RiftWarden_Charge\");")
boss = boss.replace("        phase = Phase.Charging;", "        phase = Phase.Charging;\n        PlayVisual(\"RiftWarden_Charge\");")
boss = boss.replace("        phase = Phase.TelegraphingPulse;", "        phase = Phase.TelegraphingPulse;\n        PlayVisual(\"RiftWarden_Pulse\");")
boss = boss.replace("        phase = Phase.Recovering;", "        phase = Phase.Recovering;\n        PlayVisual(\"RiftWarden_Recover\");")
boss_path.write_text(boss, encoding="utf-8", newline="\n")
replace_region(boss_path, "    void Defeat()", "void UpdateHurtFlash(", r'''    void Defeat()
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
    }''')
boss_path.write_text(boss_path.read_text(encoding="utf-8"), encoding="utf-8", newline="\n")
