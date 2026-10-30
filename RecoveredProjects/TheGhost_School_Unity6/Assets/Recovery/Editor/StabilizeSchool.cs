using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class StabilizeSchool
{
    public const string ScenePath = "Assets/Recovery/Scenes/School_Explore.unity";
    public static void Configure()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        Debug.Log("School configured for Direct3D11.");
    }

    public static void Audit()
    {
        Configure();
        EditorSceneManager.OpenScene(ScenePath);
        var report = new StringBuilder();
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in transforms)
        {
            var filter = t.GetComponent<MeshFilter>();
            var renderer = t.GetComponent<Renderer>();
            if (filter && !filter.sharedMesh)
                report.AppendLine("MESH\t" + t.gameObject.activeInHierarchy + "\t" + PathOf(t));
            if (renderer && renderer.sharedMaterials.Any(m => !m))
                report.AppendLine("MATERIAL\t" + t.gameObject.activeInHierarchy + "\t" + PathOf(t) + "\tmesh=" + (filter && filter.sharedMesh ? filter.sharedMesh.name : "none"));
        }
        foreach (var texture in Resources.FindObjectsOfTypeAll<Texture>().OrderByDescending(t => UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)).Take(30))
            report.AppendLine("TEXTURE\t" + UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture) + "\t" + AssetDatabase.GetAssetPath(texture));
        Directory.CreateDirectory("RecoveryReports");
        File.WriteAllText("RecoveryReports/current-audit.txt", report.ToString());
        Debug.Log("School audit complete.");
    }
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

    public static void CreateTestScene()
    {
        Configure();
        var scene = EditorSceneManager.OpenScene(ScenePath);
        const string testScene = "Assets/Recovery/Scenes/School_Test.unity";
        if (File.Exists(testScene)) throw new Exception("School_Test already exists; refusing to overwrite.");
        EditorSceneManager.SaveScene(scene, testScene);
        var log = new StringBuilder();
        // These roots contain unavailable game services, cosmetic libraries and remote ending sets.
        // Keep the original export and the pre-repair scene backup as the recovery source.
        string[] unused = { "NetworkEntities", "GameManager", "WarmUpShaders", "EndingScene", "MapCanvasUI", "MinimapCanvas", "MapCam" };
        foreach (var root in scene.GetRootGameObjects().Where(g => unused.Contains(g.name)))
        {
            log.AppendLine("Removed unused preview root: " + root.name + " (" + root.GetComponentsInChildren<Transform>(true).Length + " transforms)");
            Object.DestroyImmediate(root);
        }
        var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int restored = 0;
        foreach (var r in renderers.Where(r => r.sharedMaterials.Any(m => !m)))
        {
            var mesh = r.GetComponent<MeshFilter>();
            if (!mesh || !mesh.sharedMesh) continue;
            var donors = renderers.Where(d => d != r && d.GetComponent<MeshFilter>() && d.GetComponent<MeshFilter>().sharedMesh == mesh.sharedMesh
                && d.sharedMaterials.Length == r.sharedMaterials.Length && d.sharedMaterials.All(m => m)).ToArray();
            if (donors.Length == 0 || donors.Any(d => !d.sharedMaterials.SequenceEqual(donors[0].sharedMaterials))) continue;
            r.sharedMaterials = donors[0].sharedMaterials;
            restored++;
        }
        log.AppendLine("Material renderers restored from identical mesh: " + restored);
        var player = Object.FindFirstObjectByType<SchoolExplorer>();
        if (!player || !player.view || !player.GetComponent<CharacterController>()) throw new Exception("Missing player setup");
        player.view.farClipPlane = 160;
        var intro = Object.FindFirstObjectByType<StartRoom>();
        if (!intro || !intro.flashlightObject) throw new Exception("Missing intro setup");
        if (!player.view.GetComponent<FirstPersonHands>()) player.view.gameObject.AddComponent<FirstPersonHands>();
        intro.hands = player.view.GetComponent<FirstPersonHands>();
        QualitySettings.SetQualityLevel(2, true);
        QualitySettings.vSyncCount = 1;
        QualitySettings.shadowDistance = 35;
        QualitySettings.asyncUploadBufferSize = 4;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText("RecoveryReports/repair.txt", log.ToString());
    }
}
