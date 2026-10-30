using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
public static class BuildStartRoom {
 static Transform table;
 static Material Mat(string name,Color color){string path="Assets/Recovery/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;return m;}
 static GameObject Box(string name,Vector3 p,Vector3 s,Material m){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(table,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=m;return o;}
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity");
 var player=Object.FindFirstObjectByType<SchoolExplorer>();
 var old=GameObject.Find("START ROOM — editable intro");if(old)Object.DestroyImmediate(old);
 var root=new GameObject("START ROOM — editable intro");var intro=root.AddComponent<StartRoom>();intro.view=player.view;
 table=new GameObject("Bàn hướng dẫn").transform;table.SetParent(root.transform);table.position=player.transform.position+player.transform.forward*1.2f-Vector3.up*.12f;table.rotation=player.transform.rotation;
 var wood=Mat("IntroWood",new Color(.24f,.14f,.08f));var dark=Mat("IntroMetal",new Color(.07f,.08f,.09f));var paper=Mat("IntroPaper",new Color(.85f,.8f,.65f));
 Box("Mặt bàn",new Vector3(0,.82f,0),new Vector3(1.25f,.08f,.65f),wood);
 foreach(float x in new[]{-.52f,.52f})foreach(float z in new[]{-.24f,.24f})Box("Chân bàn",new Vector3(x,.39f,z),new Vector3(.07f,.78f,.07f),dark);
 var note=Box("Tờ giấy — E để đọc",new Vector3(-.24f,.87f,0),new Vector3(.32f,.012f,.38f),paper);note.AddComponent<StartRoomItem>();
 for(int i=0;i<7;i++)Box("Dòng chữ",new Vector3(-.24f,.878f,-.13f+i*.037f),new Vector3(i==0?.20f:.24f,.002f,.006f),dark).GetComponent<Collider>().enabled=false;
 var flashlight=Box("Đèn pin — E để nhặt",new Vector3(.28f,.91f,0),new Vector3(.075f,.075f,.30f),dark);flashlight.AddComponent<StartRoomItem>().flashlight=true;intro.flashlightObject=flashlight;
 var head=Box("Đầu đèn pin",new Vector3(.28f,.91f,.16f),new Vector3(.11f,.11f,.065f),dark);head.transform.SetParent(flashlight.transform,true);
 var lens=Box("Kính đèn pin",new Vector3(.28f,.91f,.196f),new Vector3(.085f,.085f,.008f),paper);lens.transform.SetParent(flashlight.transform,true);
 var light=new GameObject("Ánh sáng bàn").AddComponent<Light>();light.transform.SetParent(root.transform);light.transform.position=table.position+Vector3.up*2.3f;light.type=LightType.Point;light.range=4;light.intensity=1.4f;light.color=new Color(1,.83f,.62f);
 player.view.transform.localRotation=Quaternion.Euler(25,0,0);
 EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
 // Verify interactions are reachable and both props are on the table.
 Physics.SyncTransforms();var report="Start room created. Note text serialized on StartRoom component.\n";
 foreach(var item in new[]{note,flashlight}){var direction=item.transform.position-player.view.transform.position;var hits=Physics.RaycastAll(player.view.transform.position,direction.normalized,2.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponentInParent<SchoolExplorer>()).OrderBy(h=>h.distance).ToArray();var hit=hits.Length>0?hits[0]:default(RaycastHit);bool ok=hit.collider&&hit.collider.GetComponentInParent<StartRoomItem>()==item.GetComponent<StartRoomItem>();report+=item.name+": reachable="+ok+" hit="+(hit.collider?hit.collider.name:"none")+" distance="+direction.magnitude+"\n";}
 Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/start-room.txt",report);Debug.Log(report);
 var cam=player.view;var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/start-room.png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;
 if(!Application.isBatchMode)EditorApplication.isPlaying=true;
 }
}


