using System;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class BossPoseReview {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 var driver=UnityEngine.Object.FindAnyObjectByType<BossAnimationDriver>();
 typeof(BossAnimationDriver).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
 driver.animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;driver.animator.Rebind();driver.animator.Update(0);
 var rs=driver.GetComponentsInChildren<Renderer>();Bounds b=rs[0].bounds;foreach(var r in rs){b.Encapsulate(r.bounds);r.gameObject.layer=30;}
 var cam=new GameObject("Pose camera").AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.16f,.17f);cam.nearClipPlane=.03f;cam.transform.position=driver.transform.position+Vector3.up*1.15f+driver.transform.forward*4+driver.transform.right*1.5f;cam.transform.LookAt(driver.transform.position+Vector3.up*1.15f);
 var light=new GameObject("Pose light").AddComponent<Light>();light.type=LightType.Point;light.intensity=3;light.range=15;light.transform.position=cam.transform.position;
 foreach(var action in new[]{"Run","Lunge"}){
 driver.Play(action);driver.animator.runtimeAnimatorController.animationClips.First(c=>c.name==action).SampleAnimation(driver.animator.gameObject,action=="Run"?.23f:.4f);typeof(BossAnimationDriver).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
 Bounds posedBounds = new Bounds(); bool hasBounds=false; foreach(var skin in driver.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=new Mesh();skin.BakeMesh(mesh);foreach(var v in mesh.vertices){var world=skin.transform.TransformPoint(v);if(!hasBounds){posedBounds=new Bounds(world,Vector3.zero);hasBounds=true;}else posedBounds.Encapsulate(world);}UnityEngine.Object.DestroyImmediate(mesh);} if(hasBounds){cam.transform.position=posedBounds.center+new Vector3(1.5f,.3f,4f).normalized*Mathf.Max(2f,posedBounds.size.magnitude*1.2f);cam.transform.LookAt(posedBounds.center);light.transform.position=cam.transform.position;} var rt=new RenderTexture(900,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/boss-ferocity-"+action+".png",tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);
 typeof(BossAnimationDriver).GetMethod("RestoreOffsets",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
 }
 File.WriteAllText("RecoveryReports/boss-pose-review.txt","Run and Lunge poses rendered from scene rig with new joint offsets. No scene changes saved.\n");EditorApplication.Exit(0);
 }
}



