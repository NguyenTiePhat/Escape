using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class DoorMapCheck
{
    static DoorMapCheck(){EditorApplication.update+=Poll;}
    public static void RunBatch(){Run();EditorApplication.Exit(File.ReadAllText("RecoveryReports/door-map-check.txt").TrimEnd().EndsWith("PASS")?0:1);}
    static void Poll(){const string p="RecoveryReports/door-map-check.request";if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;File.Delete(p);Run();}
    [MenuItem("Tools/FPS/Check map door sweeps")]
    public static void Run()
    {
        // Always start from the saved closed poses, not transient edit-mode sweep results.
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        var report=new StringBuilder();bool exitPassed=false,playerSafe=false;int decorative=0,largeLeavesOpen=0;
        foreach(var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
        {
            if(door.decorative){decorative++;if(door.enabled||door.GetComponentsInChildren<Collider>().Any(c=>c.gameObject.layer==LayerMask.NameToLayer("Interactable")))throw new Exception("Small leaf still interactive: "+door.name);continue;}
            door.Initialize();door.SetImmediate(false);
            Vector3 closedCenter=door.GetComponentInChildren<BoxCollider>().bounds.center;
            door.TrySetOpen(true,60);
            for(int i=0;i<180;i++)door.Tick(1f/60);
            report.AppendLine(door.name+" angle="+door.CurrentAngle+" blocked="+door.IsBlocked+" by="+(door.BlockingCollider?door.BlockingCollider.name+" "+door.BlockingCollider.GetType().Name+" "+door.BlockingCollider.bounds:"none"));
            if(door.IsBlocked)report.AppendLine("  root="+door.transform.position+" panel="+(door.BlockingPanel?door.BlockingPanel.name+" "+door.BlockingPanel.bounds:"none"));
            if(door.name.StartsWith("RDoor")&&Mathf.Abs(door.CurrentAngle)>70)largeLeavesOpen++;
            if(door.name=="Exit door hinge")
            {
                exitPassed=door.CurrentAngle>70;
                if(exitPassed)
                {
                    var probe=new GameObject("Door player obstruction probe");
                    try
                    {
                        Vector3 pivot=door.transform.TransformPoint(door.hingeOffset);
                        probe.transform.position=pivot+Quaternion.AngleAxis(door.openAngle*.5f,door.transform.up)*(closedCenter-pivot);
                        var body=probe.AddComponent<CharacterController>();body.height=1.8f;body.radius=.25f;body.center=Vector3.zero;
                        Physics.SyncTransforms();door.TrySetOpen(false,160);door.Tick(2);
                        bool penetrating=false;
                        foreach(var panel in door.GetComponentsInChildren<BoxCollider>())
                            penetrating|=Physics.ComputePenetration(panel,panel.transform.position,panel.transform.rotation,body,body.transform.position,body.transform.rotation,out _,out float depth)&&depth>.001f;
                        playerSafe=door.IsBlocked&&door.CurrentAngle>1&&!penetrating;
                        report.AppendLine("North exit closes against player: angle="+door.CurrentAngle+" blocked="+door.IsBlocked+" penetrating="+penetrating);
                    }
                    finally{Object.DestroyImmediate(probe);Physics.SyncTransforms();}
                }
            }
            door.SetImmediate(false);
        }
        report.AppendLine("Decorative leaves="+decorative+"; large leaves opening="+largeLeavesOpen+"; north exit opens="+exitPassed+"; player safe="+playerSafe);
        report.AppendLine(exitPassed&&playerSafe&&decorative==4&&largeLeavesOpen==4?"PASS":"FAIL");
        File.WriteAllText("RecoveryReports/door-map-check.txt",report.ToString());
    }
}
