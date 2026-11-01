using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class InventoryLogicCheck
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var player=new GameObject("Inventory test player");var inv=player.AddComponent<Inventory>();
        var view=new GameObject("Camera").AddComponent<Camera>();view.transform.SetParent(player.transform);view.transform.position=new Vector3(0,2,0);inv.view=view;
        inv.interactor=player.AddComponent<PlayerInteractor>();inv.interactor.view=view;inv.interactor.interactableMask=LayerMask.GetMask("Interactable");
        inv.flashlight=view.gameObject.AddComponent<Flashlight>();
        var key=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Recovery/Items/room_1_key.asset");
        var battery=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Recovery/Items/battery.asset");
        var fuse=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Recovery/Items/fuse.asset");
        int changes=0,selections=0,drops=0;inv.OnInventoryChanged+=()=>changes++;inv.OnSlotSelected+=i=>selections++;
        Require(inv.AddItem(battery,6)&&inv.GetSlot(0).Quantity==4&&inv.GetSlot(1).Quantity==2,"stack split");
        Require(inv.AddItem(fuse,3)&&inv.AddItem(key,2),"fill five slots");
        int before=changes;Require(!inv.AddItem(fuse,1)&&inv.CountItem(fuse.id)==3&&before==changes,"atomic overflow");
        Require(!inv.RemoveItem(battery.id,7)&&inv.CountItem(battery.id)==6,"atomic remove");
        Require(inv.RemoveItem(battery.id,5)&&inv.CountItem(battery.id)==1&&inv.GetSlot(0).Empty,"remove spanning slots");
        inv.SelectSlot(1);Require(selections==1,"selection event");inv.SelectSlot(5);Require(inv.SelectedIndex==1,"invalid selection");
        inv.UseSelected();Require(inv.CountItem(battery.id)==1,"full battery should not consume");
        inv.flashlight.ConsumeBattery(50);inv.UseSelected();Require(inv.flashlight.Battery==75&&!inv.HasItem(battery.id),"battery use");
        var doorObject=new GameObject("Door");doorObject.layer=LayerMask.NameToLayer("Interactable");doorObject.transform.position=new Vector3(0,2,1.5f);doorObject.AddComponent<BoxCollider>();var door=doorObject.AddComponent<Door>();door.isLocked=true;door.requiredKeyId="wrong";
        Physics.SyncTransforms();inv.SelectSlot(3);inv.UseSelected();Require(door.isLocked&&inv.CountItem(key.id)==2,"wrong key retained");
        door.requiredKeyId=key.id;inv.UseSelected();Require(!door.isLocked&&inv.CountItem(key.id)==1,"matching key consumed once");
        inv.SelectSlot(4);inv.UseSelected();Require(inv.CountItem(key.id)==1,"unlocked door retains key");
        Object.DestroyImmediate(doorObject);Physics.SyncTransforms();
        inv.OnItemDropped+=(position,weight)=>{Require(weight==key.weight&&Vector3.Distance(position,view.transform.position)<1,"drop payload");drops++;};
        Require(inv.Drop()&&drops==1&&!inv.HasItem(key.id),"drop removes one");
        var dropped=Object.FindFirstObjectByType<PickupItem>();Require(dropped&&dropped.item==key&&dropped.quantity==1&&dropped.GetComponent<Rigidbody>().useGravity,"drop prefab and gravity");
        Object.DestroyImmediate(dropped.gameObject);
        inv.SelectSlot(2);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,2,.6f);Physics.SyncTransforms();
        Require(!inv.Drop()&&inv.CountItem(fuse.id)==3&&drops==1,"wall prevents drop without item loss");
        Object.DestroyImmediate(wall);
        Require(inv.AddItem(key,4),"refill empty slots");
        var overflow=Object.Instantiate(battery.pickupPrefab);Require(!overflow.TryPickup(inv)&&overflow.gameObject.activeSelf,"full pickup stays in world");
        Object.DestroyImmediate(overflow.gameObject);
        File.WriteAllText("RecoveryReports/inventory-logic-check.txt","PASS: five shared slots; split/merge stacks; atomic overflow/removal; selection events; full battery retained; recharge consumes one; wrong/right/already-unlocked key; drop prefab, gravity and payload; blocked drop retains item; full pickup retained.\n");
    }
}
