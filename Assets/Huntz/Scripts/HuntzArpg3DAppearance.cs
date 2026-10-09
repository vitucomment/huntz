using UnityEngine;

[DisallowMultipleComponent]
public sealed class HuntzArpg3DAppearance : MonoBehaviour
{
    [SerializeField] HuntzArpg3DAppearanceProfile[] profiles;
    [SerializeField] int activeProfileIndex;
    HuntzArpg3DRigPart[] parts;

    public string ActiveProfileName
    {
        get
        {
            if (profiles == null || profiles.Length == 0) return "Base";
            int safeIndex = Mathf.Clamp(activeProfileIndex, 0, profiles.Length - 1);
            return profiles[safeIndex] != null ? profiles[safeIndex].name : "Base";
        }
    }

    void Awake()
    {
        parts = GetComponentsInChildren<HuntzArpg3DRigPart>(true);
        Apply(activeProfileIndex);
    }

    public void Configure(HuntzArpg3DAppearanceProfile[] appearanceProfiles)
    {
        profiles = appearanceProfiles;
        activeProfileIndex = 0;
        parts = GetComponentsInChildren<HuntzArpg3DRigPart>(true);
        Apply(activeProfileIndex);
    }

    public void CycleProfile()
    {
        if (profiles == null || profiles.Length == 0) return;
        Apply((activeProfileIndex + 1) % profiles.Length);
    }

    public void Apply(int index)
    {
        if (profiles == null || profiles.Length == 0) return;
        activeProfileIndex = Mathf.Clamp(index, 0, profiles.Length - 1);
        HuntzArpg3DAppearanceProfile profile = profiles[activeProfileIndex];
        if (profile == null) return;
        if (parts == null) parts = GetComponentsInChildren<HuntzArpg3DRigPart>(true);

        foreach (HuntzArpg3DRigPart part in parts)
            if (part != null) part.ApplyColor(profile.GetColor(part.Slot));
    }
}
