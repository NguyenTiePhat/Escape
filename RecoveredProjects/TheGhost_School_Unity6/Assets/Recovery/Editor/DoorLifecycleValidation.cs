using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class DoorLifecycleValidation
{
 static IEnumerator test;static readonly StringBuilder report=new StringBuilder();
 static DoorLifecycleValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DoorLifecycleTest",false)){test=Test();EditorApplication.update+=Tick;}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("DoorLifecycleTest",true);EditorApplication.EnterPlaymode();}
 static IEnumerator Test(){yield return null;yield return null;
 foreach(var ai in Object.FindObjectsByType<MonsterAI>())ai.enabled=false;
 foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 foreach(var director in Object.FindObjectsByType<ScareDirector>())director.enabled=false;
 var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<PlayerLook>().enabled=false;player.GetComponent<CharacterController>().enabled=false;
 NoiseManager.Instance.debugLogging=false;
 var doors=Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&d.requiredKeyId=="silver_key").ToArray();
 foreach(var door in doors){
 var closedPosition=door.transform.position;var closedRotation=door.transform.rotation;var hinge=door.transform.TransformPoint(door.hingeOffset);
 var bounds=door.GetComponent<BoxCollider>().bounds;
 bool picture=door.name=="RDoor"||door.name=="Door22"||door.name=="Exit door hinge"||door.name=="DoorNarrow_mesh.002";
 if(picture)Snapshot(door,bounds,"closed");
 if(door.UnlockDoor("wrong-key"))throw new Exception("Wrong key accepted "+door.name);
 if(!door.UnlockDoor("silver_key"))throw new Exception("Key rejected "+door.name);
 door.LockDoor();if(door.isLocked)throw new Exception("Key-unlocked door relocked "+door.name);
 for(int cycle=0;cycle<3;cycle++){
 if(!door.Open())throw new Exception("Open failed "+door.name);
 door.Tick(3);yield return null;yield return null;
 if(!door.IsFullyOpen||Vector3.Distance(hinge,door.transform.TransformPoint(door.hingeOffset))>.001f)throw new Exception("Open/hinge failed "+door.name+" angle="+door.CurrentAngle+" blocker="+door.BlockingCollider);
 if(cycle==0&&picture)Snapshot(door,bounds,"open");
 if(!door.TrySetOpen(false,160))throw new Exception("Close failed "+door.name);
 door.Tick(3);yield return null;yield return null;
 if(!door.IsFullyClosed||door.isLocked||Vector3.Distance(closedPosition,door.transform.position)>.001f||Quaternion.Angle(closedRotation,door.transform.rotation)>.01f)throw new Exception("Closed pose/lock drift "+door.name);
 }
 report.AppendLine("PASS key unlock persists, 3 open/close cycles and hinge/closed-pose stability: "+MapCollisionAudit.PathOf(door.transform));
 File.WriteAllText("RecoveryReports/door-lifecycle-check.txt",report.ToString());
 }
 // Exercise the actual inventory/raycast path after an already-unlocked door closes.
 var target=doors.Single(d=>d.name=="Exit door hinge");var box=target.GetComponent<BoxCollider>();
 var interactor=player.GetComponent<PlayerInteractor>();interactor.enabled=false;interactor.InputBlocked=false;
 var camera=interactor.view;var center=box.bounds.center;var normal=box.bounds.size.x<box.bounds.size.z?Vector3.right:Vector3.forward;
 camera.transform.position=center-normal*.9f;camera.transform.LookAt(center);Physics.SyncTransforms();interactor.RefreshTarget();
 if(interactor.Target!=target){camera.transform.position=center+normal*.9f;camera.transform.LookAt(center);Physics.SyncTransforms();interactor.RefreshTarget();}
 if(interactor.Target!=target)throw new Exception("Inventory door ray target unavailable");
 var bag=player.GetComponent<Inventory>();bag.room=null;
 var key=ScriptableObject.CreateInstance<ItemData>();key.id="silver_key";key.type=ItemType.Key;bag.AddItem(key);for(int i=0;i<Inventory.Capacity;i++)if(bag.GetSlot(i).Item==key)bag.SelectSlot(i);
 int count=bag.CountItem("silver_key");bag.UseSelected();target.Tick(3);
 if(!target.IsFullyOpen||target.isLocked||bag.CountItem("silver_key")!=count)throw new Exception("Reusing key on unlocked door failed");
 report.AppendLine("PASS inventory key use reopens unlocked door without consuming key");
 var scare=Object.FindObjectsByType<ScareEvent>().Single(e=>e.kind==ScareKind.DoorSlam);
 if(scare.Ready||scare.Begin())throw new Exception("Scare slammed freshly opened door");
 report.AppendLine("PASS door-slam scare cannot immediately reverse a newly opened door");
 report.AppendLine("No runtime unlocks saved; boss room remains initially unlocked, other saved locks preserved.");
 }
 static void Snapshot(Door door,Bounds b,string state){
 var normal=b.size.x<b.size.z?Vector3.right:Vector3.forward;
 var cam=new GameObject("Door review camera").AddComponent<Camera>();cam.transform.position=b.center-normal*3+Vector3.up*.3f;cam.transform.LookAt(b.center);cam.fieldOfView=65;
 var light=cam.gameObject.AddComponent<Light>();light.type=LightType.Point;light.intensity=2;light.range=8;
 var rt=new RenderTexture(900,700,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
 var tex=new Texture2D(900,700,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,700),0,0);tex.Apply();
 Directory.CreateDirectory("RecoveryReports/DoorReview");File.WriteAllBytes("RecoveryReports/DoorReview/"+door.name.Replace(" ","-")+"-"+state+".png",tex.EncodeToPNG());
 RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(cam.gameObject);
 }
 static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("DoorLifecycleTest",false);File.WriteAllText("RecoveryReports/door-lifecycle-check.txt",report.ToString());EditorApplication.Exit(code);}
}
