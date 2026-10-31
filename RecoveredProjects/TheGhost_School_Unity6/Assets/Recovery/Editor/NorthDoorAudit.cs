using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
[UnityEditor.InitializeOnLoad] public static class NorthDoorAudit {
 static NorthDoorAudit(){UnityEditor.EditorApplication.update+=Poll;}
 static void Poll(){const string p="RecoveryReports/north-door.request"; if(!File.Exists(p)||UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating)return; if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode){UnityEditor.EditorApplication.isPlaying=false;return;} File.Delete(p); Run();}
 public static void Run(){
 if(!Object.FindFirstObjectByType<SchoolExplorer>())return;
 Physics.SyncTransforms(); var s=new StringBuilder();
 for(float x=18.5f;x<=20.5f;x+=.5f) for(float y=2.2f;y<=4.4f;y+=1f)
 foreach(var h in Physics.RaycastAll(new Vector3(x,y,60),Vector3.forward,8,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))s.AppendLine("RAY "+x+","+y+" "+h.point+" "+Path(h.transform)+" "+h.collider.GetType().Name+" "+h.collider.bounds);
 foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>c.enabled&&c.bounds.Intersects(new Bounds(new Vector3(19.5f,3.4f,63.5f),new Vector3(2,2.6f,4)))))s.AppendLine("BLOCK "+Path(c.transform)+" "+c.GetType().Name+" "+c.bounds);
 File.WriteAllText("RecoveryReports/north-door-audit.txt",s.ToString());
 }
 static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
}


