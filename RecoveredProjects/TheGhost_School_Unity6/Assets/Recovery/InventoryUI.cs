using UnityEngine;

// Event-driven display. This component never mutates inventory contents.
public sealed class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    readonly ItemData[] items=new ItemData[Inventory.Capacity];
    readonly StartRoomItem[] legacy=new StartRoomItem[Inventory.Capacity];
    readonly int[] quantities=new int[Inventory.Capacity];
    int selected;
    string message; float messageUntil;
    Texture2D flashlightIcon,noteIcon;
    public void Bind(Inventory value)
    {
        Unsubscribe(); inventory=value;
        flashlightIcon=Resources.Load<Texture2D>("Inventory/Flashlight"); noteIcon=Resources.Load<Texture2D>("Inventory/Note");
        Subscribe();
    }
    void OnEnable(){Subscribe();}
    void Subscribe(){if(!inventory)return;Unsubscribe();inventory.OnInventoryChanged+=Refresh;inventory.OnSlotSelected+=Select;inventory.OnMessage+=Message;Refresh();Select(inventory.SelectedIndex);}
    void Unsubscribe(){if(!inventory)return;inventory.OnInventoryChanged-=Refresh;inventory.OnSlotSelected-=Select;inventory.OnMessage-=Message;}
    void OnDisable(){Unsubscribe();}
    void Refresh(){for(int i=0;i<items.Length;i++){var slot=inventory.GetSlot(i);items[i]=slot.Item;legacy[i]=slot.LegacyItem;quantities[i]=slot.Quantity;}}
    void Select(int index){selected=index;}
    void Message(string value){message=value;messageUntil=Time.unscaledTime+2.5f;}
    void OnGUI()
    {
        if(!inventory || (inventory.room && inventory.room.Reading))return;
        float size=Mathf.Clamp(Screen.width*.05f,48,68),gap=6,x=Screen.width-(size+gap)*5-18,y=Screen.height-size-70;
        var old=GUI.color;
        for(int i=0;i<items.Length;i++)
        {
            var rect=new Rect(x+i*(size+gap),y,size,size);
            GUI.color=i==selected?new Color(1,.82f,.32f):new Color(.3f,.32f,.34f);
            GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=new Color(.08f,.09f,.10f,.96f);
            GUI.DrawTexture(new Rect(rect.x+3,rect.y+3,size-6,size-6),Texture2D.whiteTexture);GUI.color=Color.white;
            if(items[i] && items[i].icon){var sprite=items[i].icon;var r=sprite.textureRect;GUI.DrawTextureWithTexCoords(new Rect(rect.x+10,rect.y+10,size-20,size-20),sprite.texture,new Rect(r.x/sprite.texture.width,r.y/sprite.texture.height,r.width/sprite.texture.width,r.height/sprite.texture.height));}
            else if(legacy[i]){var icon=legacy[i].flashlight?flashlightIcon:noteIcon;if(icon)GUI.DrawTexture(new Rect(rect.x+10,rect.y+10,size-20,size-20),icon,ScaleMode.ScaleToFit);}
            GUI.Label(new Rect(rect.x+5,rect.y+2,20,20),(i+1).ToString());
            if(quantities[i]>0)GUI.Label(new Rect(rect.x+size-23,rect.y+size-23,24,22),quantities[i].ToString());
        }
        string title=items[selected]?items[selected].itemName:legacy[selected]?(legacy[selected].flashlight?"Đèn pin":"Tờ giấy"):"Tay không";
        GUI.Label(new Rect(x,y-25,350,25),title);
        if(Time.unscaledTime<messageUntil)GUI.Box(new Rect(Screen.width/2-210,Screen.height*.65f,420,34),message);
        GUI.color=old;
    }
}
