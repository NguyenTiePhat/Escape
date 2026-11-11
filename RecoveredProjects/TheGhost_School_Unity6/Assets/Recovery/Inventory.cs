using System;
using UnityEngine;

// Storage and actions only. Input and rendering live in separate components.
public sealed class Inventory : MonoBehaviour
{
    public const int Capacity = 5;
    public sealed class Slot
    {
        public ItemData Item { get; internal set; }
        public int Quantity { get; internal set; }
        public StartRoomItem LegacyItem { get; internal set; }
        public bool Empty => !Item && !LegacyItem;
    }
    readonly Slot[] slots = { new Slot(), new Slot(), new Slot(), new Slot(), new Slot() };
    public Slot GetSlot(int index) => slots[index];
    public int SelectedIndex { get; private set; }
    public Slot Selected => slots[SelectedIndex];
    public Camera view;
    public PlayerInteractor interactor;
    public Flashlight flashlight;
    public StartRoom room;
    public event Action OnInventoryChanged;
    public event Action<int> OnSlotSelected;
    public event Action<Vector3, float> OnItemDropped;
    public event Action<string> OnMessage;
    public void Notify(string message) => OnMessage?.Invoke(message);

    public bool AddItem(ItemData item, int quantity = 1)
    {
        if (!item || string.IsNullOrWhiteSpace(item.id) || quantity <= 0) return false;
        long available = 0;
        foreach (var slot in slots)
            if (slot.Empty) available += item.StackLimit;
            else if (slot.Item == item) available += Mathf.Max(0, item.StackLimit - slot.Quantity);
        if (available < quantity) { Notify("Túi đồ đã đầy (5 ô)."); return false; }
        int remaining = quantity;
        foreach (var slot in slots)
        {
            if (slot.Item != item) continue;
            int add = Mathf.Min(remaining, Mathf.Max(0, item.StackLimit - slot.Quantity));
            slot.Quantity += add; remaining -= add;
        }
        foreach (var slot in slots)
        {
            if (remaining == 0) break;
            if (!slot.Empty) continue;
            slot.Item = item; slot.Quantity = Mathf.Min(remaining, item.StackLimit); remaining -= slot.Quantity;
        }
        OnInventoryChanged?.Invoke(); return true;
    }
    public bool HasItem(string id) => CountItem(id) > 0;
    public int CountItem(string id)
    {
        int count = 0;
        foreach (var slot in slots) if (slot.Item && slot.Item.id == id) count += slot.Quantity;
        return count;
    }
    public bool RemoveItem(string id, int quantity = 1)
    {
        if (quantity <= 0 || CountItem(id) < quantity) return false;
        foreach (var slot in slots)
        {
            if (!slot.Item || slot.Item.id != id) continue;
            int remove = Mathf.Min(quantity, slot.Quantity); quantity -= remove; slot.Quantity -= remove;
            if (slot.Quantity == 0) Clear(slot);
            if (quantity == 0) break;
        }
        OnInventoryChanged?.Invoke(); return true;
    }
    // Consume a recipe atomically: listeners never see only half the recipe removed.
    public bool RemoveRecipe(ItemData[] ingredients)
    {
        if (ingredients == null || ingredients.Length == 0) return false;
        var counts = new System.Collections.Generic.Dictionary<string, int>();
        foreach (var item in ingredients)
        {
            if (!item || string.IsNullOrEmpty(item.id)) return false;
            counts.TryGetValue(item.id, out int count); counts[item.id] = count + 1;
        }
        foreach (var pair in counts) if (CountItem(pair.Key) < pair.Value) return false;
        foreach (var pair in counts)
        {
            int remaining = pair.Value;
            foreach (var slot in slots)
            {
                if (!slot.Item || slot.Item.id != pair.Key) continue;
                int remove = Mathf.Min(remaining, slot.Quantity); slot.Quantity -= remove; remaining -= remove;
                if (slot.Quantity == 0) Clear(slot);
                if (remaining == 0) break;
            }
        }
        OnInventoryChanged?.Invoke(); return true;
    }
    static void Clear(Slot slot) { slot.Item = null; slot.LegacyItem = null; slot.Quantity = 0; }
    public void SelectSlot(int index)
    {
        if (index < 0 || index >= Capacity) return;
        SelectedIndex = index; OnSlotSelected?.Invoke(index);
    }
    public bool AddLegacy(StartRoomItem item)
    {
        if (!item) return false;
        foreach (var slot in slots) if (slot.LegacyItem == item) return false;
        for (int i = 0; i < Capacity; i++) if (slots[i].Empty)
        {
            slots[i].LegacyItem = item; slots[i].Quantity = 1;
            item.gameObject.SetActive(false); OnInventoryChanged?.Invoke(); SelectSlot(i); return true;
        }
        Notify("Túi đồ đã đầy (5 ô)."); return false;
    }
    public void UseSelected()
    {
        if (room && room.Reading) return;
        var slot = Selected;
        if (slot.LegacyItem)
        {
            if (!room) return;
            if (slot.LegacyItem.flashlight) { if (room.hands) room.hands.ToggleFlashlight(); }
            else room.OpenNote();
            return;
        }
        if (!slot.Item) return;
        switch (slot.Item.type)
        {
            case ItemType.Key:
                if (interactor) interactor.RefreshTarget();
                var door = interactor ? interactor.Target as Door : null;
                var exit = door ? door.GetComponent<MainExitDoor>() : null;
                if (exit)
                {
                    if (!exit.TryOpenWithKey(slot.Item.id)) Notify("Cửa thoát hiểm cần chìa vàng được chế tạo từ khuôn.");
                    else Notify("Đang mở cửa thoát hiểm...");
                    break;
                }
                if (!door || !door.isLocked || !door.UnlockDoor(slot.Item.id)) { Notify("Chìa khóa không dùng được ở đây."); return; }
                if (door.consumeKeyOnUnlock) ConsumeSelected();
                bool opening = door.Open();
                Notify(opening ? "Đã mở cửa." : "Đã mở khóa. Cửa đang bị vật cản."); break;
            case ItemType.Battery:
                if (!flashlight || (room && !room.HasFlashlight) || !flashlight.AddBattery(slot.Item.batteryAmount)) { Notify("Đèn pin đã đầy hoặc chưa có đèn pin."); return; }
                ConsumeSelected(); break;
            default: Notify("Vật phẩm này chưa dùng được ở đây."); break;
        }
    }
    void ConsumeSelected() { if (--Selected.Quantity <= 0) Clear(Selected); OnInventoryChanged?.Invoke(); }

