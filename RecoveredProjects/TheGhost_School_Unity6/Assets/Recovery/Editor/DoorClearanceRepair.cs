using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class DoorClearanceRepair
{
 public static void Run(){
 var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");var report=new StringBuilder();int changed=0,failed=0;
 foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None).Where(d=>!d.decorative&&d.requiredKeyId=="silver_key")){
 var pos=door.transform.position;var rotation=door.transform.rotation;float originalAngle=door.openAngle;bool locked=door.isLocked;
 var box=door.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c=>c.enabled&&!c.isTrigger);if(!box)continue;
 var bounds=box.bounds;Vector3 normal=bounds.size.x<bounds.size.z?Vector3.right:Vector3.forward;
 bool solved=false;Vector3 chosen=pos;float angle=originalAngle;float maximum=0;string blocker="";
 foreach(float offset in new[]{0f,.08f,-.08f,.16f,-.16f,.24f,-.24f,.32f,-.32f}){
 foreach(float sign in new[]{Mathf.Sign(originalAngle),-Mathf.Sign(originalAngle)}){
 door.SetImmediate(false);door.transform.SetPositionAndRotation(pos+normal*offset,rotation);door.openAngle=sign*90;door.UnlockDoor();door.Initialize(true);Physics.SyncTransforms();door.Open();for(int t=0;t<180;t++)door.Tick(1f/60);
 if(Mathf.Abs(door.CurrentAngle)>maximum){maximum=Mathf.Abs(door.CurrentAngle);blocker=door.BlockingCollider?MapCollisionAudit.PathOf(door.BlockingCollider.transform):"none";}
 if(door.IsFullyOpen){chosen=pos+normal*offset;angle=sign*90;solved=true;break;}
 }if(solved)break;
 }
 door.SetImmediate(false);door.transform.SetPositionAndRotation(solved?chosen:pos,rotation);door.openAngle=solved?angle:originalAngle;door.isLocked=locked;door.Initialize(true);Physics.SyncTransforms();
 if(solved){if(Vector3.Distance(chosen,pos)>.001f||angle!=originalAngle){changed++;EditorUtility.SetDirty(door);EditorUtility.SetDirty(door.transform);}report.AppendLine("PASS "+MapCollisionAudit.PathOf(door.transform)+" shift="+(chosen-pos)+" angle="+angle);}
 else{failed++;report.AppendLine("FAIL "+MapCollisionAudit.PathOf(door.transform)+" maximum="+maximum+" blocker="+blocker);}
 }
 File.WriteAllText("RecoveryReports/door-clearance-repair.txt","changed="+changed+" failed="+failed+"\n"+report);
 if(failed>0)throw new Exception("Unresolved door clearance; no scene saved");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
}
