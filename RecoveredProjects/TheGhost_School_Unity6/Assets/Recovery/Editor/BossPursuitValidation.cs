using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class BossPursuitValidation {
 static IEnumerator test;static double deadline;
 static BossPursuitValidation(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("BossPursuitTest",false)){test=Test();deadline=EditorApplication.timeSinceStartup+30;EditorApplication.update+=Tick;}};}
 public static void Run(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("BossPursuitTest",true);EditorApplication.EnterPlaymode();}
 static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout");if(test.MoveNext())return;Finish(0);}catch(Exception e){File.WriteAllText("RecoveryReports/boss-pursuit-check.txt","FAIL "+e);Finish(1);}}
 static void Finish(int c){EditorApplication.update-=Tick;SessionState.SetBool("BossPursuitTest",false);EditorApplication.Exit(c);}
 static IEnumerator Test(){yield return null;yield return null;
 var ai=UnityEngine.Object.FindAnyObjectByType<MonsterAI>();var player=UnityEngine.Object.FindAnyObjectByType<PlayerHealth>();var agent=ai.GetComponent<NavMeshAgent>();
 foreach(var mic in UnityEngine.Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
 if(!player.IsInvulnerable)player.ToggleInvulnerability();var motor=player.GetComponent<PlayerMotor>();motor.enabled=false;player.GetComponent<PlayerLook>().enabled=false;
 Vector3 dest=Vector3.zero;bool found=false;
 for(int i=0;i<16;i++){Vector3 dir=Quaternion.Euler(0,i*22.5f,0)*Vector3.forward;if(NavMesh.SamplePosition(ai.transform.position+dir*4,out var sample,.6f,NavMesh.AllAreas)&&!NavMesh.Raycast(ai.transform.position,sample.position,out _,NavMesh.AllAreas)){dest=sample.position;found=true;break;}}
 if(!found)throw new Exception("No clear chase lane");
 var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=dest+Vector3.up*.05f;cc.enabled=true;Physics.SyncTransforms();
 ai.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(dest-ai.transform.position,Vector3.up));
 var type=typeof(MonsterAI);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
 type.GetProperty("CurrentTarget").SetValue(ai,motor);type.GetProperty("CanSeePlayer").SetValue(ai,true);
 type.GetField("lastSeenPosition",flags).SetValue(ai,dest);type.GetField("lastSightAt",flags).SetValue(ai,Time.time);
 type.GetMethod("BeginChase",flags).Invoke(ai,null);
 int hits=0;ai.OnAttackHit+=p=>hits++;float maxSpeed=0;Vector3 attackStart=Vector3.zero;bool attacking=false;float lunge=0;
 double until=EditorApplication.timeSinceStartup+12;
 while(hits==0&&EditorApplication.timeSinceStartup<until){maxSpeed=Mathf.Max(maxSpeed,agent.speed);if(ai.CurrentState==MonsterState.Attack){if(!attacking){attackStart=ai.transform.position;attacking=true;}lunge=Mathf.Max(lunge,Vector3.Distance(attackStart,ai.transform.position));}yield return null;}
 if(hits==0||maxSpeed<6.2f)throw new Exception("Pursuit failed hits="+hits+" maxSpeed="+maxSpeed+" state="+ai.CurrentState);
 if(lunge<.02f)throw new Exception("No physical lunge displacement "+lunge);
 if(player.IsDead)throw new Exception("Test invulnerability failed");
 File.WriteAllText("RecoveryReports/boss-pursuit-check.txt","PASS real NavMesh pursuit from4m, maxSpeed="+maxSpeed+"\nPASS physical lunge travel="+lunge+"\nPASS attack hit event; invulnerable test player survives\nNo scene changes saved.\n");
 }
}
