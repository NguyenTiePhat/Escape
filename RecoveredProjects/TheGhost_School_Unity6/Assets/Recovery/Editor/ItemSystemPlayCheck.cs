using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class ItemSystemPlayCheck
{
    static double started; static int stage;
    static Inventory inv; static PickupItem dropped; static float dropY;static Camera originalView;static Camera dropView;
    static ItemSystemPlayCheck()
    {
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("ItemSystemPlayCheck",false)){stage=0;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}};
    }
    static void Poll(){if(!File.Exists("RecoveryReports/item-play.request")||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete("RecoveryReports/item-play.request");EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("ItemSystemPlayCheck",true);EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Show();EditorApplication.isPlaying=true;}
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup-started<2)return;
        try
        {
            if(stage==0)
            {
                var room=Object.FindFirstObjectByType<StartRoom>();inv=room.view.GetComponentInParent<Inventory>();originalView=inv.view;
                Require(inv && room.inventory,"runtime initialization");
                var prompt=Object.FindFirstObjectByType<InteractionPromptUI>(); Require(prompt.label.font.atlasTexture.isReadable,"dynamic prompt atlas readable");Require(prompt.label.font.HasCharacters("Cửa đã khóa chọn chìa khóa mở khóa Nhặt Chìa khóa phòng 1 Pin Cầu chì", out uint[] missingGlyphs, false, true),"Vietnamese prompt glyphs");
                var legacy=Object.FindObjectsByType<StartRoomItem>(FindObjectsSortMode.None);
                Require(room.inventory.TryAdd(legacy.First(i=>i.flashlight)),"legacy flashlight");
                Require(room.inventory.TryAdd(legacy.First(i=>!i.flashlight)),"legacy note");
                inv.UseSelected();Require(room.Reading,"note works with shared inventory");room.CloseNote();
                foreach(var pickup in Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None))Require(pickup.TryPickup(inv)&&!pickup.gameObject.activeSelf,"pickup succeeds and hides immediately");
                Require(Enumerable.Range(0,5).All(i=>!inv.GetSlot(i).Empty),"five shared slots");
                int fuseSlot=Enumerable.Range(0,5).First(i=>inv.GetSlot(i).Item&&inv.GetSlot(i).Item.id=="fuse");
                inv.SelectSlot(fuseSlot);
                dropView=new GameObject("Drop check camera").AddComponent<Camera>();dropView.enabled=false;dropView.transform.position=new Vector3(-200,10,-200);inv.view=dropView;
                var lamp=legacy.First(i=>i.flashlight); inv.SelectSlot(0);Require(inv.Drop()&&lamp.gameObject.activeSelf,"legacy flashlight drop");Require(room.inventory.TryAdd(lamp),"legacy flashlight repickup");inv.SelectSlot(fuseSlot);
                int drops=0;inv.OnItemDropped+=(p,w)=>{Require(w>0,"drop weight");drops++;};
                Require(inv.Drop()&&drops==1&&!inv.HasItem("fuse"),"drop event and removal");
                dropped=Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None).Single();dropY=dropped.transform.position.y;
                stage=1;started=EditorApplication.timeSinceStartup;
            }
            else if(stage==1)
            {
                Require(dropped&&dropped.transform.position.y<dropY-.5f,"dropped item falls under physics");
                Require(dropped.TryPickup(inv)&&inv.HasItem("fuse"),"dropped item can be picked up again");
                inv.view=originalView;Object.Destroy(dropView.gameObject);
                var door=Object.FindObjectsByType<Door>(FindObjectsSortMode.None).First(d=>d.name=="Exit door hinge");
                // Aim a temporary camera at the real door without moving the character.
                var testView=new GameObject("Door input test camera").AddComponent<Camera>();testView.enabled=false;
                var panel=door.GetComponentsInChildren<BoxCollider>().First();
                testView.transform.position=panel.bounds.center-Vector3.forward*1.1f;testView.transform.rotation=Quaternion.identity;
                inv.interactor.view=testView;Physics.SyncTransforms();inv.interactor.RefreshTarget();Require(ReferenceEquals(inv.interactor.Target,door),"door ray target");
                Require(!door.isLocked,"starting exit must be unlocked"); inv.interactor.HandleInteractionInput(false,true); Require(door.IsOpen,"F opens starting exit without a key"); inv.interactor.HandleInteractionInput(false,true); Require(!door.IsOpen,"F closes starting exit"); door.LockDoor();
                int keySlot=Enumerable.Range(0,5).First(i=>inv.GetSlot(i).Item&&inv.GetSlot(i).Item.id=="room_1_key");inv.SelectSlot(keySlot);
                inv.interactor.HandleInteractionInput(false,true);Require(door.isLocked&&!door.IsOpen,"F cannot open locked door");
                inv.interactor.HandleInteractionInput(true,false);Require(!door.isLocked&&!inv.HasItem("room_1_key")&&!door.IsOpen,"E uses key without toggling door");
                inv.interactor.HandleInteractionInput(false,true);Require(door.IsOpen,"F opens unlocked door");
                inv.interactor.HandleInteractionInput(true,false);Require(door.IsOpen,"E does not toggle door");
                inv.interactor.HandleInteractionInput(false,true);Require(!door.IsOpen,"F closes door");
                inv.interactor.view=originalView;Object.Destroy(testView.gameObject);
                inv.AddItem(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Recovery/Items/room_1_key.asset"));
                inv.SelectSlot(0);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                File.WriteAllText("RecoveryReports/item-play-check.txt","PASS: shared legacy/new slots; note preserved; legacy flashlight drop/repickup; successful pickup disabled and destroyed; drop emits event and actually falls; re-pickup; real-door ray target; F locked/open/close; E consumes matching key without toggling door.\n");
                ScreenCapture.CaptureScreenshot("RecoveryReports/inventory-items-preview.png");stage=2;started=EditorApplication.timeSinceStartup;
            }
            else
            {
                Require(!dropped,"successful pickup destroys world object");
                if(!File.Exists("RecoveryReports/inventory-items-preview.png")&&EditorApplication.timeSinceStartup-started<20)return;
                Finish();
            }
        }
        catch(Exception e){File.WriteAllText("RecoveryReports/item-play-check.txt","FAIL: "+e);Debug.LogException(e);Finish();}
    }
    static void Finish(){EditorApplication.update-=Tick;SessionState.SetBool("ItemSystemPlayCheck",false);EditorApplication.isPlaying=false;}
}



