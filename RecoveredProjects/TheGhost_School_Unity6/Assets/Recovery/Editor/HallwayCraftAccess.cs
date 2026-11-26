using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
public static class HallwayCraftAccess
{
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 var doors=Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&d.requiredKeyId=="silver_key").ToArray();if(doors.Length!=1||!doors[0].isLocked)throw new Exception("Expected only HallwayRoom silver lock");var door=doors[0];
 var b=door.GetComponent<BoxCollider>().bounds;var start=new Vector3(b.center.x,2.04f,b.center.z-1.3f);var end=start+Vector3.forward*2.6f;
 if(Walk(start,end))throw new Exception("Closed door can be bypassed");door.UnlockDoor("silver_key");door.Open();door.Tick(3);if(!door.IsFullyOpen||!Walk(start,end))throw new Exception("Open door blocks player");
 var station=Object.FindAnyObjectByType<KeyCraftingStation>();var stand=new Vector3(44,2.04f,60.6f);if(!Walk(end,new Vector3(46.8f,2.04f,60.6f))||!Walk(new Vector3(46.8f,2.04f,60.6f),stand))throw new Exception("Route to machine blocked");
 var cam=new GameObject("Machine review").AddComponent<Camera>();cam.transform.position=stand+Vector3.up*1.6f;cam.transform.LookAt(station.transform.position+Vector3.up*.95f);
 var interact=Object.FindAnyObjectByType<PlayerInteractor>();interact.view=cam;interact.InputBlocked=false;interact.RefreshTarget();if(!(interact.Target is HoldNoiseAction))throw new Exception("Machine not targetable "+interact.Target);
 var light=cam.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=6;light.intensity=.8f;var rt=new RenderTexture(1000,700,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,700);tex.ReadPixels(new Rect(0,0,1000,700),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/hallway-craft.png",tex.EncodeToPNG());
 File.WriteAllText("RecoveryReports/hallway-craft-access.txt","PASS sole silver lock, closed door blocks, unlocked door opens90deg, physical CharacterController enters and walks to machine, machine raycast within normal range. No test scene saved.\n");EditorApplication.Exit(0);}
 static bool Walk(Vector3 a,Vector3 b){var go=new GameObject("Access probe");go.transform.position=a;var cc=go.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.34f;cc.center=Vector3.up*.9f;cc.stepOffset=.25f;cc.skinWidth=.025f;Physics.SyncTransforms();for(int i=0;i<400;i++){var delta=Vector3.ProjectOnPlane(b-go.transform.position,Vector3.up);if(delta.magnitude<.08f)break;cc.Move(Vector3.ClampMagnitude(delta,.03f)+Vector3.down*.03f);}bool pass=Vector3.Distance(go.transform.position,b)<.3f;Object.DestroyImmediate(go);return pass;}
}
