using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

[Serializable]
public struct HuntzHumanoidPartLook
{
    [SerializeField] HuntzHumanoidPartSlot slot;
    [SerializeField] string spriteLabel;
    [SerializeField] Color tint;

    public HuntzHumanoidPartSlot Slot => slot;
    public string SpriteLabel => spriteLabel;
    public Color Tint => tint;

    public HuntzHumanoidPartLook(HuntzHumanoidPartSlot partSlot, string label, Color color)
    {
        slot = partSlot;
        spriteLabel = label;
        tint = color;
    }
}

[CreateAssetMenu(menuName = "Huntz/Characters/Humanoid Appearance")]
public sealed class HuntzHumanoidAppearanceProfile : ScriptableObject
{
    [SerializeField] string displayName = "Humanoid Base";
    [SerializeField] SpriteLibraryAsset spriteLibrary;
    [SerializeField] HuntzHumanoidPartLook[] parts = Array.Empty<HuntzHumanoidPartLook>();

    public string DisplayName => displayName;
    public SpriteLibraryAsset SpriteLibrary => spriteLibrary;
    public HuntzHumanoidPartLook[] Parts => parts;

    public bool TryGetPart(HuntzHumanoidPartSlot slot, out HuntzHumanoidPartLook look)
    {
        if (parts != null)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Slot != slot) continue;
                look = parts[i];
                return true;
            }
        }

        look = default;
        return false;
    }

    public void Configure(string profileName, SpriteLibraryAsset library, HuntzHumanoidPartLook[] partLooks)
    {
        displayName = profileName;
        spriteLibrary = library;
        parts = partLooks ?? Array.Empty<HuntzHumanoidPartLook>();
    }
}
