using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;
[InitializeOnLoad]
public static class DoorPlayCheck
{
    static double start;
    static DoorPlayCheck() { EditorApplication.update += Poll; EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DoorPlayCheck",false)) {
            start = EditorApplication.timeSinceStartup; EditorApplication.update += Check;
        }
    }; }
    static void Poll() {
        const string request="RecoveryReports/north-door-play.request";
        if (!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode) return;
        File.Delete(request); Run();
    }
    public static void Run() {
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        SessionState.SetBool("DoorPlayCheck",true); EditorApplication.isPlaying = true;
    }
    static void Check() {
        if (EditorApplication.timeSinceStartup-start < 3) return;
        EditorApplication.update -= Check;
        int result=0;
        try {
            var p=Object.FindFirstObjectByType<SchoolExplorer>();
            var body=p.GetComponent<CharacterController>(); body.enabled=false;
            p.transform.position=new Vector3(19.49f,2.06f,61.15f);
            Physics.SyncTransforms();
            var interaction=p.GetComponent<DoorInteraction>();
            var door=interaction.FindNearby();
            if (!door || door.name!="Exit door hinge") throw new Exception("Near door not selected");
            door.Toggle(); if (!door.IsOpen) throw new Exception("Toggle open failed");
            door.SetImmediate(true);
            p.transform.position=new Vector3(19.49f,2.06f,63.65f); Physics.SyncTransforms();
            if (interaction.FindNearby()!=door) throw new Exception("Door unavailable from reverse side");
            door.Toggle(); if (door.IsOpen) throw new Exception("Toggle close failed");
            door.SetImmediate(false);
            p.transform.position=new Vector3(21.8f,2.06f,58.9f); Physics.SyncTransforms();
            if(interaction.FindNearby()==door) throw new Exception("Out-of-range interaction");
            File.WriteAllText("RecoveryReports/door-play-check.txt","PASS: proximity selection, opening, reverse-side interaction, closing, range limit. Active doors="+SchoolDoor.Active.Count);
        } catch(Exception e) { result=1; File.WriteAllText("RecoveryReports/door-play-check.txt","FAIL: "+e); }
        SessionState.SetBool("DoorPlayCheck",false); if(Application.isBatchMode) EditorApplication.Exit(result); else EditorApplication.isPlaying=false;
    }
}


