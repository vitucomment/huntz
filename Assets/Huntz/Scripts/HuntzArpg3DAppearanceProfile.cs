using UnityEngine;

[CreateAssetMenu(menuName = "Huntz/3D Humanoid Appearance", fileName = "HumanoidAppearance")]
public sealed class HuntzArpg3DAppearanceProfile : ScriptableObject
{
    public Color skin = new Color(0.86f, 0.62f, 0.42f);
    public Color cloth = new Color(0.16f, 0.42f, 0.68f);
    public Color leather = new Color(0.24f, 0.13f, 0.08f);
    public Color metal = new Color(0.62f, 0.72f, 0.78f);
    public Color hair = new Color(0.12f, 0.08f, 0.06f);
    public Color accent = new Color(0.9f, 0.56f, 0.12f);

    public Color GetColor(HuntzArpg3DPartSlot slot)
    {
        switch (slot)
        {
            case HuntzArpg3DPartSlot.Skin: return skin;
            case HuntzArpg3DPartSlot.Cloth: return cloth;
            case HuntzArpg3DPartSlot.Leather: return leather;
            case HuntzArpg3DPartSlot.Metal: return metal;
            case HuntzArpg3DPartSlot.Hair: return hair;
            case HuntzArpg3DPartSlot.Accent: return accent;
            default: return Color.white;
        }
    }
}
