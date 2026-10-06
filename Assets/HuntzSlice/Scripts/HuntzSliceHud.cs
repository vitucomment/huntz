using UnityEngine;

public sealed class HuntzSliceHud : MonoBehaviour
{
    static HuntzSliceHud instance;
    static string message;
    static float messageUntil;

    GUIStyle title;
    GUIStyle label;
    GUIStyle small;
    GUIStyle messageStyle;
    GUIStyle mapTitle;
    GUIStyle mapNode;
    GUIStyle resultTitle;
    GUIStyle resultBody;
    Texture2D panelFrame;
    Texture2D heartIcon;
    Texture2D coreIcon;
    Texture2D swordIcon;
    float viewWidth;
    float viewHeight;

    void Awake() => instance = this;
    void OnDestroy() { if (instance == this) instance = null; }

    public void ConfigureArt(Texture2D panel, Texture2D heart, Texture2D core, Texture2D sword)
    {
        panelFrame = panel;
        heartIcon = heart;
        coreIcon = core;
        swordIcon = sword;
    }

    public static void ShowMessage(string text)
    {
        message = text;
        messageUntil = Time.time + 1.5f;
    }

    void OnGUI()
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Clamp(Screen.height / 800f, 1.15f, 1.5f);
        viewWidth = Screen.width / scale;
        viewHeight = Screen.height / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        EnsureStyles();
        HuntzSlicePlayer player = HuntzSlicePlayer.Instance;
        if (player == null) player = FindAnyObjectByType<HuntzSlicePlayer>();
        HuntzSliceHunt hunt = HuntzSliceHunt.Instance;
        if (hunt == null) hunt = FindAnyObjectByType<HuntzSliceHunt>();

        bool inMainLand = hunt == null || hunt.InMainLand;
        Panel(new Rect(14, 14, 324, 166), 0.82f);
        GUI.Label(new Rect(28, 20, 290, 27), inMainLand ? "HUNTZ · MAINLAND" : "HUNTZ · EXPEDIÇÃO", title);

        if (player != null)
        {
            GUI.Label(new Rect(28, 47, 295, 22), hunt != null ? hunt.CurrentRoomTitle : "Sala de combate", label);
            DrawIcon(heartIcon, new Rect(28, 71, 18, 18));
            DrawBar(new Rect(51, 71, 263, 19), player.Health / (float)player.MaxHealth,
                new Color(0.82f, 0.2f, 0.22f), $"VIDA  {player.Health} / {player.MaxHealth}");
            if (hunt != null)
            {
                if (inMainLand)
                {
                    LabelWithIcon(new Rect(28, 97, 286, 20), $"Núcleos da Fenda: {hunt.SessionCores}", coreIcon, small);
                    LabelWithIcon(new Rect(28, 120, 286, 20), $"Dano: {hunt.PlayerDamage}  ·  +1 {(hunt.HasDamageUpgrade ? "comprado" : "disponível")}", swordIcon, small);
                }
                else
                {
                    int enemiesRemaining = hunt.EnemiesRemaining;
                    GUI.Label(new Rect(28, 94, 286, 20), $"Inimigos restantes: {enemiesRemaining}", small);
                    LabelWithIcon(new Rect(28, 115, 286, 20), $"Núcleos da Fenda: {hunt.SessionCores}", coreIcon, small);
                    LabelWithIcon(new Rect(28, 136, 286, 20), $"Dano do ataque: {hunt.PlayerDamage}", swordIcon, small);
                }
            }
        }

        if (hunt != null && !inMainLand) DrawExpeditionMap(hunt);
        HuntzSliceBoss boss = FindAnyObjectByType<HuntzSliceBoss>();
        if (boss != null && !boss.IsDefeated && hunt != null && !inMainLand && !hunt.ExpeditionEnded)
            DrawBossStatus(boss);

        float hintY = viewHeight - 151;
        if (hunt != null)
        {
            Panel(new Rect(14, hintY, Mathf.Min(viewWidth - 28, 560), 40), 0.78f);
            GUI.Label(new Rect(27, hintY + 8, Mathf.Min(viewWidth - 54, 534), 24),
                hunt.CurrentInteractionHint, small);
        }

        float controlsY = viewHeight - 101;
        Panel(new Rect(14, controlsY, 500, 88), 0.72f);
        GUI.Label(new Rect(28, controlsY + 8, 470, 22), "WASD / setas  mover em 8 direções", label);
        if (inMainLand)
        {
            GUI.Label(new Rect(28, controlsY + 31, 460, 20), "E  interagir perto do Portal HZ ou da mesa", small);
            GUI.Label(new Rect(28, controlsY + 51, 460, 20), "Núcleos e melhoria ficam apenas nesta sessão", small);
        }
        else
        {
            GUI.Label(new Rect(28, controlsY + 31, 460, 20), "J / clique esquerdo  atacar para onde olha", small);
            GUI.Label(new Rect(28, controlsY + 51, 470, 20), "Espaço  esquivar  ·  E  atravessar uma passagem", small);
        }

        if (!string.IsNullOrEmpty(message) && Time.time < messageUntil)
            GUI.Label(new Rect(0, 108, viewWidth, 44), message, messageStyle);

        if (hunt != null && inMainLand && hunt.HasLastExpeditionResult)
            DrawLastExpeditionSummary(hunt);

