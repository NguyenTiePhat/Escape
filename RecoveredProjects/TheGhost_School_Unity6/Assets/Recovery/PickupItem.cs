using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class PickupItem : MonoBehaviour, IInteractable
{
    public ItemData item;
    [Min(1)] public int quantity = 1;
    [Min(.01f)] public float dropRadius = .15f;
    bool collected;
    public string Prompt => item ? "Nhặt " + item.itemName : "Vật phẩm chưa cấu hình";
    void Awake()
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0) foreach (var col in GetComponentsInChildren<Collider>(true)) col.gameObject.layer = layer;
    }
    public void Interact()
    {
        var player = FindFirstObjectByType<PlayerInteractor>();
        var inventory = player ? player.GetComponent<Inventory>() : null;
        TryPickup(inventory);
    }
    public bool TryPickup(Inventory inventory)
    {
        if (collected || !inventory || !item || (inventory.room && inventory.room.Reading)) return false;
        if (!inventory.AddItem(item, quantity)) return false;
        collected = true; gameObject.SetActive(false); Destroy(gameObject); return true;
    }
}
