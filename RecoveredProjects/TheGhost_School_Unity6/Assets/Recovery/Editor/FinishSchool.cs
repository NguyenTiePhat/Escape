using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class FinishSchool {
 static FinishSchool(){ EditorApplication.delayCall+=Auto; }
 static void Auto(){if(File.Exists("finish-school.request") && !EditorApplication.isPlayingOrWillChangePlaymode){File.Delete("finish-school.request"); Run();}}
 [MenuItem("Recovery/Finish and inspect school")]
 public static void Run(){
 if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
 var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity");
 var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 var log=new StringBuilder();
 // Restore only exact-name mesh matches; never guess geometry for unrelated objects.
 var meshes=AssetDatabase.FindAssets("t:Mesh").Select(g=>AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(g))).Where(m=>m).GroupBy(m=>m.name).ToDictionary(g=>g.Key,g=>g.ToArray());
 int restored=0;
 foreach(var f in all.SelectMany(t=>t.GetComponents<MeshFilter>()).Where(f=>!f.sharedMesh)) {
 string key=System.Text.RegularExpressions.Regex.Replace(f.name,@" \(\d+\)$","");
 if(meshes.TryGetValue(key,out var match)&&match.Length==1){f.sharedMesh=match[0];restored++;}
 }
 int mats=0;
 foreach(var group in all.SelectMany(t=>t.GetComponents<LODGroup>())){
 var lods=group.GetLODs();
 var donor=lods.SelectMany(l=>l.renderers).Where(r=>r&&r.sharedMaterials.Length>0&&r.sharedMaterials.All(m=>m)).FirstOrDefault();
 if(!donor)continue;
 foreach(var r in lods.SelectMany(l=>l.renderers).Where(r=>r&&r.sharedMaterials.Any(m=>!m))){
 var slots=r.sharedMaterials;
 if(slots.Length==donor.sharedMaterials.Length){for(int i=0;i<slots.Length;i++)if(!slots[i]){slots[i]=donor.sharedMaterials[i];mats++;}r.sharedMaterials=slots;}
 }
 }
 var warm=all.FirstOrDefault(t=>t.name=="WarmUpShaders" && !t.parent);if(warm)warm.gameObject.SetActive(false);
 var player=UnityEngine.Object.FindFirstObjectByType<SchoolExplorer>();
 Physics.SyncTransforms();
 var origin=new Vector3(27.01f,5.5f,57.61f);
 var hits=Physics.RaycastAll(origin,Vector3.down,8,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponent<SchoolExplorer>()&&h.normal.y>.65f&&h.point.y>1.5f&&h.point.y<4).OrderBy(h=>h.point.y).ToArray();
 var start=hits.Length>0?hits[0].point+Vector3.up*.12f:new Vector3(27.01f,2.12f,57.61f);
 player.GetComponent<CharacterController>().enabled=false;player.transform.position=start;player.home=start;player.GetComponent<CharacterController>().enabled=true;
 player.view.GetComponent<Light>().intensity=1.1f;
 log.AppendLine("Exact-name meshes restored: "+restored+"; material slots restored from same LOD group: "+mats);
 log.AppendLine("Spawn: "+start+"; verified ground hits: "+hits.Length);
 foreach(var hit in hits)log.AppendLine("Ground: "+hit.collider.name+" "+hit.point);
 log.AppendLine("Remaining empty mesh slots (including hidden/LOD): "+all.SelectMany(t=>t.GetComponents<MeshFilter>()).Count(f=>!f.sharedMesh));
 log.AppendLine("Remaining empty material slots: "+all.SelectMany(t=>t.GetComponents<Renderer>()).Sum(r=>r.sharedMaterials.Count(m=>!m)));
 EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/finish.txt",log.ToString());
 SchoolRecovery.Capture();
 if(!Application.isBatchMode){Selection.activeGameObject=UnityEngine.Object.FindFirstObjectByType<SchoolExplorer>().gameObject;EditorApplication.isPlaying=true;}
 }
}

