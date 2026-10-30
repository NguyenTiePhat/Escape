using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class SchoolRecovery
{
    [MenuItem("Recovery/Prepare School Preview")]
    public static void Prepare()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/SchoolLevel.unity",OpenSceneMode.Single);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var report=new StringBuilder();
        report.AppendLine("The Ghost 1.87.15 — School map recovery validation");
        report.AppendLine("Unity: "+Application.unityVersion);
        report.AppendLine("Transforms: "+all.Length);
        int missingScripts=0, meshCount=0, missingMeshes=0, colliderCount=0, missingMats=0;
        foreach(var t in all)
        {
            missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach(var filter in t.GetComponents<MeshFilter>()) { meshCount++; if(!filter.sharedMesh) missingMeshes++; }
            colliderCount+=t.GetComponents<Collider>().Length;
            foreach(var r in t.GetComponents<Renderer>()) missingMats+=r.sharedMaterials.Count(m=>!m);
            foreach(var c in t.GetComponents<Camera>()) c.enabled=false;
            foreach(var listener in t.GetComponents<AudioListener>()) Object.DestroyImmediate(listener);
            foreach(var audio in t.GetComponents<AudioSource>()) audio.playOnAwake=false;
            foreach(var canvas in t.GetComponents<Canvas>()) if(canvas.renderMode!=RenderMode.WorldSpace) canvas.enabled=false;
        }
        report.AppendLine($"MeshFilters: {meshCount}; null mesh slots: {missingMeshes}; colliders: {colliderCount}; null material slots: {missingMats}");
        report.AppendLine("Removed unavailable gameplay script components from preview only: "+missingScripts);
        // Preserve mesh/texture references; replace only exported placeholder shaders on scene-used materials.
        int converted=0;
        var materials=all.SelectMany(t=>t.GetComponents<Renderer>()).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().ToArray();
        foreach(var m in materials)
        {
            string path=m.shader?AssetDatabase.GetAssetPath(m.shader):"";
            if(!m.shader || path.StartsWith("Assets/Shader/"))
            {
                var color=m.HasProperty("_Color")?m.GetColor("_Color"):Color.white;
                Texture tex=m.HasProperty("_MainTex")?m.GetTexture("_MainTex"):null;
                var scale=m.HasProperty("_MainTex")?m.GetTextureScale("_MainTex"):Vector2.one;
                var offset=m.HasProperty("_MainTex")?m.GetTextureOffset("_MainTex"):Vector2.zero;
                m.shader=Shader.Find("Standard"); m.color=color; m.mainTexture=tex; m.mainTextureScale=scale; m.mainTextureOffset=offset;
                if(m.GetTexture("_BumpMap")) m.EnableKeyword("_NORMALMAP");
                m.SetFloat("_Glossiness",.15f); EditorUtility.SetDirty(m); converted++;
            }
        }
        report.AppendLine($"Scene-used materials: {materials.Length}; placeholder shader replacements: {converted}");
        var renderers=all.SelectMany(t=>t.GetComponents<Renderer>()).Where(r=>r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)).ToArray();
        Bounds bounds=new Bounds(Vector3.zero,Vector3.one);
        if(renderers.Length>0) { bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds); }
        report.AppendLine("Active renderer bounds: "+bounds);
        Transform spawnGroup=all.FirstOrDefault(t=>t.name=="SurvivorsSpawnPoints");
        Transform spawn=spawnGroup && spawnGroup.childCount>0?spawnGroup.GetChild(0):all.FirstOrDefault(t=>t.name=="SpawnPoint");
        Vector3 start=spawn?spawn.position:bounds.center+Vector3.up*3;
        Physics.SyncTransforms();
        if(Physics.Raycast(start+Vector3.up*1.5f,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore)) start=hit.point+Vector3.up*.12f;
        else report.AppendLine("WARNING: no ground raycast at suggested spawn; use F to fly if needed.");
        var player=new GameObject("RECOVERY — School Explorer"); player.transform.position=start;
        if(spawn) player.transform.rotation=Quaternion.Euler(0,spawn.eulerAngles.y,0);
        var body=player.AddComponent<CharacterController>(); body.height=1.8f; body.radius=.28f; body.center=new Vector3(0,.9f,0); body.stepOffset=.3f;
        var cam=new GameObject("Inspection camera").AddComponent<Camera>(); cam.transform.SetParent(player.transform,false);
        cam.transform.localPosition=new Vector3(0,1.65f,0); cam.tag="MainCamera"; cam.fieldOfView=75; cam.nearClipPlane=.05f; cam.farClipPlane=500;
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.15f,.19f,.22f); cam.cullingMask=~0; cam.useOcclusionCulling=false;
        cam.gameObject.AddComponent<AudioListener>();
        var torch=cam.gameObject.AddComponent<Light>(); torch.type=LightType.Spot; torch.range=28; torch.spotAngle=75; torch.intensity=3;
        var explorer=player.AddComponent<SchoolExplorer>(); explorer.view=cam; explorer.home=start;
        RenderSettings.fog=false; RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.48f,.53f,.6f); RenderSettings.ambientEquatorColor=new Color(.32f,.35f,.38f); RenderSettings.ambientGroundColor=new Color(.2f,.22f,.24f);
        RenderSettings.ambientIntensity=1; RenderSettings.reflectionIntensity=.4f;
        var sun=new GameObject("RECOVERY — inspection daylight").AddComponent<Light>(); sun.type=LightType.Directional;
        sun.intensity=.85f; sun.color=new Color(.83f,.88f,1); sun.transform.rotation=Quaternion.Euler(50,-30,0); sun.shadows=LightShadows.Soft;
        report.AppendLine("Preview spawn: "+start+"; source: "+(spawn?spawn.name:"fallback"));
        Directory.CreateDirectory("Assets/Recovery/Scenes");
        EditorSceneManager.SaveScene(scene,"Assets/Recovery/Scenes/School_Explore.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Recovery/Scenes/School_Explore.unity",true)};
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("RecoveryReports"); File.WriteAllText("RecoveryReports/validation.txt",report.ToString());
        Debug.Log(report.ToString());
        if(missingMeshes>0 || missingMats>0) Debug.LogWarning("Recovery contains empty mesh/material slots; see validation report.");
    }
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity",OpenSceneMode.Single);
        var lines=new StringBuilder();
        var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var t in all.Where(t=>t.parent==null)) lines.AppendLine($"ROOT {t.name} active={t.gameObject.activeSelf} pos={t.position} children={t.childCount}");
        foreach(var t in all.Where(t=>t.name.Contains("SchoolModel") || t.name.Contains("SchoolDetails") || t.name.Contains("SurvivorsSP"))) lines.AppendLine($"LANDMARK {t.name} active={t.gameObject.activeInHierarchy} pos={t.position}");
        foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(f=>!f.sharedMesh).Take(35)) lines.AppendLine($"EMPTY MESH {f.name} active={f.gameObject.activeInHierarchy} pos={f.transform.position}");
        foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.sharedMaterials.Any(m=>!m)).Take(35)) lines.AppendLine($"EMPTY MATERIAL {r.name} active={r.gameObject.activeInHierarchy}");
        var cam=Object.FindFirstObjectByType<SchoolExplorer>().view;
        Directory.CreateDirectory("RecoveryReports");
        File.WriteAllText("RecoveryReports/inspection.txt",lines.ToString());
        Render(cam,"RecoveryReports/school-spawn.png");
        var original=cam.transform.rotation;
        cam.transform.rotation=Quaternion.Euler(0,cam.transform.eulerAngles.y+180,0);
        Render(cam,"RecoveryReports/school-reverse.png"); cam.transform.rotation=original;
    }
    static void Render(Camera cam,string path)
    {
        var rt=new RenderTexture(1600,900,24); cam.targetTexture=rt; cam.Render();
        RenderTexture.active=rt; var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,1600,900),0,0); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG());
        cam.targetTexture=null; RenderTexture.active=null; Object.DestroyImmediate(tex); Object.DestroyImmediate(rt);
    }
}
