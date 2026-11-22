using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class PickupOutlineValidation
{
 static IEnumerator test;static readonly StringBuilder report=new StringBuilder();
 static PickupOutlineValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PickupOutlineTest",false)){test=Test();EditorApplication.update+=Tick;}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("PickupOutlineTest",true);EditorApplication.EnterPlaymode();}
 static IEnumerator Test(){yield return null;yield return null;
 Directory.CreateDirectory("RecoveryReports/PickupOutline");
 foreach(var ai in Object.FindObjectsByType<MonsterAI>())ai.enabled=false;
 foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<PlayerLook>().enabled=false;
 var main=player.GetComponent<PlayerInteractor>().view;
 Check(main.GetComponent<PickupProximityOutline>(),"player camera automatically has outline component");
 Render(main,"game-start");
 var item=Object.FindAnyObjectByType<EscapeItemSpawner>().entries.Single(e=>e.item.id=="silver_key").item;
 var prop=Object.Instantiate(item.pickupPrefab,new Vector3(0,1000,0),Quaternion.identity);
 foreach(var t in prop.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
 var camera=new GameObject("Outline validation camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.08f);camera.cullingMask=1<<30;camera.nearClipPlane=.01f;camera.allowMSAA=false;
 var outline=camera.gameObject.AddComponent<PickupProximityOutline>();
 camera.transform.position=prop.transform.position+new Vector3(0,.8f,-1.3f);camera.transform.LookAt(prop.transform.position);
 int near=Render(camera,"near");Check(near>15&&outline.HighlightedCount==1,"near item draws white pixels="+near);
 camera.transform.position=prop.transform.position+new Vector3(0,2,-4);camera.transform.LookAt(prop.transform.position);
 Check(Render(camera,"far")==0&&outline.HighlightedCount==0,"beyond3m removes outline");
 camera.transform.position=prop.transform.position+new Vector3(0,.8f,-1.3f);camera.transform.LookAt(prop.transform.position);
 var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.layer=30;wall.transform.position=(camera.transform.position+prop.transform.position)*.5f;wall.transform.rotation=camera.transform.rotation;wall.transform.localScale=new Vector3(2,2,.08f);
 var mat=new Material(Shader.Find("Unlit/Color"));mat.color=new Color(.15f,.15f,.15f);wall.GetComponent<Renderer>().material=mat;
 Check(Render(camera,"blocked")==0,"opaque wall occludes outline");Object.DestroyImmediate(wall);
 outline.enabled=false;Check(Render(camera,"disabled")==0,"disable removes render commands");outline.enabled=true;Check(Render(camera,"enabled-again")>15,"reenable restores outline once");
 var bag=new GameObject("Test inventory").AddComponent<Inventory>();Check(prop.TryPickup(bag),"item collectible");Check(Render(camera,"collected")==0&&outline.HighlightedCount==0,"collection removes outline immediately");
 }
 static int Render(Camera camera,string name){var rt=new RenderTexture(960,540,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();int white=tex.GetPixels32().Count(c=>c.r>245&&c.g>245&&c.b>245);File.WriteAllBytes("RecoveryReports/PickupOutline/"+name+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);return white;}
 static void Check(bool condition,string label){if(!condition)throw new Exception(label);report.AppendLine("PASS "+label);}
 static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("PickupOutlineTest",false);File.WriteAllText("RecoveryReports/PickupOutline/check.txt",report.ToString());EditorApplication.Exit(code);}
}
