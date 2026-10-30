using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RoomMovementRepair
{
    static RoomMovementRepair() { EditorApplication.update += Poll; }
    static void Poll()
    {
        const string request = "RecoveryReports/room-movement.request";
        if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
        string action = File.ReadAllText(request).Trim();
        File.Delete(request);
        if(action == "repair") Repair(); else Audit();
    }
    public static void Audit()
    {
        Physics.SyncTransforms();
        var log = new StringBuilder();
        var player = Object.FindFirstObjectByType<SchoolExplorer>();
        log.AppendLine("Player " + player.transform.position);
        for(float x=18;x<=32;x+=1) for(float z=57;z<=63;z+=1)
        {
            var p = new Vector3(x,2.04f,z);
            var hits=Physics.RaycastAll(p+Vector3.up*.15f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
            log.AppendLine("GROUND "+p+" "+string.Join(" | ",hits.Select(h=>h.collider.name+" y="+h.point.y)));
            var blockers=Physics.OverlapCapsule(p+Vector3.up*.36f,p+Vector3.up*1.44f,.32f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.GetComponent<SchoolExplorer>());
            foreach(var c in blockers) log.AppendLine("BLOCK "+p+" "+Path(c.transform)+" "+c.GetType().Name+" "+c.bounds);
        }
        foreach(var dir in new[]{Vector3.left,Vector3.right,Vector3.forward,Vector3.back})
        foreach(var h in Physics.RaycastAll(new Vector3(27,4.5f,60),dir,15,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
            log.AppendLine("WALL "+dir+" "+h.point+" "+Path(h.transform));
        File.WriteAllText("RecoveryReports/room-movement.txt",log.ToString());
    }
    static string Path(Transform t) => t.parent ? Path(t.parent)+"/"+t.name : t.name;

    static bool Walk(Vector3 start, Vector3 end, StringBuilder log)
    {
        var go=new GameObject("Temporary room movement probe");
        go.transform.position=start;
        var body=go.AddComponent<CharacterController>();
        body.height=1.8f;body.center=new Vector3(0,.9f,0);body.radius=.34f;body.skinWidth=.025f;body.stepOffset=.25f;
        Physics.SyncTransforms();
        Vector3 direction=(end-start).normalized;
        int steps=Mathf.CeilToInt(Vector3.Distance(start,end)/.04f);
        for(int i=0;i<steps;i++) body.Move(direction*.04f+Vector3.down*.035f);
        var delta=go.transform.position-end;delta.y=0;
        bool ok=delta.magnitude<.15f && Mathf.Abs(go.transform.position.y-2.005f)<.15f;
        log.AppendLine("Walk "+start+" -> "+end+": "+(ok?"PASS":"BLOCKED")+" actual="+go.transform.position);
        Object.DestroyImmediate(go);
        return ok;
    }
    [MenuItem("Recovery/Repair room movement and floor")]
    public static void Repair()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying || scene.name!="School_Test") {
            File.WriteAllText("RecoveryReports/room-repair.txt","Pending: open and save School_Test in Edit Mode.");return;
        }
        string backup="RecoveryReports/School_Test-before-movement-fix.unity";
        if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
        var log=new StringBuilder();
        Vector3[] starts={new Vector3(24,2.06f,58),new Vector3(18,2.06f,60),new Vector3(18,2.06f,61.2f)};
        Vector3[] ends={new Vector3(18,2.06f,58),new Vector3(32,2.06f,60),new Vector3(32,2.06f,61.2f)};
        log.AppendLine("BEFORE");
        for(int i=0;i<starts.Length;i++)Walk(starts[i],ends[i],log);
        var region=new Bounds(new Vector3(24.86f,3.4f,60.29f),new Vector3(15.92f,3,6.82f));
        foreach(var c in Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None))
        {
            if(!c.enabled || !c.sharedMesh || !c.sharedMesh.name.StartsWith("Combined Mesh (root: scene)") || !c.bounds.Intersects(region))continue;
            Undo.RecordObject(c,"Disable incorrectly transformed combined-mesh collision");
            log.AppendLine("Disabled invalid scene-batch collider: "+Path(c.transform)+" mesh="+c.sharedMesh.name);
            c.enabled=false;
        }
        var floor=GameObject.Find("START ROOM — restored floor");
        Undo.RecordObject(floor.transform,"Cover full starting room floor");
        floor.transform.position=new Vector3(24.86f,1.925f,60.29f);
        floor.transform.localScale=new Vector3(15.92f,.16f,6.82f);
        // Use a separate material so the earlier scene's patch keeps its UV density.
        const string materialPath="Assets/Recovery/StartRoomFullFloor.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(floor.GetComponent<Renderer>().sharedMaterial);AssetDatabase.CreateAsset(material,materialPath);}
        material.mainTextureScale=new Vector2(15.92f*.6f,6.82f*.625f);
        material.SetTextureScale("_BumpMap",material.mainTextureScale);
        floor.GetComponent<Renderer>().sharedMaterial=material;
        EditorUtility.SetDirty(material);
        Physics.SyncTransforms();
        log.AppendLine("AFTER");bool passed=true;
        for(int i=0;i<starts.Length;i++)passed &= Walk(starts[i],ends[i],log);
        log.AppendLine("Movement checks: "+(passed?"PASS":"FAIL"));
        File.WriteAllText("RecoveryReports/room-repair.txt",log.ToString());
        if(!passed)throw new System.InvalidOperationException("Movement checks failed; scene not saved. See room-repair.txt");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Audit();
        Capture();
    }
    static void Capture()
    {
        var cam=Object.FindFirstObjectByType<SchoolExplorer>().view;
        var position=cam.transform.position;var rotation=cam.transform.rotation;
        var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(960,540,24);var image=new Texture2D(960,540,TextureFormat.RGB24,false);
        try {
            cam.transform.position=new Vector3(27,3.65f,60);cam.transform.rotation=Quaternion.Euler(23,-90,0);
            cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();
            File.WriteAllBytes("RecoveryReports/room-movement-fixed.png",image.EncodeToPNG());
        } finally {
            cam.transform.position=position;cam.transform.rotation=rotation;cam.targetTexture=oldTarget;
            RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(image);
        }
    }
}
