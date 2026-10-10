using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class HuntzArpg3DHuntDirector : MonoBehaviour
{
    const int GridSize = 3;
    const int FinalRoomIndex = GridSize * GridSize - 1;

    enum RoomKind
    {
        Encounter,
        Cache,
        SurpriseBoss,
        FinalBoss
    }

    sealed class RoomState
    {
        public RoomKind kind;
        public bool discovered;
        public bool cleared;
        public bool rewardCollected;
    }

    sealed class GateExit
    {
        public HuntzArpg3DInteractable interactable;
        public Renderer[] renderers;
    }

    [SerializeField] HuntzArpg3DPlayerController player;
    [SerializeField] HuntzArpg3DGrayboxKit arenaKit;
    [SerializeField, Min(0.2f)] float deathReturnDelay = 1.4f;

    readonly RoomState[] rooms = new RoomState[GridSize * GridSize];
    readonly List<HuntzArpg3DTarget> livingTargets = new List<HuntzArpg3DTarget>();
    readonly List<GateExit> exits = new List<GateExit>();
    readonly List<GameObject> trainingSetPieces = new List<GameObject>();
    readonly List<bool> trainingSetOriginalStates = new List<bool>();

    Transform mainlandArrival;
    Transform huntIsland;
    HuntzArpg3DInteractable entryPortal;
    HuntzArpg3DInteractable returnPortal;
    Transform dynamicContentRoot;
    int currentRoomIndex = -1;
    int runNumber;
    int fragmentsAtRunStart;
    bool inHunt;
    bool trainingSetHidden;
    float deathReturnAt = -1f;
    GUIStyle nodeStyle;

    static readonly Vector3 LockedGateColor = new Vector3(0.24f, 0.26f, 0.31f);
    static readonly Vector3 OpenGateColor = new Vector3(0.12f, 0.72f, 0.93f);

    public void Configure(HuntzArpg3DPlayerController playerController, HuntzArpg3DGrayboxKit grayboxKit = null)
    {
        player = playerController;
        if (grayboxKit != null) arenaKit = grayboxKit;
    }

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<HuntzArpg3DPlayerController>();
        if (player == null)
        {
            Debug.LogError("Huntz Hunt Director could not find the player controller.", this);
            enabled = false;
            return;
        }

        if (arenaKit == null || !arenaKit.IsComplete)
        {
            Debug.LogError("Huntz Hunt Director requires the generated graybox arena kit.", this);
            enabled = false;
            return;
        }

        player.ResetToSpawn += OnPlayerReset;
        CacheTrainingSetPieces();
        mainlandArrival = GameObject.Find("Chegada da MainLand")?.transform;
        huntIsland = GameObject.Find("Hunt — Campo de Treino")?.transform;
        if (mainlandArrival == null) mainlandArrival = player.transform;
        if (huntIsland == null) huntIsland = transform;

        HuntzArpg3DInteractable[] interactables = FindObjectsByType<HuntzArpg3DInteractable>();
        foreach (HuntzArpg3DInteractable interactable in interactables)
        {
            if (interactable == null) continue;
            if (interactable.name.StartsWith("Portal HZ", StringComparison.Ordinal))
                entryPortal = interactable;
            else if (interactable.name.Contains("Portal de Retorno"))
                returnPortal = interactable;
        }

        if (entryPortal != null) entryPortal.SetInteractionOverride(StartExpedition);
        if (returnPortal != null) returnPortal.SetInteractionOverride(LeaveExpedition);

        foreach (HuntzArpg3DTarget target in FindObjectsByType<HuntzArpg3DTarget>())
        {
            if (target != null && target.transform.IsChildOf(huntIsland))
                target.gameObject.SetActive(false);
        }

        GameObject content = new GameObject("Hunt Runtime — Salas, encontros e portais");
        content.transform.SetParent(transform, false);
        dynamicContentRoot = content.transform;
    }

    void OnDestroy()
    {
        if (player != null) player.ResetToSpawn -= OnPlayerReset;
        if (entryPortal != null) entryPortal.SetInteractionOverride(null);
        if (returnPortal != null) returnPortal.SetInteractionOverride(null);
        RestoreTrainingSet();
    }

    void Update()
    {
        if (!inHunt || player == null) return;
        if (!player.IsDead)
        {
            deathReturnAt = -1f;
            return;
        }

        if (deathReturnAt < 0f) deathReturnAt = Time.time + deathReturnDelay;
        if (Time.time < deathReturnAt) return;

        inHunt = false;
        currentRoomIndex = -1;
        ClearDynamicContent();
        RestoreTrainingSet();
        player.ReturnToMainlandAfterDeath(mainlandArrival.position,
            "Você caiu na Hunt. Voltou à MainLand; os fragmentos já coletados foram mantidos.");
    }

    void StartExpedition(HuntzArpg3DPlayerController interactingPlayer)
    {
        if (inHunt)
        {
            interactingPlayer.ShowFeedback("A expedição já está em andamento.");
            return;
        }

        player = interactingPlayer;
        HideTrainingSet();
        BuildRoomMap();
        inHunt = true;
        deathReturnAt = -1f;
        fragmentsAtRunStart = player.RiftFragments;
        Vector3 arrival = huntIsland.position + new Vector3(0f, 0.05f, -1.8f);
        player.TeleportTo(arrival, "Hunt 01 — Ruptura de Treino");
        player.FaceDirection(Vector3.forward);
        ActivateRoom(0, Vector3.forward);
    }

    void BuildRoomMap()
    {
        Array.Clear(rooms, 0, rooms.Length);
        System.Random random = new System.Random(unchecked((int)DateTime.UtcNow.Ticks + runNumber * 7919));
        runNumber++;

        for (int i = 0; i < rooms.Length; i++)
            rooms[i] = new RoomState { kind = i == FinalRoomIndex ? RoomKind.FinalBoss : RoomKind.Encounter };

        int cacheIndex = random.Next(1, FinalRoomIndex);
        int surpriseBossIndex = random.Next(1, FinalRoomIndex - 1);
        if (surpriseBossIndex >= cacheIndex) surpriseBossIndex++;
        rooms[cacheIndex].kind = RoomKind.Cache;
        rooms[surpriseBossIndex].kind = RoomKind.SurpriseBoss;
    }

    void ActivateRoom(int roomIndex, Vector3 arrivalDirection)
    {
        ClearDynamicContent();
        exits.Clear();
        livingTargets.Clear();
        currentRoomIndex = roomIndex;
        RoomState room = rooms[roomIndex];
        room.discovered = true;

        Vector3 spawn = huntIsland.position + new Vector3(0f, 0.05f, -0.85f);
        player.TeleportTo(spawn, "Hunt 01 — Ruptura de Treino");
        if (arrivalDirection.sqrMagnitude > 0.01f) player.FaceDirection(arrivalDirection);

        HuntzArpg3DArenaLayoutKind layout = room.kind == RoomKind.Cache
            ? HuntzArpg3DArenaLayoutKind.Cache
            : room.kind == RoomKind.FinalBoss || room.kind == RoomKind.SurpriseBoss
                ? HuntzArpg3DArenaLayoutKind.Boss
                : HuntzArpg3DArenaLayoutKind.Combat;
        HuntzArpg3DGrayboxArenaBuilder.Build(dynamicContentRoot, huntIsland.position, arenaKit, layout, roomIndex);

        if (!room.cleared) SpawnRoomEncounter(room);
        CreateAvailableExits(roomIndex, room.cleared);
        if (room.kind == RoomKind.Cache && !room.rewardCollected) CreateCacheChest(room);

        if (livingTargets.Count == 0 && !room.cleared)
            ClearCurrentRoom();

        string roomName = GetRoomTypeName(room);
        player.ShowFeedback("Sala " + RoomLabel(roomIndex) + " — " + roomName + ". " +
            (room.cleared ? "Saídas abertas." : "Derrote os inimigos para abrir as saídas."));
    }

    void SpawnRoomEncounter(RoomState room)
    {
        Vector3 center = huntIsland.position;
        if (room.kind == RoomKind.Cache) return;

        if (room.kind == RoomKind.FinalBoss || room.kind == RoomKind.SurpriseBoss)
        {
            Vector3 bossPosition = center + new Vector3(0f, 0f, 2.2f);
            CreateTarget("Guardião da Fenda", bossPosition, true,
                room.kind == RoomKind.FinalBoss ? 16 : 10,
                room.kind == RoomKind.FinalBoss ? 8f : 6.5f,
                room.kind == RoomKind.FinalBoss ? 1.75f : 1.5f,
                1,
                room.kind == RoomKind.FinalBoss ? new Color(0.54f, 0.18f, 0.73f) : new Color(0.9f, 0.34f, 0.14f));
            return;
        }

        CreateTarget("Slime de Cinzas", center + new Vector3(-1.8f, 0f, 1.25f), false,
            3, 7f, 1.65f, 1, new Color(0.7f, 0.22f, 0.24f));
        CreateTarget("Slime de Cinzas", center + new Vector3(1.8f, 0f, 1.25f), false,
            3, 7f, 1.65f, 1, new Color(0.75f, 0.28f, 0.3f));
    }

    HuntzArpg3DTarget CreateTarget(string title, Vector3 position, bool isBoss, int health,
        float aggroRange, float speed, int contactDamage, Color color)
    {
        GameObject targetObject = new GameObject(title);
        targetObject.transform.SetParent(dynamicContentRoot, true);
        targetObject.transform.position = position;

        CharacterController motor = targetObject.AddComponent<CharacterController>();
        motor.height = isBoss ? 1.85f : 1.14f;
        motor.radius = isBoss ? 0.62f : 0.5f;
        motor.center = Vector3.up * (motor.height * 0.5f);
        motor.stepOffset = 0.2f;
        motor.skinWidth = 0.05f;

        if (isBoss)
        {
            CreateVisual(PrimitiveType.Capsule, targetObject.transform, new Vector3(0f, 0.95f, 0f),
                new Vector3(0.95f, 0.98f, 0.78f), color);
            CreateVisual(PrimitiveType.Sphere, targetObject.transform, new Vector3(0f, 1.35f, 0.36f),
                new Vector3(0.32f, 0.38f, 0.28f), new Color(0.95f, 0.75f, 0.3f));
            CreateVisual(PrimitiveType.Cube, targetObject.transform, new Vector3(-0.57f, 1.46f, 0f),
                new Vector3(0.2f, 0.9f, 0.22f), color * 0.72f).transform.localRotation = Quaternion.Euler(0f, 0f, -22f);
            CreateVisual(PrimitiveType.Cube, targetObject.transform, new Vector3(0.57f, 1.46f, 0f),
                new Vector3(0.2f, 0.9f, 0.22f), color * 0.72f).transform.localRotation = Quaternion.Euler(0f, 0f, 22f);
        }
        else
        {
            CreateVisual(PrimitiveType.Sphere, targetObject.transform, new Vector3(0f, 0.48f, 0f),
                new Vector3(1.05f, 0.8f, 0.95f), color);
            CreateVisual(PrimitiveType.Sphere, targetObject.transform, new Vector3(0f, 0.55f, 0.36f),
                new Vector3(0.26f, 0.28f, 0.16f), new Color(0.95f, 0.7f, 0.23f));
        }

        HuntzArpg3DTarget target = targetObject.AddComponent<HuntzArpg3DTarget>();
        target.Configure(title, health, aggroRange, speed, isBoss ? 1.45f : 1.15f,
            contactDamage, targetObject.GetComponentsInChildren<Renderer>());
        target.Defeated += OnTargetDefeated;
        livingTargets.Add(target);
        return target;
    }

    void OnTargetDefeated(HuntzArpg3DTarget target)
    {
        livingTargets.Remove(target);
        if (livingTargets.Count == 0) ClearCurrentRoom();
    }

    void ClearCurrentRoom()
    {
        if (!inHunt || currentRoomIndex < 0) return;
        RoomState room = rooms[currentRoomIndex];
        if (room.cleared) return;

        room.cleared = true;
        int reward = room.kind == RoomKind.FinalBoss ? 5 : room.kind == RoomKind.SurpriseBoss ? 3 : 1;
        player.AddRiftFragments(reward);

        foreach (GateExit exit in exits)
            SetRenderersColor(exit.renderers, new Color(OpenGateColor.x, OpenGateColor.y, OpenGateColor.z));

        player.ShowFeedback(room.kind == RoomKind.FinalBoss
            ? "Chefe derrotado! A Hunt foi concluída."
            : "Sala limpa. +" + reward + " fragmento(s) de fenda; escolha uma saída.");

        if (room.kind == RoomKind.FinalBoss)
            FinishExpedition();
    }

    void CreateAvailableExits(int roomIndex, bool unlocked)
    {
        int row = roomIndex / GridSize;
        int column = roomIndex % GridSize;
        if (row > 0) CreateExit(roomIndex, roomIndex - GridSize, Vector3.back, "Norte", unlocked);
        if (column < GridSize - 1) CreateExit(roomIndex, roomIndex + 1, Vector3.right, "Leste", unlocked);
        if (row < GridSize - 1) CreateExit(roomIndex, roomIndex + GridSize, Vector3.forward, "Sul", unlocked);
        if (column > 0) CreateExit(roomIndex, roomIndex - 1, Vector3.left, "Oeste", unlocked);
    }

    void CreateExit(int sourceIndex, int destinationIndex, Vector3 direction, string label, bool unlocked)
    {
        Vector3 offset = new Vector3(direction.x * 4.35f, 0f, direction.z * 3.25f);
        GameObject gate = new GameObject("Saída " + label + " — sala " + RoomLabel(destinationIndex));
        gate.transform.SetParent(dynamicContentRoot, true);
        gate.transform.position = huntIsland.position + offset;
        gate.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Color gateColor = unlocked
            ? new Color(OpenGateColor.x, OpenGateColor.y, OpenGateColor.z)
            : new Color(LockedGateColor.x, LockedGateColor.y, LockedGateColor.z);
        CreateVisual(PrimitiveType.Cube, gate.transform, new Vector3(-0.62f, 0.92f, 0f),
            new Vector3(0.2f, 1.9f, 0.22f), gateColor);
        CreateVisual(PrimitiveType.Cube, gate.transform, new Vector3(0.62f, 0.92f, 0f),
            new Vector3(0.2f, 1.9f, 0.22f), gateColor);
        CreateVisual(PrimitiveType.Cube, gate.transform, new Vector3(0f, 1.9f, 0f),
            new Vector3(1.43f, 0.2f, 0.22f), gateColor);
        CreateVisual(PrimitiveType.Cube, gate.transform, new Vector3(0f, 0.94f, -0.04f),
            new Vector3(1.02f, 1.58f, 0.08f), new Color(0.18f, 0.56f, 0.69f));

        SphereCollider trigger = gate.AddComponent<SphereCollider>();
        trigger.center = new Vector3(0f, 0.9f, 0f);
        trigger.radius = 1.55f;
        trigger.isTrigger = true;
        HuntzArpg3DInteractable interactable = gate.AddComponent<HuntzArpg3DInteractable>();
        interactable.Configure("Portal " + label, "Pressione E para seguir à sala " + RoomLabel(destinationIndex) + ".");
        interactable.SetInteractionOverride(interactingPlayer =>
        {
            RoomState source = rooms[sourceIndex];
            if (!source.cleared)
            {
                interactingPlayer.ShowFeedback("Derrote os inimigos desta sala para abrir os portais.");
                return;
            }

            ActivateRoom(destinationIndex, direction);
        });

        exits.Add(new GateExit
        {
            interactable = interactable,
            renderers = gate.GetComponentsInChildren<Renderer>()
        });
    }

    void CreateCacheChest(RoomState room)
    {
        GameObject chest = new GameObject("Baú da Ruptura");
        chest.transform.SetParent(dynamicContentRoot, true);
        chest.transform.position = huntIsland.position + new Vector3(0f, 0.12f, 0.7f);
        CreateVisual(PrimitiveType.Cube, chest.transform, new Vector3(0f, 0.36f, 0f),
            new Vector3(1.15f, 0.72f, 0.82f), new Color(0.39f, 0.23f, 0.12f));
        CreateVisual(PrimitiveType.Cube, chest.transform, new Vector3(0f, 0.79f, 0f),
            new Vector3(1.2f, 0.16f, 0.86f), new Color(0.88f, 0.62f, 0.22f));
        BoxCollider trigger = chest.AddComponent<BoxCollider>();
        trigger.center = new Vector3(0f, 0.52f, 0f);
        trigger.size = new Vector3(1.6f, 1.4f, 1.4f);
        trigger.isTrigger = true;
        HuntzArpg3DInteractable interactable = chest.AddComponent<HuntzArpg3DInteractable>();
        interactable.Configure("Baú da Ruptura", "Pressione E para recolher os fragmentos.");
        interactable.SetInteractionOverride(interactingPlayer =>
        {
            if (room.rewardCollected) return;
            room.rewardCollected = true;
            interactingPlayer.AddRiftFragments(3);
            interactingPlayer.ShowFeedback("Baú aberto: +3 fragmentos de fenda.");
            chest.SetActive(false);
        });
    }

    void LeaveExpedition(HuntzArpg3DPlayerController interactingPlayer)
    {
        if (!inHunt)
        {
            interactingPlayer.ShowFeedback("Entre pelo Portal HZ para iniciar uma Hunt.");
            return;
        }

        int collected = interactingPlayer.RiftFragments - fragmentsAtRunStart;
        inHunt = false;
        currentRoomIndex = -1;
        deathReturnAt = -1f;
        ClearDynamicContent();
        RestoreTrainingSet();
        interactingPlayer.TeleportTo(mainlandArrival.position, "MainLand");
        interactingPlayer.ShowFeedback("Você encerrou a Hunt antes do chefe. Fragmentos coletados: " + collected + ".");
    }

    void FinishExpedition()
    {
        int collected = player.RiftFragments - fragmentsAtRunStart;
        inHunt = false;
        currentRoomIndex = -1;
        ClearDynamicContent();
        RestoreTrainingSet();
        player.TeleportTo(mainlandArrival.position, "MainLand");
        player.ShowFeedback("Hunt concluída! O chefe caiu. Fragmentos obtidos nesta expedição: " + collected + ".");
    }

    void OnPlayerReset()
    {
        if (!inHunt) return;
        inHunt = false;
        currentRoomIndex = -1;
        deathReturnAt = -1f;
        ClearDynamicContent();
        RestoreTrainingSet();
    }

    void CacheTrainingSetPieces()
    {
        string[] names =
        {
            "Pilar de Pedra",
            "Barreira de Madeira A",
            "Barreira de Madeira B",
            "Acampamento de Caça",
            "Vigia da Guilda"
        };

        foreach (string objectName in names)
        {
            GameObject sceneObject = GameObject.Find(objectName);
            if (sceneObject == null) continue;
            trainingSetPieces.Add(sceneObject);
            trainingSetOriginalStates.Add(sceneObject.activeSelf);
        }
    }

    void HideTrainingSet()
    {
        if (trainingSetHidden) return;
        for (int i = 0; i < trainingSetPieces.Count; i++)
        {
            if (trainingSetPieces[i] != null) trainingSetPieces[i].SetActive(false);
        }
        trainingSetHidden = true;
    }

    void RestoreTrainingSet()
    {
        if (!trainingSetHidden) return;
        for (int i = 0; i < trainingSetPieces.Count; i++)
        {
            if (trainingSetPieces[i] != null) trainingSetPieces[i].SetActive(trainingSetOriginalStates[i]);
        }
        trainingSetHidden = false;
    }

    void ClearDynamicContent()
    {
        livingTargets.Clear();
        exits.Clear();
        if (dynamicContentRoot == null) return;
        for (int i = dynamicContentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = dynamicContentRoot.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    static GameObject CreateVisual(PrimitiveType primitive, Transform parent, Vector3 localPosition,
        Vector3 localScale, Color color)
    {
        GameObject visual = GameObject.CreatePrimitive(primitive);
        visual.name = primitive.ToString();
        visual.transform.SetParent(parent, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = localScale;
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        Renderer renderer = visual.GetComponent<Renderer>();
        SetRendererColor(renderer, color);
        return visual;
    }

    static void SetRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        renderer.SetPropertyBlock(block);
    }

    static void SetRenderersColor(Renderer[] renderers, Color color)
    {
        if (renderers == null) return;
        foreach (Renderer renderer in renderers) SetRendererColor(renderer, color);
    }

    string RoomLabel(int index)
    {
        int row = index / GridSize;
        int column = index % GridSize;
        return ((char)('A' + row)).ToString() + (column + 1);
    }

    string GetRoomTypeName(RoomState room)
    {
        if (room.kind == RoomKind.FinalBoss) return "chefe final";
        if (room.kind == RoomKind.SurpriseBoss) return "chefe surpresa";
        if (room.kind == RoomKind.Cache) return room.rewardCollected ? "baú saqueado" : "recompensa oculta";
        return room.cleared ? "encontro limpo" : "encontro";
    }

    void OnGUI()
    {
        if (!inHunt || currentRoomIndex < 0) return;

        const float panelWidth = 238f;
        const float panelHeight = 316f;
        Rect panel = new Rect(Mathf.Max(520f, Screen.width - panelWidth - 14f), 14f, panelWidth, panelHeight);
        GUI.Box(panel, "HUNT 01 — FÁCIL");
        GUI.Label(new Rect(panel.x + 12f, panel.y + 30f, panelWidth - 24f, 20f),
            "Sala " + RoomLabel(currentRoomIndex) + " • " + GetRoomTypeName(rooms[currentRoomIndex]));
        int remaining = livingTargets.Count;
        GUI.Label(new Rect(panel.x + 12f, panel.y + 50f, panelWidth - 24f, 20f),
            remaining > 0 ? "Inimigos restantes: " + remaining : "Portais abertos");

        const float cell = 42f;
        const float gap = 18f;
        float mapX = panel.x + 38f;
        float mapY = panel.y + 83f;
        GUI.color = new Color(0.22f, 0.25f, 0.31f);
        for (int row = 0; row < GridSize; row++)
        {
            for (int column = 0; column < GridSize; column++)
            {
                float x = mapX + column * (cell + gap) + cell;
                float y = mapY + row * (cell + gap) + cell * 0.5f;
                if (column < GridSize - 1)
                    GUI.DrawTexture(new Rect(x, y, gap, 3f), Texture2D.whiteTexture);
                if (row < GridSize - 1)
                    GUI.DrawTexture(new Rect(x - cell * 0.5f, y, 3f, gap), Texture2D.whiteTexture);
            }
        }
        GUI.color = Color.white;

        if (nodeStyle == null)
        {
            nodeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 17
            };
            nodeStyle.normal.textColor = Color.white;
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            RoomState room = rooms[i];
            int row = i / GridSize;
            int column = i % GridSize;
            Rect node = new Rect(mapX + column * (cell + gap), mapY + row * (cell + gap), cell, cell);
            Color nodeColor = i == currentRoomIndex ? new Color(0.9f, 0.61f, 0.14f)
                : room.cleared ? new Color(0.18f, 0.49f, 0.33f)
                : room.discovered ? new Color(0.18f, 0.39f, 0.58f)
                : new Color(0.18f, 0.2f, 0.25f);
            GUI.color = nodeColor;
            GUI.DrawTexture(node, Texture2D.whiteTexture);
            GUI.color = Color.white;

            string symbol = !room.discovered ? "?"
                : room.kind == RoomKind.FinalBoss ? "F"
                : room.kind == RoomKind.Cache ? "R"
                : room.kind == RoomKind.SurpriseBoss ? "B"
                : i == 0 ? "I"
                : room.cleared ? "✓" : "•";
            GUI.Label(node, symbol, nodeStyle);
        }

        GUI.Label(new Rect(panel.x + 12f, panel.y + 259f, panelWidth - 24f, 18f),
            "I entrada  •  R saque  •  B surpresa  •  F chefe");
        GUI.Label(new Rect(panel.x + 12f, panel.y + 277f, panelWidth - 24f, 18f),
            "E abre portais quando a sala estiver limpa");
    }
}
