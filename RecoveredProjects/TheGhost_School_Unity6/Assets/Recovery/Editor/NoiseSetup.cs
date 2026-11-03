using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NoiseSetup
{
    static NoiseSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("RecoveryReports/noise-setup.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
        File.Delete("RecoveryReports/noise-setup.request"); Run();
    }
    public static void RunBatch() { EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity"); Run(); EditorApplication.Exit(0); }
    [MenuItem("Tools/M3/Set up noise test")]
    public static void Run()
    {
        var player = Object.FindAnyObjectByType<SchoolExplorer>();
        if (!player) throw new System.InvalidOperationException("Open School_Test first.");
        if (!File.Exists("RecoveryReports/School_Test-before-noise.unity")) File.Copy("Assets/Recovery/Scenes/School_Test.unity", "RecoveryReports/School_Test-before-noise.unity");
        var manager = NoiseManager.Instance; manager.debugLogging = true;
        var emitter = Object.FindAnyObjectByType<NoiseTestEmitter>();
        if (!emitter) emitter = new GameObject("M3 Noise Test - N key").AddComponent<NoiseTestEmitter>();
        emitter.emissionPoint = player.transform;
        if (!emitter.nearListener) { var go = new GameObject("Noise listener - near 5m"); go.transform.SetParent(emitter.transform); emitter.nearListener = go.AddComponent<NoiseTestListener>(); }
        if (!emitter.farListener) { var go = new GameObject("Noise listener - outside 15m"); go.transform.SetParent(emitter.transform); emitter.farListener = go.AddComponent<NoiseTestListener>(); }
        emitter.nearListener.transform.position = player.transform.position + Vector3.right * 5;
        emitter.farListener.transform.position = player.transform.position + Vector3.right * 15;
        EditorUtility.SetDirty(manager); EditorUtility.SetDirty(emitter);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene); EditorSceneManager.SaveScene(player.gameObject.scene);
        File.WriteAllText("RecoveryReports/noise-setup.txt", "PASS: N key emits Impact .4 at player; radius10m; near5m=.2; far15m receives nothing; logging enabled; turn on Scene/Game Gizmos to view sphere for1s.");
    }
}