        GUI.matrix = previousMatrix;
    }

    void DrawBossStatus(HuntzSliceBoss boss)
    {
        const float panelWidth = 440f;
        Rect panel = new Rect((viewWidth - panelWidth) * 0.5f, 14f, panelWidth, 76f);
        Panel(panel, 0.86f);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 7f, panel.width - 28f, 22f),
            boss.BossName, label);
        DrawBar(new Rect(panel.x + 14f, panel.y + 32f, panel.width - 28f, 19f),
            boss.Health / (float)boss.MaxHealth, new Color(0.62f, 0.16f, 0.25f),
            $"CHEFE  {boss.Health} / {boss.MaxHealth}");
        GUI.Label(new Rect(panel.x + 14f, panel.y + 53f, panel.width - 28f, 19f),
            boss.StateLabel, small);
    }

    void DrawLastExpeditionSummary(HuntzSliceHunt hunt)
    {
        float panelWidth = Mathf.Min(430f, viewWidth - 370f);
        if (panelWidth < 250f) return;
        const float panelHeight = 128f;
        Rect panel = new Rect(viewWidth - panelWidth - 14f, 14f, panelWidth, panelHeight);
        Panel(panel, 0.96f);

        GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 26f), "RESUMO DA ÚLTIMA HUNT", resultTitle);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 43f, panel.width - 32f, 76f),
            hunt.LastExpeditionSummary, resultBody);
    }

    void DrawExpeditionMap(HuntzSliceHunt hunt)
    {
        const float panelWidth = 198f;
        const float panelHeight = 202f;
        Rect panel = new Rect(viewWidth - panelWidth - 14f, 14f, panelWidth, panelHeight);
        Panel(panel, 0.8f);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 9f, panel.width - 24f, 24f), "MAPA DA HUNT", mapTitle);
        GUI.Label(new Rect(panel.x + 12f, panel.y + 31f, panel.width - 20f, 18f), "Salas desconhecidas: ?", small);

        HuntzSliceRoom[] rooms = hunt.Rooms;
        Vector2 MapCenter(HuntzSliceRoom room) => new Vector2(
            panel.x + 37f + room.MapPosition.x * 48f,
            panel.y + 69f + room.MapPosition.y * 43f);

        Color oldColor = GUI.color;
        GUI.color = new Color(0.72f, 0.79f, 0.81f, 0.62f);
        foreach (HuntzSliceRoom room in rooms)
        {
            if (room == null) continue;
            foreach (int connectedId in room.Connections)
            {
                if (connectedId <= room.Id) continue;
                HuntzSliceRoom connected = null;
                foreach (HuntzSliceRoom candidate in rooms)
                    if (candidate != null && candidate.Id == connectedId) { connected = candidate; break; }
                if (connected != null) DrawMapLink(MapCenter(room), MapCenter(connected));
            }
        }
        GUI.color = oldColor;

        foreach (HuntzSliceRoom room in rooms)
        {
            if (room == null) continue;
            Vector2 center = MapCenter(room);
            Rect nodeRect = new Rect(center.x - 13f, center.y - 13f, 26f, 26f);
            Color nodeColor = hunt.IsCurrent(room.Id)
                ? new Color(0.98f, 0.76f, 0.28f, 1f)
                : hunt.IsCleared(room.Id)
                    ? new Color(0.35f, 0.78f, 0.56f, 0.95f)
                    : new Color(0.46f, 0.56f, 0.61f, 0.9f);
            GUI.color = nodeColor;
            GUI.Box(nodeRect, GUIContent.none);
            GUI.color = Color.white;

            string marker = hunt.IsCurrent(room.Id) ? "●"
                : hunt.IsCleared(room.Id) ? "✓"
                : room.IsFinalRoom ? "F"
                : "?";
            GUI.Label(nodeRect, marker, mapNode);
        }
        GUI.color = oldColor;
    }

    void DrawMapLink(Vector2 start, Vector2 end)
    {
        Vector2 delta = end - start;
        Vector2 center = (start + end) * 0.5f;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, center);
        GUI.DrawTexture(new Rect(center.x - delta.magnitude * 0.5f, center.y - 1f, delta.magnitude, 2f), Texture2D.whiteTexture);
        GUIUtility.RotateAroundPivot(-angle, center);
    }

    void Panel(Rect rect, float alpha)
    {
        Color old = GUI.color;
        GUI.color = panelFrame != null
            ? new Color(1f, 1f, 1f, alpha)
            : new Color(0.04f, 0.055f, 0.07f, alpha);
        if (panelFrame != null) DrawNineSlice(rect, panelFrame, 6f, 6f);
        else GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void LabelWithIcon(Rect rect, string value, Texture2D icon, GUIStyle style)
    {
        DrawIcon(icon, new Rect(rect.x, rect.y + 1f, 17f, 17f));
        GUI.Label(new Rect(rect.x + 22f, rect.y, rect.width - 22f, rect.height), value, style);
    }

    void DrawIcon(Texture2D icon, Rect rect)
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

    void DrawBar(Rect rect, float fill, Color color, string text)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = color;
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height), Texture2D.whiteTexture);
        GUI.color = old;
        GUI.Label(rect, text, small);
    }

    void EnsureStyles()
    {
        if (title != null) return;
        title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
        title.normal.textColor = new Color(1f, 0.88f, 0.55f);
        label = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        label.normal.textColor = Color.white;
        small = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        small.normal.textColor = new Color(1f, 1f, 1f, 0.92f);
        small.alignment = TextAnchor.MiddleLeft;
        messageStyle = new GUIStyle(label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        mapTitle = new GUIStyle(label) { fontSize = 15, alignment = TextAnchor.MiddleCenter };
        mapNode = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        mapNode.normal.textColor = Color.white;
        mapNode.hover.textColor = Color.white;
        mapNode.active.textColor = Color.white;
        resultTitle = new GUIStyle(title) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
        resultTitle.normal.textColor = new Color(1f, 0.82f, 0.42f);
        resultBody = new GUIStyle(small) { fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
    }
}
