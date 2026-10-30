using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SchoolPlayCheck
{
    const string Key = "SchoolPlayCheck.Running";
    static double started;
    static int frames;
    static readonly StringBuilder report = new StringBuilder();
    static SchoolPlayCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                report.Clear();
                started = EditorApplication.timeSinceStartup;
                frames = 0;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Recovery/Open lightweight test scene")]
    public static void OpenTestScene()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
    }
    [MenuItem("Recovery/Check test scene in Play Mode")]
    public static void RunInteractive()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Run();
    }
    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) report.AppendLine("ERROR: " + message + "\n" + stack);
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup - started < 8) return;
        try
        {
            var player = Object.FindFirstObjectByType<SchoolExplorer>();
            var intro = Object.FindFirstObjectByType<StartRoom>();
            if (!player || !intro) throw new Exception("Player or intro missing");
            var body = player.GetComponent<CharacterController>();
            if (frames++ < 120)
            {
                // Use the actual controller against the scene physics, with the cursor released.
                body.Move((Vector3.down * 2 + Vector3.left * .6f) / 60);
                return;
            }
            report.AppendLine("Graphics: " + SystemInfo.graphicsDeviceType);
            report.AppendLine("Play frames checked: " + frames);
            report.AppendLine("Player position: " + player.transform.position + "; grounded=" + body.isGrounded);
            if (player.transform.position.y < player.home.y - 1) throw new Exception("Player fell through floor");
            var rig = player.view.transform.Find("First person arms");
            report.AppendLine("Hands initially hidden: " + (rig && !rig.gameObject.activeSelf));
            if (!rig || rig.gameObject.activeSelf) throw new Exception("Hands must be hidden before pickup");
            intro.hands.SetEquipped(true);
            if (!rig.gameObject.activeSelf) throw new Exception("Equipping hands failed");
            intro.hands.SetEquipped(false);
            var oldTarget = player.view.targetTexture;
            var rt = RenderTexture.GetTemporary(960, 540, 24);
            player.view.targetTexture = rt;
            player.view.Render();
            RenderTexture.active = rt;
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
            File.WriteAllBytes("RecoveryReports/test-play.png", image.EncodeToPNG());
            player.view.targetTexture = oldTarget; RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt); Object.Destroy(image);
            report.AppendLine("RESULT: " + (report.ToString().Contains("ERROR:") ? "FAIL" : "PASS"));
        }
        catch (Exception ex) { report.AppendLine("RESULT: FAIL\n" + ex); }
        File.WriteAllText("RecoveryReports/play-check.txt", report.ToString());
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        if (Application.isBatchMode) EditorApplication.Exit(report.ToString().Contains("FAIL") ? 1 : 0);
        else { EditorApplication.isPlaying = false; Debug.Log(report.ToString()); }
    }
}