    public bool Drop()
    {
        if (!view || Selected.Empty || (room && room.Reading)) return false;
        var slot = Selected;
        if (!slot.LegacyItem && (!slot.Item || !slot.Item.pickupPrefab)) { Notify("Vật phẩm này chưa có prefab để thả."); return false; }
        // Check the complete spawn volume and path before removing anything.
        var origin = view.transform.position;
        var direction = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        if (direction.sqrMagnitude < .1f) direction = view.transform.forward;
        float radius = slot.LegacyItem ? .25f : slot.Item.pickupPrefab.dropRadius;
        var position = origin + direction * .85f;
        foreach (var hit in Physics.SphereCastAll(origin, radius, direction, .85f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform)) { Notify("Không đủ chỗ để thả vật phẩm."); return false; }
        foreach (var hit in Physics.OverlapSphere(position, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform)) { Notify("Không đủ chỗ để thả vật phẩm."); return false; }
        if (slot.LegacyItem)
        {
            var legacy = slot.LegacyItem;
            legacy.transform.SetParent(null, true); legacy.transform.position = position;
            legacy.gameObject.SetActive(true);
            var legacyBody = legacy.GetComponent<Rigidbody>();
            if (!legacyBody) legacyBody = legacy.gameObject.AddComponent<Rigidbody>();
            legacyBody.isKinematic = false; legacyBody.useGravity = true; legacyBody.mass = legacy.flashlight ? .5f : .05f;
            legacyBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            legacyBody.linearVelocity = direction * .8f;
            NoiseOnImpact.Prepare(legacyBody, gameObject);
            float legacyWeight = legacyBody.mass;
            Clear(slot); OnInventoryChanged?.Invoke(); OnItemDropped?.Invoke(position, legacyWeight); return true;
        }
        var dropped = Instantiate(slot.Item.pickupPrefab, position, Quaternion.identity);
        dropped.item = slot.Item; dropped.quantity = 1;
        var body = dropped.GetComponent<Rigidbody>();
        if (!body) body = dropped.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = false; body.useGravity = true; body.mass = Mathf.Max(.01f, slot.Item.weight);
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearVelocity = direction * .8f;
        NoiseOnImpact.Prepare(body, gameObject);
        float weight = slot.Item.weight;
        ConsumeSelected(); OnItemDropped?.Invoke(position, weight); return true;
    }
}
