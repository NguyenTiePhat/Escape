using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerInventory : MonoBehaviour
{
    public const int Capacity = 5;
    readonly List<StartRoomItem> items = new List<StartRoomItem>();
    public int SelectedIndex { get; private set; }
    public int Count => items.Count;
    public bool HasFlashlight => items.Exists(item => item && item.flashlight);
    public StartRoomItem Selected => SelectedIndex < items.Count ? items[SelectedIndex] : null;
    StartRoom room;
    Texture2D flashlightIcon, noteIcon;
    public void Initialize(StartRoom owner) { room = owner; flashlightIcon = Resources.Load<Texture2D>("Inventory/Flashlight"); noteIcon = Resources.Load<Texture2D>("Inventory/Note"); }
    public bool TryAdd(StartRoomItem item)
    {
        if (!item || items.Contains(item) || items.Count >= Capacity) return false;
        items.Add(item); item.gameObject.SetActive(false); Select(items.Count - 1); return true;
    }
    public void Select(int index)
    {
        SelectedIndex = Mathf.Clamp(index, 0, Capacity - 1);
        if (room && room.hands) room.hands.SetHeldItem(Selected != null, Selected && !Selected.flashlight);
    }
    public void UseSelected()
    {
        if (!room || room.Reading || !Selected) return;
        if (Selected.flashlight) room.hands.ToggleFlashlight(); else room.OpenNote();
    }
    void Update()
    {
        if (!room || room.Reading || Cursor.lockState != CursorLockMode.Locked) return;
        for (int i=0;i<Capacity;i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))) Select(i);
        float wheel = Input.GetAxis("Mouse ScrollWheel");
        if (wheel != 0) Select((SelectedIndex + (wheel > 0 ? Capacity - 1 : 1)) % Capacity);
    }
    public void DrawBar()
    {
        float size = Mathf.Clamp(Screen.width * .055f, 40, 58), gap = 5;
        float width = size * Capacity + gap * (Capacity - 1), x = (Screen.width - width) / 2;
        var old = GUI.color;
        var number = new GUIStyle(GUI.skin.label) { fontSize=11, alignment=TextAnchor.UpperLeft };
        for (int i=0;i<Capacity;i++)
        {
            var rect = new Rect(x+i*(size+gap),12,size,size);
            GUI.color = i == SelectedIndex ? new Color(.74f,.53f,.18f,1) : new Color(.34f,.36f,.33f,.85f);
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color = new Color(.12f,.14f,.12f,.94f);
            GUI.DrawTexture(new Rect(rect.x+2,rect.y+2,size-4,size-4),Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (i<items.Count)
            {
                var icon=items[i].flashlight ? flashlightIcon : noteIcon;
                if(icon)GUI.DrawTexture(new Rect(rect.x+8,rect.y+8,size-16,size-16),icon,ScaleMode.ScaleToFit,true);
                else GUI.Label(rect,items[i].flashlight?"Đèn":"Giấy");
            }
            GUI.Label(new Rect(rect.x+4,rect.y+2,18,16),(i+1).ToString(),number);
        }
        var label = new GUIStyle(GUI.skin.label) {fontSize=18,alignment=TextAnchor.UpperCenter};
        GUI.Label(new Rect(x-80,size+16,width+160,26),Selected ? (Selected.flashlight ? "Đèn pin" : "Tờ giấy") : "Tay không",label);
        GUI.color = old;
    }
}

