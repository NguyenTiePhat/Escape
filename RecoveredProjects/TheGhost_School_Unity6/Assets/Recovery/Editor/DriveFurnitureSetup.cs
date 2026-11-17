using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class DriveFurnitureSetup {
 const string ScenePath="Assets/Recovery/Scenes/School_Test.unity";
 const string Root="Assets/Recovery/DriveFurniture";
 const string Report="RecoveryReports/DriveFurniture";
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static readonly string[] Models={"BanDanhBongBan","Tutrungbaydodungdiali","TuNhoo","BanGheHocSinh"};
 const string SceneRoot="Drive furnishings - curated additions";
 static bool rebuild;
 public static void Rebuild(){PreparePrefabs();rebuild=true;Build();}
 static void PreparePrefabs(){foreach(string name in Models){var wrapper=new GameObject(name);var model=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx"));model.name="Imported model";model.transform.SetParent(wrapper.transform,false);var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+name+".mat");foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"/Prefabs/"+name+".prefab");Object.DestroyImmediate(wrapper);}AssetDatabase.SaveAssets();}
 static readonly List<string> log=new List<string>();
 public static void ReviewClassrooms(){
  EditorSceneManager.OpenScene(ScenePath);var list=new List<string>();
  foreach(string room in new[]{"Classroom01","Classroom02"}){var t=GameObject.Find("Level/SchoolModel/Interior/"+room).transform;foreach(var f in t.GetComponentsInChildren<MeshFilter>(true)){var r=f.GetComponent<Renderer>();if(System.Text.RegularExpressions.Regex.IsMatch(f.name,"Student|Desk|Chair|Blackboard|Teacher|Bookcase"))list.Add(room+"/"+f.name+" pos="+f.transform.position.ToString("F2")+" rotation="+f.transform.eulerAngles.ToString("F1")+" scale="+f.transform.lossyScale.ToString("F2")+" bounds="+(r?r.bounds.ToString("F2"):"-"));}}
  File.WriteAllLines(Report+"/classroom-review.txt",list);
  Render("review-geography-room",new Vector3(50.8f,3.65f,72),new Vector3(61,3.1f,67.5f));
  Render("review-biology-room",new Vector3(68.5f,3.65f,50.5f),new Vector3(55,3.1f,46.5f));
 }
 public static void Build(){
  if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
  var scene=EditorSceneManager.OpenScene(ScenePath);var previous=GameObject.Find(SceneRoot);if(previous){if(!rebuild)throw new Exception("Drive furnishings already exist; refusing duplicates");Object.DestroyImmediate(previous);}
  const string backup="RecoveryReports/School_Test-before-drive-furnishing.unity";if(!File.Exists(backup))File.Copy(ScenePath,backup);
  var originals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).SelectMany(t=>new Object[]{t.gameObject}.Concat(t.GetComponents<Component>().Where(c=>c))).ToDictionary(o=>o,o=>EditorJsonUtility.ToJson(o));
  Physics.SyncTransforms();log.Clear();var upstairsBefore=Grid(28,40,173,137,8.476f);var gymBefore=Grid(-13,0,125,145,2.08f);var classroomBefore=Grid(49,41,65,45,1.98f);
  var root=new GameObject(SceneRoot);
  Place(root,"Gym - table tennis beside the court","BanDanhBongBan",new Vector3(-5.8f,2.08f,30),90,3.6f);
  Place(root,"Reading alcove - geography display","Tutrungbaydodungdiali",new Vector3(48.55f,8.476f,59.6f),-90,4.3f);
  Place(root,"Biology classroom - rear desk 1 facing board","BanGheHocSinh",new Vector3(62.8f,1.98f,43.1f),270,1.2f);
  Place(root,"Biology classroom - rear desk 2 facing board","BanGheHocSinh",new Vector3(62.8f,1.98f,45),270,1.2f);
  Place(root,"Window landing - maintenance cabinet","TuNhoo",new Vector3(50.65f,8.476f,48.4f),90,2.1f);
  Physics.SyncTransforms();
  CheckConnectivity(upstairsBefore,Grid(28,40,173,137,8.476f),173,Index(52,50,28,40,173),"Second floor");
  CheckConnectivity(gymBefore,Grid(-13,0,125,145,2.08f),125,Index(2,30,-13,0,125),"Gym");
  CheckConnectivity(classroomBefore,Grid(49,41,65,45,1.98f),65,Index(52,50,49,41,65),"Biology classroom");
  foreach(var pair in originals)if(!pair.Key||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Existing object changed: "+pair.Key);
  log.Add("PASS "+originals.Count+" pre-existing objects/components unchanged. Original materials, doors, gameplay and NavMesh assets preserved.");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllLines(Report+"/placement-check.txt",log);CaptureAfter();
 }
 static void Place(GameObject parent,string name,string model,Vector3 feet,float yaw,float scale){
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+model+".prefab");if(!prefab)throw new Exception("Missing prefab "+model);
  var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=name;go.transform.SetParent(parent.transform,false);var bounds=BoundsOf(go);
  go.transform.SetPositionAndRotation(feet-Vector3.up*bounds.min.y*scale,Quaternion.Euler(0,yaw,0));go.transform.localScale=Vector3.one*scale;
  var center=go.transform.TransformPoint(bounds.center);var half=bounds.extents*scale-new Vector3(.025f,.035f,.025f);
  var hits=Physics.OverlapBox(center,half,go.transform.rotation,~0,QueryTriggerInteraction.Ignore).Where(c=>c.enabled).ToArray();
  if(hits.Length>0){var error=name+" overlaps "+string.Join(",",hits.Select(c=>PathOf(c.transform)));File.WriteAllText(Report+"/placement-failure.txt",error);throw new Exception(error);}
  foreach(float dx in new[]{-.8f,.8f})foreach(float dz in new[]{-.8f,.8f}){var foot=go.transform.TransformPoint(new Vector3(bounds.center.x+bounds.extents.x*dx,bounds.min.y,bounds.center.z+bounds.extents.z*dz));if(!Physics.RaycastAll(foot+Vector3.up*.25f,Vector3.down,.5f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.7f&&Mathf.Abs(h.point.y-feet.y)<.08f))throw new Exception("No floor support: "+name+" corner="+foot);}
  var box=go.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
  var obstacle=go.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.center=bounds.center;obstacle.size=bounds.size;obstacle.carving=true;obstacle.carveOnlyStationary=true;
  Physics.SyncTransforms();var visual=BoundsOf(go);if(Vector3.Distance(visual.center,box.bounds.center)>.01f||Vector3.Distance(visual.size,box.bounds.size)>.02f)throw new Exception("Visual/collision size mismatch: "+name);log.Add("ADDED "+name+" source="+model+" feet="+feet+" yaw="+yaw+" scale="+scale+" bounds="+visual);
 }
 static bool[] Grid(float x0,float z0,int width,int height,float floor){var result=new bool[width*height];for(int z=0;z<height;z++)for(int x=0;x<width;x++){var p=new Vector3(x0+x*.25f,floor,z0+z*.25f);bool supported=Physics.RaycastAll(p+Vector3.up*.2f,Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.7f&&Mathf.Abs(h.point.y-floor)<.08f);result[z*width+x]=supported&&!Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.55f,.34f,~0,QueryTriggerInteraction.Ignore);}return result;}
 static int Index(float x,float z,float x0,float z0,int width)=>Mathf.RoundToInt((z-z0)*4)*width+Mathf.RoundToInt((x-x0)*4);
 static HashSet<int> Reach(bool[] cells,int width,int start){var visited=new HashSet<int>();var q=new Queue<int>();if(!cells[start])throw new Exception("Route origin is blocked: "+start);q.Enqueue(start);visited.Add(start);while(q.Count>0){int p=q.Dequeue();foreach(int n in new[]{p-1,p+1,p-width,p+width}){if(n<0||n>=cells.Length||Math.Abs(n%width-p%width)>1||!cells[n]||!visited.Add(n))continue;q.Enqueue(n);}}return visited;}
 static void CheckConnectivity(bool[] before,bool[] after,int width,int start,string name){var old=Reach(before,width,start);var current=Reach(after,width,start);int disconnected=old.Count(i=>after[i]&&!current.Contains(i));log.Add(name+": previously connected="+old.Count+" after="+current.Count+" newly isolated free samples="+disconnected);if(disconnected>0)throw new Exception("Furniture disconnected an existing route: "+name);}
 public static void VerifySaved(){
  EditorSceneManager.OpenScene(ScenePath);var root=GameObject.Find(SceneRoot);if(!root||root.transform.childCount!=5)throw new Exception("Expected five saved Drive furniture instances");
  foreach(var r in root.GetComponentsInChildren<Renderer>())if(!r.sharedMaterial||!r.sharedMaterial.mainTexture||!r.sharedMaterial.shader||!r.GetComponent<MeshFilter>().sharedMesh)throw new Exception("Missing imported render asset");
  var original=GameObject.Find("Floor 2 - XAPK furnishings");if(!original||original.GetComponentsInChildren<MeshRenderer>().Length!=17)throw new Exception("Original furnishings missing");
  var door=GameObject.Find("Gameplay/Doors/DoorNarrowP1/DoorNarrow_mesh.002").GetComponent<Door>();if(!door||!door.isLocked||door.requiredKeyId!="silver_key")throw new Exception("Library door key setup changed");
  var routes=new[]{new[]{new Vector3(52,8.53f,50),new Vector3(52,8.53f,45),new Vector3(52,8.53f,42.7f)},new[]{new Vector3(46.5f,8.53f,54),new Vector3(46.5f,8.53f,60.5f)},new[]{new Vector3(-3.5f,2.15f,27),new Vector3(-3.5f,2.15f,33),new Vector3(-7.5f,2.15f,33),new Vector3(-7.5f,2.15f,27)},new[]{new Vector3(63.9f,2.04f,42.4f),new Vector3(63.9f,2.04f,45)}};
  foreach(var route in routes){root.SetActive(false);Physics.SyncTransforms();bool before=Traverse(route);root.SetActive(true);Physics.SyncTransforms();bool after=Traverse(route);if(!before||!after)throw new Exception("Traversal check failed: before="+before+" after="+after+" route="+route[0]);File.AppendAllText(Report+"/placement-check.txt","PASS CharacterController route "+string.Join(" -> ",route.Select(p=>p.ToString("F2")))+"\n");}
  File.AppendAllText(Report+"/placement-check.txt","PASS saved scene readback: five Drive instances resolve; 17 previous furnishings and silver-key library door retained.\n");
 }
 static bool Traverse(Vector3[] route){var go=new GameObject("Temporary Drive placement traversal");go.transform.position=route[0];var cc=go.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.34f;cc.center=Vector3.up*.9f;cc.skinWidth=.025f;cc.stepOffset=.3f;bool pass=true;for(int i=1;i<route.Length;i++){for(int n=0;n<700;n++){var d=route[i]-go.transform.position;d.y=0;if(d.magnitude<.08f)break;cc.Move(Vector3.ClampMagnitude(d,.05f)+Vector3.down*.06f);}var remaining=route[i]-go.transform.position;remaining.y=0;if(remaining.magnitude>.1f){pass=false;break;}}Object.DestroyImmediate(go);return pass;}
 public static void CaptureAfter(){Render("after-gym",new Vector3(-1.8f,3.8f,33),new Vector3(-5.8f,2.7f,30));Render("after-display",new Vector3(44.7f,10.2f,57.2f),new Vector3(48.7f,9.3f,59.5f));Render("after-study",new Vector3(63.9f,3.7f,42.3f),new Vector3(54,3.3f,46));Render("after-cabinet",new Vector3(53.1f,10.2f,50.4f),new Vector3(50.5f,9.2f,48.4f));}
 static Bounds BoundsOf(GameObject g){var rs=g.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
 public static void PrepareMaterials(){
  Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Prefabs");
  foreach(string name in Models){
   for(int i=0;i<4;i++){string path=Root+"/Textures/"+name+"_0_"+i+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.maxTextureSize=i==0?2048:1024;imp.mipmapEnabled=true;imp.textureCompression=TextureImporterCompression.Compressed;imp.sRGBTexture=i==0;imp.textureType=i==1?TextureImporterType.NormalMap:TextureImporterType.Default;imp.SaveAndReimport();}
   string matPath=Root+"/Materials/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,matPath);}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+"_0_0.png");mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+"_0_1.png"));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_BumpScale",.65f);mat.SetFloat("_Metallic",.05f);mat.SetFloat("_Glossiness",.16f);EditorUtility.SetDirty(mat);
  }AssetDatabase.SaveAssets();PreparePrefabs();
 }
 public static void CandidatePreview(){
  PrepareMaterials();EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();
  var lines=new List<string>();
  foreach(var p in new[]{new Vector3(-10,2,18),new Vector3(64.1f,2,69),new Vector3(51.1f,2,44),new Vector3(51.1f,2,46.3f),new Vector3(53.9f,8.48f,45.3f)}){
   lines.Add("Candidate "+p);foreach(var c in Physics.OverlapBox(p+Vector3.up,Vector3.one,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))lines.Add(PathOf(c.transform)+" "+c.bounds.ToString("F2"));
  }File.WriteAllLines(Report+"/candidate-colliders.txt",lines);
  Render("before-gym",new Vector3(-5,3.7f,22),new Vector3(-10,2.6f,18));
  Render("before-geography",new Vector3(61,3.7f,66),new Vector3(64.1f,2.9f,69));
  Render("before-classroom",new Vector3(54,3.7f,48),new Vector3(51.1f,2.7f,44));
  Render("before-landing",new Vector3(51,10.2f,49),new Vector3(53.9f,9.3f,45.3f));
 }
 static void Render(string name,Vector3 position,Vector3 target){var go=new GameObject("Temporary Drive furniture preview");var cam=go.AddComponent<Camera>();go.transform.position=position;go.transform.LookAt(target);cam.nearClipPlane=.03f;cam.farClipPlane=100;cam.fieldOfView=70;var light=go.AddComponent<Light>();light.type=LightType.Point;light.range=22;light.intensity=2.3f;var rt=new RenderTexture(1280,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,800);tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes(Report+"/"+name+".png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
 public static void Survey(){
  var scene=EditorSceneManager.OpenScene(ScenePath);Physics.SyncTransforms();Directory.CreateDirectory(Report);
  var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  var lines=new List<string>();
  foreach(var t in all.Where(t=>System.Text.RegularExpressions.Regex.IsMatch(t.name,"Classroom|Geography|Biology|GymFloor|Gym$|MainOffice|SecurityRoom|Toilet|ComoreRoom|3rd Floor"))){var rs=t.GetComponentsInChildren<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();var b=rs.Length>0?rs[0].bounds:new Bounds(t.position,Vector3.zero);foreach(var r in rs)b.Encapsulate(r.bounds);lines.Add(PathOf(t)+" pos="+t.position.ToString("F2")+" bounds="+b.ToString("F2"));}
  File.WriteAllLines(Report+"/rooms.txt",lines);
  lines.Clear();
  for(float z=0;z<=74;z+=.5f)for(float x=-18;x<=72;x+=.5f){var p=new Vector3(x,2.3f,z);bool floor=Physics.RaycastAll(p,Vector3.down,.6f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.7f&&h.point.y>1.9f&&h.point.y<2.1f);bool block=Physics.CheckCapsule(new Vector3(x,2.4f,z),new Vector3(x,3.6f,z),.34f,~0,QueryTriggerInteraction.Ignore);lines.Add(x+","+z+","+(floor?1:0)+","+(block?1:0));}
  File.WriteAllLines(Report+"/ground-grid.csv",lines);
  foreach(string name in new[]{"BanDanhBongBan","Tutrungbaydodungdiali","TuNhoo","BanGheHocSinh"}){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/"+name+".fbx");if(!asset)continue;var go=Object.Instantiate(asset);var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);File.AppendAllText(Report+"/unity-models.txt",name+" bounds="+b.ToString("F4")+" materials="+string.Join(",",rs.SelectMany(r=>r.sharedMaterials).Select(m=>m?m.name:"NULL"))+"\n");Object.DestroyImmediate(go);}
 }
}


