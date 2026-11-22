using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class RebuildPickupPlacement
{
 const string Root="Assets/Recovery/";
 public static void Run(){
 var scene=EditorSceneManager.OpenScene(Root+"Scenes/School_Test.unity");
 var spawner=Object.FindAnyObjectByType<EscapeItemSpawner>();if(!spawner||spawner.entries.Length!=6)throw new Exception("Missing six item definitions");
 Physics.SyncTransforms();var report=new StringBuilder();var positions=new List<Vector3>();
 foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).OrderBy(r=>r.transform.position.x).ThenBy(r=>r.transform.position.z)){
 var path=PathOf(r.transform);
 if(!System.Text.RegularExpressions.Regex.IsMatch(path,"TeacherDesk|TeacherTable|ReadingTable|WorkTable|Bàn hướng dẫn",System.Text.RegularExpressions.RegexOptions.IgnoreCase))continue;
 var b=r.bounds;if(b.size.x<.55f||b.size.z<.35f||b.size.x>4||b.size.z>4||b.max.y<2.4f||b.max.y>10)continue;
 Vector3 point=new Vector3(b.center.x,b.max.y+.018f,b.center.z);
 if(positions.Any(p=>Vector3.Distance(p,point)<1.2f))continue;
 // Require a real rendered tabletop with broad physical support, not a thin shelf/book pile.
 bool supported=true;
 foreach(var offset in new[]{Vector3.zero,new Vector3(.23f,0,.09f),new Vector3(-.23f,0,-.09f),new Vector3(.23f,0,-.09f),new Vector3(-.23f,0,.09f)}){
 if(!Physics.Raycast(point+offset+Vector3.up*.08f,Vector3.down,out var hit,.15f,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.95f||Mathf.Abs(hit.point.y-b.max.y)>.035f){supported=false;break;}}
 if(!supported||Physics.OverlapBox(point+Vector3.up*.065f,new Vector3(.25f,.05f,.12f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Length>0)continue;
 if(!StandingView(point,out _))continue;
 positions.Add(point);report.AppendLine("Table "+path+" position="+point);
 }
 if(positions.Count<8)throw new Exception("Too few clear tabletop locations: "+positions.Count+"\n"+report);
 var newRoot=new GameObject("Visible tabletop pickup locations").transform;newRoot.SetParent(spawner.transform,false);
 var candidates=positions.Select((p,i)=>{var t=new GameObject("Clear tabletop "+(i+1)).transform;t.SetParent(newRoot,false);t.position=p;return t;}).ToArray();
 var keyEntry=spawner.entries.Single(e=>e.item.id=="silver_key");
 // Keep the key near the starting flashlight, where the player can see it immediately.
 var keyPoints=keyEntry.points.Where(t=>t).OrderBy(t=>Vector3.Distance(t.position,new Vector3(22.28f,2.88f,61.73f))).Take(3).Select(t=>t.position).ToArray();
 if(keyPoints.Length<2)throw new Exception("Starting key locations unavailable");
 keyEntry.points=keyPoints.Select((p,i)=>{var t=new GameObject("Starting table key "+i).transform;t.SetParent(newRoot,false);t.position=p;return t;}).ToArray();
 foreach(var entry in spawner.entries)if(entry!=keyEntry)entry.points=candidates;
 foreach(Transform child in spawner.transform.Cast<Transform>().ToArray())if(child!=newRoot)Object.DestroyImmediate(child.gameObject);
 Resize("SilverKey",1.35f);Resize("Screws",1.6f);
 foreach(var name in new[]{"RP_Silver","RP_Steel"}){var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"ReferenceProps/Materials/"+name+".mat");if(mat){mat.SetFloat("_Metallic",.3f);mat.SetFloat("_Glossiness",.28f);EditorUtility.SetDirty(mat);}}
 EditorUtility.SetDirty(spawner);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 report.AppendLine("Six pickups per round; silver key3 starting table candidates, other five items share "+candidates.Length+" clear tabletop candidates. ReferenceProps retained; no level geometry restored or removed.");
 File.WriteAllText("RecoveryReports/pickup-placement-rebuilt.txt",report.ToString());
 }
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static void Resize(string name,float scale){string path=Root+"ReferenceProps/Prefabs/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);try{root.transform.localScale=Vector3.one*scale;var box=root.GetComponent<BoxCollider>();root.GetComponent<PickupItem>().dropRadius=Mathf.Max(.06f,box.size.magnitude*.5f*scale);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
 public static bool StandingView(Vector3 target,out Vector3 eye){
 for(int k=0;k<24;k++){var near=target+Quaternion.Euler(0,k*15,0)*Vector3.forward*.85f-Vector3.up*.85f;
 if(!NavMesh.SamplePosition(near,out var stand,.5f,NavMesh.AllAreas)||target.y-stand.position.y<.45f||target.y-stand.position.y>1.25f)continue;
 if(Physics.OverlapCapsule(stand.position+Vector3.up*.4f,stand.position+Vector3.up*1.45f,.3f,~0,QueryTriggerInteraction.Ignore).Any(c=>!(c is CharacterController)))continue;
 eye=stand.position+Vector3.up*1.6f;var delta=target+Vector3.up*.03f-eye;
 if(!Physics.Raycast(eye,delta.normalized,delta.magnitude-.04f,~0,QueryTriggerInteraction.Ignore))return true;
 }eye=default;return false;
 }
}



