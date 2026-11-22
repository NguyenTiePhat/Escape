using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class SetupLockerDrawers {
 static SetupLockerDrawers(){EditorApplication.update+=Poll;}
 public static void Batch(){File.Delete("RecoveryReports/drawers-pending");EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");Run();EditorApplication.Exit(0);}
static void Poll(){if(Application.isBatchMode)return;if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Poll;if(!File.Exists("RecoveryReports/drawers-pending"))return;File.Delete("RecoveryReports/drawers-pending");try{Run();}catch(Exception e){File.WriteAllText("RecoveryReports/drawers-error.txt",e.ToString());}}
 public static void Run(){
 var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/Recovery/Scenes/School_Test.unity")throw new Exception("School_Test must be active");
 var target=GameObject.Find("Level/SchoolModel/Interior/Corridors/CorridorDetails/LockersSet3/HidingBox (7)");
 var old=target.transform.Find("Wooden locker replacement");if(!old)throw new Exception("Replacement missing");
 if(target.transform.Find("Interactive wooden drawers"))throw new Exception("Already installed");
 File.Copy(scene.path,"RecoveryReports/School_Test-before-drawers.unity",true);
 var oldRender=old.GetComponentsInChildren<Renderer>();Bounds bounds=oldRender[0].bounds;foreach(var r in oldRender)bounds.Encapsulate(r.bounds);
 var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Recovery/WoodenLocker/LockerDrawers.fbx");if(!asset)throw new Exception("Import pending");
 var model=(GameObject)PrefabUtility.InstantiatePrefab(asset);model.name="Interactive wooden drawers";model.transform.SetParent(target.transform,false);
 // Preserve the previous model's occupied world volume.
 var rs=model.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
 model.transform.localScale=Vector3.Scale(model.transform.localScale,new Vector3(bounds.size.x/b.size.x,bounds.size.y/b.size.y,bounds.size.z/b.size.z));
 b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);model.transform.position+=bounds.center-b.center;
 var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovery/WoodenLocker/Locker.mat");
 var liner=new Material(Shader.Find("Standard"));liner.color=new Color(.12f,.085f,.055f);liner.SetFloat("_Glossiness",.1f);AssetDatabase.CreateAsset(liner,"Assets/Recovery/WoodenLocker/DrawerLining.mat");
 foreach(var r in rs)r.sharedMaterials=Enumerable.Repeat(r.name=="Carcass"||r.name=="DrawerLower"||r.name=="DrawerUpper"?mat:liner,Mathf.Max(1,r.sharedMaterials.Length)).ToArray();
 foreach(var c in target.GetComponentsInChildren<Collider>())c.enabled=false;
 old.gameObject.SetActive(false);
 var body=rs.First(r=>r.name=="Carcass");body.gameObject.AddComponent<MeshCollider>().sharedMesh=body.GetComponent<MeshFilter>().sharedMesh;
 string report="";
 foreach(string name in new[]{"DrawerLower","DrawerUpper"}){
 var r=rs.First(x=>x.name==name);var t=r.transform;var mf=r.GetComponent<MeshFilter>();var box=t.gameObject.AddComponent<BoxCollider>();box.center=mf.sharedMesh.bounds.center;box.size=mf.sharedMesh.bounds.size;
 t.gameObject.layer=LayerMask.NameToLayer("Interactable");
 var d=t.gameObject.AddComponent<SlidingDrawer>();d.cabinet=model.transform;
 Vector3 direction=r.bounds.center-body.bounds.center;direction.y=0;direction.Normalize();
 d.localTravel=t.parent.InverseTransformVector(direction*.45f);
 foreach(var child in t.GetComponentsInChildren<MeshFilter>())if(child!=mf)child.gameObject.AddComponent<BoxCollider>();
 d.Initialize();Vector3 start=t.localPosition;d.Interact();for(int i=0;i<90;i++)d.Tick(1f/60);float distance=Vector3.Distance(t.position,t.parent.TransformPoint(start));
 if(distance<.43f)throw new Exception(name+" blocked at "+distance);
 d.Interact();for(int i=0;i<90;i++)d.Tick(1f/60);if(Vector3.Distance(start,t.localPosition)>.001f)throw new Exception("Close failed");
 report+=name+" open/close PASS travel="+distance+" direction="+direction+"\n";
 }
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("RecoveryReports/drawers-check.txt",report);
 }
}

