using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MapCollisionAudit
{
 const int VisualLayer=30;
 const int ActualMask=~(1<<VisualLayer);
 public static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public static void RebuildAndAudit(){M6NavigationSetup.Run();Run();}
 public static void Run()
 {
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 var report=new StringBuilder();var proxies=new GameObject("Temporary visual geometry audit");
 int built=0,failed=0;
 foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
 var filter=renderer.GetComponent<MeshFilter>();if(!renderer.enabled||!filter||!filter.sharedMesh||renderer.bounds.size.magnitude>250)continue;
 try{
 var so=new SerializedObject(renderer);int first=so.FindProperty("m_StaticBatchInfo.firstSubMesh").intValue;int count=so.FindProperty("m_StaticBatchInfo.subMeshCount").intValue;
 var mesh=filter.sharedMesh;var go=new GameObject("Visual proxy");go.layer=VisualLayer;go.transform.SetParent(proxies.transform,false);
 if(count>0){var copy=new Mesh();copy.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;copy.CombineMeshes(Enumerable.Range(first,count).Select(i=>new CombineInstance{mesh=mesh,subMeshIndex=i,transform=Matrix4x4.identity}).ToArray(),true,true);mesh=copy;}
 else{go.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);go.transform.localScale=filter.transform.lossyScale;}
 go.AddComponent<MeshCollider>().sharedMesh=mesh;built++;
 }catch(Exception e){failed++;report.AppendLine("PROXY FAIL "+PathOf(renderer.transform)+" "+e.Message);}
 }
 Physics.SyncTransforms();report.AppendLine("Visual meshes="+built+" failures="+failed);
 var suspects=new Dictionary<Collider,List<Vector3>>();int floorCells=0,clearCells=0;
 foreach(float level in new[]{2f,8.5f,15f})for(float x=-20;x<=74;x+=.75f)for(float z=-2;z<=78;z+=.75f){
 if(!Physics.Raycast(new Vector3(x,level+.35f,z),Vector3.down,out var floor,.7f,ActualMask,QueryTriggerInteraction.Ignore)||floor.normal.y<.7f)continue;
 var p=floor.point+Vector3.up*.035f;floorCells++;
 var a=p+Vector3.up*.36f;var b=p+Vector3.up*1.45f;
 var blockers=Physics.OverlapCapsule(a,b,.32f,ActualMask,QueryTriggerInteraction.Ignore).Where(c=>!(c is CharacterController)&&!c.GetComponentInParent<Door>()&&!c.GetComponentInParent<MonsterAI>()&&!c.GetComponentInParent<PickupItem>()).ToArray();
 if(blockers.Length==0){clearCells++;continue;}
 if(Physics.OverlapCapsule(a,b,.30f,1<<VisualLayer,QueryTriggerInteraction.Ignore).Length>0)continue;
 foreach(var c in blockers){if(!suspects.TryGetValue(c,out var points))suspects[c]=points=new List<Vector3>();points.Add(p);}
 }
 report.AppendLine("Floor samples="+floorCells+" physically clear="+clearCells+" suspect colliders="+suspects.Count);
 foreach(var pair in suspects.OrderByDescending(p=>p.Value.Count))report.AppendLine("SUSPECT cells="+pair.Value.Count+" path="+PathOf(pair.Key.transform)+" type="+pair.Key.GetType().Name+" bounds="+pair.Key.bounds+" samples="+string.Join(";",pair.Value.Take(8).Select(v=>v.ToString("F2"))));
 File.WriteAllText("RecoveryReports/map-collision-audit.txt",report.ToString());
 Object.DestroyImmediate(proxies);
 }
}


