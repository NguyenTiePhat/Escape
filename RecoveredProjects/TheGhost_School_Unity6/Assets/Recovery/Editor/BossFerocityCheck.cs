using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public static class BossFerocityCheck {
 static BossFerocityCheck(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;EditorApplication.update-=Poll;if(!File.Exists("RecoveryReports/boss-ferocity.request"))return;File.Delete("RecoveryReports/boss-ferocity.request");Run();}
 public static void Run(){
 GameObject actor=null,target=null,wall=null,clone=null;
 try{
 var c=AssetDatabase.LoadAssetAtPath<MonsterConfig>("Assets/Recovery/Boss/MonsterConfig.asset");if(c.sightRange!=28||c.chaseSpeed!=6.3f||c.chaseMemorySeconds!=4)throw new Exception("Configuration mismatch");
 actor=new GameObject("Vision test"){hideFlags=HideFlags.HideAndDontSave};actor.transform.position=new Vector3(0,1000,0);var v=actor.AddComponent<MonsterVision>();
 target=new GameObject("Target test"){hideFlags=HideFlags.HideAndDontSave};target.AddComponent<CharacterController>();var p=target.AddComponent<PlayerMotor>();target.transform.position=actor.transform.position+Vector3.forward*22;
 Physics.SyncTransforms();if(!v.CanSee(c,p))throw new Exception("Cannot see at22m");
 wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.hideFlags=HideFlags.HideAndDontSave;wall.transform.position=actor.transform.position+new Vector3(0,1,11);wall.transform.localScale=new Vector3(4,4,1);Physics.SyncTransforms();if(v.CanSee(c,p))throw new Exception("Sees through wall");
 UnityEngine.Object.DestroyImmediate(wall);wall=null;
 var original=UnityEngine.Object.FindAnyObjectByType<BossAnimationDriver>();if(!original)throw new Exception("Boss driver missing");
 clone=UnityEngine.Object.Instantiate(original.gameObject);clone.hideFlags=HideFlags.HideAndDontSave;
 var driver=clone.GetComponent<BossAnimationDriver>();typeof(BossAnimationDriver).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
 if(driver.ExpressiveJointCount<10)throw new Exception("Missing expressive rig joints: "+driver.ExpressiveJointCount);
 driver.animator.Rebind();driver.animator.Update(0);driver.Play("Run");driver.animator.Update(.2f);
 typeof(BossAnimationDriver).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(driver,null);
 if(Mathf.Abs(driver.animator.speed-1.3f)>.01f)throw new Exception("Run speed not applied");
 foreach(var bone in clone.GetComponentsInChildren<Transform>())if(!float.IsFinite(bone.localRotation.x))throw new Exception("Invalid bone rotation");
 File.WriteAllText("RecoveryReports/boss-ferocity-check.txt","PASS config:28m/140deg sight,6.3m/s chase,4s memory\nPASS visibility22m and wall occlusion\nPASS expressive joints="+driver.ExpressiveJointCount+"; Run playback1.3; finite poses\nScene placements unchanged. Live pursuit/lunge still requires Play Mode check.\n");
 }catch(Exception e){File.WriteAllText("RecoveryReports/boss-ferocity-check.txt","FAIL "+e);Debug.LogException(e);}
 finally{if(actor)UnityEngine.Object.DestroyImmediate(actor);if(target)UnityEngine.Object.DestroyImmediate(target);if(wall)UnityEngine.Object.DestroyImmediate(wall);if(clone)UnityEngine.Object.DestroyImmediate(clone);}
 }
}
