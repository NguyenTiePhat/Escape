using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MainExitSetup
{
 const string Root="Assets/Recovery/M6";
 public static void Run()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
  if(!File.Exists("RecoveryReports/School_Test-before-main-exit.unity"))EditorSceneManager.SaveScene(scene,"RecoveryReports/School_Test-before-main-exit.unity",true);
  var left=GameObject.Find("Gameplay/Doors/DoorMain_L");var right=GameObject.Find("Gameplay/Doors/DoorMain_R");
  if(!left||!right)throw new Exception("Verified main door leaves missing");
  var report=new StringBuilder();
  var frame=GameObject.Find("Level/SchoolModel/Doors/DoorMain");
  foreach(var collider in frame.GetComponents<BoxCollider>())if(collider.bounds.size.z>1f&&collider.bounds.size.y>2f){collider.enabled=false;report.AppendLine("Disabled obsolete solid blocker across main doorway; frame and leaf colliders retained.");}

  var winRoot=GameObject.Find("M6 Escape Game");if(!winRoot)winRoot=new GameObject("M6 Escape Game");
  var win=winRoot.GetComponent<EscapeWinController>();if(!win)win=winRoot.AddComponent<EscapeWinController>();
  Door a=SetupMain(left,1,win);Door b=SetupMain(right,-1,win);
  a.GetComponent<MainExitDoor>().partner=b;b.GetComponent<MainExitDoor>().partner=a;
  // Both leaves swing outward. Verify actual swept collision before committing the scene.
  a.UnlockDoor();b.UnlockDoor();a.Initialize(true);b.Initialize(true);
  bool beganA=a.Open(1),beganB=b.Open(1);
  for(int i=0;i<120;i++){a.Tick(1f/60);b.Tick(1f/60);}
  report.AppendLine("Main door sweep: "+beganA+"/"+beganB+" angles="+a.CurrentAngle+","+b.CurrentAngle+" blockers="+a.BlockingCollider+","+b.BlockingCollider);
  bool cleared=a.IsFullyOpen&&b.IsFullyOpen;
  a.SetImmediate(false);b.SetImmediate(false);a.LockDoor();b.LockDoor();
  if(!cleared){File.WriteAllText("RecoveryReports/main-exit-setup.txt","FAIL\n"+report);throw new Exception("Main door swing blocked; scene not saved");}

  foreach(var door in Object.FindObjectsByType<Door>(FindObjectsInactive.Include)) {
   if(door.decorative||door==a||door==b)continue;
   door.isLocked=true;door.requiredKeyId="silver_key";door.consumeKeyOnUnlock=false;
   door.lockedPrompt="Cửa đã khóa — chọn chìa bạc, nhấn E để mở.";EditorUtility.SetDirty(door);report.AppendLine("Silver door="+PathOf(door.transform));
  }
  var gold=AssetDatabase.LoadAssetAtPath<ItemData>(Root+"/Items/main_exit_key.asset");if(!gold||!gold.pickupPrefab)throw new Exception("Crafted key missing");
  gold.itemName="Chìa khóa vàng";gold.description="Chìa được chế tạo từ khuôn và phôi. Chỉ mở cửa thoát hiểm chính.";
  var goldMaterial=MakeMaterial("GoldKey", "Brass",new Color(1.45f,1.3f,.7f));
  var silverMaterial=MakeMaterial("SilverKey", "Steel",new Color(1.6f,1.6f,1.65f));
  var goldRoot=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(gold.pickupPrefab));
  foreach(var renderer in goldRoot.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(goldMaterial,renderer.sharedMaterials.Length).ToArray();
  PrefabUtility.SaveAsPrefabAsset(goldRoot,AssetDatabase.GetAssetPath(gold.pickupPrefab));PrefabUtility.UnloadPrefabContents(goldRoot);
  string itemPath=Root+"/Items/silver_key.asset";var silver=AssetDatabase.LoadAssetAtPath<ItemData>(itemPath);
  if(!silver){silver=ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(silver,itemPath);}
  silver.id="silver_key";silver.itemName="Chìa khóa bạc";silver.description="Mở các cửa thường trong trường. Không mở được cửa thoát hiểm chính.";silver.type=ItemType.Key;silver.stackable=false;silver.maxStack=1;silver.weight=.08f;
  var silverRoot=Object.Instantiate(gold.pickupPrefab.gameObject);silverRoot.name="Chìa khóa bạc";silverRoot.GetComponent<PickupItem>().item=silver;
  foreach(var renderer in silverRoot.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(silverMaterial,renderer.sharedMaterials.Length).ToArray();
  silver.pickupPrefab=PrefabUtility.SaveAsPrefabAsset(silverRoot,Root+"/Prefabs/SilverKey.prefab").GetComponent<PickupItem>();Object.DestroyImmediate(silverRoot);
  var existing=Object.FindObjectsByType<PickupItem>().FirstOrDefault(p=>p.item&&(p.item.id=="room_1_key"||p.item.id=="silver_key"));
  if(!existing)throw new Exception("No existing starting-room key placement found; refusing unknown placement");
  Vector3 position=existing.transform.position;Quaternion rotation=existing.transform.rotation;Object.DestroyImmediate(existing.gameObject);
  var placed=(GameObject)PrefabUtility.InstantiatePrefab(silver.pickupPrefab.gameObject);placed.name="Silver key - starting room table";placed.transform.SetPositionAndRotation(position,rotation);
  RenderKeyIcon(gold,Root+"/Icons/main_exit_key.png");RenderKeyIcon(silver,Root+"/Icons/silver_key.png");
  EditorUtility.SetDirty(gold);EditorUtility.SetDirty(silver);
  var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Recovery/Items/InteractionFont.asset");
  if(font){font.TryAddCharacters("Chìa khóa bạc vàng Cửa thoát hiểm chính chế tạo từ khuôn chọn nhấn để mở",out _);EditorUtility.SetDirty(font);foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);}
  var builds=EditorBuildSettings.scenes.ToList();if(!builds.Any(s=>s.path==scene.path))builds.Add(new EditorBuildSettingsScene(scene.path,true));else foreach(var entry in builds.Where(s=>s.path==scene.path))entry.enabled=true;EditorBuildSettings.scenes=builds.ToArray();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  report.AppendLine("Silver key at="+position+"; gold id=main_exit_key; win upon fully opening both verified main leaves; School_Test restart scene enabled.");
  File.WriteAllText("RecoveryReports/main-exit-setup.txt","PASS\n"+report);
 }
 static Door SetupMain(GameObject leaf,int side,EscapeWinController win) {
  var door=leaf.GetComponent<Door>();if(!door)door=leaf.AddComponent<Door>();var box=leaf.GetComponent<BoxCollider>();if(!box)throw new Exception("Main leaf collider missing");
  Physics.SyncTransforms();var before=box.bounds;
  var scale=leaf.transform.localScale;scale.y*=2.44f/before.size.y;leaf.transform.localScale=scale;
  Physics.SyncTransforms();leaf.transform.position+=new Vector3(0,before.center.y-box.bounds.center.y,Mathf.Max(0,63.78f-box.bounds.center.z));
  Physics.SyncTransforms();var bounds=box.bounds;var hinge=new Vector3(side>0?bounds.max.x:bounds.min.x,bounds.center.y,bounds.center.z);
  door.hingeOffset=leaf.transform.InverseTransformPoint(hinge);door.openAngle=side*90;door.speed=90;door.decorative=false;door.enabled=true;door.requiredKeyId="main_exit_key";door.consumeKeyOnUnlock=false;
  door.lockedPrompt="Cửa thoát hiểm đã khóa — cần chìa vàng chế tạo từ khuôn. Chọn chìa rồi nhấn E.";
  foreach(var collider in leaf.GetComponentsInChildren<Collider>()){collider.gameObject.layer=LayerMask.NameToLayer("Interactable");GameObjectUtility.SetStaticEditorFlags(collider.gameObject,0);}
  if(!leaf.GetComponent<DoorNoise>())leaf.AddComponent<DoorNoise>();
  var exit=leaf.GetComponent<MainExitDoor>();if(!exit)exit=leaf.AddComponent<MainExitDoor>();exit.game=win;
  return door;
 }
 static Material MakeMaterial(string name,string source,Color tint) {
  string path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!material){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+source+".mat"));AssetDatabase.CreateAsset(material,path);}material.color=tint;material.SetFloat("_Metallic",.72f);material.SetFloat("_Glossiness",.28f);EditorUtility.SetDirty(material);return material;
 }
 static void RenderKeyIcon(ItemData item,string path) {
  var model=Object.Instantiate(item.pickupPrefab.gameObject);model.transform.position=new Vector3(1000,1000,1000);
  var rs=model.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
  var camera=new GameObject("Key icon camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.10f,.09f);camera.orthographic=true;camera.orthographicSize=bounds.size.magnitude*.6f;camera.transform.position=bounds.center+new Vector3(.3f,1,-.5f);camera.transform.LookAt(bounds.center);
  var light=new GameObject("Key icon light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.transform.rotation=Quaternion.Euler(40,-30,0);
  var rt=new RenderTexture(256,256,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var texture=new Texture2D(256,256,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,256,256),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(texture);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(model);
  AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);
 }
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
}
