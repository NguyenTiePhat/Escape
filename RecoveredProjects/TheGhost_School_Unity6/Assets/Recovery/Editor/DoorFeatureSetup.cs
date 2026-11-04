using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class DoorFeatureSetup
{
    static DoorFeatureSetup(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string path="RecoveryReports/door-feature.request";
        if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string action=File.ReadAllText(path).Trim();File.Delete(path);
        if(action=="audit")Audit();else Run();
    }
    [MenuItem("Tools/FPS/Audit door leaves")]
    public static void Audit()
    {
        var s=new StringBuilder();
        foreach(var door in Object.FindObjectsByType<SchoolDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var box=door.GetComponent<BoxCollider>();
            s.AppendLine(PathOf(door.transform)+" active="+door.gameObject.activeInHierarchy+" width="+Width(box)+" localPos="+door.transform.localPosition+" pos="+door.transform.position);
            if(door.transform.parent)
                foreach(Transform sibling in door.transform.parent)
                { var b=sibling.GetComponent<BoxCollider>();if(b)s.AppendLine("  SIBLING "+sibling.name+" width="+Width(b)+" parent="+sibling.parent.name); }
        }
        File.WriteAllText("RecoveryReports/door-leaf-audit.txt",s.ToString());
    }
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static float Width(BoxCollider box) => box ? Mathf.Max(Mathf.Abs(box.size.x*box.transform.lossyScale.x),Mathf.Abs(box.size.z*box.transform.lossyScale.z)) : 0;
    public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");Run();if(Application.isBatchMode)EditorApplication.Exit(0);}
    [MenuItem("Tools/FPS/Set up interactive doors")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
        var scene=EditorSceneManager.GetActiveScene();
        if(!File.Exists("RecoveryReports/School_Test-before-door-feature.unity"))EditorSceneManager.SaveScene(scene,"RecoveryReports/School_Test-before-door-feature.unity",true);
        Audit();
        var s=new StringBuilder();int active=0,decorations=0;
        int layer=LayerMask.NameToLayer("Interactable");if(layer<0)throw new Exception("Missing Interactable layer");
        // The recovered small leaves live under SchoolModel, while the large leaves live
        // under Gameplay. Match their actual panel centres, heights and widths across roots.
        var boxes=Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var smallLeaves=boxes.Where(b=>System.Text.RegularExpressions.Regex.IsMatch(b.name,@"^LDoor\d*( \(\d+\))?$")).ToArray();
        var largeLeaves=boxes.Where(b=>System.Text.RegularExpressions.Regex.IsMatch(b.name,@"^RDoor\d*( \(\d+\))?$")).ToArray();
        var used=new System.Collections.Generic.HashSet<BoxCollider>();
        foreach(var small in smallLeaves)
        {
            Vector3 center=small.transform.TransformPoint(small.center);
            var big=largeLeaves.Where(b=>!used.Contains(b)&&Width(b)>Width(small)*1.2f)
                .Where(b=>Mathf.Abs(b.transform.TransformPoint(b.center).y-center.y)<.15f)
                .Where(b=>Vector3.Distance(b.transform.TransformPoint(b.center),center)<(Width(b)+Width(small))*.5f+.15f)
                .OrderBy(b=>Vector3.Distance(b.transform.TransformPoint(b.center),center)).FirstOrDefault();
            if(!big)throw new Exception("No large counterpart found for "+PathOf(small.transform));
            used.Add(big);
            // Recovered large leaves overlap the fixed infill by ~4 cm and scrape the
            // floor. Fit the mesh and collider together, leaving a real swing/floor gap.
            var fit=big.transform.localScale;
            fit.x*=(big.name=="RDoor1 (1)"?1.26f:1.30f)/Width(big);
            fit.y*=2.68f/Mathf.Abs(big.size.y*big.transform.lossyScale.y);
            big.transform.localScale=fit;
            var largeDoor=big.GetComponent<SchoolDoor>();if(!largeDoor)largeDoor=big.gameObject.AddComponent<SchoolDoor>();
            largeDoor.decorative=false;largeDoor.enabled=true;
            var smallDoor=small.GetComponent<SchoolDoor>();if(!smallDoor)smallDoor=small.gameObject.AddComponent<SchoolDoor>();
            smallDoor.decorative=true;smallDoor.enabled=false;
            s.AppendLine("PAIR movable="+PathOf(big.transform)+" width="+Width(big)+" decorative="+PathOf(small.transform)+" width="+Width(small));
        }
        foreach(var door in Object.FindObjectsByType<SchoolDoor>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Undo.RecordObject(door,"Configure Door interaction");
            door.speed=60;door.strongSpeed=160;
            // Mount the north leaf flush with the far face of the 32 cm wall. A thick
            // leaf centred inside that wall cannot swing without clipping a jamb.
            if(door.name=="Exit door hinge")
            {
                door.hingeOffset=Vector3.zero;
                var renderer=door.GetComponent<Renderer>();
                door.transform.position+=Vector3.forward*(62.66f-renderer.bounds.center.z);
            }
            door.enabled=!door.decorative;
            foreach(var c in door.GetComponentsInChildren<Collider>(true))
            {
                c.gameObject.layer=door.decorative?0:layer;
                GameObjectUtility.SetStaticEditorFlags(c.gameObject,door.decorative?GameObjectUtility.GetStaticEditorFlags(c.gameObject):0);
            }
            if(door.decorative)decorations++;else active++;
            if(!door.decorative&&door.name.StartsWith("RDoor"))
            {
                if(door.name=="RDoor1 (1)")door.transform.position=new Vector3(16.97f,door.transform.position.y,55.54f);
                door.Initialize();door.SetImmediate(false);door.openAngle=100;
                door.TrySetOpen(true,60);door.Tick(2);float positive=Mathf.Abs(door.CurrentAngle);door.SetImmediate(false);
                door.openAngle=-100;door.TrySetOpen(true,60);door.Tick(2);float negative=Mathf.Abs(door.CurrentAngle);door.SetImmediate(false);
                door.openAngle=negative>positive+.1f?-Mathf.Floor(negative):Mathf.Floor(positive);
                s.AppendLine("SWING "+door.name+" positive clearance="+positive+" negative clearance="+negative+" chosen="+door.openAngle);
            }
            EditorUtility.SetDirty(door);
        }
        foreach(var old in Object.FindObjectsByType<DoorInteraction>(FindObjectsInactive.Include,FindObjectsSortMode.None)){old.enabled=false;EditorUtility.SetDirty(old);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        s.AppendLine("Movable="+active+" decorative="+decorations+"; E=60 deg/s, Shift+E=160 deg/s. F input disabled.");
        File.WriteAllText("RecoveryReports/door-feature-setup.txt",s.ToString());
    }
}
