using System;
using UnityEngine;

public enum HuntzArpg3DAction
{
    Idle,
    Run,
    Attack,
    Dodge,
    Hurt,
    Death
}

[DisallowMultipleComponent]
public sealed class HuntzArpg3DRig : MonoBehaviour
{
    static readonly int MoveSpeedId = Animator.StringToHash("MoveSpeed");
    static readonly int GaitRateId = Animator.StringToHash("GaitRate");
    static readonly int AttackId = Animator.StringToHash("Attack");
    static readonly int DodgeId = Animator.StringToHash("Dodge");
    static readonly int HurtId = Animator.StringToHash("Hurt");
    static readonly int DeathId = Animator.StringToHash("Death");

    [SerializeField] Animator animator;
    [SerializeField] Avatar retargetAvatar;
    [SerializeField] Transform skeletonRoot;
    [SerializeField] Transform hips;
    [SerializeField] Transform spine;
    [SerializeField] Transform chest;
    [SerializeField] Transform head;
    [SerializeField] Transform leftHand;
    [SerializeField] Transform rightHand;
    [SerializeField] Transform leftFoot;
    [SerializeField] Transform rightFoot;
    [SerializeField] Transform leftToeBase;
    [SerializeField] Transform rightToeBase;
    [SerializeField] Transform weaponSocket;
    [SerializeField] HuntzArpg3DWeaponGeometry weaponGeometry;
    [SerializeField, Min(0.1f)] float attackDuration = 0.62f;
    [SerializeField, Min(0.1f)] float dodgeDuration = 0.32f;
    [SerializeField, Min(0.1f)] float hurtDuration = 0.28f;
    bool attackImpactArmed;

    public event Action WeaponImpact;
    public Animator Animator => animator;
    public Avatar RetargetAvatar => retargetAvatar;
    public Transform SkeletonRoot => skeletonRoot;
    public Transform Hips => hips;
    public Transform Spine => spine;
    public Transform Chest => chest;
    public Transform Head => head;
    public Transform LeftHand => leftHand;
    public Transform RightHand => rightHand;
    public Transform LeftFoot => leftFoot;
    public Transform RightFoot => rightFoot;
    public Transform LeftToeBase => leftToeBase;
    public Transform RightToeBase => rightToeBase;
    public Transform WeaponSocket => weaponSocket;
    public HuntzArpg3DWeaponGeometry WeaponGeometry => weaponGeometry;
    public float AttackDuration => attackDuration;
    public float DodgeDuration => dodgeDuration;
    public float HurtDuration => hurtDuration;

    public void Configure(Animator targetAnimator, Transform socket, Avatar humanoidRetargetAvatar,
        float attackSeconds, float dodgeSeconds, float hurtSeconds)
    {
        animator = targetAnimator;
        retargetAvatar = humanoidRetargetAvatar;
        weaponSocket = socket;
        attackDuration = Mathf.Max(0.1f, attackSeconds);
        dodgeDuration = Mathf.Max(0.1f, dodgeSeconds);
        hurtDuration = Mathf.Max(0.1f, hurtSeconds);
        ResolveBoneReferences();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        ResolveBoneReferences();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        if (!ValidateRig(out string issue))
            Debug.LogError("Huntz humanoid rig is incomplete: " + issue, this);
    }

