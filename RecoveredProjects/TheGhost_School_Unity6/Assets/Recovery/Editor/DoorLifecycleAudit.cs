using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class DoorLifecycleAudit
{
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");var r=new StringBuilder();
 foreach(var d in Object.FindObjectsByType<Door>().Where(d=>!d.decorative)){
 r.AppendLine(MapCollisionAudit.PathOf(d.transform)+" pos="+d.transform.position.ToString("F3")+" local="+d.transform.localPosition.ToString("F3")+" scale="+d.transform.lossyScale+" hinge="+d.hingeOffset+" angle="+d.openAngle+" locked="+d.isLocked);
 r.AppendLine(" COMPONENTS "+string.Join(";",d.GetComponents<Component>().Select(c=>c?c.GetType().Name:"missing")));
 foreach(var b in d.GetComponentsInChildren<BoxCollider>())r.AppendLine(" BOX "+b.name+" center="+b.center+" size="+b.size+" world="+b.bounds);
 foreach(var a in d.GetComponentsInChildren<Animator>())r.AppendLine(" ANIMATOR "+a.name+" enabled="+a.enabled);
 foreach(var a in d.GetComponentsInChildren<Animation>())r.AppendLine(" ANIMATION "+a.name+" enabled="+a.enabled+" autoplay="+a.playAutomatically);
 }
 foreach(var e in Object.FindObjectsByType<ScareEvent>())r.AppendLine("SCARE "+e.name+" kind="+e.kind+" door="+(e.door?MapCollisionAudit.PathOf(e.door.transform):"none"));
 File.WriteAllText("RecoveryReports/door-lifecycle-audit.txt",r.ToString());EditorApplication.Exit(0);}
}
