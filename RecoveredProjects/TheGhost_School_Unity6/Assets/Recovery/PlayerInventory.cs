using UnityEngine;

// Compatibility for the recovered note/flashlight scripts and editor tools.
public sealed class PlayerInventory : MonoBehaviour
{
    public const int Capacity = Inventory.Capacity;
    Inventory storage;
    StartRoom room;
    public int SelectedIndex => storage ? storage.SelectedIndex : 0;
    public StartRoomItem Selected => storage ? storage.Selected.LegacyItem : null;
    public int Count { get { int count=0; if(storage) for(int i=0;i<Capacity;i++) if(!storage.GetSlot(i).Empty) count++; return count; } }
    public bool HasFlashlight { get { if(storage) for(int i=0;i<Capacity;i++) { var item=storage.GetSlot(i).LegacyItem; if(item && item.flashlight)return true; } return false; } }
    public void Initialize(StartRoom owner)
    {
        room=owner;
        var player=owner.view.GetComponentInParent<PlayerInteractor>();
        storage=player.GetComponent<Inventory>(); if(!storage)storage=player.gameObject.AddComponent<Inventory>();
        storage.view=owner.view; storage.interactor=player; storage.room=owner;
        storage.flashlight=owner.view.GetComponent<Flashlight>(); if(!storage.flashlight)storage.flashlight=owner.view.gameObject.AddComponent<Flashlight>();
        storage.OnSlotSelected-=SelectionChanged; storage.OnSlotSelected+=SelectionChanged;
        storage.OnInventoryChanged-=RefreshHands; storage.OnInventoryChanged+=RefreshHands;
        var input=player.GetComponent<InventoryInput>(); if(!input)input=player.gameObject.AddComponent<InventoryInput>(); input.inventory=storage;
        var ui=player.GetComponent<InventoryUI>(); if(!ui)ui=player.gameObject.AddComponent<InventoryUI>(); ui.Bind(storage);
        RefreshHands();
    }
    void SelectionChanged(int index) => RefreshHands();
    void RefreshHands(){if(room && room.hands) room.hands.SetHeldItem(Selected, Selected && !Selected.flashlight);}
    void OnDestroy(){if(storage){storage.OnSlotSelected-=SelectionChanged;storage.OnInventoryChanged-=RefreshHands;}}
    public bool TryAdd(StartRoomItem item)=> storage && storage.AddLegacy(item);
    public void Select(int index){if(storage)storage.SelectSlot(index);}
    public void UseSelected(){if(storage)storage.UseSelected();}
    public void DrawBar(){} // InventoryUI owns the five slots now.
}
