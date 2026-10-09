using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class HuntzDirectionalSpriteClip
{
    public string action;
    public string direction;
    public Sprite[] frames = new Sprite[0];
    [Min(1f)] public float framesPerSecond = 10f;
    public bool loop;
    public bool usesEquippedWeaponOverlay;
    public float Duration => frames != null ? frames.Length / Mathf.Max(1f, framesPerSecond) : 0f;
}

[CreateAssetMenu(menuName = "Huntz/Directional Sprite Set")]
public sealed class HuntzDirectionalSpriteSet : ScriptableObject
{
    [SerializeField] HuntzDirectionalSpriteClip[] clips = new HuntzDirectionalSpriteClip[0];

    Dictionary<string, HuntzDirectionalSpriteClip> clipsByKey;
    public IReadOnlyList<HuntzDirectionalSpriteClip> Clips => clips;

    public HuntzDirectionalSpriteClip Find(string action, string direction)
    {
        if (clipsByKey == null) RebuildLookup();
        if (!clipsByKey.TryGetValue(GetKey(action, direction), out HuntzDirectionalSpriteClip clip))
            clipsByKey.TryGetValue(GetKey(action, "None"), out clip);
        return clip;
    }

    public void SetClips(HuntzDirectionalSpriteClip[] value)
    {
        clips = value ?? new HuntzDirectionalSpriteClip[0];
        RebuildLookup();
    }

    void OnEnable() => RebuildLookup();
    void OnValidate() => RebuildLookup();

    void RebuildLookup()
    {
        clipsByKey = new Dictionary<string, HuntzDirectionalSpriteClip>();
        if (clips == null) return;

        foreach (HuntzDirectionalSpriteClip clip in clips)
        {
            if (clip == null || string.IsNullOrEmpty(clip.action) || string.IsNullOrEmpty(clip.direction)) continue;
            clipsByKey[GetKey(clip.action, clip.direction)] = clip;
        }
    }

    static string GetKey(string action, string direction) => action + "_" + direction;
}
