using UnityEngine;

[DisallowMultipleComponent]
public sealed class HuntzEquippableWeapon : MonoBehaviour
{
    static readonly Vector2[] HandAnchors =
    {
        new Vector2(0.19f, 0.35f),
        new Vector2(0.22f, 0.39f),
        new Vector2(-0.22f, 0.42f),
        new Vector2(-0.17f, 0.61f),
        new Vector2(-0.13f, 0.62f),
        new Vector2(0.15f, 0.62f),
        new Vector2(0.22f, 0.47f),
        new Vector2(0.18f, 0.35f)
    };

    const string StarterWeaponResource = "Huntz/Weapons/StarterSword";

    [SerializeField] SpriteRenderer characterRenderer;
    [SerializeField] HuntzWeaponDefinition equippedWeapon;
    [SerializeField] Transform socketRoot;

    Transform visualRoot;
    HuntzDirectionalAnimator directionalAnimator;
    Transform weaponTransform;
    SpriteRenderer weaponRenderer;
    Sprite equippedSprite;
    int directDamageBonus;
    float directReachBonus;
    float directWidthBonus;
    float impactPoseUntil;
    bool socketDriven;

    public HuntzWeaponDefinition EquippedWeapon => equippedWeapon;
    public Sprite EquippedSprite => equippedSprite;
    public int DamageBonus => equippedWeapon != null ? equippedWeapon.DamageBonus : directDamageBonus;
    public float ReachBonus => equippedWeapon != null ? equippedWeapon.ReachBonus : directReachBonus;
    public float WidthBonus => equippedWeapon != null ? equippedWeapon.WidthBonus : directWidthBonus;

    public void Initialize(SpriteRenderer bodyRenderer, Transform characterVisualRoot,
        HuntzDirectionalAnimator characterAnimator = null)
    {
        socketRoot = null;
        socketDriven = false;
        characterRenderer = bodyRenderer != null ? bodyRenderer : characterRenderer;
        visualRoot = characterVisualRoot != null ? characterVisualRoot : visualRoot;
        directionalAnimator = characterAnimator != null ? characterAnimator : directionalAnimator;
        EnsureRenderer();

        if (equippedWeapon != null)
        {
            Equip(equippedWeapon);
            return;
        }

        HuntzWeaponDefinition starterDefinition = Resources.Load<HuntzWeaponDefinition>(StarterWeaponResource);
        if (starterDefinition != null)
        {
            Equip(starterDefinition);
            return;
        }

        Sprite starterSprite = Resources.Load<Sprite>(StarterWeaponResource);
        if (starterSprite != null) Equip(starterSprite);
    }

    public void InitializeAtSocket(Transform weaponSocket)
    {
        if (weaponSocket == null) return;
        socketRoot = weaponSocket;
        socketDriven = true;
        visualRoot = weaponSocket;
        HuntzHumanoidRig humanoidRig = weaponSocket.GetComponentInParent<HuntzHumanoidRig>();
        if (characterRenderer == null && humanoidRig != null)
            characterRenderer = humanoidRig.GetComponentInChildren<SpriteRenderer>(true);
        EnsureRenderer();

        if (equippedWeapon != null)
        {
            Equip(equippedWeapon);
            return;
        }

        HuntzWeaponDefinition starterDefinition = Resources.Load<HuntzWeaponDefinition>(StarterWeaponResource);
        if (starterDefinition != null)
        {
            Equip(starterDefinition);
            return;
        }

        Sprite starterSprite = Resources.Load<Sprite>(StarterWeaponResource);
        if (starterSprite != null) Equip(starterSprite);
    }

    public bool Equip(HuntzWeaponDefinition definition)
    {
        if (definition == null || definition.Sprite == null) return false;
        equippedWeapon = definition;
        directDamageBonus = 0;
        directReachBonus = 0f;
        directWidthBonus = 0f;
        SetSprite(definition.Sprite);
        return true;
    }

    public bool Equip(Sprite sprite, int bonusDamage = 0, float bonusReach = 0f, float bonusWidth = 0f)
    {
        if (sprite == null) return false;
        equippedWeapon = null;
        directDamageBonus = Mathf.Max(0, bonusDamage);
        directReachBonus = Mathf.Max(0f, bonusReach);
        directWidthBonus = Mathf.Max(0f, bonusWidth);
        SetSprite(sprite);
        return true;
    }

    public void Unequip()
    {
        equippedWeapon = null;
        equippedSprite = null;
        if (weaponRenderer != null) weaponRenderer.enabled = false;
    }

