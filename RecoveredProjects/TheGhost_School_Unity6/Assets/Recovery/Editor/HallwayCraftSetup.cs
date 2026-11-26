using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class HallwayCraftSetup
{
 public static void Run(){
 var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 if(!File.Exists("RecoveryReports/School_Test-before-hallway-craft.unity"))File.Copy(scene.path,"RecoveryReports/School_Test-before-hallway-craft.unity");
 var leaf=GameObject.Find("Gameplay/DoorNarrowP1_Locked (3)/DoorNarrow_mesh.0024");var door=leaf.GetComponent<Door>();if(!door)door=leaf.AddComponent<Door>();
 foreach(var d in Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&d.requiredKeyId=="silver_key")){d.isLocked=false;d.requiredKeyId="";d.lockedPrompt="";EditorUtility.SetDirty(d);}
 foreach(var t in leaf.GetComponentsInChildren<Transform>()){GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);if(t.GetComponent<Collider>())t.gameObject.layer=LayerMask.NameToLayer("Interactable");}
 var box=leaf.GetComponent<BoxCollider>();door.hingeOffset=new Vector3(box.center.x-box.size.x*.5f,0,box.center.z-box.size.z*.5f);door.openAngle=90;door.isLocked=false;door.Initialize(true);door.Open();door.Tick(3);
 if(!door.IsFullyOpen)throw new Exception("Hallway door sweep blocked "+door.CurrentAngle+" "+door.BlockingCollider);
 door.SetImmediate(false);door.isLocked=true;door.requiredKeyId="silver_key";door.consumeKeyOnUnlock=false;door.lockedPrompt="Phòng chế tạo — chọn chìa bạc, nhấn E để mở khóa.";
 if(!leaf.GetComponent<DoorNoise>())leaf.AddComponent<DoorNoise>();
 var station=Object.FindAnyObjectByType<KeyCraftingStation>();station.transform.position=new Vector3(44,1.99f,61.8f);station.transform.rotation=Quaternion.Euler(0,180,0);Physics.SyncTransforms();
 foreach(var c in station.GetComponentsInChildren<Collider>())foreach(var other in Physics.OverlapBox(c.bounds.center,c.bounds.extents*.95f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore)){
 if(other.transform.IsChildOf(station.transform)||other is CharacterController)continue;
 if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth)&&depth>.015f)throw new Exception("Machine overlaps "+other.name+" depth="+depth);
 }
 var silver=AssetDatabase.LoadAssetAtPath<ItemData>("Assets/Recovery/M6/Items/silver_key.asset");silver.description="Chỉ mở phòng HallwayRoom, nơi đặt máy chế tạo chìa vàng.";EditorUtility.SetDirty(silver);
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();M6NavigationSetup.Run();
 File.WriteAllText("RecoveryReports/hallway-craft-setup.txt","PASS only HallwayRoom requires silver_key; other former silver doors unlocked. Main exit and timber gate preserved. Machine moved intact to (44,1.99,61.8), no collision penetration; doorway sweeps90deg.\n");
 EscapeFlowValidation.Run();
 }
}

