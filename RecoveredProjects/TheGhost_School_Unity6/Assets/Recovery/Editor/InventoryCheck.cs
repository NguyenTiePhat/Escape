using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class InventoryCheck
{
 static double started;static int stage;
 static InventoryCheck(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("InventoryCheck",false)){started=EditorApplication.timeSinceStartup;stage=0;EditorApplication.update+=Tick;}};}
 public static void Run(){if(File.Exists("RecoveryReports/inventory-preview.png"))File.Delete("RecoveryReports/inventory-preview.png");EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Show();EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("InventoryCheck",true);EditorApplication.isPlaying=true;}
 static void Assert(bool ok,string m){if(!ok)throw new Exception(m);}
 static void Tick(){
 if(EditorApplication.timeSinceStartup-started<3)return;
 if(stage==1){if(!File.Exists("RecoveryReports/inventory-preview.png")){if(EditorApplication.timeSinceStartup-started>45){File.AppendAllText("RecoveryReports/inventory-check.txt","\nCapture timed out.");Finish(1);}return;}Finish(0);return;}
 try{
 var room=Object.FindFirstObjectByType<StartRoom>();var inv=room.inventory;
 Assert(inv&&inv.Count==0,"Inventory should start empty");
 var props=Object.FindObjectsByType<StartRoomItem>(FindObjectsSortMode.None);
 var lamp=props.First(p=>p.flashlight);var note=props.First(p=>!p.flashlight);
 Assert(inv.TryAdd(lamp)&&!lamp.gameObject.activeSelf,"Pickup lamp");
 Assert(!inv.TryAdd(lamp),"Duplicate pickup");
 bool before=room.hands.FlashlightOn;inv.UseSelected();Assert(room.hands.FlashlightOn!=before,"Selected flashlight use");
 Assert(inv.TryAdd(note),"Pickup note");
 var rig=room.view.transform.Find("First person arms");
 Assert(rig.Find("Held note").gameObject.activeInHierarchy,"Note held model");
 Assert(!rig.Find("Flashlight beam").gameObject.activeInHierarchy,"Unequipped light leaked");
 inv.UseSelected();Assert(room.Reading&&room.HasReadNote,"Use note opens saved content");room.CloseNote();
 inv.Select(4);Assert(inv.Selected==null&&!rig.gameObject.activeSelf,"Empty slot must empty hands");
 var temp=new GameObject("Capacity test");var capacity=temp.AddComponent<PlayerInventory>();
 for(int i=0;i<5;i++){var item=new GameObject("Test item").AddComponent<StartRoomItem>();Assert(capacity.TryAdd(item),"Fill slot "+i);}
 var extra=new GameObject("Overflow item").AddComponent<StartRoomItem>();Assert(!capacity.TryAdd(extra)&&extra.gameObject.activeSelf&&capacity.Count==5,"Full inventory must retain world item");
 Object.Destroy(extra.gameObject);Object.Destroy(temp);
 Assert(Resources.Load<Texture2D>("Inventory/Flashlight")&&Resources.Load<Texture2D>("Inventory/Note"),"Missing icons");
 inv.Select(0);Assert(rig.gameObject.activeSelf&&!rig.Find("Held note").gameObject.activeSelf,"Reselect lamp");
 room.view.transform.localRotation=Quaternion.Euler(5,0,0);
 File.WriteAllText("RecoveryReports/inventory-check.txt","PASS: empty start; pickup; duplicate rejection; selected flashlight use; held note; no unequipped light; note reading; empty hands; five-slot capacity; overflow retained; icons loaded; re-equip.");
 ScreenCapture.CaptureScreenshot("RecoveryReports/inventory-preview.png");stage=1;
 }catch(Exception e){File.WriteAllText("RecoveryReports/inventory-check.txt","FAIL: "+e);Finish(1);}
 }
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("InventoryCheck",false);if(Application.isBatchMode)EditorApplication.Exit(code);else EditorApplication.Exit(code);}
}

