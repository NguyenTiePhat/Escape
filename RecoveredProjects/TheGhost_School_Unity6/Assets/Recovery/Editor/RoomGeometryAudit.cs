using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;
public static class RoomGeometryAudit {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity");
 var p=Object.FindFirstObjectByType<SchoolExplorer>();var s=new StringBuilder();s.AppendLine("PLAYER "+p.transform.position+" forward="+p.transform.forward);
 foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
 if(r.bounds.SqrDistance(p.transform.position)<80 && r.bounds.size.magnitude>2 && r.bounds.center.y<12){var f=r.GetComponent<MeshFilter>();s.AppendLine(r.name+" | "+r.bounds+" | mesh="+(f&&f.sharedMesh?f.sharedMesh.name:"NULL")+" | mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL"))+" | collider="+r.GetComponents<Collider>().Length);}}
 foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.bounds.SqrDistance(p.transform.position)<12))s.AppendLine("COL "+c.name+" "+c.bounds+" trigger="+c.isTrigger);
 File.WriteAllText("RecoveryReports/room-geometry.txt",s.ToString());
 }
}
