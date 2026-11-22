using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class PickupItem : MonoBehaviour, IInteractable
{
    public ItemData item;
    [Min(1)] public int quantity = 1;
    [Min(.01f)] public float dropRadius = .15f;
    static readonly System.Collections.Generic.HashSet<PickupItem> activeItems = new System.Collections.Generic.HashSet<PickupItem>();
    public static System.Collections.Generic.IEnumerable<PickupItem> ActiveItems => activeItems;
    public Renderer[] VisualRenderers { get; private set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry() => activeItems.Clear();
    void OnEnable() { VisualRenderers = GetComponentsInChildren<Renderer>(true); activeItems.Add(this); }
    void OnDisable() { activeItems.Remove(this); }
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

