using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class ValidateLockerDrawers {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 var root=GameObject.Find("Level/SchoolModel/Interior/Corridors/CorridorDetails/LockersSet3/HidingBox (7)/Interactive wooden drawers");
 var drawers=root.GetComponentsInChildren<SlidingDrawer>();
 var go=new GameObject("Drawer test viewer");var cam=go.AddComponent<Camera>();var inter=go.AddComponent<PlayerInteractor>();inter.view=cam;inter.interactableMask=LayerMask.GetMask("Interactable");
 string report="";
 foreach(var d in drawers){var box=d.GetComponent<BoxCollider>();Vector3 dir=d.transform.parent.TransformVector(d.localTravel).normalized;
 cam.transform.position=box.bounds.center+dir*.9f;cam.transform.LookAt(box.bounds.center);Physics.SyncTransforms();inter.RefreshTarget();if(!ReferenceEquals(inter.Target,d))throw new Exception("Raycast failed "+d.name+" target="+inter.Target);
 inter.HandleInteractionInput(true,false);for(int i=0;i<60;i++)d.Tick(1f/60);if(!d.IsOpen)throw new Exception("E did not open");
 cam.transform.position=box.bounds.center+dir*.9f;cam.transform.LookAt(box.bounds.center);Physics.SyncTransforms();inter.HandleInteractionInput(true,false);for(int i=0;i<60;i++)d.Tick(1f/60);if(d.IsOpen)throw new Exception("E did not close");report+=d.name+" E/raycast open-close PASS\n";}
 var renders=root.GetComponentsInChildren<Renderer>();Bounds b=renders[0].bounds;foreach(var r in renders){b.Encapsulate(r.bounds);r.gameObject.layer=30;}
 var direction=drawers[0].transform.parent.TransformVector(drawers[0].localTravel).normalized;
 cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.15f,.15f,.15f);cam.transform.position=b.center+direction*3+Vector3.Cross(direction,Vector3.up)*1.1f+Vector3.up*.6f;cam.transform.LookAt(b.center);cam.nearClipPlane=.02f;
 var light=new GameObject("test light").AddComponent<Light>();light.type=LightType.Point;light.intensity=3;light.range=10;light.transform.position=cam.transform.position;
 foreach(var d in drawers){d.Interact();for(int i=0;i<60;i++)d.Tick(1f/60);}
 var rt=new RenderTexture(800,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(800,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,800,900),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/drawers-open.png",tex.EncodeToPNG());
 File.WriteAllText("RecoveryReports/drawers-interaction-check.txt",report);EditorApplication.Exit(0);
 }
}
