using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class OriginalRoomAudit {
 static OriginalRoomAudit(){EditorApplication.update+=Poll;}
 static void Poll(){string p="RecoveryReports/original-room.request";if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}File.Delete(p);Run();}
 static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
 static void Run(){
 var scene=SceneManager.GetActiveScene();if(scene.name!="School_Test")return;
 string backup="RecoveryReports/School_Test-before-original-room-rework.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
 var nested=GameObject.Find("START ROOM — compact walls");if(nested)Undo.DestroyObjectImmediate(nested);
 var s=new StringBuilder();
 var area=new Bounds(new Vector3(24.8f,4.5f,60),new Vector3(17,6,8));
 foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.bounds.Intersects(area))){s.AppendLine(Path(r.transform)+" bounds="+r.bounds+" position="+r.transform.position+" mesh="+(r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh.name:"none"));}
 File.WriteAllText("RecoveryReports/original-room-objects.txt",s.ToString());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 }
}
