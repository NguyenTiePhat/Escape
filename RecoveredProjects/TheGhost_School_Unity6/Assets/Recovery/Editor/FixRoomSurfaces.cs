using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;
public static class FixRoomSurfaces {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity");
 var player=Object.FindFirstObjectByType<SchoolExplorer>();var report=new StringBuilder();
 var renderers=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
 int surfaces=0,colliders=0;
 Directory.CreateDirectory("Assets/Recovery/RepairedSurfaces");
 foreach(var r in renderers){
 string n=r.name.ToLowerInvariant();
 if(!(n.Contains("floor")||n.Contains("wall")||n.Contains("ceiling")))continue;
 if(r.sharedMaterials.Length==0||r.sharedMaterials.Any(m=>!m))continue;
 var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;
 // Keep exact vertices/UVs and all doorway openings; add reversed triangles for back faces.
 foreach(var material in r.sharedMaterials){material.shader=Shader.Find("Recovery/OpaqueTwoSided");EditorUtility.SetDirty(material);}surfaces++;
// Static-batch meshes contain scene-space geometry, not this object's local geometry.
// Attaching the whole batch here creates huge, incorrectly transformed invisible walls.
if(!f.sharedMesh.name.StartsWith("Combined Mesh (root: scene)") && !r.GetComponents<MeshCollider>().Any(c=>c.enabled&&!c.isTrigger)){
 var col=r.gameObject.AddComponent<MeshCollider>();col.sharedMesh=f.sharedMesh;colliders++;
 }
 }
 // Restore the missing visible patch over the existing solid starting-room floor, bounded to the room.
 var existing=GameObject.Find("START ROOM — restored floor");if(existing)Object.DestroyImmediate(existing);
 var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="START ROOM — restored floor";
 // Keep the visual surface and collider above the original floor to avoid z-fighting.
 floor.transform.position=new Vector3(26.0f,1.925f,60.1f);floor.transform.localScale=new Vector3(5.0f,.16f,6.4f);
 var source=renderers.FirstOrDefault(r=>r.name=="FirstFloor (1)");
 var mat=new Material(source&&source.sharedMaterial?source.sharedMaterial:new Material(Shader.Find("Standard")));
 mat.name="Start room floor";mat.mainTextureScale=new Vector2(3,4);
 string path="Assets/Recovery/StartRoomFloor.mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);if(saved){EditorUtility.CopySerialized(mat,saved);Object.DestroyImmediate(mat);mat=saved;}else AssetDatabase.CreateAsset(mat,path);
 floor.GetComponent<Renderer>().sharedMaterial=mat;
 var controller=player.GetComponent<CharacterController>();controller.radius=.34f;controller.skinWidth=.025f;controller.stepOffset=.25f;
 player.view.nearClipPlane=.025f;player.view.useOcclusionCulling=false;
 Physics.SyncTransforms();
 bool grounded=Physics.Raycast(new Vector3(27.01f,2.5f,57.61f),Vector3.down,out var hit,1,~0,QueryTriggerInteraction.Ignore);
 report.AppendLine("Double-sided architectural meshes: "+surfaces+"; mesh colliders added: "+colliders);
 report.AppendLine("Starting floor patch: x 23.5..28.5, z 56.9..63.3, top y=2.005. Existing floor collider retained below patch.");
 report.AppendLine("Ground raycast: "+grounded+" "+(grounded?hit.collider.name:"none"));
 EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/surface-repair.txt",report.ToString());
 var cam=player.view;cam.transform.localRotation=Quaternion.Euler(32,0,0);var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/repaired-floor.png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;
 if(!Application.isBatchMode)EditorApplication.isPlaying=true;
 }
}


