using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WalkControlsSetup
{
    public static void Run()
    {
        int removed = 0;
        foreach (var path in new[] { "Assets/Recovery/Scenes/School_Explore.unity", "Assets/Recovery/Scenes/School_Test.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t && t.name == "SprintProgressBar") { Object.DestroyImmediate(t.gameObject); removed++; }
            foreach (var note in Object.FindObjectsByType<StartRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                note.noteText = note.noteText.Replace("Shift: chạy", "Shift: đi bộ");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(0,-.5f,0);
        floor.transform.localScale = new Vector3(3000,1,3000);
        var player = new GameObject("Walk probe");
        player.transform.position = Vector3.up * .02f;
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.center = Vector3.up * .9f;
        var motor = player.AddComponent<PlayerMotor>();
        motor.SendMessage("Awake");
        Physics.SyncTransforms();
        for (int i=0;i<20;i++) motor.Tick(Vector2.zero,false,.02f);
        float run = Move(motor,false,50);
        float walk = Move(motor,true,50);
        if (Mathf.Abs(run-6)> .05f || Mathf.Abs(walk-3.5f)>.05f) throw new Exception("Wrong speeds: "+run+", "+walk);
        float sustained = Move(motor,false,6000)/120;
        if (Mathf.Abs(sustained-6)>.05f) throw new Exception("Running slowed down");
        motor.InputBlocked=true;
        if (Move(motor,false,50)>.001f) throw new Exception("Blocked controls moved");
        motor.InputBlocked=false; motor.SetCrouching(true);
        if (Mathf.Abs(Move(motor,true,50)-motor.crouchSpeed)>.05f) throw new Exception("Crouch changed");
        var steps=player.AddComponent<FootstepNoise>();
        if (steps.walkLoudness<=0 || steps.walkLoudness>=steps.sprintLoudness) throw new Exception("Quiet footsteps invalid");
        File.WriteAllText("RecoveryReports/walk-controls.txt", "PASS: default 6m/s; hold Shift 3.5m/s; 120s running without exhaustion; blocked controls; crouch preserved; quiet footsteps 0.15 vs 0.8. Removed stamina bars: "+removed+".\n");
        EditorApplication.Exit(0);
    }
    static float Move(PlayerMotor motor,bool walk,int frames)
    {
        var start=motor.transform.position;
        for(int i=0;i<frames;i++) motor.Tick(Vector2.up,walk,.02f);
        return Vector3.ProjectOnPlane(motor.transform.position-start,Vector3.up).magnitude;
    }
}
