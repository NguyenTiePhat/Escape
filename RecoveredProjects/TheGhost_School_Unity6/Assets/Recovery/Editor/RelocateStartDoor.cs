using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class RelocateStartDoor
{
    static RelocateStartDoor(){EditorApplication.update+=Poll;}
    static void Poll(){
        const string request="RecoveryReports/relocate-start-door.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;return;}
        File.Delete(request);
        try{Run();}catch(Exception e){File.WriteAllText("RecoveryReports/relocate-start-door-error.txt",e.ToString());Debug.LogException(e);}
    }
    public static void Run(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var player=Object.FindFirstObjectByType<SchoolExplorer>();
        if(!player)throw new Exception("Open School_Test first");
        const string backup="RecoveryReports/School_Test-before-north-exit.unity";
        if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
        var root=GameObject.Find("START ROOM — original room corrections").transform;
        var report=new StringBuilder();
        var door=root.Find("Exit door hinge");
        if(!door)throw new Exception("Exit door missing");
        var control=door.GetComponent<SchoolDoor>();
        foreach(var name in new[]{"Partition south","Partition north","Exit lintel","Former blocked door - flush masonry","Door jamb south","Door jamb north","Door header","North door left jamb","North door right jamb"}){
            var old=root.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovery/OriginalRoomWall.mat");
        // A solid infill removes the former east annex, rather than retaining a room behind a sealed door.
        Box(root,"East annex removed - solid infill",new Vector3(27.91f,4.575f,59.65f),new Vector3(9.82f,5.14f,5.5f),material);
        Box(root,"South room reduction - solid infill",new Vector3(20.1f,4.575f,57.7f),new Vector3(5.8f,5.14f,1.6f),material);
        // North opening matches the existing structural doorway x18.78..20.21.
        Box(root,"North door lintel",new Vector3(19.49f,4.6825f,62.44f),new Vector3(1.3f,.645f,.32f),material);
        Box(root,"North continuous wall left",new Vector3(17.995f,3.505f,62.44f),new Vector3(1.69f,3f,.32f),material);
        Box(root,"North continuous wall right",new Vector3(21.57f,3.505f,62.44f),new Vector3(2.86f,3f,.32f),material);
        door.rotation=Quaternion.Euler(0,180,0);
        door.localScale=Vector3.one*106.6f;
        var renderer=door.GetComponent<Renderer>();
        var size=renderer.bounds.size;var scale=door.localScale;
        door.localScale=new Vector3(scale.x*1.27f/size.x,scale.y*2.28f/size.y,scale.z*.12f/size.z);
        door.position+=new Vector3(19.49f,3.175f,62.46f)-renderer.bounds.center;
        control.openAngle=100;
        control.Initialize();control.SetImmediate(false);
        // Remove the chair assembly whose seat crosses the new passage, including its separate collision hierarchy.
        var chairs=GameObject.Find("Level/SchoolPropsContent/EasterEggRoom/SchoolEasterEggRoom_Details/LibraryChair");
        if(chairs){chairs.SetActive(false);report.AppendLine("Removed chairs overlapping new entrance: "+chairs.name);}
        var chairCollisions=GameObject.Find("Level/SchoolPropsContent/EasterEggRoom/DetailsCollisions/LibraryChair");
        if(chairCollisions)chairCollisions.SetActive(false);
        // Keep only the starting room footprint in the recovery floor/ceiling patches.
        var floor=GameObject.Find("START ROOM — restored floor");
        floor.transform.position=new Vector3(20.075f,1.925f,60.48f);floor.transform.localScale=new Vector3(5.85f,.16f,3.96f);
        var ceiling=GameObject.Find("START ROOM — restored ceiling");
        ceiling.transform.position=new Vector3(20.075f,5.065f,60.48f);ceiling.transform.localScale=new Vector3(5.85f,.12f,3.96f);
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)){
            if(l.type!=LightType.Point)continue;
            var p=l.transform.position;
            if(p.x>17&&p.x<33&&p.z>56.9f&&p.z<62.4f&&p.y>5){
                if(p.x>23){l.gameObject.SetActive(false);continue;}
                l.transform.position=new Vector3(20.8f,4.65f,60.25f);
            }
        }
        Physics.SyncTransforms();
        bool closed=!Walk(new Vector3(19.49f,2.06f,60.4f),new Vector3(19.49f,2.06f,64.35f),report);
        control.SetImmediate(true);
        bool open=Walk(new Vector3(19.49f,2.06f,60.4f),new Vector3(19.49f,2.06f,64.35f),report);
        bool reverse=Walk(new Vector3(19.49f,2.06f,64.35f),new Vector3(19.49f,2.06f,60.4f),report);
        bool oldExitSealed=!Walk(new Vector3(21.8f,2.06f,59.9f),new Vector3(26.8f,2.06f,59.9f),report);
        control.SetImmediate(false);
        var body=player.GetComponent<CharacterController>();body.enabled=false;
        player.home=new Vector3(21.8f,2.06f,60.6f);player.transform.position=player.home;player.transform.rotation=Quaternion.identity;
        player.view.transform.localRotation=Quaternion.Euler(25,0,0);body.enabled=true;
        Physics.SyncTransforms();
        bool props=true;
        foreach(var item in Object.FindFirstObjectByType<StartRoom>().GetComponentsInChildren<StartRoomItem>()){
            var delta=item.transform.position-player.view.transform.position;
            var hits=Physics.RaycastAll(player.view.transform.position,delta.normalized,2.6f,~0,QueryTriggerInteraction.Ignore).Where(h=>!h.collider.GetComponentInParent<SchoolExplorer>()).OrderBy(h=>h.distance).ToArray();
            bool ok=hits.Length>0&&hits[0].collider.GetComponentInParent<StartRoomItem>()==item;props&=ok;report.AppendLine(item.name+" reachable="+ok);
        }
        report.AppendLine("Closed blocks="+closed+" Open passes="+open+" Reverse passes="+reverse+" Old exit sealed="+oldExitSealed+" Props reachable="+props);
        File.WriteAllText("RecoveryReports/relocate-start-door.txt",report.ToString());
        Capture(player.view,"RecoveryReports/north-exit-closed.png");
        control.SetImmediate(true);Capture(player.view,"RecoveryReports/north-exit-open.png");control.SetImmediate(false);
        if(!closed||!open||!reverse||!oldExitSealed||!props)throw new Exception("Layout validation failed; scene not saved. See report.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene,"Assets/Recovery/Scenes/School_Test.unity");
        AssetDatabase.SaveAssets();
        File.AppendAllText("RecoveryReports/relocate-start-door.txt","RESULT: PASS\n");
    }
    static void Box(Transform root,string name,Vector3 pos,Vector3 size,Material mat){
        var old=root.Find(name);if(old)Object.DestroyImmediate(old.gameObject);
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(name.StartsWith("North ")){
            var filter=g.GetComponent<MeshFilter>();var mesh=Object.Instantiate(filter.sharedMesh);
            var verts=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
            for(int i=0;i<verts.Length;i++){
                var world=g.transform.TransformPoint(verts[i]);
                uv[i]=new Vector2(Mathf.Abs(normals[i].z)>.5f?(world.x-17.15f)/3f:(world.z-62.28f)/3f,(world.y-2.005f)/3f);
            }
            mesh.uv=uv;mesh.RecalculateTangents();
            var path="Assets/Recovery/"+name.Replace(" ","_")+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);saved.vertices=mesh.vertices;saved.uv=mesh.uv;saved.RecalculateTangents();EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);filter.sharedMesh=saved;}
            else{AssetDatabase.CreateAsset(mesh,path);filter.sharedMesh=mesh;}
        }
    }
    static bool Walk(Vector3 start,Vector3 end,StringBuilder report){
        var probe=new GameObject("Layout traversal probe");probe.transform.position=start;
        var cc=probe.AddComponent<CharacterController>();cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.radius=.34f;cc.skinWidth=.025f;cc.stepOffset=.25f;Physics.SyncTransforms();
        var direction=(end-start).normalized;
        for(int i=0;i<Mathf.CeilToInt(Vector3.Distance(start,end)/.04f);i++)cc.Move(direction*.04f+Vector3.down*.035f);
        var delta=probe.transform.position-end;delta.y=0;bool pass=delta.magnitude<.16f&&Mathf.Abs(probe.transform.position.y-2)<.2f;
        report.AppendLine("Walk "+start+" -> "+end+" actual="+probe.transform.position+" pass="+pass);Object.DestroyImmediate(probe);return pass;
    }
    static void Capture(Camera cam,string path){
        var pos=cam.transform.position;var rot=cam.transform.rotation;var target=cam.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1100,650,24);var tex=new Texture2D(1100,650,TextureFormat.RGB24,false);
        try{cam.transform.position=new Vector3(20.8f,3.65f,59.45f);cam.transform.LookAt(new Vector3(20.3f,3.4f,62.45f));cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1100,650),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());}
        finally{cam.transform.position=pos;cam.transform.rotation=rot;cam.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
    }
}