    public void SetPose(string action, Vector2 facing, float actionProgress)
    {
        EnsureRenderer();
        if (weaponRenderer == null || equippedSprite == null) return;

        int directionIndex = HuntzDirectionalAnimator.GetDirectionIndex(facing);
        bool attacking = action == "Attack";

        bool attackUsesWeaponLayer = directionalAnimator == null ||
            directionalAnimator.CurrentActionUsesEquippedWeapon;
        weaponRenderer.enabled = !attacking || attackUsesWeaponLayer;
        if (!weaponRenderer.enabled) return;

        if (socketDriven && socketRoot != null)
        {
            weaponTransform.localPosition = Vector3.zero;
            weaponTransform.localRotation = Quaternion.identity;
            weaponRenderer.sortingLayerID = characterRenderer != null ? characterRenderer.sortingLayerID : 0;
            int socketOrder = characterRenderer != null ? characterRenderer.sortingOrder : 0;
            weaponRenderer.sortingOrder = socketOrder + (directionIndex >= 3 && directionIndex <= 5 ? -1 : 1);
            return;
        }

        Vector2 facingDirection = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector2.right;
        float facingAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
        Vector2 anchor = CharacterRendererOffset() + HandAnchors[directionIndex];
        Vector2 backward = -facingDirection;
        Vector2 forward = facingDirection;
        float angleOffset = 45f;

        if (attacking)
        {
            float progress = Mathf.Clamp01(actionProgress);
            if (Time.time < impactPoseUntil)
            {
                anchor += forward * 0.2f + Vector2.up * 0.04f;
                angleOffset = -45f;
            }
            else if (progress < 0.34f)
            {
                float phase = progress / 0.34f;
                anchor += Vector2.Lerp(Vector2.zero, backward * 0.1f + Vector2.up * 0.14f, phase);
                angleOffset = Mathf.LerpAngle(45f, 125f, phase);
            }
            else if (progress < 0.5f)
            {
                float phase = (progress - 0.34f) / 0.16f;
                anchor += Vector2.Lerp(backward * 0.1f + Vector2.up * 0.14f,
                    forward * 0.2f + Vector2.up * 0.04f, phase);
                angleOffset = Mathf.LerpAngle(125f, -45f, phase);
            }
            else
            {
                float phase = (progress - 0.5f) / 0.5f;
                anchor += Vector2.Lerp(forward * 0.2f + Vector2.up * 0.04f, Vector2.zero, phase);
                angleOffset = Mathf.LerpAngle(-45f, 45f, phase);
            }
        }

        weaponTransform.localPosition = anchor;
        weaponTransform.localRotation = Quaternion.Euler(0f, 0f, facingAngle + angleOffset);
        weaponRenderer.sortingLayerID = characterRenderer != null ? characterRenderer.sortingLayerID : 0;
        int behindCharacter = directionIndex >= 3 && directionIndex <= 5 ? -1 : 1;
        weaponRenderer.sortingOrder = characterRenderer != null
            ? characterRenderer.sortingOrder + behindCharacter
            : behindCharacter;
    }

    public void SetImpactPose(Vector2 facing, float holdDuration)
    {
        impactPoseUntil = Time.time + Mathf.Max(0f, holdDuration);
        SetPose("Attack", facing, 0.4f);
    }

    void SetSprite(Sprite sprite)
    {
        EnsureRenderer();
        equippedSprite = sprite;
        if (weaponRenderer == null) return;
        weaponRenderer.sprite = sprite;
        weaponRenderer.enabled = sprite != null;
    }

    void EnsureRenderer()
    {
        if (socketDriven && socketRoot != null)
            visualRoot = socketRoot;
        else if (visualRoot == null)
        {
            Transform visual = transform.Find("Visual");
            visualRoot = visual != null ? visual : transform;
        }

        if (!socketDriven && characterRenderer == null && visualRoot != null)
            characterRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>(true);

        if (weaponRenderer == null)
        {
            Transform existing = visualRoot.Find("Equipped Weapon");
            if (existing != null)
            {
                weaponTransform = existing;
                weaponRenderer = existing.GetComponent<SpriteRenderer>();
            }
            else
            {
                GameObject weaponObject = new GameObject("Equipped Weapon");
                weaponTransform = weaponObject.transform;
                weaponTransform.SetParent(visualRoot, false);
                weaponRenderer = weaponObject.AddComponent<SpriteRenderer>();
                weaponRenderer.maskInteraction = SpriteMaskInteraction.None;
                weaponRenderer.sortingLayerID = characterRenderer != null ? characterRenderer.sortingLayerID : 0;
                weaponRenderer.enabled = false;
            }
        }

        if (weaponTransform != null && weaponTransform.parent == visualRoot)
        {
            if (socketDriven)
            {
                weaponTransform.localPosition = Vector3.zero;
                weaponTransform.localRotation = Quaternion.identity;
            }
            else if (characterRenderer != null)
            {
                weaponTransform.localPosition = characterRenderer.transform.localPosition;
            }
        }
    }

    Vector2 CharacterRendererOffset()
    {
        return characterRenderer != null
            ? (Vector2)characterRenderer.transform.localPosition
            : Vector2.zero;
    }
}
