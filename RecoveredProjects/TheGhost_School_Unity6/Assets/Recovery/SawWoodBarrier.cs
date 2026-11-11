using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(HoldNoiseAction))]
public sealed class SawWoodBarrier : MonoBehaviour, IHoldActionGate
{
    public ItemData saw;
    public GameObject boards;
    public Door door;
    public string Prompt => "Chọn cưa rồi giữ E để cưa gỗ";
    public bool CanAdvance(PlayerHoldAction player)
    {
        var bag = player ? player.GetComponent<Inventory>() : null;
        return bag && saw && bag.Selected.Item && bag.Selected.Item.id == saw.id && bag.HasItem(saw.id);
    }
    public bool Complete(PlayerHoldAction player)
    {
        if (!CanAdvance(player) || !boards || !door) return false;
        boards.SetActive(false);
        door.UnlockDoor(); door.Open(1f);
        return true;
    }
}
