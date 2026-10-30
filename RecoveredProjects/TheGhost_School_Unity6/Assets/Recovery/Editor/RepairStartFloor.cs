using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RepairStartFloor
{
    const string Request = "RecoveryReports/repair-start-floor.request";
    static RepairStartFloor() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
        File.Delete(Request);
        if (SceneManager.GetActiveScene().isDirty)
        {
            File.WriteAllText("RecoveryReports/start-floor-result.txt", "Pending: save scene changes, then use Recovery/Repair starting floor.");
            return;
        }
        Run();
    }
    [MenuItem("Recovery/Repair starting floor")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || (scene.name != "School_Test" && scene.name != "School_Explore")) return;
        var floor = GameObject.Find("START ROOM — restored floor");
        if (!floor) throw new System.InvalidOperationException("Starting floor patch not found.");
        Directory.CreateDirectory("RecoveryReports");
        string backup = "RecoveryReports/" + scene.name + "-before-floor-fix.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        Undo.RecordObject(floor.transform, "Separate overlapping starting floor");
        // Original visual floor is at 1.95 m; existing collision floor reaches 1.98 m.
        // Put both the replacement surface and its collider above them, leaving a 2.5 cm step.
        const float top = 2.005f;
        var position = floor.transform.position;
        position.y = top - floor.transform.localScale.y * .5f;
        floor.transform.position = position;
        var collider = floor.GetComponent<BoxCollider>();
        Undo.RecordObject(collider, "Match starting floor collision");
        collider.enabled = true; collider.isTrigger = false;
        collider.center = Vector3.zero; collider.size = Vector3.one;
        Physics.SyncTransforms();
        var log = new StringBuilder();
        log.AppendLine("Scene: " + scene.path);
        log.AppendLine("Floor top: " + top + "; original visual floor: 1.95; old collision top: 1.98");
        int passed = 0;
        // Short rays start just above the surface, below furniture, to check floor coverage.
        for (int x = 0; x < 9; x++) for (int z = 0; z < 11; z++)
        {
            Vector3 origin = new Vector3(23.65f + x * .58f, top + .02f, 57.05f + z * .60f);
            if (collider.Raycast(new Ray(origin, Vector3.down), out var hit, .1f) && Mathf.Abs(hit.point.y-top)<.001f) passed++;
        }
        log.AppendLine("Floor collider samples: " + passed + "/99");
        if (passed != 99) throw new System.InvalidOperationException(log.ToString());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        var player = Object.FindFirstObjectByType<SchoolExplorer>();
        if (player && player.view)
        {
            var camera = player.view;
            var rotation = camera.transform.localRotation;
            var target = camera.targetTexture;
            var active = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(960,540,24);
            var image = new Texture2D(960,540,TextureFormat.RGB24,false);
            try
            {
                camera.transform.localRotation = Quaternion.Euler(45,0,0);
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0,0,960,540),0,0); image.Apply();
                File.WriteAllBytes("RecoveryReports/start-floor-fixed.png",image.EncodeToPNG());
            }
            finally
            {
                camera.transform.localRotation = rotation; camera.targetTexture = target;
                RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(image);
            }
        }
        File.WriteAllText("RecoveryReports/start-floor-result.txt",log.ToString());
        Debug.Log(log.ToString());
    }
}