    public bool ValidateRig(out string issue)
    {
        ResolveBoneReferences();
        if (animator == null)
        {
            issue = "Animator reference is missing.";
            return false;
        }

        if (animator.avatar == null || !animator.avatar.isValid)
        {
            issue = "Animator Avatar is missing or invalid.";
            return false;
        }

        if (animator.avatar.isHuman)
        {
            issue = "Transform-curve locomotion currently requires the Animator's Generic Avatar.";
            return false;
        }

        if (retargetAvatar == null || !retargetAvatar.isValid || !retargetAvatar.isHuman)
        {
            issue = "A valid Humanoid Avatar mapping for retargeting is missing.";
            return false;
        }

        if (animator.runtimeAnimatorController == null)
        {
            issue = "Animator Controller is missing.";
            return false;
        }

        if (skeletonRoot == null || hips == null || spine == null || chest == null || head == null ||
            leftHand == null || rightHand == null || leftFoot == null || rightFoot == null ||
            leftToeBase == null || rightToeBase == null)
        {
            issue = "A required humanoid bone could not be resolved.";
            return false;
        }

        if (weaponSocket == null || !weaponSocket.IsChildOf(rightHand))
        {
            issue = "WeaponSocket must be parented under RightHand.";
            return false;
        }

        if (weaponGeometry == null || !weaponGeometry.transform.IsChildOf(weaponSocket) || !weaponGeometry.IsConfigured)
        {
            issue = "A configured weapon hit volume must be parented under WeaponSocket.";
            return false;
        }

        issue = null;
        return true;
    }

    void ResolveBoneReferences()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (skeletonRoot == null) skeletonRoot = transform.Find("Armature");
        if (hips == null) hips = transform.Find("Armature/Hips");
        if (spine == null) spine = transform.Find("Armature/Hips/Spine");
        if (chest == null) chest = transform.Find("Armature/Hips/Spine/Chest");
        if (head == null) head = transform.Find("Armature/Hips/Spine/Chest/Neck/Head");
        if (leftHand == null) leftHand = transform.Find("Armature/Hips/Spine/Chest/LeftShoulder/LeftUpperArm/LeftLowerArm/LeftHand");
        if (rightHand == null) rightHand = transform.Find("Armature/Hips/Spine/Chest/RightShoulder/RightUpperArm/RightLowerArm/RightHand");
        if (leftFoot == null) leftFoot = transform.Find("Armature/Hips/LeftUpperLeg/LeftLowerLeg/LeftFoot");
        if (rightFoot == null) rightFoot = transform.Find("Armature/Hips/RightUpperLeg/RightLowerLeg/RightFoot");
        if (leftToeBase == null) leftToeBase = transform.Find("Armature/Hips/LeftUpperLeg/LeftLowerLeg/LeftFoot/LeftToeBase");
        if (rightToeBase == null) rightToeBase = transform.Find("Armature/Hips/RightUpperLeg/RightLowerLeg/RightFoot/RightToeBase");
        if (weaponGeometry == null && weaponSocket != null)
            weaponGeometry = weaponSocket.GetComponentInChildren<HuntzArpg3DWeaponGeometry>(true);
    }

    public bool TryGetWeaponHitCapsule(out Vector3 start, out Vector3 end, out float radius)
    {
        if (weaponGeometry != null) return weaponGeometry.TryGetHitCapsule(out start, out end, out radius);
        start = Vector3.zero;
        end = Vector3.zero;
        radius = 0f;
        return false;
    }

    public void SetMoveSpeed(float speed, float gaitRate)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetFloat(MoveSpeedId, speed, 0.08f, Time.deltaTime);
            animator.SetFloat(GaitRateId, gaitRate, 0.05f, Time.deltaTime);
        }
    }

    public void PlayAttack()
    {
        attackImpactArmed = true;
        TriggerOnly(AttackId);
    }

    public void PlayDodge()
    {
        attackImpactArmed = false;
        TriggerOnly(DodgeId);
    }

    public void PlayHurt()
    {
        attackImpactArmed = false;
        TriggerOnly(HurtId);
    }

    public void PlayDeath()
    {
        attackImpactArmed = false;
        TriggerOnly(DeathId);
    }

    public void ResetRig()
    {
        attackImpactArmed = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.Rebind();
        animator.Update(0f);
    }

    public void OnWeaponImpact()
    {
        if (!attackImpactArmed) return;
        attackImpactArmed = false;
        WeaponImpact?.Invoke();
    }

    void TriggerOnly(int triggerId)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.ResetTrigger(AttackId);
        animator.ResetTrigger(DodgeId);
        animator.ResetTrigger(HurtId);
        animator.ResetTrigger(DeathId);
        animator.SetTrigger(triggerId);
    }
}
