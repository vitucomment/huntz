using UnityEngine;

[CreateAssetMenu(menuName = "Huntz/Combat/Weapon Definition")]
public sealed class HuntzWeaponDefinition : ScriptableObject
{
    [SerializeField] string displayName = "Espada de treino";
    [SerializeField] Sprite sprite;
    [SerializeField, Min(0)] int damageBonus;
    [SerializeField, Min(0f)] float reachBonus;
    [SerializeField, Min(0f)] float widthBonus;

    public string DisplayName => displayName;
    public Sprite Sprite => sprite;
    public int DamageBonus => damageBonus;
    public float ReachBonus => reachBonus;
    public float WidthBonus => widthBonus;

    public void Configure(string itemName, Sprite itemSprite, int itemDamageBonus = 0,
        float itemReachBonus = 0f, float itemWidthBonus = 0f)
    {
        displayName = itemName;
        sprite = itemSprite;
        damageBonus = Mathf.Max(0, itemDamageBonus);
        reachBonus = Mathf.Max(0f, itemReachBonus);
        widthBonus = Mathf.Max(0f, itemWidthBonus);
    }
}
