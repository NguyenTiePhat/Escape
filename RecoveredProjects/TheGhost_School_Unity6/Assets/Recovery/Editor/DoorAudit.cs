using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class DoorAudit {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 var s=new StringBuilder();
 foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.Contains("Door") && !t.name.Contains("mesh"))){
 s.AppendLine(t.name+" active="+t.gameObject.activeInHierarchy+" pos="+t.position+" rot="+t.eulerAngles+" scale="+t.lossyScale);
 foreach(var r in t.GetComponentsInChildren<Renderer>(true))s.AppendLine("  render "+r.name+" "+r.bounds+" localmesh="+r.GetComponent<MeshFilter>()?.sharedMesh?.bounds);
 foreach(var c in t.GetComponentsInChildren<Collider>(true))s.AppendLine("  collider "+c.name+" "+c.GetType().Name+" "+c.bounds);
 }
 File.WriteAllText("RecoveryReports/door-audit.txt",s.ToString());
 }
}
