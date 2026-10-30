using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class ReworkOriginalRoom {
 static ReworkOriginalRoom(){EditorApplication.update+=Poll;}
 static void Poll(){string p="RecoveryReports/rework-original.request";if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}File.Delete(p);Run();}
 static Transform root;static Material wall;
 static GameObject Box(string n,Vector3 p,Vector3 s){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=s;g.GetComponent<Renderer>().sharedMaterial=wall;return g;}
 static void Run(){
 var scene=SceneManager.GetActiveScene();if(scene.name!="School_Test")return;
 if(GameObject.Find("START ROOM — original room corrections"))return;
 EditorSceneManager.SaveScene(scene,"RecoveryReports/School_Test-before-original-corrections.unity",true);
 root=new GameObject("START ROOM — original room corrections").transform;
 var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Wall01_mtl.mat");wall=source?new Material(source):new Material(Shader.Find("Standard"));wall.shader=Shader.Find("Standard");wall.SetFloat("_Glossiness",.08f);AssetDatabase.CreateAsset(wall,"Assets/Recovery/OriginalRoomWall.mat");
 // One cross-wall joins the existing north/south boundaries. No inner box room.
 Box("Partition south",new Vector3(24.5f,4.575f,58.045f),new Vector3(.2f,5.14f,2.33f));
 Box("Partition north",new Vector3(24.5f,4.575f,61.745f),new Vector3(.2f,5.14f,2.49f));
 Box("Exit lintel",new Vector3(24.5f,5.775f,59.9f),new Vector3(.2f,2.74f,1.4f));
 var door=GameObject.Find("Level/SchoolModel/Interior/Corridors/CorridorDetails/DoorNarrowSolid (6)");if(door){Undo.RecordObject(door,"Remove blocked decorative door");door.SetActive(false);}
 Box("Former blocked door - flush masonry",new Vector3(19.5f,4.575f,62.48f),new Vector3(1.9f,5.14f,.18f));
 Box("West south corner joint",new Vector3(16.98f,4.575f,56.98f),new Vector3(.16f,5.14f,.20f));
 Box("West north corner joint",new Vector3(16.98f,4.575f,62.4f),new Vector3(.16f,5.14f,.22f));
 var intro=Object.FindFirstObjectByType<StartRoom>();var table=intro.transform.Find("Bàn hướng dẫn");table.position=new Vector3(21.8f,2.005f,61.93f);table.rotation=Quaternion.identity;
 var lamp=intro.transform.Find("Ánh sáng bàn");if(lamp)lamp.position=new Vector3(21.8f,4.7f,61.4f);
 var player=Object.FindFirstObjectByType<SchoolExplorer>();var body=player.GetComponent<CharacterController>();body.enabled=false;player.home=new Vector3(21.8f,2.06f,60.6f);player.transform.position=player.home;player.transform.rotation=Quaternion.identity;body.enabled=true;player.view.transform.localRotation=Quaternion.Euler(25,0,0);
 Physics.SyncTransforms();var report=new StringBuilder();
 var probe=new GameObject("Door travel probe");probe.transform.position=new Vector3(22,2.06f,59.9f);var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.radius=.34f;cc.skinWidth=.025f;cc.stepOffset=.25f;Physics.SyncTransforms();for(int i=0;i<125;i++)cc.Move(new Vector3(.04f,-.035f,0));bool pass=probe.transform.position.x>26.8f;report.AppendLine("Exit traversal="+pass+" "+probe.transform.position);Object.DestroyImmediate(probe);
 foreach(var item in intro.GetComponentsInChildren<StartRoomItem>()){var d=item.transform.position-player.view.transform.position;var hits=Physics.RaycastAll(player.view.transform.position,d.normalized,2.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponentInParent<SchoolExplorer>()).OrderBy(h=>h.distance).ToArray();bool ok=hits.Length>0&&hits[0].collider.GetComponentInParent<StartRoomItem>()==item;pass &=ok;report.AppendLine(item.name+" reachable="+ok);}
 File.WriteAllText("RecoveryReports/original-rework.txt",report.ToString());if(!pass)throw new System.Exception("Original-room validation failed; not saved");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 var cam=player.view;var p=cam.transform.position;var q=cam.transform.rotation;var target=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(960,540,24);var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
 try{for(int i=0;i<3;i++){cam.transform.position=new Vector3(21.6f,3.65f,59.6f);cam.transform.rotation=Quaternion.Euler(10,i==0?0:i==1?90:-90,0);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/original-rework-"+i+".png",tex.EncodeToPNG());}}finally{cam.transform.position=p;cam.transform.rotation=q;cam.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
 }
}
