using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class M6EscapeSetup
{
 const string Root="Assets/Recovery/M6";
 static readonly string[] ids={"key_frame","metal_blank","saw_blade","saw_handle","screws","main_exit_key"};
 static readonly string[] models={"KeyFrame","MetalBlank","SawBlade","SawHandle","Screws","MainKey"};
 static readonly string[] names={"Khung in chìa khóa","Phôi kim loại","Lưỡi cưa","Tay cầm cưa","Ốc vít","Chìa khóa cửa chính"};
 static Dictionary<string,Material> materials=new Dictionary<string,Material>();
 public static void Run()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
  if(GameObject.Find("M6 Escape Items"))throw new Exception("M6 already placed; refusing to duplicate scene content");
  Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Items");Directory.CreateDirectory(Root+"/Prefabs");Directory.CreateDirectory(Root+"/Icons");AssetDatabase.Refresh();
  CreateMaterials();
  var items=new ItemData[6];
  for(int i=0;i<6;i++) {
   string path=Root+"/Items/"+ids[i]+".asset";var item=AssetDatabase.LoadAssetAtPath<ItemData>(path);
   if(!item){item=ScriptableObject.CreateInstance<ItemData>();AssetDatabase.CreateAsset(item,path);}
   item.id=ids[i];item.itemName=names[i];item.type=i==5?ItemType.Key:ItemType.PuzzleItem;item.stackable=false;item.maxStack=1;item.weight=i==2?.5f:.2f;
   item.description=i==0?"Khung kim loại cũ dùng để định hình chìa khóa tại máy.":i==1?"Phôi thép chưa gia công, dùng cùng khung in chìa khóa.":i==5?"Chìa khóa được gia công cho cửa chính.":"Một bộ phận để lắp cưa. Giữ lại để dùng ở bước sau.";
   var root=new GameObject(names[i]);var model=LoadModel(models[i]);model.transform.SetParent(root.transform,false);
   var bounds=BoundsOf(root);var collider=root.AddComponent<BoxCollider>();collider.center=bounds.center-root.transform.position;collider.size=Vector3.Max(bounds.size,Vector3.one*.015f);
   root.layer=LayerMask.NameToLayer("Interactable");var body=root.AddComponent<Rigidbody>();body.mass=item.weight;body.isKinematic=true;
   var pickup=root.AddComponent<PickupItem>();pickup.item=item;pickup.dropRadius=Mathf.Max(.06f,bounds.extents.magnitude);
   item.pickupPrefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+models[i]+".prefab").GetComponent<PickupItem>();
   RenderIcon(root,Root+"/Icons/"+ids[i]+".png");item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Icons/"+ids[i]+".png");EditorUtility.SetDirty(item);Object.DestroyImmediate(root);items[i]=item;
  }
  AssetDatabase.SaveAssets();
  if(!File.Exists("RecoveryReports/School_Test-before-M6-items.unity"))EditorSceneManager.SaveScene(scene,"RecoveryReports/School_Test-before-M6-items.unity",true);
  var motor=Object.FindAnyObjectByType<PlayerMotor>();Physics.SyncTransforms();
  if(!NavMesh.SamplePosition(motor.transform.position,out var start,2,NavMesh.AllAreas))throw new Exception("Player NavMesh unavailable");
  // Different existing map regions; every pickup point must have a reachable standing position.
  var regions=new[]{new Vector4(10,16,52,57),new Vector4(2,10,52,58),new Vector4(3,12,66,71),new Vector4(2,13,58,64),new Vector4(13,22,64,71)};
  var allPoints=new List<Vector3>();var points=new List<Vector3>[5];
  for(int i=0;i<5;i++) {
   points[i]=FindSurfaces(regions[i],start.position,allPoints);
   File.AppendAllText("RecoveryReports/m6-placement-debug.txt",ids[i]+" count="+points[i].Count+"\n");
   if(points[i].Count<2)throw new Exception("Not enough reachable spawn surfaces for "+ids[i]+": "+points[i].Count);
   allPoints.AddRange(points[i]);
  }
  Vector3 machinePosition=Vector3.zero;bool found=false;
  for(float x=14.5f;x>=10f&&!found;x-=.7f)for(float z=53.5f;z<=56&&!found;z+=.7f) {
   if(!NavMesh.SamplePosition(new Vector3(x,2,z),out var floor,.35f,NavMesh.AllAreas))continue;
   Vector3 p=floor.position;
   if(Physics.OverlapBox(p+Vector3.up*.7f,new Vector3(.64f,.66f,.44f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Length>0)continue;
   if(!CanReach(p+Vector3.forward*.9f,p+Vector3.up,start.position))continue;
   if(allPoints.Any(v=>Vector3.Distance(v,p+Vector3.up)<1.4f))continue;
   machinePosition=p;found=true;
  }
  if(!found)throw new Exception("No clear reachable machine footprint in West workshop room");
  var group=new GameObject("M6 Escape Items");var spawner=group.AddComponent<EscapeItemSpawner>();spawner.entries=new EscapeItemSpawner.Entry[5];
  var report=new System.Text.StringBuilder("PASS\n");
  for(int i=0;i<5;i++) {
   var area=new GameObject(ids[i]+" spawn points").transform;area.SetParent(group.transform);
   var transforms=new List<Transform>();foreach(var position in points[i]){var t=new GameObject("Candidate "+(transforms.Count+1)).transform;t.SetParent(area);t.position=position;transforms.Add(t);report.AppendLine(ids[i]+"="+position);}
   spawner.entries[i]=new EscapeItemSpawner.Entry{item=items[i],points=transforms.ToArray()};
  }
  var station=new GameObject("M6 Key Cutting Machine");var visual=LoadModel("KeyMachine");visual.transform.SetParent(station.transform,false);
  var stationBounds=BoundsOf(station);var box=station.AddComponent<BoxCollider>();box.center=stationBounds.center;box.size=stationBounds.size;station.layer=LayerMask.NameToLayer("Interactable");
  var hold=station.AddComponent<HoldNoiseAction>();hold.duration=10;hold.loudness=.9f;
  var noiseOrigin=new GameObject("Machine noise origin").transform;noiseOrigin.SetParent(station.transform,false);noiseOrigin.localPosition=new Vector3(0,.95f,.5f);hold.noiseOrigin=noiseOrigin;
  var craft=station.AddComponent<KeyCraftingStation>();craft.keyFrame=items[0];craft.metalBlank=items[1];craft.finishedKey=items[5];
  var tray=station.GetComponentsInChildren<Renderer>().First(r=>r.name.Contains("Receiving tray"));
  var output=new GameObject("Finished key pickup position").transform;output.SetParent(station.transform);output.position=new Vector3(tray.bounds.center.x,tray.bounds.max.y+.012f,tray.bounds.center.z);craft.outputPoint=output;
  // Separate colliders avoid covering the output key with one giant machine hitbox.
  Object.DestroyImmediate(box);
  foreach(var renderer in station.GetComponentsInChildren<MeshRenderer>()) {
   if(renderer.name.Contains("label")||renderer.name.Contains("slot"))continue;
   var col=renderer.gameObject.AddComponent<BoxCollider>();renderer.gameObject.layer=station.layer;
  }
  PrefabUtility.SaveAsPrefabAsset(station,Root+"/Prefabs/KeyMachine.prefab");station.transform.position=machinePosition;
  report.AppendLine("Machine West workshop room="+machinePosition+" duration=10 loudness=.9");
  // Make accented ingredient names available in the existing prompt font.
  var font=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Recovery/Items/InteractionFont.asset");
  if(font){font.TryAddCharacters(string.Join(" ",names)+" Giữ E để làm chìa khóa Thiếu Hoàn tất",out string missing);EditorUtility.SetDirty(font);foreach(var atlas in font.atlasTextures)EditorUtility.SetDirty(atlas);}
  M6NavigationSetup.Bake();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("RecoveryReports/m6-items-setup.txt",report.ToString());
  RenderMachine(station);
 }
 static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 static GameObject LoadModel(string name) {
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx");if(!asset)throw new Exception("Missing model "+name);
  var model=Object.Instantiate(asset);model.name=name;
  foreach(var r in model.GetComponentsInChildren<Renderer>()) {var ms=r.sharedMaterials;for(int i=0;i<ms.Length;i++){string n=ms[i]?ms[i].name.Split('.')[0]:"Steel";ms[i]=materials.ContainsKey(n)?materials[n]:materials["Steel"];}r.sharedMaterials=ms;}
  return model;
 }
 static List<Vector3> FindSurfaces(Vector4 region,Vector3 origin,List<Vector3> used) {
  var result=new List<Vector3>();
  for(float x=region.x;x<=region.y;x+=.65f)for(float z=region.z;z<=region.w;z+=.65f) {
   if(!Physics.Raycast(new Vector3(x,3.8f,z),Vector3.down,out var hit,1.45f,~0,QueryTriggerInteraction.Ignore)||hit.normal.y<.9f)continue;
   Vector3 point=hit.point+Vector3.up*.018f;
   if(used.Any(p=>Vector3.Distance(p,point)<1.8f)||result.Any(p=>Vector3.Distance(p,point)<.64f))continue;
   if(Physics.OverlapBox(point+Vector3.up*.07f,new Vector3(.27f,.05f,.12f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore).Length>0)continue;
   bool reachable=false;foreach(var offset in new[]{Vector3.forward,Vector3.back,Vector3.left,Vector3.right})if(CanReach(point+offset*.95f,point,origin)){reachable=true;break;}
   if(reachable)result.Add(point);
   if(result.Count==3)return result;
  }
  return result;
 }
 static bool CanReach(Vector3 near,Vector3 target,Vector3 origin) {
  near.y=2;
  if(!NavMesh.SamplePosition(near,out var stand,.5f,NavMesh.AllAreas)||Mathf.Abs(stand.position.y-2)>.5f)return false;
  var path=new NavMeshPath();if(!NavMesh.CalculatePath(origin,stand.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
  if(Physics.OverlapCapsule(stand.position+Vector3.up*.4f,stand.position+Vector3.up*1.5f,.3f,~0,QueryTriggerInteraction.Ignore).Any(c=>!(c is CharacterController)))return false;
  Vector3 eye=stand.position+Vector3.up*1.6f;Vector3 delta=target-eye;
  if(delta.magnitude>2.5f)return false;
  return !Physics.Raycast(eye,delta.normalized,delta.magnitude-.04f,~0,QueryTriggerInteraction.Ignore);
 }
 static void CreateMaterials() {
  var palettes=new Dictionary<string,Color>{{"Steel",new Color(.32f,.33f,.30f)},{"DarkSteel",new Color(.10f,.12f,.11f)},{"Paint",new Color(.20f,.28f,.22f)},{"Wood",new Color(.28f,.16f,.075f)},{"Brass",new Color(.48f,.35f,.15f)},{"Recess",new Color(.025f,.025f,.02f)}};
  foreach(var pair in palettes) {
   var texture=new Texture2D(256,256,TextureFormat.RGB24,false);
   for(int y=0;y<256;y++)for(int x=0;x<256;x++) {
    float n=Mathf.PerlinNoise(x*.11f,y*.11f),fine=Mathf.PerlinNoise(x*.8f,y*.8f),grain=Mathf.PerlinNoise(x*(pair.Key=="Wood"?.3f:.035f),y*(pair.Key=="Wood"?.012f:.035f));
    Color c=pair.Value*Mathf.Lerp(.55f,1.2f,n*.45f+grain*.45f+fine*.1f);
    if(pair.Key!="Wood"&&pair.Key!="Recess"&&grain>.62f)c=Color.Lerp(c,new Color(.23f,.105f,.045f),(grain-.62f)*2.2f);
    if(pair.Key=="Steel"&&fine>.7f)c*=1.3f;
    texture.SetPixel(x,y,c);
   }
   texture.Apply();string path=Root+"/Materials/"+pair.Key+"_wear.png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
   var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+pair.Key+".mat");if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Root+"/Materials/"+pair.Key+".mat");}
   material.color=Color.white;material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.SetFloat("_Metallic",pair.Key=="Wood"||pair.Key=="Recess"?0:pair.Key=="Paint"?.35f:.72f);material.SetFloat("_Glossiness",pair.Key=="Wood"?.12f:.24f);EditorUtility.SetDirty(material);materials[pair.Key]=material;
  }
 }
 static void RenderIcon(GameObject root,string path) {
  var b=BoundsOf(root);var camera=new GameObject("Icon camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.10f,.12f,.11f,1);camera.orthographic=true;camera.orthographicSize=b.size.magnitude*.60f;camera.transform.position=b.center+new Vector3(.3f,1,-.5f).normalized*3;camera.transform.LookAt(b.center);
  var light=new GameObject("Icon light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(40,-30,0);
  Capture(camera,path,256,256);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);
  AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
 }
 public static void RepairIcons() {
  foreach(var id in ids) {
   string path=Root+"/Icons/"+id+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
   var item=AssetDatabase.LoadAssetAtPath<ItemData>(Root+"/Items/"+id+".asset");item.icon=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(!item.icon)throw new Exception("Icon import failed: "+id);EditorUtility.SetDirty(item);
  }
  AssetDatabase.SaveAssets();
 }
 public static void Preview() { EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity"); RenderMachine(GameObject.Find("M6 Key Cutting Machine")); }
 static void RenderMachine(GameObject root) {
  var b=BoundsOf(root);var camera=new GameObject("Machine preview camera").AddComponent<Camera>();camera.transform.position=b.center+new Vector3(0,.8f,1.65f);camera.transform.LookAt(b.center);camera.nearClipPlane=.05f;
  var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Point;light.range=6;light.intensity=3;light.transform.position=b.center+new Vector3(0,2,1);
  Capture(camera,"RecoveryReports/m6-machine-preview.png",960,720);Object.DestroyImmediate(light.gameObject);Object.DestroyImmediate(camera.gameObject);
 }
 static void Capture(Camera camera,string path,int width,int height) {
  var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var texture=new Texture2D(width,height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(texture);rt.Release();Object.DestroyImmediate(rt);
 }
}
