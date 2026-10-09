using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

public enum HuntzHumanoidRigAction
{
    Idle,
    Walk,
    Attack,
    Dodge,
    Hurt,
    Death
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class HuntzHumanoidRig : MonoBehaviour
{
    static readonly string[] DirectionNames =
    {
        "South", "SouthWest", "West", "NorthWest",
        "North", "NorthEast", "East", "SouthEast"
    };

    [SerializeField] Animator animator;
    [SerializeField] SpriteLibrary spriteLibrary;
    [SerializeField] Transform weaponSocket;
    [SerializeField, Min(0f)] float transitionSeconds = 0.08f;
    [SerializeField, Min(0f)] float impactPoseSeconds = 0.12f;

    HuntzEquippableWeapon weapon;
    Vector2 facing = Vector2.down;
    HuntzHumanoidRigAction currentAction = HuntzHumanoidRigAction.Idle;
    int currentDirectionIndex;
    bool hasPlayedState;

    public Animator Animator => animator;
    public SpriteLibrary SpriteLibrary => spriteLibrary;
    public Transform WeaponSocket => weaponSocket;
    public Vector2 Facing => facing;
    public HuntzHumanoidRigAction CurrentAction => currentAction;
    public string CurrentDirection => DirectionNames[currentDirectionIndex];
    public event Action WeaponImpact;

    void Awake()
    {
        ResolveReferences();
        weapon = GetComponent<HuntzEquippableWeapon>();
        if (weapon != null && weaponSocket != null)
            weapon.InitializeAtSocket(weaponSocket);
    }

    void OnEnable()
    {
        ResolveReferences();
        if (animator != null && !hasPlayedState)
            Play(HuntzHumanoidRigAction.Idle, Vector2.down, true);
    }

    public void Configure(Animator targetAnimator, SpriteLibrary targetLibrary, Transform socket)
    {
        animator = targetAnimator;
        spriteLibrary = targetLibrary;
        weaponSocket = socket;
        ResolveReferences();
    }

    public bool Play(HuntzHumanoidRigAction action, Vector2 direction, bool restart = false)
    {
        ResolveReferences();
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        int requestedDirection = GetDirectionIndex(direction);
        bool sameState = hasPlayedState && currentAction == action && currentDirectionIndex == requestedDirection;
        currentDirectionIndex = requestedDirection;
        facing = DirectionVectors[currentDirectionIndex];
        string stateName = "Base Layer." + action + "_" + DirectionNames[currentDirectionIndex];
        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            Debug.LogWarning($"[Huntz] Rig animation state '{stateName}' is missing on '{name}'.", this);
            return false;
        }

        if (sameState && !restart) return true;

        currentAction = action;
        hasPlayedState = true;
        if (!restart && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
            animator.CrossFadeInFixedTime(stateHash, transitionSeconds, 0, 0f);
        else
            animator.Play(stateHash, 0, 0f);

        ApplyDepthSorting();
        if (weapon != null) weapon.SetPose(action.ToString(), facing, 0f);
        return true;
    }

    public void AnimationEvent_WeaponImpact()
    {
        if (weapon != null) weapon.SetImpactPose(facing, impactPoseSeconds);
        WeaponImpact?.Invoke();
    }

    static readonly Vector2[] DirectionVectors =
    {
        new Vector2(0f, -1f), new Vector2(-1f, -1f),
        new Vector2(-1f, 0f), new Vector2(-1f, 1f),
        new Vector2(0f, 1f), new Vector2(1f, 1f),
        new Vector2(1f, 0f), new Vector2(1f, -1f)
    };

    static int GetDirectionIndex(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return 0;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        int index = Mathf.RoundToInt((-90f - angle) / 45f);
        index %= 8;
        if (index < 0) index += 8;
        return index;
    }

    void ApplyDepthSorting()
    {
        HuntzHumanoidRigPart[] parts = GetComponentsInChildren<HuntzHumanoidRigPart>(true);
        bool facingFront = currentDirectionIndex < 3 || currentDirectionIndex >= 6;
        for (int i = 0; i < parts.Length; i++)
        {
            SpriteRenderer renderer = parts[i].Renderer;
            if (renderer == null) continue;
            int bias = facingFront ? parts[i].FrontSortingBias : parts[i].BackSortingBias;
            renderer.sortingOrder = bias;
        }
    }

    void ResolveReferences()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteLibrary == null) spriteLibrary = GetComponent<SpriteLibrary>();
        if (weaponSocket == null)
        {
            Transform rig = transform.Find("Rig");
            if (rig != null) weaponSocket = rig.Find("Pelvis/Torso/RightShoulder/UpperArm/Forearm/Hand/WeaponSocket");
        }
    }
}
