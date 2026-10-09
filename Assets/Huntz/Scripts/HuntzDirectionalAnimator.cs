using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Plays sprite actions with eight directions or a single direction-independent sequence.</summary>
[DisallowMultipleComponent]
public sealed class HuntzDirectionalAnimator : MonoBehaviour
{
    static readonly string[] DirectionNames =
    {
        "South", "SouthWest", "West", "NorthWest",
        "North", "NorthEast", "East", "SouthEast"
    };

    static readonly Vector2[] DirectionVectors =
    {
        new Vector2(0f, -1f), new Vector2(-1f, -1f),
        new Vector2(-1f, 0f), new Vector2(-1f, 1f),
        new Vector2(0f, 1f), new Vector2(1f, 1f),
        new Vector2(1f, 0f), new Vector2(1f, -1f)
    };

    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] string characterId = "Hero";
    [SerializeField] HuntzDirectionalSpriteSet spriteSet;
    [SerializeField] string initialAction;

    readonly HashSet<string> reportedIssues = new HashSet<string>();
    HuntzDirectionalSpriteClip currentAnimation;
    string currentClip;
    string appliedClip;
    float elapsed;
    bool referencesResolved;
    string currentAction;
    float playbackSpeed = 1f;

    public string CharacterId => characterId;
    public HuntzDirectionalSpriteSet SpriteSet => spriteSet;
    public SpriteRenderer TargetRenderer => spriteRenderer;
    public string CurrentClipName => currentClip;
    public string CurrentAction => currentAction;
    public bool CurrentActionUsesEquippedWeapon => currentAnimation != null && currentAnimation.usesEquippedWeaponOverlay;
    public int CurrentFrameIndex { get; private set; }
    public float PlaybackSpeed { get => playbackSpeed; set => playbackSpeed = Mathf.Max(0f, value); }
    public event Action<string, int, int> FrameChanged;

    void Awake() => ResolveReferences();

    void OnEnable()
    {
        ResolveReferences();
        currentAnimation = null;
        currentClip = null;
        appliedClip = null;
        currentAction = null;
        elapsed = 0f;
        playbackSpeed = 1f;
        if (!string.IsNullOrEmpty(initialAction)) PlayAction(initialAction, Vector2.down, true);
    }

    public void Configure(SpriteRenderer target, string character, HuntzDirectionalSpriteSet frames, string autoPlayAction = null)
    {
        spriteRenderer = target;
        characterId = character;
        spriteSet = frames;
        if (autoPlayAction != null) initialAction = autoPlayAction;
        currentAnimation = null;
        currentClip = null;
        appliedClip = null;
        currentAction = null;
        elapsed = 0f;
        referencesResolved = false;
        ResolveReferences();
    }

    public bool PlayAction(string action, Vector2 facing, bool restart = false)
    {
        if (string.IsNullOrEmpty(action)) return false;
        ResolveReferences();
        if (spriteRenderer == null)
        {
            ReportIssue("No SpriteRenderer is assigned or found under the character.");
            return false;
        }
        if (spriteSet == null)
        {
            ReportIssue($"Directional sprite set for '{characterId}' could not be loaded.");
            return false;
        }

        int directionIndex = GetDirectionIndex(facing);
        string direction = DirectionNames[directionIndex];
        HuntzDirectionalSpriteClip clip = spriteSet.Find(action, direction);
        string clipName = $"{characterId}_{action}" + (clip != null && clip.direction == "None" ? "" : "_" + direction);
        if (clip == null)
        {
            ReportIssue($"Directional sprite clip '{clipName}' is missing from '{spriteSet.name}'.");
            return false;
        }
        if (clip.frames == null || clip.frames.Length == 0)
        {
            ReportIssue($"Directional sprite clip '{clipName}' has no frames.");
            return false;
        }

        if (!restart && currentClip == clipName) return true;

        // Preserve the gait phase when steering between directions of the same action.
        float phase = !restart && currentAction == action && currentAnimation != null
            ? elapsed / Mathf.Max(0.001f, currentAnimation.Duration) : 0f;
        currentClip = clipName;
        currentAction = action;
        currentAnimation = clip;
        elapsed = phase * clip.Duration;
        ApplyFrame(Mathf.FloorToInt(elapsed * clip.framesPerSecond));
        return true;
    }

    public float GetActionDuration(string action, Vector2 facing, float fallback)
    {
        ResolveReferences();
        HuntzDirectionalSpriteClip clip = spriteSet != null
            ? spriteSet.Find(action, DirectionNames[GetDirectionIndex(facing)]) : null;
        return clip != null && clip.Duration > 0f ? clip.Duration : fallback;
    }

    public int GetActionFrameCount(string action, Vector2 facing, int fallback)
    {
        ResolveReferences();
        HuntzDirectionalSpriteClip clip = spriteSet != null
            ? spriteSet.Find(action, DirectionNames[GetDirectionIndex(facing)]) : null;
        return clip != null && clip.frames != null && clip.frames.Length > 0 ? clip.frames.Length : fallback;
    }

    public float GetActionFramesPerSecond(string action, Vector2 facing, float fallback)
    {
        ResolveReferences();
        HuntzDirectionalSpriteClip clip = spriteSet != null
            ? spriteSet.Find(action, DirectionNames[GetDirectionIndex(facing)]) : null;
        return clip != null && clip.framesPerSecond > 0f ? clip.framesPerSecond : fallback;
    }

    public bool PlayClip(string clipName, bool restart = false)
    {
        string prefix = characterId + "_";
        string action = clipName.StartsWith(prefix, System.StringComparison.Ordinal)
            ? clipName.Substring(prefix.Length) : clipName;
        return PlayAction(action, Vector2.down, restart);
    }

    // Gameplay selects an action in Update; presentation advances after that decision.
    void LateUpdate()
    {
        if (currentAnimation == null || currentAnimation.frames == null || currentAnimation.frames.Length == 0)
            return;

        float framesPerSecond = Mathf.Max(1f, currentAnimation.framesPerSecond);
        float duration = currentAnimation.frames.Length / framesPerSecond;
        elapsed += Time.deltaTime * playbackSpeed;
        if (currentAnimation.loop && duration > 0f) elapsed %= duration;
        else elapsed = Mathf.Min(elapsed, Mathf.Max(0f, duration - 0.0001f));

        int frameIndex = Mathf.Min(Mathf.FloorToInt(elapsed * framesPerSecond), currentAnimation.frames.Length - 1);
        ApplyFrame(frameIndex);
    }

    void ResolveReferences()
    {
        if (referencesResolved) return;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Transform visual = transform.Find("Visual");
            if (spriteRenderer == null && visual != null) spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);
        }

        if (spriteRenderer != null)
        {
            Animation legacyAnimation = spriteRenderer.GetComponent<Animation>();
            if (legacyAnimation != null) legacyAnimation.enabled = false;
        }

        if (spriteSet == null && !string.IsNullOrEmpty(characterId))
            spriteSet = Resources.Load<HuntzDirectionalSpriteSet>($"Huntz/Animations/{characterId}");

        referencesResolved = spriteRenderer != null && spriteSet != null;
    }

    void ApplyFrame(int frameIndex)
    {
        if (spriteRenderer == null || currentAnimation == null || currentAnimation.frames == null) return;
        string previousClip = appliedClip;
        int previousFrame = CurrentFrameIndex;
        CurrentFrameIndex = Mathf.Clamp(frameIndex, 0, currentAnimation.frames.Length - 1);
        Sprite frame = currentAnimation.frames[CurrentFrameIndex];
        if (frame != null) spriteRenderer.sprite = frame;
        if (previousClip != currentClip || previousFrame != CurrentFrameIndex)
        {
            int previousFrameInClip = previousClip == currentClip ? previousFrame : -1;
            appliedClip = currentClip;
            FrameChanged?.Invoke(currentAction, previousFrameInClip, CurrentFrameIndex);
        }
    }

    void ReportIssue(string message)
    {
        if (reportedIssues.Add(message)) Debug.LogWarning($"[Huntz] {message}", this);
    }

    public static int GetDirectionIndex(Vector2 facing)
    {
        if (facing.sqrMagnitude < 0.001f) return 0;
        facing.Normalize();

        int bestIndex = 0;
        float bestDot = float.NegativeInfinity;
        for (int i = 0; i < DirectionVectors.Length; i++)
        {
            float dot = Vector2.Dot(facing, DirectionVectors[i].normalized);
            if (dot <= bestDot) continue;
            bestDot = dot;
            bestIndex = i;
        }

        return bestIndex;
    }
}
