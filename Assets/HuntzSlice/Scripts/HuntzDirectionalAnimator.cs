using UnityEngine;

/// <summary>Plays eight-way SpriteRenderer clips by action and facing direction.</summary>
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

    [SerializeField] Animation animationPlayer;
    [SerializeField] string characterId = "Hero";
    [SerializeField, Min(0f)] float crossFadeDuration = 0.04f;

    string currentClip;

    public void Configure(Animation target, string character)
    {
        animationPlayer = target;
        characterId = character;
        currentClip = null;
    }

    public bool PlayAction(string action, Vector2 facing)
    {
        if (animationPlayer == null || string.IsNullOrEmpty(action)) return false;
        int directionIndex = GetDirectionIndex(facing);
        string clipName = $"{characterId}_{action}_{DirectionNames[directionIndex]}";
        AnimationState state = animationPlayer[clipName];
        if (state == null) return false;

        state.wrapMode = action == "Idle" || action == "Walk" ? WrapMode.Loop : WrapMode.Once;
        if (currentClip == clipName)
        {
            if (animationPlayer.IsPlaying(clipName)) return true;
            if (state.wrapMode == WrapMode.Once) return true;
        }

        if (animationPlayer.isPlaying) animationPlayer.CrossFade(clipName, crossFadeDuration);
        else animationPlayer.Play(clipName);
        currentClip = clipName;
        return true;
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
