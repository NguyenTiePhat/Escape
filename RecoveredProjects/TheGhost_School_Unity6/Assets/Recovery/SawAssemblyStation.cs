using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SawAssemblyStation : MonoBehaviour, IInteractable
{
    public ItemData blade, handle, screws, finishedSaw;
    public Transform outputPoint;
    readonly bool[] installed = new bool[3];
    bool assembling;
    public event Action OnAssemblyChanged;
    public bool IsComplete { get; private set; }
    public PickupItem Output { get; private set; }
    public int InstalledCount => (installed[0] ? 1 : 0) + (installed[1] ? 1 : 0) + (installed[2] ? 1 : 0);
    public bool IsInstalled(int index) => index >= 0 && index < 3 && installed[index];
    public ItemData Ingredient(int index) => index == 0 ? blade : index == 1 ? handle : index == 2 ? screws : null;
    public string Prompt => IsComplete ? "Cưa đã lắp xong — nhặt cưa" : "Chọn lưỡi cưa, tay cầm hoặc ốc vít rồi nhấn E để lắp (" + InstalledCount + "/3)";
    public void Interact()
    {
        var player = FindFirstObjectByType<PlayerInteractor>();
        TryInstall(player ? player.GetComponent<Inventory>() : null);
    }
    public bool TryInstall(Inventory inventory)
    {
        if (!isActiveAndEnabled || assembling || IsComplete || !inventory || (inventory.room && inventory.room.Reading)) return false;
        var selected = inventory.Selected.Item;
        int index = -1;
        for (int i = 0; i < 3; i++) if (selected && Ingredient(i) && selected.id == Ingredient(i).id) { index = i; break; }
        if (index < 0) { inventory.Notify("Hãy chọn lưỡi cưa, tay cầm hoặc ốc vít để lắp."); return false; }
        if (installed[index]) { inventory.Notify("Bộ phận này đã được lắp."); return false; }
        if (!finishedSaw || !finishedSaw.pickupPrefab || !outputPoint) { inventory.Notify("Chỗ ghép cưa chưa được cấu hình đầy đủ."); return false; }
        assembling = true;
        PickupItem result = null;
        try
        {
            if (InstalledCount == 2)
            {
                result = Instantiate(finishedSaw.pickupPrefab, outputPoint.position, outputPoint.rotation);
                result.gameObject.SetActive(false); result.item = finishedSaw;
            }
            if (!inventory.RemoveItem(selected.id)) { if (result) Destroy(result.gameObject); return false; }
            installed[index] = true;
            if (result)
            {
                Output = result; IsComplete = true;
                var body = result.GetComponent<Rigidbody>();
                if (body) { body.isKinematic = false; body.useGravity = true; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; }
                result.gameObject.SetActive(true);
                inventory.Notify("Cưa đã hoàn chỉnh. Nhặt cưa tại chỗ ghép.");
            }
            else inventory.Notify("Đã lắp " + selected.itemName + " (" + InstalledCount + "/3).");
            OnAssemblyChanged?.Invoke();
            return true;
        }
        finally { assembling = false; }
    }
}
