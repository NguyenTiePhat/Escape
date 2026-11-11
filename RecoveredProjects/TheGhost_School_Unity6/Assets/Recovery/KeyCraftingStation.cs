using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(HoldNoiseAction))]
public sealed class KeyCraftingStation : MonoBehaviour, IHoldActionGate
{
    public ItemData keyFrame, metalBlank, finishedKey;
    public Transform outputPoint;
    Inventory inventory;
    HoldNoiseAction action;
    int frames, blanks;
    bool consuming;
    string missing = "";
    string lastNotice;
    public PickupItem Output { get; private set; }
    public string Prompt => string.IsNullOrEmpty(missing) ? "Giữ E để làm chìa khóa" : "Thiếu: " + missing;
    void Awake() { action = GetComponent<HoldNoiseAction>(); }
    bool HasIngredients(Inventory bag) => bag && keyFrame && metalBlank && bag.HasItem(keyFrame.id) && bag.HasItem(metalBlank.id);
    public bool CanAdvance(PlayerHoldAction player)
    {
        var bag = player.GetComponent<Inventory>();
        if (inventory != bag)
        {
            if (inventory) inventory.OnInventoryChanged -= InventoryChanged;
            if (action.Elapsed > 0) action.ResetProgress();
            inventory = bag;
            if (inventory) inventory.OnInventoryChanged += InventoryChanged;
            Snapshot();
        }
        UpdateMissing();
        if (!HasIngredients(inventory))
        {
            if (inventory && lastNotice != missing) { inventory.Notify("Thiếu: " + missing); lastNotice = missing; }
            return false;
        }
        lastNotice = null;
        return finishedKey && finishedKey.pickupPrefab && outputPoint && !Output;
    }
    void Snapshot()
    {
        frames = inventory && keyFrame ? inventory.CountItem(keyFrame.id) : 0;
        blanks = inventory && metalBlank ? inventory.CountItem(metalBlank.id) : 0;
    }
    void UpdateMissing()
    {
        missing = "";
        if (!inventory || !keyFrame || !inventory.HasItem(keyFrame.id)) missing = keyFrame ? keyFrame.itemName : "khung in chìa khóa";
        if (!inventory || !metalBlank || !inventory.HasItem(metalBlank.id)) missing += (missing.Length > 0 ? ", " : "") + (metalBlank ? metalBlank.itemName : "phôi kim loại");
    }
    void InventoryChanged()
    {
        if (!consuming && action.Elapsed > 0 &&
            ((keyFrame && inventory.CountItem(keyFrame.id) < frames) || (metalBlank && inventory.CountItem(metalBlank.id) < blanks)))
        {
            action.ResetProgress(); inventory.Notify("Đã bỏ nguyên liệu: tiến độ làm chìa trở về 0.");
        }
        Snapshot(); UpdateMissing();
    }
    public bool Complete(PlayerHoldAction player)
    {
        if (!HasIngredients(inventory) || Output || !finishedKey || !finishedKey.pickupPrefab || !outputPoint) return false;
        var result = Instantiate(finishedKey.pickupPrefab, outputPoint.position, outputPoint.rotation);
        result.gameObject.SetActive(false); result.item = finishedKey;
        consuming = true;
        bool removed;
        try { removed = inventory.RemoveRecipe(new[] { keyFrame, metalBlank }); }
        finally { consuming = false; }
        if (!removed) { Destroy(result.gameObject); return false; }
        var body = result.GetComponent<Rigidbody>(); if (body) { body.isKinematic = true; body.useGravity = false; }
        Output = result; result.gameObject.SetActive(true);
        inventory.Notify("Chìa khóa đã làm xong. Nhặt chìa trên khay của máy.");
        return true;
    }
    void OnDestroy() { if (inventory) inventory.OnInventoryChanged -= InventoryChanged; }
}
