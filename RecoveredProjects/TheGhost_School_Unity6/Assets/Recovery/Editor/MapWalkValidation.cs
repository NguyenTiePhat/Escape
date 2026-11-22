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
public static class MapWalkValidation
{
 static IEnumerator test;static readonly StringBuilder report=new StringBuilder();static int passed,blocked;
 static MapWalkValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MapWalkTest",false)){test=Test();EditorApplication.update+=Tick;}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("MapWalkTest",true);EditorApplication.EnterPlaymode();}
 static IEnumerator Test(){yield return null;yield return null;
 foreach(var ai in Object.FindObjectsByType<MonsterAI>())ai.enabled=false;
 foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 var player=Object.FindAnyObjectByType<PlayerMotor>();player.GetComponent<PlayerLook>().enabled=false;
 for(int i=0;i<80;i++)player.MoveGrounded(Vector3.zero,.02f);
 var cc=player.GetComponent<CharacterController>();float baseY=player.transform.position.y;bool first=player.TryJump(),doubleJump=player.TryJump();float peak=baseY;bool airborneRejected=true;
 for(int i=0;i<120;i++){player.MoveGrounded(Vector3.zero,.02f);peak=Mathf.Max(peak,player.transform.position.y);if(i>3&&!cc.isGrounded&&player.TryJump())airborneRejected=false;}
 if(!first||doubleJump||!airborneRejected||peak-baseY<.7f||!cc.isGrounded)throw new Exception("Jump failed first="+first+" double="+doubleJump+" apex="+(peak-baseY)+" grounded="+cc.isGrounded);
 report.AppendLine("PASS jump: apex="+(peak-baseY)+"m, airborne/repeated jump rejected, landed grounded");
 player.InputBlocked=true;if(player.TryJump())throw new Exception("Blocked input jumped");player.InputBlocked=false;
 player.ActionMovementLocked=true;if(player.TryJump())throw new Exception("Crafting jumped");player.ActionMovementLocked=false;
 player.SetCrouching(true);if(player.TryJump())throw new Exception("Crouch jumped");player.SetCrouching(false);
 report.AppendLine("PASS jump respects input, crafting and crouch locks");player.enabled=false;
 var doors=Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Where(d=>!d.decorative&&d.requiredKeyId=="silver_key").ToArray();
 var doorBounds=doors.Select(d=>d.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c=>c.enabled&&!c.isTrigger)?.bounds??default).ToArray();
 int closedBlocked=0;foreach(var bounds in doorBounds)if(ClosedDoorBlocks(bounds))closedBlocked++;
 if(closedBlocked!=doors.Length)throw new Exception("Closed door bypass: "+closedBlocked+"/"+doors.Length);report.AppendLine("PASS all "+closedBlocked+" closed doors still block physical traversal");
 foreach(var door in doors){door.UnlockDoor("silver_key");door.Open();}
 for(int tick=0;tick<240;tick++)foreach(var door in doors)door.Tick(1f/60);
 foreach(var door in doors)report.AppendLine("DOOR OPEN "+MapCollisionAudit.PathOf(door.transform)+" angle="+door.CurrentAngle+" blocked="+(door.BlockingCollider?MapCollisionAudit.PathOf(door.BlockingCollider.transform):"none"));
 Physics.SyncTransforms();
 Route("West ground corridor",new[]{new Vector3(5.3f,2.04f,56),new Vector3(21.1f,2.04f,54.6f),new Vector3(28.95f,2.04f,55.95f),new Vector3(44.6f,2.04f,52.45f)});
 Route("Central ground corridor to real end wall",new[]{new Vector3(44,2.04f,54),new Vector3(53.2f,2.04f,54)});
 Route("East ground corridor",new[]{new Vector3(55.2f,2.04f,54),new Vector3(68,2.04f,54)});
 Route("Gym court",new[]{new Vector3(3,2.12f,30),new Vector3(3,2.12f,5)});
 Route("Gym cross court",new[]{new Vector3(-9,2.12f,18),new Vector3(12,2.12f,18)});
 Route("Upper corridor",new[]{new Vector3(31,8.52f,49),new Vector3(31,8.52f,54),new Vector3(68,8.52f,54)});
 Route("Reading alcove",new[]{new Vector3(40,8.52f,54),new Vector3(40,8.52f,58),new Vector3(41,8.52f,60)});
 Route("Upper north room",new[]{new Vector3(52,8.52f,54),new Vector3(52,8.52f,44)});
 Route("Main stairs to upper floor",new[]{new Vector3(44.9f,2.04f,52.15f),new Vector3(44.9f,5.38f,46.3f),new Vector3(43.8f,5.38f,45.95f),new Vector3(41.35f,5.38f,46.4f),new Vector3(40.05f,8.52f,52.3f),new Vector3(40.05f,8.52f,54)});
 Route("Stairs to saw landing",new[]{new Vector3(37.1f,8.52f,52.15f),new Vector3(37.1f,11.88f,45.65f),new Vector3(36.8f,11.88f,45.35f),new Vector3(36.35f,11.88f,45.35f),new Vector3(35.7f,11.88f,46.2f),new Vector3(35.3f,15.01f,52)});
 for(int i=0;i<doors.Length;i++){
 var b=doorBounds[i];if(b.size.sqrMagnitude<.01f)continue;Vector3 normal=b.size.x<b.size.z?Vector3.right:Vector3.forward;
 var a=b.center-normal*.95f;var end=b.center+normal*.95f;
 if(!Physics.Raycast(a+Vector3.up*.4f,Vector3.down,out var fa,3,~0,QueryTriggerInteraction.Ignore)||!Physics.Raycast(end+Vector3.up*.4f,Vector3.down,out var fb,3,~0,QueryTriggerInteraction.Ignore))continue;
 a.y=fa.point.y+.04f;end.y=fb.point.y+.04f;if(Mathf.Abs(a.y-end.y)>.4f)continue;
 Route("Door "+MapCollisionAudit.PathOf(doors[i].transform),new[]{a,end});
 }
 var barrier=Object.FindAnyObjectByType<SawWoodBarrier>();if(barrier&&barrier.boards){barrier.boards.SetActive(false);barrier.door.UnlockDoor();barrier.door.Open();for(int i=0;i<240;i++)barrier.door.Tick(1f/60);report.AppendLine("ROOF DOOR angle="+barrier.door.CurrentAngle+" blocker="+barrier.door.BlockingCollider);Route("Roof door after removing timber",new[]{new Vector3(35.29f,15.01f,52.3f),new Vector3(35.29f,15.01f,54.5f)});}
 report.AppendLine("Traversal totals passed="+passed+" blocked="+blocked+". Door keys authorized only in test memory; no scene saved.");
 }
 static bool ClosedDoorBlocks(Bounds b){var normal=b.size.x<b.size.z?Vector3.right:Vector3.forward;var start=b.center-normal*.95f;var end=b.center+normal*.95f;start.y=end.y=b.min.y+.04f;var go=new GameObject("Closed door probe");go.transform.position=start;var cc=go.AddComponent<CharacterController>();cc.height=1.8f;cc.center=Vector3.up*.9f;cc.radius=.34f;cc.skinWidth=.025f;cc.stepOffset=.25f;Physics.SyncTransforms();for(int i=0;i<100;i++)cc.Move(normal*.025f+Vector3.down*.04f);bool blocked=Vector3.ProjectOnPlane(end-go.transform.position,Vector3.up).magnitude>.25f;Object.DestroyImmediate(go);return blocked;}
 static void Route(string name,Vector3[] points){var go=new GameObject("Traversal probe");go.transform.position=points[0];var body=go.AddComponent<CharacterController>();body.height=1.8f;body.radius=.34f;body.center=Vector3.up*.9f;body.skinWidth=.025f;body.stepOffset=.25f;Physics.SyncTransforms();
 bool success=true;
 for(int i=1;i<points.Length;i++){
 for(int n=0;n<2000;n++){var delta=points[i]-go.transform.position;delta.y=0;if(delta.magnitude<.09f)break;body.Move(Vector3.ClampMagnitude(delta,.045f)+Vector3.down*.045f);}
 var remaining=points[i]-go.transform.position;remaining.y=0;
 if(remaining.magnitude>.15f||Mathf.Abs(points[i].y-go.transform.position.y)>.5f){success=false;if(name.Contains("corridor"))Snapshot(go.transform.position+Vector3.up*1.6f,points[i]+Vector3.up*1.6f,name);report.AppendLine("BLOCKED "+name+" at="+go.transform.position+" toward="+points[i]);foreach(var c in Physics.OverlapCapsule(go.transform.position+Vector3.up*.4f,go.transform.position+Vector3.up*1.45f,.5f,~0,QueryTriggerInteraction.Ignore).Where(c=>c!=body))report.AppendLine(" CONTACT "+MapCollisionAudit.PathOf(c.transform)+" "+c.bounds);break;}}
 if(success){passed++;report.AppendLine("PASS traversal "+name);}else blocked++;
 Object.DestroyImmediate(go);
 }
 static void Snapshot(Vector3 eye,Vector3 target,string name){var cam=new GameObject("Route view").AddComponent<Camera>();cam.transform.position=eye;cam.transform.LookAt(target);var rt=new RenderTexture(960,540,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/map-blocked-"+name.Replace(" ","-")+".png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(cam.gameObject);}
 static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("MapWalkTest",false);File.WriteAllText("RecoveryReports/map-walk-check.txt",report.ToString());EditorApplication.Exit(code);}
}




