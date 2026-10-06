using UnityEngine;

public class IsoChest : IsoInteractable
{
    [SerializeField] int potions = 2;
    [SerializeField] bool opened;

    public void Setup(int potionCount) => potions = potionCount;

    public override string Prompt => opened ? "" : "Abrir baú";

    public override void Interact(IsoPlayer player)
    {
        if (opened) return;
        opened = true;
        player.AddPotions(potions);
        IsoGame.Message($"Você encontrou {potions} poções!");

        Transform lockT = transform.Find("Lock");
        if (lockT != null) lockT.gameObject.SetActive(false);
        Transform body = transform.Find("Body");
        if (body == null) body = transform.Find("Chest Sprite");
        if (body != null)
        {
            var sr = body.GetComponent<SpriteRenderer>();
            sr.color = new Color(sr.color.r * 0.6f, sr.color.g * 0.6f, sr.color.b * 0.6f, sr.color.a);
        }
    }
}
