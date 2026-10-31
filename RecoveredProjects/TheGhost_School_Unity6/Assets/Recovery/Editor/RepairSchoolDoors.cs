using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RepairSchoolDoors
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        if (!File.Exists("RecoveryReports/School_Test-before-doors.unity"))
            EditorSceneManager.SaveScene(scene, "RecoveryReports/School_Test-before-doors.unity", true);
        var report = new StringBuilder();
        var player = Object.FindFirstObjectByType<SchoolExplorer>();
        if (!player.GetComponent<DoorInteraction>()) player.gameObject.AddComponent<DoorInteraction>();
        // Recovered moving leaves have their own hinge transform; frames stay fixed.
        foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            if (!Regex.IsMatch(filter.name, @"^(Door\d*|[LR]Door\d*)( \(\d+\))?$")) continue;
            if (!filter.GetComponent<BoxCollider>() || !filter.sharedMesh) continue;
            if (filter.sharedMesh.name.StartsWith("Combined Mesh"))
            {
                var original = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/" + (filter.name.StartsWith("LDoor") ? "LDoor" : "RDoor") + ".asset");
                if (!original) throw new Exception("Missing unbatched door mesh: " + filter.name);
                filter.sharedMesh = original;
                var serialized = new SerializedObject(filter.GetComponent<MeshRenderer>());
                serialized.FindProperty("m_StaticBatchInfo.firstSubMesh").intValue = 0;
                serialized.FindProperty("m_StaticBatchInfo.subMeshCount").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var t in filter.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
            if (!filter.GetComponent<SchoolDoor>()) filter.gameObject.AddComponent<SchoolDoor>();
            report.AppendLine("Interactive leaf: " + filter.name);
        }
        var root = GameObject.Find("START ROOM — original room corrections").transform;
        if (root.Find("East annex removed - solid infill")) throw new InvalidOperationException("Layout superseded. Use RelocateStartDoor to preserve the north exit.");
        var previous = root.Find("Exit door hinge");
        if (previous) Object.DestroyImmediate(previous.gameObject);
        foreach (string name in new[] { "Door jamb south", "Door jamb north", "Door header" })
        { var old = root.Find(name); if (old) Object.DestroyImmediate(old.gameObject); }
        // The old north door was decorative against a solid boundary. Relocate the usable door
        // to the existing east opening (z 59.2..60.6), recessed in the 20 cm partition.
        var source = Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).First(f => f.name == "Door12");
        var leaf = Object.Instantiate(source.gameObject, root);
        leaf.name = "Exit door hinge";
        leaf.transform.rotation = Quaternion.Euler(0, 90, 0);
        leaf.transform.localScale = Vector3.one * 106.6f;
        var renderer = leaf.GetComponent<Renderer>();
        var size = renderer.bounds.size;
        var scale = leaf.transform.localScale;
        leaf.transform.localScale = new Vector3(scale.x * 1.27f / size.z, scale.y * 2.28f / size.y, scale.z * .12f / size.x);
        leaf.transform.position += new Vector3(24.5f, 3.175f, 59.9f) - renderer.bounds.center;
        var door = leaf.GetComponent<SchoolDoor>();
        if (!door) door = leaf.AddComponent<SchoolDoor>();
        var wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovery/OriginalRoomWall.mat");
        Box(root, "Door jamb south", new Vector3(24.5f, 3.205f, 59.23f), new Vector3(.2f,2.4f,.06f), wall);
        Box(root, "Door jamb north", new Vector3(24.5f, 3.205f, 60.57f), new Vector3(.2f,2.4f,.06f), wall);
        Box(root, "Door header", new Vector3(24.5f, 4.37f, 59.9f), new Vector3(.2f,.07f,1.28f), wall);
        Physics.SyncTransforms();
        // Awake is invoked explicitly for edit-mode physics checks.
        door.Initialize();
        door.SetImmediate(false);
        bool closed = !Traverse();
        door.SetImmediate(true);
        bool open = Traverse();
        report.AppendLine("Closed door blocks controller: " + closed);
        report.AppendLine("Open door allows controller through wall: " + open);
        door.SetImmediate(false);
        report.AppendLine("Door bounds: " + renderer.bounds);
        if (!closed || !open) { File.WriteAllText("RecoveryReports/door-repair.txt", report.ToString()); throw new Exception("Door traversal failed"); }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Capture(player.view, "RecoveryReports/door-closed.png");
        door.SetImmediate(true);
        Capture(player.view, "RecoveryReports/door-open.png");
        door.SetImmediate(false);
        report.AppendLine("RESULT: PASS");
        File.WriteAllText("RecoveryReports/door-repair.txt", report.ToString());
    }
    static void Box(Transform root, string name, Vector3 position, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube); box.name = name;
        box.transform.SetParent(root); box.transform.position = position; box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
    }
    static bool Traverse()
    {
        var probe = new GameObject("Door traversal probe");
        probe.transform.position = new Vector3(22.8f, 2.06f, 59.9f);
        var body = probe.AddComponent<CharacterController>(); body.height = 1.8f; body.center = new Vector3(0,.9f,0);
        body.radius = .34f; body.skinWidth = .025f; body.stepOffset = .25f;
        Physics.SyncTransforms();
        for (int i=0; i<110; i++) body.Move(new Vector3(.04f,-.035f,0));
        bool pass = probe.transform.position.x > 26.8f;
        Object.DestroyImmediate(probe); return pass;
    }
    static void Capture(Camera cam, string path)
    {
        var pos=cam.transform.position; var rot=cam.transform.rotation; var target=cam.targetTexture; var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(960,540,24); var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
        try { cam.transform.position=new Vector3(21.7f,3.65f,59.3f); cam.transform.LookAt(new Vector3(24.5f,3.2f,59.9f));
            cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,960,540),0,0); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG()); }
        finally { cam.transform.position=pos; cam.transform.rotation=rot; cam.targetTexture=target; RenderTexture.active=active; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(tex); }
    }
}


