using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class MapCollisionRepair
{
 public static void Run(){
 var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
 Directory.CreateDirectory("Assets/Recovery/ColliderRepairs");AssetDatabase.Refresh();
 var report=new StringBuilder();int count=0;
 foreach(var c in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)){
 if(!c.enabled||c.isTrigger||!c.sharedMesh)continue;
 var r=c.GetComponent<MeshRenderer>();var f=c.GetComponent<MeshFilter>();
 if(!r||!r.enabled||!f||!f.sharedMesh||c.sharedMesh!=f.sharedMesh||!c.sharedMesh.name.StartsWith("Combined Mesh"))continue;
 var so=new SerializedObject(r);int first=so.FindProperty("m_StaticBatchInfo.firstSubMesh").intValue;int parts=so.FindProperty("m_StaticBatchInfo.subMeshCount").intValue;
 if(parts<=0)continue;
 var before=c.bounds;var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
 mesh.CombineMeshes(Enumerable.Range(first,parts).Select(i=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=c.transform.worldToLocalMatrix}).ToArray(),true,true);mesh.RecalculateBounds();
 string id=GlobalObjectId.GetGlobalObjectIdSlow(c).targetObjectId.ToString();string path="Assets/Recovery/ColliderRepairs/Collider_"+id+".asset";
 var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
 mesh.name="Exact visual collision "+c.name;c.sharedMesh=mesh;EditorUtility.SetDirty(c);Physics.SyncTransforms();
 report.AppendLine(MapCollisionAudit.PathOf(c.transform)+" old="+before+" new="+c.bounds+" renderer="+r.bounds);
 count++;
 }
 if(count==0)throw new Exception("No batched mesh colliders were found to repair");
 report.Insert(0,"Rebuilt "+count+" mesh colliders from each renderer's own static-batch submeshes in correct local coordinates. Visual meshes, walls, doors, furnishings and user deletions retained.\n");
 EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText("RecoveryReports/map-collision-repair.txt",report.ToString());
 Preview(new Vector3(31.3f,10.1f,55),new Vector3(31.3f,10.1f,60),"west-upper-landing");
 Preview(new Vector3(52,10.1f,57),new Vector3(60,10.1f,60),"upper-hall");
 }
 static void Preview(Vector3 eye,Vector3 target,string name){var camera=new GameObject("Audit view").AddComponent<Camera>();camera.transform.position=eye;camera.transform.LookAt(target);camera.nearClipPlane=.03f;var rt=new RenderTexture(960,540,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/map-"+name+".png",tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(camera.gameObject);}
}
