using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

[InitializeOnLoad]
public static class RoomLightingRepair
{
    static RoomLightingRepair(){EditorApplication.update+=Poll;}
    static void Poll(){
        string path="RecoveryReports/room-lighting.request";
        if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        string action;try{action=File.ReadAllText(path).Trim();}catch(IOException){return;}
        File.Delete(path);if(SceneManager.GetActiveScene().name!="School_Test")return;
        if(action=="repair")Repair();else Audit();
    }
    public static void Audit(){
        var s=new StringBuilder();Physics.SyncTransforms();
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))s.AppendLine("LIGHT "+l.name+" "+l.type+" enabled="+l.enabled+" intensity="+l.intensity+" shadows="+l.shadows+" bias="+l.shadowBias+" normalBias="+l.shadowNormalBias);
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.ToLower().Contains("ceiling")))s.AppendLine("CEILING "+r.name+" "+r.bounds+" materials="+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name)));
        foreach(float x in new[]{18f,24f,30f})foreach(float z in new[]{58f,60f,62f})foreach(var h in Physics.RaycastAll(new Vector3(x,3.7f,z),Vector3.up,12,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))s.AppendLine("UP "+x+","+z+" "+h.collider.name+" "+h.point);
        File.WriteAllText("RecoveryReports/room-lighting-audit.txt",s.ToString());Capture("before");
    }
    static void Capture(string suffix){
        var cam=Object.FindFirstObjectByType<SchoolExplorer>().view;var p=cam.transform.position;var q=cam.transform.rotation;var target=cam.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(960,540,24);var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
        try{cam.transform.position=new Vector3(27,3.65f,60);cam.transform.rotation=Quaternion.Euler(-25,-90,0);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/room-ceiling-"+suffix+".png",tex.EncodeToPNG());}
        finally{cam.transform.position=p;cam.transform.rotation=q;cam.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
    [MenuItem("Recovery/Repair starting room ceiling and lighting")]
    static void Repair(){
        var scene=SceneManager.GetActiveScene();if(scene.name!="School_Test"||EditorApplication.isPlaying)return;
        string backup="RecoveryReports/School_Test-before-ceiling-fix.unity";
        if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
        var ceiling=GameObject.Find("START ROOM — restored ceiling");
        if(!ceiling){ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.name="START ROOM — restored ceiling";Undo.RegisterCreatedObjectUndo(ceiling,"Restore room ceiling");}
        ceiling.transform.position=new Vector3(24.86f,7.16f,60.29f);
        ceiling.transform.localScale=new Vector3(15.92f,.12f,6.82f);
        const string matPath="Assets/Recovery/StartRoomCeiling.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!mat){var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/CeilingAcoustic_mtl.mat");mat=source?new Material(source):new Material(Shader.Find("Standard"));mat.shader=Shader.Find("Standard");AssetDatabase.CreateAsset(mat,matPath);}
        mat.mainTextureScale=new Vector2(15.92f/2,6.82f/2);mat.SetTextureScale("_BumpMap",mat.mainTextureScale);mat.SetFloat("_Glossiness",.08f);EditorUtility.SetDirty(mat);
        ceiling.GetComponent<Renderer>().sharedMaterial=mat;
        ceiling.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
        // Exported baked lighting belongs to the original geometry, not the repaired room.
        // Clear only renderer bindings in this room; retain the source lighting assets.
        var roomBounds=new Bounds(new Vector3(24.86f,4.6f,60.29f),new Vector3(16.2f,5.5f,7.1f));
        int cleared=0;
        foreach(var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)){
            if(!renderer.bounds.Intersects(roomBounds)||renderer.lightmapIndex<0)continue;
            Undo.RecordObject(renderer,"Remove stale recovered room lightmap binding");renderer.lightmapIndex=-1;cleared++;
        }
        var daylight=GameObject.Find("RECOVERY — inspection daylight").GetComponent<Light>();
        Undo.RecordObject(daylight,"Improve wall shadow filtering");daylight.shadows=LightShadows.Soft;daylight.shadowCustomResolution=2048;daylight.shadowBias=.025f;daylight.shadowNormalBias=.15f;
        // This was a debugging sun added for map inspection, not an original room light.
        daylight.enabled=false;
        QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowCascades=4;
        foreach(float x in new[]{21f,29f}){
            string name="START ROOM — ceiling light "+x;
            var go=GameObject.Find(name);if(!go){go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Add room lighting");}
            go.transform.position=new Vector3(x,6.6f,60.1f);
            var light=go.GetComponent<Light>();if(!light)light=go.AddComponent<Light>();
            light.type=LightType.Point;light.range=11;light.intensity=1.8f;light.color=new Color(.91f,.95f,1f);
            light.shadows=LightShadows.Soft;light.shadowCustomResolution=1024;light.shadowBias=.025f;light.shadowNormalBias=.1f;light.renderMode=LightRenderMode.ForcePixel;
        }
        Physics.SyncTransforms();int covered=0;
        foreach(float x in new[]{18f,24f,30f})foreach(float z in new[]{58f,60f,62f})if(ceiling.GetComponent<Collider>().Raycast(new Ray(new Vector3(x,3.7f,z),Vector3.up),out var hit,4)&&Mathf.Abs(hit.point.y-7.1f)<.01f)covered++;
        if(covered!=9)throw new System.InvalidOperationException("Incomplete ceiling coverage");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Capture("after");
        File.WriteAllText("RecoveryReports/room-lighting-result.txt","Ceiling underside y=7.10; 9/9 coverage checks PASS. Opaque ceiling blocks daylight; two shadowed room lights. Directional shadow resolution 2048; point lights 1024. Stale lightmap bindings cleared: "+cleared+". Original walls and movement colliders retained.");
    }
}
