using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InventoryInput : MonoBehaviour
{
    public Inventory inventory;
    static readonly Key[] SlotKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
    void Update()
    {
        if(!inventory || !Application.isFocused || Cursor.lockState!=CursorLockMode.Locked || (inventory.room && inventory.room.Reading))return;
        var keyboard=Keyboard.current;
        if(keyboard!=null)
        {
            for(int i=0;i<SlotKeys.Length;i++)if(keyboard[SlotKeys[i]].wasPressedThisFrame)inventory.SelectSlot(i);
            if(keyboard.gKey.wasPressedThisFrame)inventory.Drop();
        }
        float wheel=Mouse.current!=null?Mouse.current.scroll.ReadValue().y:0;
        if(wheel!=0)inventory.SelectSlot((inventory.SelectedIndex+(wheel>0?4:1))%Inventory.Capacity);
    }
}
