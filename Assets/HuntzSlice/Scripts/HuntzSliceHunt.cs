using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owns the temporary MainLand-to-hunt loop and all session-only progression.
[DisallowMultipleComponent]
public sealed class HuntzSliceHunt : MonoBehaviour
{
    sealed class RoomState
    {
        public bool visited;
        public bool cleared;
        public bool rewardClaimed;
    }

    public static HuntzSliceHunt Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRuntimeSingleton() => Instance = null;

    [SerializeField] Transform roomLibrary;
    [SerializeField] Transform gateRoot;
    [SerializeField] HuntzSlicePlayer player;
    [SerializeField] Sprite gateRingSprite;
    [SerializeField] Sprite gateCenterSprite;
    [SerializeField] int startingRoomId;
    [SerializeField] float gateDistance = 1.65f;
    [SerializeField] float interactionDistance = 0.85f;
    [SerializeField] Transform mainLandRoot;
    [SerializeField] Transform huntEnvironmentRoot;
    [SerializeField] Transform portalPoint;
    [SerializeField] Transform preparationPoint;
    [SerializeField] Vector2 mainLandSpawn = new Vector2(0f, -1.45f);
    [SerializeField] int damageUpgradeCost = 1;

    [System.NonSerialized] Dictionary<int, RoomState> states;
    [System.NonSerialized] List<HuntzSliceGate> activeGates;
    [System.NonSerialized] HuntzSliceRoom[] rooms;

    int currentRoomId = -1;
    int sessionCores;
    int damageBonus;
    int expeditionCoresEarned;
    int roomsVisited;
    bool routeComplete;
    bool expeditionWon;
    bool inMainLand = true;
    bool hasLastExpeditionResult;
    bool missingRoomStateLogged;
    string lastExpeditionSummary = "";

    public HuntzSliceRoom[] Rooms => rooms;
    public int CurrentRoomId => currentRoomId;
    public int SessionCores => sessionCores;
    public int DamageBonus => damageBonus;
    public int PlayerDamage => player != null ? player.CurrentAttackDamage : 1 + damageBonus;
    public bool HasDamageUpgrade => damageBonus > 0;
    public int DamageUpgradeCost => damageUpgradeCost;
    public string DamageUpgradeName => "+1 de dano";
    public string CoreName => "Núcleo da Fenda";
    public bool InMainLand => inMainLand;
    public bool HasLastExpeditionResult => hasLastExpeditionResult;
    public string LastExpeditionSummary => lastExpeditionSummary;
    public string CurrentRoomTitle => inMainLand ? "MainLand · Refúgio HZ" : GetRoom(currentRoomId)?.Title ?? "Preparando expedição";
    public bool RouteComplete => routeComplete;
    public bool ExpeditionEnded => routeComplete;
    public bool ExpeditionWon => expeditionWon;
    public int EnemiesRemaining
    {
        get
        {
            HuntzSliceBoss boss = GetCurrentBoss();
            return HuntzSliceEnemy.Active.Count + (boss != null && !boss.IsDefeated ? 1 : 0);
        }
    }

    void Awake()
    {
        Instance = this;
        EnsureRuntimeData(true);
    }

    void OnEnable() => Instance = this;

    void Start()
    {
        EnsureRuntimeData(false);
        if (player == null) player = FindAnyObjectByType<HuntzSlicePlayer>();

        inMainLand = true;
        currentRoomId = -1;
        if (mainLandRoot != null) mainLandRoot.gameObject.SetActive(true);
        if (huntEnvironmentRoot != null) huntEnvironmentRoot.gameObject.SetActive(false);
        if (player != null) player.ResetForExpedition(mainLandSpawn, damageBonus);
        HuntzSliceHud.ShowMessage("Refúgio HZ · prepare-se e use o Portal HZ para iniciar a hunt.");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (Instance != this) Instance = this;
        EnsureRuntimeData(false);

        Keyboard keyboard = Keyboard.current;
        if (inMainLand)
        {
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                TryUseMainLandPoint();
            return;
        }

        if (currentRoomId < 0 || routeComplete) return;
        if (!TryGetRoomState(currentRoomId, out RoomState state))
        {
            if (!missingRoomStateLogged)
            {
                Debug.LogError($"[Huntz] Estado ausente para a sala ativa {currentRoomId}.", this);
                missingRoomStateLogged = true;
            }
            return;
        }

        HuntzSliceRoom currentRoom = GetRoom(currentRoomId);
        if (currentRoom != null && currentRoom.IsFinalRoom)
        {
            if (GetCurrentBoss() == null && !state.cleared && !missingRoomStateLogged)
            {
                Debug.LogError("[Huntz] A Câmara final está sem o chefe configurado.", this);
                missingRoomStateLogged = true;
            }
            return;
        }

        if (!state.cleared && HuntzSliceEnemy.Active.Count == 0)
            ClearCurrentRoom();

        if (state.cleared && keyboard != null && keyboard.eKey.wasPressedThisFrame)
            TryUseNearbyGate();
    }

