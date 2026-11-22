using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class DoorMountRepair
{
 public static void Run(){
 var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 const string backup="RecoveryReports/School_Test-before-door-lifecycle-repair.unity";
 if(!File.Exists(backup))File.Copy(scene.path,backup);
 var baseline=File.ReadAllText("RecoveryReports/School_Test-before-map-collision-audit.unity");
 var report=new StringBuilder();int failed=0;
 foreach(var door in Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&d.requiredKeyId=="silver_key")){
 var box=door.GetComponent<BoxCollider>();if(!box)throw new Exception("Expected leaf box "+door.name);
 var id=GlobalObjectId.GetGlobalObjectIdSlow(door.transform).targetObjectId;
 var block=Regex.Match(baseline,@"(?ms)^--- !u!4 &"+id+@"\r?\n.*?(?=^--- !u!|\z)");
 if(!block.Success)throw new Exception("Original mount missing "+door.name);
 var p=Regex.Match(block.Value,@"m_LocalPosition: \{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}");
 var originalLocal=new Vector3(Parse(p.Groups[1].Value),Parse(p.Groups[2].Value),Parse(p.Groups[3].Value));
 var before=door.transform.position;door.transform.localPosition=originalLocal;
 var rotation=door.transform.localRotation;bool locked=door.isLocked;float initialAngle=door.openAngle;
 var center=box.center;var half=box.size*.5f;float edge=center.x-half.x;
 if(Mathf.Abs(center.x+half.x)<Mathf.Abs(edge))edge=center.x+half.x;
 float inward=Mathf.Sign(center.x-edge);var scale=door.transform.lossyScale;
 bool solved=false;Vector3 hinge=door.hingeOffset;float chosen=initialAngle,best=0;string blocker="";
 foreach(float inset in new[]{0f,.02f,.04f,.06f,.08f}){
 foreach(float face in new[]{-1f,1f,0f}){
 foreach(float sign in new[]{Mathf.Sign(initialAngle),-Mathf.Sign(initialAngle)}){
 door.SetImmediate(false);door.transform.localPosition=originalLocal;door.transform.localRotation=rotation;
 door.hingeOffset=new Vector3(edge+inward*inset/Mathf.Abs(scale.x),0,center.z+face*half.z);
 door.openAngle=sign*90;door.isLocked=false;door.Initialize(true);door.Open();door.Tick(3);
 if(Mathf.Abs(door.CurrentAngle)>best){best=Mathf.Abs(door.CurrentAngle);blocker=door.BlockingCollider?MapCollisionAudit.PathOf(door.BlockingCollider.transform):"none";}
 if(door.IsFullyOpen){hinge=door.hingeOffset;chosen=door.openAngle;solved=true;break;}
 }if(solved)break;}if(solved)break;}
 door.SetImmediate(false);door.transform.localPosition=originalLocal;door.transform.localRotation=rotation;
 if(solved){door.hingeOffset=hinge;door.openAngle=chosen;}door.isLocked=locked;door.Initialize(true);
 report.AppendLine((solved?"PASS ":"FAIL ")+MapCollisionAudit.PathOf(door.transform)+" restored shift="+(door.transform.position-before)+" hinge="+door.hingeOffset.ToString("F6")+" angle="+door.openAngle+" max="+best+" blocker="+blocker+" mesh="+door.GetComponent<MeshFilter>().sharedMesh.bounds+" box="+box.size.ToString("F6"));
 if(!solved)failed++;
 }
 File.WriteAllText("RecoveryReports/door-mount-repair.txt",report.ToString());
 if(failed>0)throw new Exception("Unresolved door mounts: "+failed+"; no scene saved");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorApplication.Exit(0);
 }
 static float Parse(string value)=>float.Parse(value,CultureInfo.InvariantCulture);
}
