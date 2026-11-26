using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;
public static class DistributedPickupPlacement
{
 const int Visual=30,Actual=~(1<<Visual);
 static int[] approachStats=new int[4];
 static readonly HashSet<string> sightBlockers=new HashSet<string>();
 [Serializable] public class Candidate {public Vector3 position,approach;public string region,support;}
 [Serializable] public class Candidates {public List<Candidate> points=new List<Candidate>();}
 public static void Audit(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");PrepareSurfaceColliders();Scan();EditorApplication.Exit(0);}
 public static void Apply(){
 var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 const string backup="RecoveryReports/School_Test-before-distributed-pickups.unity";
 if(!File.Exists(backup))File.Copy(scene.path,backup);
 var spawner=Object.FindAnyObjectByType<EscapeItemSpawner>();
 var key=spawner.entries.Single(e=>e.item.id=="silver_key");
 var keyPositions=key.points.Select(t=>t.position).ToArray();
 var oldRoots=spawner.entries.SelectMany(e=>e.points).Where(t=>t&&t.parent&&t.parent.parent==spawner.transform).Select(t=>t.parent).Distinct().ToArray();
 PrepareSurfaceColliders();var candidates=Scan();
 if(candidates.points.Select(p=>p.region).Distinct().Count()!=5||candidates.points.GroupBy(p=>p.region).Any(g=>g.Count()<2))throw new Exception("Need five regions with multiple safe candidates");
 var root=new GameObject("Distributed reachable furniture locations").transform;root.SetParent(spawner.transform,false);
 var points=candidates.points.Select((p,i)=>Create(root,p,i)).ToArray();
 foreach(var e in spawner.entries)if(e!=key)e.points=points;
 var keyPoints=new List<Transform>();
 foreach(var p in keyPositions){
 if(!RebuildPickupPlacement.StandingView(p,out var eye))continue;
 keyPoints.Add(Create(root,new Candidate{position=p,approach=eye-Vector3.up*1.6f,region="Start",support="Starting room table"},keyPoints.Count));
 }
 if(keyPoints.Count<2)throw new Exception("Starting key requires at least two reachable positions");
 key.points=keyPoints.ToArray();
 foreach(var old in oldRoots)Object.DestroyImmediate(old.gameObject);
 EditorUtility.SetDirty(spawner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 File.WriteAllText("RecoveryReports/distributed-pickup-setup.txt","Saved "+points.Length+" ingredient candidates in5 regions, key candidates="+key.points.Length+". Each round:1 ingredient per region,2 upstairs and3 ground-floor; silver key stays accessible before unlocking doors. Three imported furniture colliders now match actual meshes; no furniture moved.\n");
 DistributedPickupValidation.Run();
 }
 static Transform Create(Transform parent,Candidate p,int index){var t=new GameObject(p.region+" furniture "+index).transform;t.SetParent(parent,false);t.position=p.position;var location=t.gameObject.AddComponent<EscapeSpawnLocation>();location.region=p.region;location.supportPath=p.support;location.approachPosition=p.approach;return t;}
 static void PrepareSurfaceColliders(){
 foreach(string name in new[]{"Gym - table tennis beside the court","Biology classroom - rear desk 1 facing board","Biology classroom - rear desk 2 facing board"}){
 var root=GameObject.Find("Drive furnishings - curated additions/"+name);if(!root)throw new Exception("Missing furniture "+name);
 foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
 if(!f.sharedMesh)continue;var c=f.GetComponent<MeshCollider>();if(!c)c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;c.convex=false;c.enabled=true;
 }
 var box=root.GetComponent<BoxCollider>();if(box)box.enabled=false;
 }
 Physics.SyncTransforms();
 }
 public static Candidates Scan(){
 var proxies=new GameObject("Temporary pickup surface audit");var meshes=new List<Mesh>();
 var surfaces=new List<(MeshRenderer renderer,MeshCollider proxy)>();var result=new Candidates();var report=new StringBuilder();
 try{
 foreach(var r in Object.FindObjectsByType<MeshRenderer>()){
 var f=r.GetComponent<MeshFilter>();if(!r.enabled||!f||!f.sharedMesh||r.bounds.size.magnitude>250)continue;
 var go=new GameObject(MapCollisionAudit.PathOf(r.transform));go.layer=Visual;go.transform.SetParent(proxies.transform,false);var mesh=f.sharedMesh;
 var so=new SerializedObject(r);int first=so.FindProperty("m_StaticBatchInfo.firstSubMesh").intValue,count=so.FindProperty("m_StaticBatchInfo.subMeshCount").intValue;
 if(count>0){mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(Enumerable.Range(first,count).Select(i=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=Matrix4x4.identity}).ToArray(),true,true);meshes.Add(mesh);}
 else{go.transform.SetPositionAndRotation(f.transform.position,f.transform.rotation);go.transform.localScale=f.transform.lossyScale;}
 var proxy=go.AddComponent<MeshCollider>();proxy.sharedMesh=mesh;
 string name=r.name+"/"+(r.transform.parent?r.transform.parent.name:"");
 if(Regex.IsMatch(name,"Desk|Table|Locker|Cabinet|Shelv|Cupboard|Counter|Comore|Bookcase|Worktop|Bench",RegexOptions.IgnoreCase)&&!Regex.IsMatch(name,"Chair|Door|Wall|Window|Books|Frame",RegexOptions.IgnoreCase))surfaces.Add((r,proxy));
 }
 Physics.SyncTransforms();var boss=Object.FindAnyObjectByType<MonsterAI>();NavMesh.SamplePosition(boss.transform.position,out var start,1,NavMesh.AllAreas);
 foreach(var surface in surfaces.OrderBy(s=>MapCollisionAudit.PathOf(s.renderer.transform))){
 var b=surface.renderer.bounds;if(b.size.x<.6f||b.size.z<.36f||b.size.x>8||b.size.z>8)continue;
 var accepted=new List<Vector3>();int hits=0,supports=0,clears=0;approachStats=new int[4];
 foreach(float floor in new[]{2f,8.5f,15f}){
 if(b.max.y<floor+.45f||b.min.y>floor+1.75f)continue;
 foreach(float x in Axis(b.min.x+.28f,b.max.x-.28f))foreach(float z in Axis(b.min.z+.16f,b.max.z-.16f)){
 var ray=new Ray(new Vector3(x,Mathf.Min(floor+1.75f,b.max.y+.1f),z),Vector3.down);
 if(!surface.proxy.Raycast(ray,out var hit,1.3f)||hit.normal.y<.98f||hit.point.y<floor+.45f)continue;
 var p=hit.point+Vector3.up*.018f;string region=Region(p);if(region==null||accepted.Any(a=>Vector3.Distance(a,p)<1f)||result.points.Any(a=>Vector3.Distance(a.position,p)<.8f))continue;hits++;
 bool clear=true;
 foreach(var o in new[]{Vector3.zero,new Vector3(.26f,0,.14f),new Vector3(-.26f,0,-.14f),new Vector3(.26f,0,-.14f),new Vector3(-.26f,0,.14f)}){
 if(!surface.proxy.Raycast(new Ray(p+o+Vector3.up*.04f,Vector3.down),out var visual,.1f)||visual.normal.y<.98f||Mathf.Abs(visual.point.y-hit.point.y)>.02f||
 !Physics.Raycast(p+o+Vector3.up*.04f,Vector3.down,out var physical,.12f,Actual,QueryTriggerInteraction.Ignore)||physical.normal.y<.98f||Mathf.Abs(physical.point.y-hit.point.y)>.035f){clear=false;break;}}
 if(!clear)continue;supports++;
 if(Physics.OverlapBox(p+Vector3.up*.08f,new Vector3(.28f,.065f,.16f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Length>0)continue;clears++;
 if(!Approach(p,floor,start.position,out var stand))continue;
 accepted.Add(p);result.points.Add(new Candidate{position=p,approach=stand,region=region,support=MapCollisionAudit.PathOf(surface.renderer.transform)});
 }
 }
 report.AppendLine("SURFACE "+MapCollisionAudit.PathOf(surface.renderer.transform)+" bounds="+b+" hits="+hits+" support="+supports+" clear="+clears+" accepted="+accepted.Count+" approach="+string.Join(",",approachStats));
 }
 static IEnumerable<float> Axis(float min,float max){yield return (min+max)*.5f;for(float p=min;p<=max;p+=.35f)if(Mathf.Abs(p-(min+max)*.5f)>.08f)yield return p;}
 foreach(var group in result.points.GroupBy(p=>p.region))report.AppendLine(group.Key+" candidates="+group.Count());
 foreach(var p in result.points)report.AppendLine(p.region+" "+p.position.ToString("F3")+" stand="+p.approach.ToString("F3")+" "+p.support);
 File.WriteAllText("RecoveryReports/distributed-pickup-candidates.txt",report.ToString());File.WriteAllText("RecoveryReports/distributed-pickup-candidates.json",JsonUtility.ToJson(result,true));return result;
 }finally{Object.DestroyImmediate(proxies);foreach(var m in meshes)Object.DestroyImmediate(m);}
 }
 static string Region(Vector3 p){if(p.y>14)return null;if(p.y>8)return p.x<50?"UpperWest":"UpperEast";if(p.x<20)return p.z<40?"GroundSouth":"GroundWest";if(p.x>45)return "GroundEast";return null;}
 static bool Approach(Vector3 p,float floor,Vector3 start,out Vector3 stand){
 foreach(float radius in new[]{.75f,1.05f,1.35f,1.65f,1.9f})for(int i=0;i<24;i++){
 var near=p+Quaternion.Euler(0,i*15,0)*Vector3.forward*radius;near.y=floor;
 if(!NavMesh.SamplePosition(near,out var nav,.4f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-floor)>.3f)continue;
 approachStats[0]++;
 if(Physics.OverlapCapsule(nav.position+Vector3.up*.38f,nav.position+Vector3.up*1.45f,.31f,Actual,QueryTriggerInteraction.Ignore).Any(c=>!(c is CharacterController)&&!c.GetComponentInParent<MonsterAI>()))continue;
 approachStats[1]++;
 var eye=nav.position+Vector3.up*1.6f;var delta=p+Vector3.up*.03f-eye;if(delta.magnitude>2.2f)continue;
 if(Physics.Raycast(eye,delta.normalized,out var obstruction,delta.magnitude-.04f,~0,QueryTriggerInteraction.Ignore)){
 if(p.x<0&&p.z<40&&sightBlockers.Add(obstruction.collider.name))File.AppendAllText("RecoveryReports/pickup-sight-rejections.txt",obstruction.collider.name+" layer="+obstruction.collider.gameObject.layer+" point="+p+" eye="+eye+" hit="+obstruction.point+"\n");continue;}
 approachStats[2]++;
 var path=new NavMeshPath();if(!NavMesh.CalculatePath(start,nav.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){if(p.x>45&&p.y<5)File.AppendAllText("RecoveryReports/pickup-path-rejections.txt",p+" stand="+nav.position+" last="+(path.corners.Length>0?path.corners.Last().ToString():"none")+"\n");continue;}
 approachStats[3]++;stand=nav.position;return true;
 }stand=default;return false;
 }
}
