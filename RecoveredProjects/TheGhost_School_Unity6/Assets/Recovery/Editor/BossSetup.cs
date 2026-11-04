using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class BossSetup {
 public static void Run(){try{
 const string scene="Assets/Recovery/Scenes/School_Test.unity";
 EditorSceneManager.OpenScene(scene);
 if(!File.Exists("RecoveryReports/School_Test-before-M4-boss.unity"))File.Copy(scene,"RecoveryReports/School_Test-before-M4-boss.unity");
 var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Recovery/Boss/M4Boss.fbx"); if(!model)throw new Exception("FBX not imported");
 var previous=GameObject.Find("M4 Boss"); if(previous)UnityEngine.Object.DestroyImmediate(previous);
 var root=new GameObject("M4 Boss");var visual=(GameObject)PrefabUtility.InstantiatePrefab(model);visual.transform.SetParent(root.transform,false);
 var renderers=visual.GetComponentsInChildren<Renderer>(true); if(renderers.Length==0)throw new Exception("No model renderers");
 Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds); if(b.size.y<.001f)throw new Exception("Invalid bounds");
 visual.transform.localScale*=2.2f/b.size.y;
 b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);visual.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
 root.transform.position=new Vector3(20,2.005f,65.5f);root.transform.rotation=Quaternion.Euler(0,180,0);
 foreach(var a in visual.GetComponentsInChildren<Animator>(true)){a.applyRootMotion=false;a.enabled=false;}
 foreach(var r in renderers){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){var original=mats[i];var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));mat.name="Boss_"+r.name+"_"+i;if(original){mat.color=original.color;if(original.mainTexture)mat.mainTexture=original.mainTexture;}else mat.color=new Color(.55f,.52f,.48f);string p="Assets/Recovery/Boss/"+mat.name.Replace(":","_").Replace("/","_")+".mat";p=AssetDatabase.GenerateUniqueAssetPath(p);AssetDatabase.CreateAsset(mat,p);mats[i]=mat;}r.sharedMaterials=mats;}
 PrefabUtility.SaveAsPrefabAsset(root,"Assets/Recovery/Boss/M4Boss.prefab");EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
 File.WriteAllText("RecoveryReports/M4-boss-setup.txt","PASS: imported FBX; placed M4 Boss at "+root.transform.position+"; height 2.2m; renderers="+renderers.Length+". No SkinnedMeshRenderer from old boss found in original recovered scene; old boss identity not confirmed. No AI added.");
 var camObj=new GameObject("Boss preview camera");var cam=camObj.AddComponent<Camera>();cam.transform.position=root.transform.position+new Vector3(0,1.4f,3);cam.transform.LookAt(root.transform.position+Vector3.up*1.1f);cam.nearClipPlane=.05f;var rt=new RenderTexture(800,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(800,800,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,800),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/M4-boss-preview.png",tex.EncodeToPNG());EditorApplication.Exit(0);
 }catch(Exception e){File.WriteAllText("RecoveryReports/M4-boss-setup.txt",e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}}
}