    public bool IsVisited(int roomId) => TryGetRoomState(roomId, out RoomState state) && state.visited;
    public bool IsCleared(int roomId) => TryGetRoomState(roomId, out RoomState state) && state.cleared;
    public bool IsCurrent(int roomId) => !inMainLand && currentRoomId == roomId;

    public string CurrentInteractionHint
    {
        get
        {
            if (inMainLand)
            {
                float portalDistance = portalPoint != null && player != null
                    ? Vector2.Distance(player.transform.position, portalPoint.position) : float.MaxValue;
                float preparationDistance = preparationPoint != null && player != null
                    ? Vector2.Distance(player.transform.position, preparationPoint.position) : float.MaxValue;

                if (portalDistance <= interactionDistance) return "[E] Ativar Portal HZ e iniciar uma nova hunt";
                if (preparationDistance <= interactionDistance)
                    return HasDamageUpgrade
                        ? "Melhoria de teste já comprada nesta sessão: +1 de dano."
                        : $"[E] Comprar melhoria de teste (+1 dano · {damageUpgradeCost} Núcleo da Fenda)";
                return "Aproxime-se do Portal HZ para caçar ou da mesa para preparar o equipamento.";
            }

            if (routeComplete)
                return expeditionWon ? "Chefe derrotado. Retornando à MainLand." : "Expedição encerrada. Retornando à MainLand.";
            if (currentRoomId < 0) return "Preparando expedição";
            if (!TryGetRoomState(currentRoomId, out RoomState state)) return "Preparando expedição";
            if (!state.cleared) return "Derrote os inimigos para abrir as passagens.";

            HuntzSliceGate nearest = FindNearestGate(out float distance);
            if (nearest != null && distance <= 0.72f)
            {
                HuntzSliceRoom destination = GetRoom(nearest.DestinationRoomId);
                string destinationName = IsVisited(nearest.DestinationRoomId) && destination != null
                    ? destination.Title : "sala desconhecida";
                return $"[E] Entrar em {destinationName}";
            }

            return activeGates.Count > 0
                ? "Aproxime-se de uma passagem brilhante e pressione E."
                : "Não há outras passagens nesta sala.";
        }
    }

    public void Configure(Transform roomsRoot, Transform exitsRoot, HuntzSlicePlayer playerActor,
        Sprite ringSprite, Sprite centerSprite, int entryRoomId, Transform landRoot,
        Transform huntRoot, Transform portal, Transform preparation, Vector2 landSpawn)
    {
        roomLibrary = roomsRoot;
        gateRoot = exitsRoot;
        player = playerActor;
        gateRingSprite = ringSprite;
        gateCenterSprite = centerSprite;
        startingRoomId = entryRoomId;
        mainLandRoot = landRoot;
        huntEnvironmentRoot = huntRoot;
        portalPoint = portal;
        preparationPoint = preparation;
        mainLandSpawn = landSpawn;
    }

    void TryUseMainLandPoint()
    {
        if (player == null) return;
        float portalDistance = portalPoint != null
            ? Vector2.Distance(player.transform.position, portalPoint.position) : float.MaxValue;
        float preparationDistance = preparationPoint != null
            ? Vector2.Distance(player.transform.position, preparationPoint.position) : float.MaxValue;

        if (portalDistance <= interactionDistance)
        {
            BeginExpedition();
            return;
        }

        if (preparationDistance <= interactionDistance)
        {
            BuyDamageUpgrade();
            return;
        }

        HuntzSliceHud.ShowMessage("Aproxime-se do Portal HZ ou da mesa de preparação para interagir.");
    }

    void BuyDamageUpgrade()
    {
        if (HasDamageUpgrade)
        {
            HuntzSliceHud.ShowMessage("A melhoria +1 de dano já foi comprada nesta sessão.");
            return;
        }
        if (sessionCores < damageUpgradeCost)
        {
            HuntzSliceHud.ShowMessage($"Falta {damageUpgradeCost - sessionCores} Núcleo da Fenda para a melhoria.");
            return;
        }

        sessionCores -= damageUpgradeCost;
        damageBonus = 1;
        if (player != null) player.SetDamageBonus(damageBonus);
        HuntzSliceHud.ShowMessage("Melhoria aplicada: seus ataques agora causam +1 de dano nesta sessão.");
    }

