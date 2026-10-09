using UnityEngine;
using UnityEngine.U2D.Animation;

[DisallowMultipleComponent]
public sealed class HuntzHumanoidAppearance : MonoBehaviour
{
    [SerializeField] SpriteLibrary spriteLibrary;
    [SerializeField] HuntzHumanoidAppearanceProfile initialProfile;

    public HuntzHumanoidAppearanceProfile CurrentProfile { get; private set; }

    public void Configure(SpriteLibrary library, HuntzHumanoidAppearanceProfile profile)
    {
        spriteLibrary = library;
        initialProfile = profile;
        if (profile != null) Apply(profile);
    }

    void Awake()
    {
        if (spriteLibrary == null) spriteLibrary = GetComponent<SpriteLibrary>();
        if (initialProfile != null) Apply(initialProfile);
    }

    public bool Apply(HuntzHumanoidAppearanceProfile profile)
    {
        if (profile == null) return false;
        if (spriteLibrary == null) spriteLibrary = GetComponent<SpriteLibrary>();
        if (spriteLibrary != null && profile.SpriteLibrary != null)
            spriteLibrary.spriteLibraryAsset = profile.SpriteLibrary;

        HuntzHumanoidRigPart[] parts = GetComponentsInChildren<HuntzHumanoidRigPart>(true);
        for (int i = 0; i < parts.Length; i++)
        {
            if (profile.TryGetPart(parts[i].Slot, out HuntzHumanoidPartLook look))
                parts[i].Apply(look.SpriteLabel, look.Tint, spriteLibrary);
        }

        CurrentProfile = profile;
        return true;
    }
}
