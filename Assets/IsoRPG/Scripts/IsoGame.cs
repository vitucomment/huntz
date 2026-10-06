using UnityEngine;

// HUD, dialogue box, toast messages and win condition for the isometric concept.
public class IsoGame : MonoBehaviour
{
    public static IsoGame Instance { get; private set; }

    public bool InDialogue => dialogueLines != null && dialogueIndex < dialogueLines.Length;

    // NonSerialized: editor domain reload would otherwise turn a null array into an empty one.
    [System.NonSerialized] string dialogueName;
    [System.NonSerialized] string[] dialogueLines;
    int dialogueIndex;
    string message;
    float messageUntil;
    int startingEnemies;
    bool won;

    GUIStyle label, small, toast, speaker, body;
    Texture2D panelFrame;
    Texture2D heartIcon;
    Texture2D potionIcon;
    float viewWidth;
    float viewHeight;

    public static void Message(string text, float duration = 2.5f)
    {
        if (Instance == null) return;
        Instance.message = text;
        Instance.messageUntil = Time.time + duration;
    }

    void Awake() => Instance = this;

    public void ConfigureArt(Texture2D panel, Texture2D heart, Texture2D potion)
    {
        panelFrame = panel;
        heartIcon = heart;
        potionIcon = potion;
    }

    void Start() => startingEnemies = IsoEnemy.Active.Count;

    void Update()
    {
        if (!won && startingEnemies > 0 && IsoEnemy.Active.Count == 0)
        {
            won = true;
            Message("A vila está a salvo! Obrigado, herói.", 9999f);
        }
    }

    public void StartDialogue(string speakerName, string[] lines)
    {
        if (lines == null || lines.Length == 0) return;
        dialogueName = speakerName;
        dialogueLines = lines;
        dialogueIndex = 0;
    }

    public void AdvanceDialogue()
    {
        dialogueIndex++;
        if (dialogueIndex >= dialogueLines.Length) dialogueLines = null;
    }

    void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Clamp(Screen.height / 800f, 1.15f, 1.35f);
        viewWidth = Screen.width / scale;
        viewHeight = Screen.height / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        EnsureStyles();
        var p = IsoPlayer.Instance;
        if (p != null)
        {
            Panel(new Rect(12, 10, 250, 100), 0.45f);
            GUI.Label(new Rect(24, 16, 220, 26), $"Nível {p.Level}", label);
            DrawIcon(heartIcon, new Rect(24, 46, 17, 17));
            Bar(new Rect(45, 46, 205, 18), p.Hp / (float)p.MaxHp, new Color(0.85f, 0.25f, 0.3f), $"HP {p.Hp}/{p.MaxHp}");
            Bar(new Rect(24, 68, 226, 10), p.Xp / (float)p.XpToNext, new Color(0.35f, 0.65f, 1f), null);
            DrawIcon(potionIcon, new Rect(24, 82, 17, 17));
            GUI.Label(new Rect(45, 82, 205, 22), $"Poções: {p.Potions}   [Q] usar", small);

            if (p.NearbyInteractable != null && !InDialogue)
            {
                var r = new Rect(viewWidth / 2f - 130, viewHeight - 120, 260, 32);
                Panel(r, 0.6f);
                GUI.Label(r, $"[E] {p.NearbyInteractable.Prompt}", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
            }
        }

        GUI.Label(new Rect(16, viewHeight - 30, 800, 24), "WASD mover  ·  Espaço/clique atacar  ·  E interagir  ·  Q poção", small);

        if (message != null && Time.time < messageUntil)
            GUI.Label(new Rect(0, 24, viewWidth, 40), message, toast);

        if (InDialogue)
        {
            var r = new Rect(viewWidth / 2f - 340, viewHeight - 190, 680, 140);
            Panel(r, 0.82f);
            GUI.Label(new Rect(r.x + 18, r.y + 12, 640, 26), dialogueName, speaker);
            GUI.Label(new Rect(r.x + 18, r.y + 44, 644, 64), dialogueLines[dialogueIndex], body);
            GUI.Label(new Rect(r.x + 18, r.y + 110, 644, 22), "[E] continuar", new GUIStyle(small) { alignment = TextAnchor.MiddleRight });
        }

        GUI.matrix = previousMatrix;
    }

    void Panel(Rect r, float alpha)
    {
        var old = GUI.color;
        GUI.color = panelFrame != null ? new Color(1f, 1f, 1f, alpha) : new Color(0.05f, 0.06f, 0.08f, alpha);
        if (panelFrame != null) DrawNineSlice(r, panelFrame, 6f, 6f);
        else GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void DrawIcon(Texture2D icon, Rect rect)
    {
        if (icon != null) GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
    }

    static void DrawNineSlice(Rect destination, Texture2D texture, float sourceBorder, float targetBorder)
    {
        float[] widths = { targetBorder, Mathf.Max(0f, destination.width - targetBorder * 2f), targetBorder };
        float[] heights = { targetBorder, Mathf.Max(0f, destination.height - targetBorder * 2f), targetBorder };
        float[] sourceWidths = { sourceBorder, texture.width - sourceBorder * 2f, sourceBorder };
        float[] sourceHeights = { sourceBorder, texture.height - sourceBorder * 2f, sourceBorder };
        float y = destination.y;
        float sourceTop = 0f;
        for (int row = 0; row < 3; row++)
        {
            float x = destination.x;
            float sourceX = 0f;
            for (int column = 0; column < 3; column++)
            {
                Rect target = new Rect(x, y, widths[column], heights[row]);
                Rect uv = new Rect(sourceX / texture.width,
                    1f - (sourceTop + sourceHeights[row]) / texture.height,
                    sourceWidths[column] / texture.width,
                    sourceHeights[row] / texture.height);
                GUI.DrawTextureWithTexCoords(target, texture, uv, true);
                x += widths[column];
                sourceX += sourceWidths[column];
            }
            y += heights[row];
            sourceTop += sourceHeights[row];
        }
    }

    void Bar(Rect r, float fill, Color color, string text)
    {
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Texture2D.whiteTexture);
        GUI.color = old;
        if (text != null) GUI.Label(r, text, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
    }

    void EnsureStyles()
    {
        if (label != null) return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
        label.normal.textColor = Color.white;
        small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
        toast = new GUIStyle(label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        toast.normal.textColor = new Color(1f, 0.85f, 0.3f);
        speaker = new GUIStyle(label) { fontSize = 18 };
        speaker.normal.textColor = new Color(1f, 0.8f, 0.35f);
        body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
        body.normal.textColor = Color.white;
    }
}