    void BeginExpedition()
    {
        if (!inMainLand || player == null || GetRoom(startingRoomId) == null)
        {
            HuntzSliceHud.ShowMessage("Não foi possível iniciar a hunt: confira a sala de entrada.");
            return;
        }

        EnsureRuntimeData(false);
        ClearGates();
        states.Clear();
        foreach (HuntzSliceRoom room in rooms)
            if (room != null) states[room.Id] = new RoomState();

        routeComplete = false;
        expeditionWon = false;
        inMainLand = false;
        currentRoomId = -1;
        expeditionCoresEarned = 0;
        roomsVisited = 0;
        missingRoomStateLogged = false;

        if (mainLandRoot != null) mainLandRoot.gameObject.SetActive(false);
        if (huntEnvironmentRoot != null) huntEnvironmentRoot.gameObject.SetActive(true);

        foreach (HuntzSliceRoom room in rooms)
        {
            if (room == null) continue;
            room.Content.SetActive(false);
            room.Content.SetActive(true);
            foreach (HuntzSliceEnemy enemy in room.GetComponentsInChildren<HuntzSliceEnemy>(true))
                enemy.ResetForExpedition();
            HuntzSliceBoss boss = room.GetComponentInChildren<HuntzSliceBoss>(true);
            if (boss != null) boss.ResetForExpedition();
            room.Content.SetActive(room.Id == startingRoomId);
        }

        player.ResetForExpedition(Vector2.zero, damageBonus);
        EnterRoom(startingRoomId, true);
    }

    void ClearCurrentRoom()
    {
        if (!TryGetRoomState(currentRoomId, out RoomState state)) return;
        state.cleared = true;
        HuntzSliceRoom room = GetRoom(currentRoomId);

        if (room != null && room.RiftCoreReward > 0 && !state.rewardClaimed)
        {
            state.rewardClaimed = true;
            sessionCores += room.RiftCoreReward;
            expeditionCoresEarned += room.RiftCoreReward;
            HuntzSliceHud.ShowMessage($"Desvio concluído: +{room.RiftCoreReward} Núcleo(s) da Fenda.");
        }
        else
        {
            HuntzSliceHud.ShowMessage("Sala limpa. As passagens foram abertas.");
        }

        CreateGates(room);
    }

    public void CompleteFinalBoss()
    {
        HuntzSliceRoom finalRoom = GetRoom(currentRoomId);
        if (routeComplete || inMainLand || finalRoom == null || !finalRoom.IsFinalRoom) return;

        if (TryGetRoomState(currentRoomId, out RoomState state)) state.cleared = true;
        sessionCores++;
        expeditionCoresEarned++;
        FinishExpedition(true);
    }

    public void EndByPlayerDeath()
    {
        if (routeComplete || inMainLand) return;
        FinishExpedition(false);
    }

    void FinishExpedition(bool won)
    {
        expeditionWon = won;
        routeComplete = true;
        ClearGates();

        string outcome = won ? "HUNT CONCLUÍDA · Guardião da Fenda derrotado." : $"EXPEDIÇÃO ENCERRADA · derrota em {CurrentRoomTitle}.";
        string reward = expeditionCoresEarned == 1
            ? "+1 Núcleo da Fenda"
            : $"+{expeditionCoresEarned} Núcleos da Fenda";
        lastExpeditionSummary = $"{outcome}\n{roomsVisited} sala(s) visitada(s) · {reward} · saldo: {sessionCores}";
        hasLastExpeditionResult = true;

        if (mainLandRoot != null) mainLandRoot.gameObject.SetActive(true);
        if (huntEnvironmentRoot != null) huntEnvironmentRoot.gameObject.SetActive(false);
        inMainLand = true;
        currentRoomId = -1;
        if (player != null) player.ResetForExpedition(mainLandSpawn, damageBonus);
        HuntzSliceHud.ShowMessage(won ? "Você voltou à MainLand com a recompensa." : "Você voltou à MainLand após a derrota.");
    }

    void CreateGates(HuntzSliceRoom room)
    {
        EnsureRuntimeCollections();
        ClearGates();
        if (room == null || gateRoot == null) return;

        foreach (int destinationId in room.Connections)
        {
            HuntzSliceRoom destination = GetRoom(destinationId);
            if (destination == null) continue;

            Vector2 gridDirection = (Vector2)(destination.MapPosition - room.MapPosition);
            Vector2 worldDirection = new Vector2(
                (gridDirection.x - gridDirection.y) * 0.5f,
                (gridDirection.x + gridDirection.y) * 0.25f).normalized;
            Vector2 position = worldDirection * gateDistance;

            GameObject gate = new GameObject($"Passagem para sala {destinationId}");
            gate.transform.SetParent(gateRoot, false);
            gate.transform.position = position;
            gate.transform.localScale = Vector3.one * 0.58f;

            SpriteRenderer ring = gate.AddComponent<SpriteRenderer>();
            ring.sprite = gateRingSprite;
            ring.color = new Color(0.3f, 0.88f, 1f, 0.95f);
            ring.sortingOrder = 0;

            GameObject coreObject = new GameObject("Portal glow");
            coreObject.transform.SetParent(gate.transform, false);
            coreObject.transform.localScale = Vector3.one * 0.38f;
            SpriteRenderer core = coreObject.AddComponent<SpriteRenderer>();
            core.sprite = gateCenterSprite;
            core.color = new Color(0.44f, 0.9f, 1f, 0.48f);
            core.sortingOrder = 1;

            HuntzSliceGate marker = gate.AddComponent<HuntzSliceGate>();
            marker.Configure(destinationId);
            gate.AddComponent<HuntzSliceDepth>();
            activeGates.Add(marker);
        }
    }

