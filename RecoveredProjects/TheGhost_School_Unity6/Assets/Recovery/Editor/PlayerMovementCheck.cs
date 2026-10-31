using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class PlayerMovementCheck
{
    static double started;
    static PlayerMovementCheck(){
        EditorApplication.update+=Poll;
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MovementCheck",false)){
                started=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;
            }
        };
    }
    static void Poll(){
        const string path="RecoveryReports/player-movement-check.request";
        if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(path);SessionState.SetBool("MovementCheck",true);EditorApplication.isPlaying=true;
    }
    public static void Run(){UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("MovementCheck",true);EditorApplication.isPlaying=true;}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Check(){
        if(EditorApplication.timeSinceStartup-started<2)return;
        EditorApplication.update-=Check;GameObject ceiling=null;
        try{
            var player=Object.FindFirstObjectByType<SchoolExplorer>();
            player.enabled=false;var motor=player.GetComponent<PlayerMotor>();var body=player.GetComponent<CharacterController>();
            body.enabled=false;player.transform.position=new Vector3(20.5f,2.06f,60);body.enabled=true;
            for(int i=0;i<20;i++)player.MoveGrounded(Vector3.zero,false,1f/60);
            Require(body.isGrounded,"Not grounded");
            float foot=player.transform.position.y+body.center.y-body.height*.5f;
            Require(player.SetCrouching(true),"Crouch failed");
            for(int i=0;i<60;i++)motor.Tick(Vector2.zero,false,1f/60);
            Require(Mathf.Abs(player.transform.position.y+body.center.y-body.height*.5f-foot)<.001f,"Crouch moved feet");
            Require(body.height<1.2f,"Crouch capsule too tall");
            ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube);ceiling.transform.position=new Vector3(20.5f,foot+1.55f,60);ceiling.transform.localScale=new Vector3(2,.3f,2);Physics.SyncTransforms();
            Require(!player.SetCrouching(false)&&player.IsCrouching,"Stood through low ceiling");
            Object.DestroyImmediate(ceiling);ceiling=null;Physics.SyncTransforms();
            Require(player.SetCrouching(false),"Could not stand in clear space");
            for(int i=0;i<10;i++)player.MoveGrounded(Vector3.zero,false,1f/60);
            for(int i=0;i<60;i++)motor.Tick(Vector2.zero,false,1f/60);
            Require(body.height>1.7f,"Standing did not restore capsule");
            File.WriteAllText("RecoveryReports/player-movement-check.txt","PASS: smooth crouch with fixed feet; low ceiling blocks standing; clear space allows standing. See FpsControllerCheck for speed/stamina/look checks.");
        }catch(Exception e){File.WriteAllText("RecoveryReports/player-movement-check.txt","FAIL: "+e);}
        finally{if(ceiling)Object.DestroyImmediate(ceiling);SessionState.SetBool("MovementCheck",false);if(Application.isBatchMode)EditorApplication.Exit(File.ReadAllText("RecoveryReports/player-movement-check.txt").StartsWith("PASS")?0:1);else EditorApplication.isPlaying=false;}
    }
}
