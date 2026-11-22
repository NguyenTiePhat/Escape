using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ReplaceLocker3 {
 const string Root="Assets/Recovery/WoodenLocker";
 public static void Run(){
 var scene=Application.isBatchMode ? EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity") : UnityEngine.SceneManagement.SceneManager.GetActiveScene(); if(scene.path!="Assets/Recovery/Scenes/School_Test.unity")throw new Exception("Open School_Test first");
 var target=GameObject.Find("Level/SchoolModel/Interior/Corridors/CorridorDetails/LockersSet3/HidingBox (7)");
 if(!target)throw new Exception("Exact target missing");
 if(target.transform.Find("Wooden locker replacement"))throw new Exception("Already replaced");
 File.Copy(scene.path,"RecoveryReports/School_Test-before-wooden-locker.unity",true);
 var old=target.GetComponentsInChildren<Renderer>();
 if(old.Length==0)throw new Exception("No original renderers");
 Bounds bounds=old[0].bounds;foreach(var r in old)bounds.Encapsulate(r.bounds);
 if(bounds.size.magnitude>8)throw new Exception("Unexpected old bounds "+bounds);
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Locker.fbx");
 if(!prefab)throw new Exception("FBX not imported");
 var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.name="Wooden locker replacement";
 model.transform.SetParent(target.transform,false);
 var renders=model.GetComponentsInChildren<Renderer>();
 Bounds source=renders[0].bounds;foreach(var r in renders)source.Encapsulate(r.bounds);
 // Fit the replacement to the original occupied volume in world axes.
 model.transform.localScale=Vector3.Scale(model.transform.localScale,new Vector3(bounds.size.x/source.size.x,bounds.size.y/source.size.y,bounds.size.z/source.size.z));
 source=renders[0].bounds;foreach(var r in renders)source.Encapsulate(r.bounds);
 model.transform.position+=bounds.center-source.center;
 var mat=new Material(Shader.Find("Standard"));mat.name="Old wooden locker";
 mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Locker_Locker_BaseColor.png");
 string np=Root+"/Textures/Locker_Locker_Normal.png";
 var imp=(TextureImporter)AssetImporter.GetAtPath(np);imp.textureType=TextureImporterType.NormalMap;imp.SaveAndReimport();
 mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(np));mat.EnableKeyword("_NORMALMAP");mat.SetFloat("_Glossiness",.16f);mat.SetFloat("_Metallic",0);
 AssetDatabase.CreateAsset(mat,Root+"/Locker.mat");
 foreach(var r in renders)r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
 foreach(var r in old)r.enabled=false;
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 var report="Target="+target.name+" under LockersSet3\nOld bounds="+bounds+"\nNew bounds="+source+"\nOriginal renderers disabled="+old.Length+"; existing colliders/components retained\n";
 File.WriteAllText("RecoveryReports/wooden-locker.txt",report);
 var camObj=new GameObject("Preview camera");var cam=camObj.AddComponent<Camera>();cam.transform.position=bounds.center+new Vector3(2,1,-3).normalized*bounds.size.y*1.7f;cam.transform.LookAt(bounds.center);cam.nearClipPlane=.03f;
 var lightObj=new GameObject("Preview light");var light=lightObj.AddComponent<Light>();light.type=LightType.Point;light.range=10;light.intensity=2;light.transform.position=cam.transform.position;
 var rt=new RenderTexture(1000,1000,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1000,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1000,1000),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/wooden-locker.png",tex.EncodeToPNG());
 UnityEngine.Object.DestroyImmediate(camObj);UnityEngine.Object.DestroyImmediate(lightObj);
 if(Application.isBatchMode) EditorApplication.Exit(0);
 }
}

[InitializeOnLoad]
public static class Locker3Pending {
 static Locker3Pending(){EditorApplication.update+=Check;}
 static void Check(){
 if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
 EditorApplication.update-=Check;
 const string marker="RecoveryReports/locker3-pending";
 if(!File.Exists(marker))return;
 File.Delete(marker);
 try{ReplaceLocker3.Run();}catch(Exception e){File.WriteAllText("RecoveryReports/locker3-error.txt",e.ToString());Debug.LogException(e);}
 }
}