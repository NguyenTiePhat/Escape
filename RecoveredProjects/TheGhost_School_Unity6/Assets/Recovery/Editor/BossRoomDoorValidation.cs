using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class BossRoomDoorValidation
{
    const string DoorPath="Gameplay/Doors/DoorP1/Door";
    static IEnumerator test;
    static readonly StringBuilder report=new StringBuilder();
    static BossRoomDoorValidation(){EditorApplication.playModeStateChanged+=s=>{
        if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("BossRoomDoorTest",false)){test=Test();EditorApplication.update+=Tick;}
    };}
    public static void Run()
    {
        const string scenePath="Assets/Recovery/Scenes/School_Test.unity";
        var scene=EditorSceneManager.OpenScene(scenePath);
        const string backup="RecoveryReports/School_Test-before-boss-room-unlock.unity";
        if(!File.Exists(backup))File.Copy(scenePath,backup);
        var door=GameObject.Find(DoorPath).GetComponent<Door>();
        door.isLocked=false;
        var config=Object.FindAnyObjectByType<MonsterAI>().config;
        config.investigateSpeed=3.8f;config.noisePrioritySeconds=.6f;
        EditorUtility.SetDirty(door);EditorUtility.SetDirty(config);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        SessionState.SetBool("BossRoomDoorTest",true);EditorApplication.EnterPlaymode();
    }
    static IEnumerator Test()
    {
        yield return null;yield return null;
        NoiseManager.Instance.debugLogging=false;
        foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
        var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<PlayerLook>().enabled=false;
        var health=player.GetComponent<PlayerHealth>();if(!health.IsInvulnerable)health.ToggleInvulnerability();
        var boss=Object.FindAnyObjectByType<MonsterAI>();var agent=boss.GetComponent<NavMeshAgent>();
        boss.config=Object.Instantiate(boss.config);boss.config.sightRange=0;
        boss.enabled=false;boss.enabled=true;
        var door=GameObject.Find(DoorPath).GetComponent<Door>();
        var otherDoors=Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&d!=door).ToArray();
        var locks=otherDoors.Select(d=>d.isLocked).ToArray();
        if(door.isLocked||!door.IsFullyClosed)throw new Exception("Boss room door must start closed and unlocked");
        report.AppendLine("PASS boss room door starts closed/unlocked; all other door locks recorded");
        Time.timeScale=3;Time.maximumDeltaTime=.05f;
        var loudPoint=new Vector3(68,8.5f,54);
        NoiseManager.EmitNoise(loudPoint,1,NoiseSource.Footstep);
        if(Vector3.Distance(boss.LastNoisePosition,loudPoint)>.01f)throw new Exception("Far loud noise ignored");
        float until=Time.time+.7f;while(Time.time<until)yield return null;
        var destination=new Vector3(-5.4f,2,40.3f);
        if(!NavMesh.SamplePosition(destination,out var target,1,NavMesh.AllAreas))throw new Exception("Door exit target missing");
        NoiseManager.EmitNoise(target.position,.01f,NoiseSource.Footstep);
        if(Vector3.Distance(boss.LastNoisePosition,target.position)>.01f)throw new Exception("Old loud noise still suppresses fresh quiet noise");
        report.AppendLine("PASS new 0.01 quiet noise overrides old loud sound after 0.6s");
        double deadline=EditorApplication.timeSinceStartup+25;float peak=0;
        while(EditorApplication.timeSinceStartup<deadline&&Vector3.Distance(boss.transform.position,target.position)>.4f)
        {peak=Mathf.Max(peak,agent.velocity.magnitude);yield return null;}
        if(Vector3.Distance(boss.transform.position,target.position)>.4f||Mathf.Abs(door.CurrentAngle)<20)throw new Exception("Boss failed to open/cross its room door: "+boss.transform.position+" angle="+door.CurrentAngle);
        if(peak<2.5f)throw new Exception("Investigation movement remained slow: "+peak);
        report.AppendLine("PASS full AI opens and crosses its own room door without a key; peak speed="+peak+" configured="+boss.config.investigateSpeed);
        until=Time.time+.7f;while(Time.time<until)yield return null;
        var next=target.position+Vector3.forward*.3f;
        NoiseManager.EmitNoise(next,.01f,NoiseSource.Footstep);
        if(Vector3.Distance(boss.LastNoisePosition,next)>.01f)throw new Exception("Equal-volume new noise ignored");
        report.AppendLine("PASS equal-volume new footsteps retarget investigation");
        for(int i=0;i<otherDoors.Length;i++)if(otherDoors[i].isLocked!=locks[i])throw new Exception("Changed another door lock: "+otherDoors[i]);
        report.AppendLine("PASS other room/escape door locks unchanged; no test-only state saved");
    }
    static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
    static void Finish(int code){Time.timeScale=1;EditorApplication.update-=Tick;SessionState.SetBool("BossRoomDoorTest",false);File.WriteAllText("RecoveryReports/boss-room-door-check.txt",report.ToString());EditorApplication.Exit(code);}
}
