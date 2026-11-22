using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class PickupVisibilityCheck
{
 static IEnumerator test;static readonly StringBuilder report=new StringBuilder();
 static PickupVisibilityCheck(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PickupVisibility",false)){test=Test();EditorApplication.update+=Tick;}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("PickupVisibility",true);EditorApplication.EnterPlaymode();}
 static IEnumerator Test(){yield return null;yield return null;
 Directory.CreateDirectory("RecoveryReports/PickupVisibility");
 foreach(var ai in Object.FindObjectsByType<MonsterAI>())ai.enabled=false;
 foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<PlayerLook>().enabled=false;
 var camera=player.GetComponent<PlayerInteractor>().view;
 Capture(camera,"start");
 var spawner=Object.FindAnyObjectByType<EscapeItemSpawner>();
 report.AppendLine("State="+GameStateManager.Instance.State+" spawned="+spawner.Spawned.Count+" active="+spawner.gameObject.activeInHierarchy);
 if(spawner.Spawned.Count!=6)throw new Exception("Not six spawned pickups");
 foreach(var prop in spawner.Spawned){var b=prop.GetComponent<Collider>().bounds;var visual=prop.GetComponentsInChildren<Renderer>();
 report.AppendLine(prop.item.id+" pos="+prop.transform.position+" scale="+prop.transform.lossyScale+" collider="+b+" visual="+string.Join(";",visual.Select(r=>r.name+":"+r.enabled+":"+r.bounds)));
 bool seen=false;
 for(int k=0;k<24&&!seen;k++){var near=prop.transform.position+Quaternion.Euler(0,k*15,0)*Vector3.forward*.85f;near.y=prop.transform.position.y-.85f;
 if(!NavMesh.SamplePosition(near,out var stand,.5f,NavMesh.AllAreas))continue;
 var eye=stand.position+Vector3.up*1.6f;var dir=b.center-eye;
 if(Physics.Raycast(eye,dir.normalized,out var hit,2.5f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<PickupItem>()==prop){camera.transform.position=eye;camera.transform.LookAt(b.center);seen=true;}
 }
 if(!seen){camera.transform.position=b.center+new Vector3(0,1,-1);camera.transform.LookAt(b.center);report.AppendLine("Fallback view (not proven reachable)");}
 Capture(camera,prop.item.id);
 }
 }
 static void Capture(Camera cam,string name){var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/PickupVisibility/"+name+".png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(tex);rt.Release();Object.DestroyImmediate(rt);}
 static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("PickupVisibility",false);File.WriteAllText("RecoveryReports/PickupVisibility/report.txt",report.ToString());EditorApplication.Exit(code);}
}

