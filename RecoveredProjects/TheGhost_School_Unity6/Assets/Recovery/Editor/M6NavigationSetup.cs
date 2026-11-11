using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

public static class M6NavigationSetup
{
 static bool ValidBounds(Bounds b) => b.center.x > -80 && b.center.x < 130 && b.center.z > -40 && b.center.z < 150 && b.center.y > -20 && b.center.y < 50 && b.size.x < 200 && b.size.y < 80 && b.size.z < 200;
 public static void Run()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
  if(!File.Exists("RecoveryReports/School_Test-before-M6-navigation.unity"))EditorSceneManager.SaveScene(scene,"RecoveryReports/School_Test-before-M6-navigation.unity",true);
  Bake();
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
 }
 public static void Bake()
 {
  Physics.SyncTransforms();
  var marks=new List<NavMeshBuildMarkup>();
  foreach(var door in Object.FindObjectsByType<Door>())if(!door.decorative)marks.Add(new NavMeshBuildMarkup{root=door.transform,ignoreFromBuild=true});
  foreach(var player in Object.FindObjectsByType<PlayerMotor>())marks.Add(new NavMeshBuildMarkup{root=player.transform,ignoreFromBuild=true});
  foreach(var monster in Object.FindObjectsByType<MonsterAI>())marks.Add(new NavMeshBuildMarkup{root=monster.transform,ignoreFromBuild=true});
  foreach(var pickup in Object.FindObjectsByType<PickupItem>())marks.Add(new NavMeshBuildMarkup{root=pickup.transform,ignoreFromBuild=true});
  foreach(var pickup in Object.FindObjectsByType<StartRoomItem>())marks.Add(new NavMeshBuildMarkup{root=pickup.transform,ignoreFromBuild=true});
  var colliders=Object.FindObjectsByType<Collider>().Where(c=>c.enabled&&!c.isTrigger&&!(c is CharacterController)&&ValidBounds(c.bounds)).ToArray();
  var bounds=colliders[0].bounds;foreach(var c in colliders)bounds.Encapsulate(c.bounds);bounds.Expand(2);
  var sources=new List<NavMeshBuildSource>();NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,marks,sources);
  sources.RemoveAll(s=>s.component is Collider c && !ValidBounds(c.bounds));
  var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.42f;settings.agentHeight=2.2f;settings.agentClimb=.3f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.07f;settings.minRegionArea=1;
  var baked=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);if(!baked)throw new Exception("NavMesh build failed");
  var loader=Object.FindAnyObjectByType<MonsterNavMesh>();if(!loader)throw new Exception("Missing NavMesh loader");
  loader.enabled=false;var old=loader.data;
  var temporary=NavMesh.AddNavMeshData(baked);
  var boss=Object.FindAnyObjectByType<MonsterAI>();var playerMotor=Object.FindAnyObjectByType<PlayerMotor>();
  var report=new System.Text.StringBuilder("Sources="+sources.Count+" bounds="+bounds+"\n");
  bool pass=NavMesh.SamplePosition(boss.transform.position,out var bossPoint,1,NavMesh.AllAreas)&&NavMesh.SamplePosition(playerMotor.transform.position,out _,1,NavMesh.AllAreas);
  foreach(var target in new[]{playerMotor.transform.position,new Vector3(13.5f,2,53.8f),new Vector3(5.46f,2,60.5f),new Vector3(8.87f,2,68.8f),new Vector3(18.5f,2,68)}) {
   bool sample=NavMesh.SamplePosition(target,out var point,1,NavMesh.AllAreas);var path=new NavMeshPath();bool reachable=sample&&NavMesh.CalculatePath(bossPoint.position,point.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
   report.AppendLine(target+" => "+reachable+" "+path.status+" corners="+string.Join(";",path.corners.Select(c=>c.ToString("F2"))));pass &=reachable;
   if(!reachable && path.corners.Length>0)foreach(var c in Physics.OverlapSphere(path.corners.Last()+Vector3.up,2,~0,QueryTriggerInteraction.Ignore))report.AppendLine("BLOCKER "+c.name+" "+c.bounds+" door="+c.GetComponentInParent<Door>());
  }
  temporary.Remove();
  if(!pass){loader.data=old;loader.enabled=true;Object.DestroyImmediate(baked);File.WriteAllText("RecoveryReports/m6-navigation-check.txt","FAIL\n"+report);throw new Exception("Baked NavMesh still has disconnected target regions");}
  const string asset="Assets/Recovery/M6/SchoolConnectedNavMesh.asset";
  var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(asset);
  if(existing){EditorUtility.CopySerialized(baked,existing);Object.DestroyImmediate(baked);EditorUtility.SetDirty(existing);baked=existing;}else AssetDatabase.CreateAsset(baked,asset);
  loader.data=baked;loader.enabled=true;EditorUtility.SetDirty(loader);AssetDatabase.SaveAssets();
  File.WriteAllText("RecoveryReports/m6-navigation-check.txt","PASS\n"+report);
 }
}
