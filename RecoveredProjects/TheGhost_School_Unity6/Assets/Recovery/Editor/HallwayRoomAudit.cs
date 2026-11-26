using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class HallwayRoomAudit
{
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");var r=new StringBuilder();
 foreach(var t in Object.FindObjectsByType<Transform>().Where(t=>t.name.StartsWith("HallwayRoom"))){r.AppendLine(MapCollisionAudit.PathOf(t)+" pos="+t.position);foreach(var c in t.GetComponentsInChildren<Collider>())r.AppendLine(" COLLIDER "+MapCollisionAudit.PathOf(c.transform)+" "+c.bounds);foreach(var m in t.GetComponentsInChildren<Renderer>())r.AppendLine(" VISUAL "+m.name+" "+m.bounds);}
 foreach(var d in Object.FindObjectsByType<Door>().Where(d=>!d.decorative))r.AppendLine("DOOR "+MapCollisionAudit.PathOf(d.transform)+" "+d.transform.position);
 var station=Object.FindAnyObjectByType<KeyCraftingStation>();r.AppendLine("STATION "+MapCollisionAudit.PathOf(station.transform)+" "+station.transform.position);foreach(var c in station.transform.root.GetComponentsInChildren<Collider>())r.AppendLine(" MACHINE "+MapCollisionAudit.PathOf(c.transform)+" "+c.bounds);
 foreach(var t in GameObject.Find("Gameplay/DoorNarrowP1_Locked (3)").GetComponentsInChildren<Transform>()){r.AppendLine("TARGET "+MapCollisionAudit.PathOf(t)+" pos="+t.position+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name)));foreach(var c in t.GetComponents<Collider>())r.AppendLine(" TARGET BOX "+c.bounds);}
 var cam=new GameObject("Room view").AddComponent<Camera>();cam.transform.position=new Vector3(44,4,55);cam.transform.LookAt(new Vector3(44,3,61));var light=cam.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=15;light.intensity=3;var rt=new RenderTexture(1000,750,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,750);tex.ReadPixels(new Rect(0,0,1000,750),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/hallway-room-view.png",tex.EncodeToPNG());
 File.WriteAllText("RecoveryReports/hallway-room-audit.txt",r.ToString());EditorApplication.Exit(0);}
}
