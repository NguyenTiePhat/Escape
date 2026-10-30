using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class CompactStartRoom {
 static CompactStartRoom(){EditorApplication.update+=Poll;}
 static void Poll(){const string p="RecoveryReports/compact-room.request";if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}File.Delete(p);Run();}
 static Transform root;
 static GameObject Box(string n,Vector3 p,Vector3 s,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
 [MenuItem("Recovery/Build compact starting room")]
 public static void Run(){
 var scene=SceneManager.GetActiveScene();if(scene.name!="School_Test")return;
 string backup="RecoveryReports/School_Test-before-compact-room.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
 if(GameObject.Find("START ROOM — compact walls"))throw new System.Exception("Compact room already exists; edit it instead of rebuilding.");
 root=new GameObject("START ROOM — compact walls").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Compact starting room");
 var mat=new Material(Shader.Find("Standard"));mat.color=new Color(.43f,.45f,.42f);mat.SetFloat("_Glossiness",.08f);AssetDatabase.CreateAsset(mat,"Assets/Recovery/CompactRoomWalls.mat");
 // Interior: x24.1..28.9, z58.1..61.9. West doorway clear width 1.4m, height 2.4m.
 Box("North wall",new Vector3(26.5f,3.505f,62),new Vector3(5.2f,3,.2f),mat);
 Box("South wall",new Vector3(26.5f,3.505f,58),new Vector3(5.2f,3,.2f),mat);
 Box("East wall",new Vector3(29,3.505f,60),new Vector3(.2f,3,4),mat);
 Box("West wall south jamb",new Vector3(24,3.505f,58.65f),new Vector3(.2f,3,1.3f),mat);
 Box("West wall north jamb",new Vector3(24,3.505f,61.35f),new Vector3(.2f,3,1.3f),mat);
 Box("Door lintel",new Vector3(24,4.705f,60),new Vector3(.2f,.6f,1.4f),mat);
 Box("Room ceiling",new Vector3(26.5f,5.065f,60),new Vector3(5.2f,.12f,4.2f),mat);
 var intro=Object.FindFirstObjectByType<StartRoom>();var table=intro.transform.Find("Bàn hướng dẫn");Undo.RecordObject(table,"Move table against north wall");table.position=new Vector3(27.2f,2.005f,61.55f);table.rotation=Quaternion.identity;
 var tableLight=intro.transform.Find("Ánh sáng bàn");if(tableLight)tableLight.position=new Vector3(27.2f,4.6f,61.1f);
 var lamp=new GameObject("Compact room light");lamp.transform.SetParent(root);lamp.transform.position=new Vector3(26.5f,4.65f,60);var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=7;light.intensity=1.7f;light.color=new Color(1,.91f,.78f);light.shadows=LightShadows.Soft;
 var player=Object.FindFirstObjectByType<SchoolExplorer>();var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=new Vector3(27.2f,2.06f,60.25f);player.home=player.transform.position;player.transform.rotation=Quaternion.identity;controller.enabled=true;player.view.transform.localRotation=Quaternion.Euler(25,0,0);
 Physics.SyncTransforms();var report=new StringBuilder();
 var probe=new GameObject("Movement validation");probe.transform.position=new Vector3(26.5f,2.06f,60);var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.radius=.34f;cc.skinWidth=.025f;cc.stepOffset=.25f;Physics.SyncTransforms();
 for(int i=0;i<100;i++)cc.Move(new Vector3(-.04f,-.035f,0));bool exit=probe.transform.position.x<22.7f;report.AppendLine("Doorway traversal: "+exit+" position="+probe.transform.position);Object.DestroyImmediate(probe);
 bool items=true;foreach(var item in intro.GetComponentsInChildren<StartRoomItem>()){var d=item.transform.position-player.view.transform.position;var hits=Physics.RaycastAll(player.view.transform.position,d.normalized,2.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponentInParent<SchoolExplorer>()).OrderBy(h=>h.distance).ToArray();bool ok=hits.Length>0&&hits[0].collider.GetComponentInParent<StartRoomItem>()==item;items &=ok;report.AppendLine(item.name+" reachable="+ok);}
 File.WriteAllText("RecoveryReports/compact-room.txt",report.ToString());if(!exit||!items)throw new System.Exception("Validation failed; inspect compact-room.txt before saving");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 var cam=player.view;var rt=RenderTexture.GetTemporary(960,540,24);var old=cam.targetTexture;var active=RenderTexture.active;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/compact-room.png",tex.EncodeToPNG());}finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
 }
}
