using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FloorTwoFurnishing
{
    const string ScenePath = "Assets/Recovery/Scenes/School_Test.unity";
    const string Reports = "RecoveryReports/";
    const string FurnitureRoot = "Floor 2 - XAPK furnishings";
    static readonly List<GameObject> added = new List<GameObject>();
    static readonly List<string> placementLog = new List<string>();
    [MenuItem("Recovery/Furnish second floor from XAPK")]
    public static void Build()
    {
        if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=EditorSceneManager.OpenScene(ScenePath);
        if(GameObject.Find(FurnitureRoot))throw new Exception("Second-floor furnishings already exist; refusing to duplicate or replace them.");
        var originals=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).SelectMany(t=>new Object[]{t.gameObject}.Concat(t.GetComponents<Component>().Where(c=>c))).ToDictionary(o=>o,o=>EditorJsonUtility.ToJson(o));
        Physics.SyncTransforms();
        var before=WalkGrid();
        const string backup=Reports+"School_Test-before-floor2-furnishing.unity";
        if(!File.Exists(backup))File.Copy(ScenePath,backup);
        added.Clear();placementLog.Clear();
        var root=new GameObject(FurnitureRoot);
        var west=Group(root,"West landing - lockers and waiting bench");
        for(int i=0;i<3;i++)Place(west,"Locker bank "+(i+1),"Lockers","Lockers_mtl",29.35f,52.9f+i*1.4f,90,1.1f);
        Place(west,"Waiting bench","Bench_LOD0","Bench_mtl",29.35f,50.15f,90,1.3f);
        Place(west,"Wastebasket","TrashCanSmall_LOD1","TrashCans_mtl",30.1f,56.15f,0,1.3f);
        var reading=Group(root,"North alcove - quiet reading tables");
        foreach(float x in new[]{38.4f,44.2f}) {
            var table=Place(reading,"Reading table "+x,"LibraryTable","LibraryTable_mtl",x,60,0,1.15f);
            Place(reading,"Reading chair west "+x,"LibraryChair_LOD1","LibraryTable_mtl",x-1.2f,60,90,1.15f);
            Place(reading,"Reading chair east "+x,"LibraryChair_LOD1","LibraryTable_mtl",x+1.2f,60,-85,1.15f);
            if(table) {
                var top=table.GetComponent<Renderer>().bounds.max.y;
                Place(reading,"Books on reading table "+x,"BookStack03","Books_mtl",x+.18f,60.1f,12,1.15f,top+.005f,false);
            }
        }
        Place(reading,"North wall bench","Bench_LOD0","Bench_mtl",35.4f,61.85f,0,1.3f);
        Place(reading,"Reading alcove wastebasket","TrashCanSmall_LOD1","TrashCans_mtl",47.4f,61.75f,0,1.3f);
        var east=Group(root,"Library approach - waiting benches");
        Place(east,"Bench west of library approach","Bench_LOD0","Bench_mtl",58.0f,52.8f,0,1.3f);
        Place(east,"Bench east of library approach","Bench_LOD0","Bench_mtl",67.1f,52.8f,0,1.3f);
        Place(east,"Library approach wastebasket","TrashCanSmall_LOD1","TrashCans_mtl",69.0f,52.85f,0,1.3f);
        var south=Group(root,"South window landing - resting area");
        Place(south,"Window landing bench","Bench_LOD0","Bench_mtl",53.65f,43.0f,0,1.3f);
        Physics.SyncTransforms();
        var after=WalkGrid();
        ValidateRoutes(before,after);
        foreach(var pair in originals)if(!pair.Key||EditorJsonUtility.ToJson(pair.Key)!=pair.Value)throw new Exception("Existing object changed: "+pair.Key);
        if(added.Count<15)throw new Exception("Too few valid placements; inspect placement report.");
        placementLog.Add("PASS: "+originals.Count+" pre-existing objects/components unchanged before save.");
        placementLog.Add("PASS: "+added.Count+" new mesh objects; original materials/meshes reused without edits; original NavMesh preserved, new furniture uses carving obstacles.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(Reports+"floor2-furnishing-check.txt",placementLog);
        Preview();
    }
    static Transform Group(GameObject root,string name){var g=new GameObject(name);g.transform.SetParent(root.transform,false);return g.transform;}
    static GameObject Place(Transform parent,string name,string meshName,string materialName,float x,float z,float angle,float scale,float surface=8.476f,bool solid=true)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/"+meshName+".asset");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/"+materialName+".mat");
        if(!mesh||!material||mesh.subMeshCount!=1)throw new Exception("Invalid recovered asset: "+meshName);
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.SetPositionAndRotation(new Vector3(x,surface-mesh.bounds.min.y*scale,z),Quaternion.Euler(0,angle,0));g.transform.localScale=Vector3.one*scale;
        g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        if(solid){
            var b=mesh.bounds;var center=g.transform.TransformPoint(b.center);var half=b.extents*scale-new Vector3(.015f,.03f,.015f);
            var hits=Physics.OverlapBox(center,half,g.transform.rotation,~0,QueryTriggerInteraction.Ignore).Where(c=>c.enabled).ToArray();
            if(hits.Length>0){placementLog.Add("SKIPPED "+name+": occupied by "+string.Join(",",hits.Select(h=>h.name)));Object.DestroyImmediate(g);return null;}
            foreach(float dx in new[]{-.85f,.85f})foreach(float dz in new[]{-.85f,.85f}){
                var foot=g.transform.TransformPoint(new Vector3(b.center.x+b.extents.x*dx,b.min.y,b.center.z+b.extents.z*dz));
                if(!Physics.RaycastAll(foot+Vector3.up*.2f,Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.7f&&Mathf.Abs(h.point.y-surface)<.06f))throw new Exception("Unsupported placement: "+name);
            }
            var collider=g.AddComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;
            var obstacle=g.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.center=b.center;obstacle.size=b.size;obstacle.carving=true;obstacle.carveOnlyStationary=true;
        }
        added.Add(g);Physics.SyncTransforms();placementLog.Add("ADDED "+name+" at "+g.transform.position.ToString("F3")+" mesh="+meshName+" material="+materialName);return g;
    }
    const int GridWidth=91,GridHeight=69;
    static bool[] WalkGrid(){var grid=new bool[GridWidth*GridHeight];for(int iz=0;iz<GridHeight;iz++)for(int ix=0;ix<GridWidth;ix++){
        var p=new Vector3(28+ix*.5f,8.476f,40+iz*.5f);
        bool floor=Physics.RaycastAll(p+Vector3.up*.2f,Vector3.down,.4f,~0,QueryTriggerInteraction.Ignore).Any(h=>h.normal.y>.7f&&Mathf.Abs(h.point.y-p.y)<.06f);
        grid[iz*GridWidth+ix]=floor&&!Physics.CheckCapsule(p+Vector3.up*.36f,p+Vector3.up*1.55f,.34f,~0,QueryTriggerInteraction.Ignore);
    }return grid;}
    static int Index(float x,float z)=>Mathf.RoundToInt((z-40)*2)*GridWidth+Mathf.RoundToInt((x-28)*2);
    static HashSet<int> Reach(bool[] grid,int start){var visited=new HashSet<int>();var q=new Queue<int>();if(!grid[start])return visited;visited.Add(start);q.Enqueue(start);while(q.Count>0){int p=q.Dequeue();foreach(int n in new[]{p-1,p+1,p-GridWidth,p+GridWidth}){if(n<0||n>=grid.Length||Math.Abs(n%GridWidth-p%GridWidth)>1||!grid[n]||!visited.Add(n))continue;q.Enqueue(n);}}return visited;}
    static void ValidateRoutes(bool[] before,bool[] after){
        var start=Index(40,54);var oldReach=Reach(before,start);var newReach=Reach(after,start);
        var targets=new[]{new Vector2(31,49),new Vector2(31,54),new Vector2(36,54),new Vector2(43,54),new Vector2(52,54),new Vector2(60,54),new Vector2(68,54),new Vector2(52,44),new Vector2(40,59),new Vector2(46.5f,60),new Vector2(64,58),new Vector2(64,67),new Vector2(58.5f,67)};
        int checkedCount=0;foreach(var target in targets){int i=Index(target.x,target.y);if(!oldReach.Contains(i)){placementLog.Add("Pre-existing unreachable sample "+target);continue;}checkedCount++;if(!newReach.Contains(i))throw new Exception("New furniture blocked a previously reachable route: "+target);}
        if(checkedCount<8)throw new Exception("Insufficient route samples: "+checkedCount);
        placementLog.Add("PASS: "+checkedCount+" previously reachable destinations remain connected for player-radius capsules (0.34m); hall, stairs, reading alcove and library approach.");
    }
    public static void Preview(){
        Render("floor2-after-hall",new Vector3(39.8f,10.1f,54.5f),new Vector3(60,9.6f,54.5f));
        Render("floor2-after-reading",new Vector3(47.5f,10.1f,57.8f),new Vector3(39,9.3f,60));
        Render("floor2-after-west",new Vector3(34f,10.1f,54.8f),new Vector3(29.3f,9.4f,53.8f));
        Render("floor2-after-library",new Vector3(63.8f,10.1f,57.8f),new Vector3(66,9.5f,68));
    }
    public static void ValidateSaved()
    {
        EditorSceneManager.OpenScene(ScenePath);var root=GameObject.Find(FurnitureRoot);
        if(!root||root.GetComponentsInChildren<MeshRenderer>().Length!=17)throw new Exception("Saved furniture missing");
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(!r.sharedMaterial||!r.sharedMaterial.mainTexture||!r.sharedMaterial.shader||!r.GetComponent<MeshFilter>().sharedMesh)throw new Exception("Missing furniture mesh/material/texture: "+r.name);
        root.SetActive(false);Physics.SyncTransforms();var before=WalkGrid();root.SetActive(true);Physics.SyncTransforms();var after=WalkGrid();placementLog.Clear();ValidateRoutes(before,after);
        var routes=new[]{new[]{new Vector3(31,8.52f,49),new Vector3(31,8.52f,54),new Vector3(52,8.52f,54),new Vector3(68,8.52f,54)},new[]{new Vector3(40,8.52f,54),new Vector3(40,8.52f,58),new Vector3(41,8.52f,60)},new[]{new Vector3(52,8.52f,54),new Vector3(52,8.52f,44)}};
        foreach(var route in routes){var probe=new GameObject("Temporary traversal probe");probe.transform.position=route[0];var cc=probe.AddComponent<CharacterController>();cc.radius=.34f;cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.skinWidth=.025f;cc.stepOffset=.3f;
            for(int i=1;i<route.Length;i++){for(int step=0;step<1000;step++){var delta=route[i]-probe.transform.position;delta.y=0;if(delta.magnitude<.08f)break;cc.Move(Vector3.ClampMagnitude(delta,.06f)+Vector3.down*.08f);}var remaining=route[i]-probe.transform.position;remaining.y=0;if(remaining.magnitude>.1f)throw new Exception("Character traversal blocked at "+probe.transform.position+" toward "+route[i]);}
            placementLog.Add("PASS actual CharacterController traversal "+string.Join(" -> ",route.Select(p=>p.ToString("F2"))));Object.DestroyImmediate(probe);
        }
        placementLog.Add("PASS saved scene reloaded: all 17 meshes, materials and textures resolve.");
        File.AppendAllLines(Reports+"floor2-furnishing-check.txt",placementLog);
    }
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
    public static void Survey()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        Physics.SyncTransforms();
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        File.WriteAllLines(Reports+"floor2-renderers.txt",all.SelectMany(t=>t.GetComponents<MeshRenderer>()).Where(r=>r.bounds.center.y>8.4f&&r.bounds.center.y<13.6f).Select(r=>PathOf(r.transform)+" enabled="+r.enabled+" active="+r.gameObject.activeInHierarchy+" "+r.bounds.ToString("F2")+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL"))));
        var lines=new List<string>();
        for(float z=40;z<=74;z+=.25f)for(float x=28;x<=73;x+=.25f){
            var hits=Physics.RaycastAll(new Vector3(x,9.2f,z),Vector3.down,1.1f,~0,QueryTriggerInteraction.Ignore);
            bool floor=hits.Any(h=>h.normal.y>.7f&&h.point.y>8.35f&&h.point.y<8.65f);
            bool blocked=Physics.CheckCapsule(new Vector3(x,8.9f,z),new Vector3(x,10.1f,z),.32f,~0,QueryTriggerInteraction.Ignore);
            lines.Add(x+","+z+","+(floor?1:0)+","+(blocked?1:0));
        }
        File.WriteAllLines(Reports+"floor2-grid.csv",lines);
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
            Render("floor2-before-library",new Vector3(63.8f,10.1f,57.8f),new Vector3(66,9.5f,68));
            Render("floor2-before-hall",new Vector3(39.8f,10.1f,54.5f),new Vector3(60,9.6f,54.5f));
        }
    }
    static void Render(string name,Vector3 position,Vector3 target)
    {
        var cam=new GameObject("Temporary furniture inspection").AddComponent<Camera>();
        cam.transform.position=position;cam.transform.LookAt(target);cam.nearClipPlane=.03f;cam.farClipPlane=100;cam.fieldOfView=70;
        var light=cam.gameObject.AddComponent<Light>();light.type=LightType.Point;light.range=25;light.intensity=3;
        var rt=new RenderTexture(1280,800,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
        var tex=new Texture2D(1280,800);tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes(Reports+name+".png",tex.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(cam.gameObject);
    }
    public static void Audit()
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var lines=new List<string>();
        foreach(var t in all) {
            var f=t.GetComponent<MeshFilter>(); var r=t.GetComponent<Renderer>();
            if(System.Text.RegularExpressions.Regex.IsMatch(t.name,"Floor|Stair|Room|Class|Hall|Desk|Chair|Table|Locker|Bench|Shelf|Board",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                lines.Add(PathOf(t)+" active="+t.gameObject.activeInHierarchy+" pos="+t.position.ToString("F2")+" mesh="+(f&&f.sharedMesh?f.sharedMesh.name:"-")+" bounds="+(r?r.bounds.ToString("F2"):"-")+" materials="+(r?string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")):""));
        }
        File.WriteAllLines(Reports+"floor2-audit.txt",lines);
        lines.Clear();
        foreach(var c in all.SelectMany(t=>t.GetComponents<Collider>()).Where(c=>c.enabled&&c.gameObject.activeInHierarchy))
          lines.Add(PathOf(c.transform)+" | "+c.GetType().Name+" | "+c.bounds.ToString("F3"));
        File.WriteAllLines(Reports+"floor2-colliders.txt",lines);
        var meshes=AssetDatabase.FindAssets("t:Mesh").Select(g=>AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(g))).Where(m=>m&&System.Text.RegularExpressions.Regex.IsMatch(m.name,"StudentDesk|Teacher|Library|Bench|Shelf|Locker"));
        File.WriteAllLines(Reports+"floor2-meshes.txt",meshes.Select(m=>m.name+" "+m.bounds.ToString("F3")+" vertices="+m.vertexCount+" submeshes="+m.subMeshCount));
    }
}
