using System;
using System.IO;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class CrouchPassageValidation
{
 static IEnumerator test; static readonly StringBuilder report=new StringBuilder();
 static CrouchPassageValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("CrouchPassageTest",false)){test=Test();EditorApplication.update+=Tick;}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("CrouchPassageTest",true);EditorApplication.EnterPlaymode();}
 static IEnumerator Test(){yield return null;yield return null;
 foreach(var ai in Object.FindObjectsByType<MonsterAI>())ai.enabled=false;
 foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 var motor=Object.FindAnyObjectByType<PlayerMotor>();motor.enabled=false;motor.GetComponent<PlayerLook>().enabled=false;
 foreach(var door in Object.FindObjectsByType<Door>())if(door.requiredKeyId=="silver_key"){door.UnlockDoor("silver_key");door.SetImmediate(true);}
 var cc=motor.GetComponent<CharacterController>();
 var start=new Vector3(8.1f,2.04f,55.85f);
 var route=new[]{new Vector3(21.1f,2,54.6f),new Vector3(21.45f,2,54.6f),new Vector3(23.4f,2,54.85f),new Vector3(28.45f,2,55.95f),new Vector3(28.95f,2,55.95f),new Vector3(44.6f,2,52.45f)};
 for(int posture=0;posture<2;posture++){
 cc.enabled=false;motor.transform.position=start;cc.enabled=true;Physics.SyncTransforms();motor.SetCrouching(posture==1);
 for(int i=0;i<40;i++)motor.Tick(Vector2.zero,false,.02f);
 bool reached=true;
 foreach(var target in route){for(int i=0;i<1200;i++){Vector3 delta=target-motor.transform.position;delta.y=0;if(delta.magnitude<.08f)break;motor.MoveGrounded(delta.normalized*1.8f,.02f);}if(Vector3.ProjectOnPlane(target-motor.transform.position,Vector3.up).magnitude>.15f){reached=false;break;}}
 report.AppendLine((posture==0?"Standing":"Crouching")+" ended="+motor.transform.position+" height="+cc.height+" reached="+reached);
 if(posture==1&&!reached)throw new Exception("Crouching passage traversal failed");
 } report.AppendLine("PASS physical CharacterController crosses low passage using existing PlayerMotor.SetCrouching (same posture as C). Silver doors opened in test memory. No geometry, gameplay settings or scene saved.");
 }
 static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
 static void Finish(int code){EditorApplication.update-=Tick;SessionState.SetBool("CrouchPassageTest",false);File.WriteAllText("RecoveryReports/crouch-passage-check.txt",report.ToString());EditorApplication.Exit(code);}
}

