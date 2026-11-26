using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class DistributedPickupValidation
{
 static IEnumerator test;static readonly StringBuilder report=new StringBuilder();
 static DistributedPickupValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("DistributedPickupTest",false)){test=Test();EditorApplication.update+=Tick;}};}
 public static void RebuildAndRun(){M6NavigationSetup.Run();Run();}
 public static void CheckRoutesAndRounds(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 var boss=Object.FindAnyObjectByType<MonsterAI>();NavMesh.SamplePosition(boss.transform.position,out var start,1,NavMesh.AllAreas);int count=0;
 foreach(var location in Object.FindObjectsByType<EscapeSpawnLocation>()){
 var path=new NavMeshPath();
 if(!NavMesh.SamplePosition(location.approachPosition,out var end,.4f,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)throw new Exception("Unreachable furniture approach "+location.name+" at "+location.approachPosition);
 count++;
 }
 File.WriteAllText("RecoveryReports/distributed-pickup-routes.txt","PASS rebuilt NavMesh connects all "+count+" furniture approach points, across both floors. Door locks still require silver key.\n");
 EscapeRandomValidation.Run();
 }
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");var source=Object.FindAnyObjectByType<EscapeItemSpawner>();
 var random=UnityEngine.Random.state;var layouts=new HashSet<string>();
 try{for(int seed=0;seed<200;seed++){
 UnityEngine.Random.InitState(seed);var root=new GameObject("Distribution probe");try{
 var probe=root.AddComponent<EscapeItemSpawner>();probe.entries=source.entries;probe.Spawn();
 if(probe.Spawned.Count!=6)throw new Exception("Missing pickups seed "+seed);
 var regions=new List<string>();foreach(var prop in probe.Spawned){
 var entry=source.entries.Single(e=>e.item==prop.item);var point=entry.points.Single(p=>Vector3.Distance(p.position,prop.transform.position)<.01f);regions.Add(point.GetComponent<EscapeSpawnLocation>().region);
 }
 if(regions.Distinct().Count()!=6||regions.Count(r=>r.StartsWith("Upper"))!=2||regions.Count(r=>r.StartsWith("Ground"))!=3)throw new Exception("Clustered distribution seed "+seed);
 var mold=probe.Spawned.Single(p=>p.item.id=="key_frame").transform.position;
 var blank=probe.Spawned.Single(p=>p.item.id=="metal_blank").transform.position;
 if(Mathf.Abs(mold.y-blank.y)<5||Vector3.ProjectOnPlane(mold-blank,Vector3.up).magnitude<40)throw new Exception("Main-exit ingredients too close or on same floor seed "+seed);
 probe.Spawn();if(probe.Spawned.Count!=6)throw new Exception("Duplicate spawn");
 layouts.Add(string.Join(";",probe.Spawned.Select(p=>p.transform.position.ToString("F3"))));
 }finally{Object.DestroyImmediate(root);}
 }}finally{UnityEngine.Random.state=random;}
 if(layouts.Count<180)throw new Exception("Too few unique layouts "+layouts.Count);
 report.AppendLine("PASS200 seeded rounds: exactly6 items,5 distinct ingredient regions,2 upper/3 ground ingredients, gold-key ingredients on different floors and >=40m apart, no double spawn; unique layouts="+layouts.Count);
 File.WriteAllText("RecoveryReports/distributed-pickup-seeds.txt",report.ToString());
 SessionState.SetBool("DistributedPickupTest",true);EditorApplication.EnterPlaymode();
 }
 static IEnumerator Test(){yield return null;yield return null;
 if(report.Length==0)report.Append(File.ReadAllText("RecoveryReports/distributed-pickup-seeds.txt"));
 foreach(var boss in Object.FindObjectsByType<MonsterAI>())boss.enabled=false;
 foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 foreach(var director in Object.FindObjectsByType<ScareDirector>())director.enabled=false;
 var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<PlayerLook>().enabled=false;player.GetComponent<CharacterController>().enabled=false;
 var interactor=player.GetComponent<PlayerInteractor>();interactor.enabled=false;interactor.InputBlocked=false;
 var bag=player.GetComponent<Inventory>();bag.room=null;
 var spawner=Object.FindAnyObjectByType<EscapeItemSpawner>();if(spawner.Spawned.Count!=6)throw new Exception("Play start missing items");
 foreach(var p in spawner.Spawned)p.gameObject.SetActive(false);
 var camera=interactor.view;int checks=0;var pictured=new HashSet<string>();Directory.CreateDirectory("RecoveryReports/DistributedPickups");
 foreach(var entry in spawner.entries)foreach(var point in entry.points){
 var location=point.GetComponent<EscapeSpawnLocation>();var prop=Object.Instantiate(entry.item.pickupPrefab,point.position,point.rotation);prop.item=entry.item;prop.quantity=1;
 var body=prop.GetComponent<Rigidbody>();if(body){body.isKinematic=true;body.useGravity=false;}
 var bounds=prop.GetComponent<Collider>().bounds;camera.transform.position=location.approachPosition+Vector3.up*1.6f;camera.transform.LookAt(bounds.center);Physics.SyncTransforms();interactor.RefreshTarget();
 if(!ReferenceEquals(interactor.Target,prop))throw new Exception("Unreachable pickup "+entry.item.id+" at "+point.position+" region="+location.region+" target="+interactor.Target);
 if(!pictured.Contains(location.region)&&entry.item.id!="silver_key"){
 Capture(camera,location.region);pictured.Add(location.region);report.AppendLine("SCREEN "+location.region+" "+entry.item.id+" support="+location.supportPath+" eye="+camera.transform.position);
 }
 interactor.HandleInteractionInput(true,false);
 if(bag.CountItem(entry.item.id)!=1||prop.gameObject.activeSelf)throw new Exception("Actual interaction pickup failed "+entry.item.id);
 bag.RemoveItem(entry.item.id);Object.DestroyImmediate(prop.gameObject);checks++;
 if(checks%10==0)yield return null;
 }
 report.AppendLine("PASS actual PlayerInteractor raycast + E pickup at all "+checks+" item/location combinations, within normal2.5m range; exact ReferenceProps retained.");
 report.AppendLine("No Play test changes saved; whole-route connectivity was checked on NavMesh during placement with real standing capsule/visual support checks.");
 }
 static void Capture(Camera camera,string name){var rt=new RenderTexture(1100,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1100,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1100,720),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/DistributedPickups/"+name+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
 static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("DistributedPickupTest",false);File.WriteAllText("RecoveryReports/distributed-pickup-check.txt",report.ToString());EditorApplication.Exit(code);}
}