    void TryUseNearbyGate()
    {
        HuntzSliceGate nearest = FindNearestGate(out float distance);
        if (nearest == null || distance > 0.72f)
        {
            HuntzSliceHud.ShowMessage("Aproxime-se de uma passagem brilhante.");
            return;
        }
        EnterRoom(nearest.DestinationRoomId, false);
    }

    HuntzSliceGate FindNearestGate(out float nearestDistance)
    {
        HuntzSliceGate nearest = null;
        nearestDistance = float.MaxValue;
        if (player == null || activeGates == null) return null;

        for (int i = activeGates.Count - 1; i >= 0; i--)
        {
            HuntzSliceGate gate = activeGates[i];
            if (gate == null)
            {
                activeGates.RemoveAt(i);
                continue;
            }
            float distance = Vector2.Distance(player.transform.position, gate.transform.position);
            if (distance < nearestDistance)
            {
                nearest = gate;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    void EnterRoom(int roomId, bool firstRoom)
    {
        EnsureRuntimeData(false);
        HuntzSliceRoom destination = GetRoom(roomId);
        if (destination == null) return;

        if (currentRoomId >= 0)
        {
            HuntzSliceRoom previous = GetRoom(currentRoomId);
            if (previous != null) previous.Content.SetActive(false);
        }

        ClearGates();
        currentRoomId = roomId;
        destination.Content.SetActive(true);

        if (!TryGetRoomState(roomId, out RoomState state)) return;
        bool isFirstVisit = !state.visited;
        state.visited = true;
        if (isFirstVisit) roomsVisited++;
        if (player != null)
        {
            player.MoveToRoom(Vector2.zero);
            if (!firstRoom && isFirstVisit && destination.FirstVisitHealing > 0)
                player.Heal(destination.FirstVisitHealing);
        }

        if (firstRoom)
            HuntzSliceHud.ShowMessage("Derrote os Ash Slimes para abrir as passagens.");
        else if (isFirstVisit)
            HuntzSliceHud.ShowMessage(destination.Title);
        else
            HuntzSliceHud.ShowMessage("Você voltou a uma sala já visitada.");

        if (state.cleared && !destination.IsFinalRoom)
            CreateGates(destination);
    }

    void ClearGates()
    {
        EnsureRuntimeCollections();
        activeGates.Clear();
        if (gateRoot == null) return;
        for (int i = gateRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = gateRoot.GetChild(i);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    HuntzSliceRoom GetRoom(int roomId)
    {
        if (rooms == null) return null;
        foreach (HuntzSliceRoom room in rooms)
            if (room != null && room.Id == roomId) return room;
        return null;
    }

    HuntzSliceBoss GetCurrentBoss()
    {
        HuntzSliceRoom room = GetRoom(currentRoomId);
        return room != null ? room.GetComponentInChildren<HuntzSliceBoss>() : null;
    }

    void EnsureRuntimeData(bool deactivateRoomContent)
    {
        EnsureRuntimeCollections();
        if (roomLibrary == null) return;
        if (rooms == null || rooms.Length == 0)
            rooms = roomLibrary.GetComponentsInChildren<HuntzSliceRoom>(true);

        foreach (HuntzSliceRoom room in rooms)
        {
            if (room == null) continue;
            if (!states.ContainsKey(room.Id)) states.Add(room.Id, new RoomState());
            if (deactivateRoomContent && room.Content.activeSelf) room.Content.SetActive(false);
        }
    }

    void EnsureRuntimeCollections()
    {
        if (states == null) states = new Dictionary<int, RoomState>();
        if (activeGates == null) activeGates = new List<HuntzSliceGate>();
        if (rooms == null) rooms = new HuntzSliceRoom[0];
    }

    bool TryGetRoomState(int roomId, out RoomState state)
    {
        EnsureRuntimeData(false);
        if (states.TryGetValue(roomId, out state)) return true;
        if (GetRoom(roomId) == null)
        {
            state = null;
            return false;
        }
        state = new RoomState();
        states.Add(roomId, state);
        return true;
    }
}
