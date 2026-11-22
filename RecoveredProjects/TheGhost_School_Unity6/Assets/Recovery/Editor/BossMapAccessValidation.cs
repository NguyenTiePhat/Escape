using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BossMapAccessValidation
{
    static IEnumerator test;
    static readonly StringBuilder report = new StringBuilder();
    static BossMapAccessValidation() { EditorApplication.playModeStateChanged += s => {
        if (s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BossMapTest",false)) {test=Test();EditorApplication.update+=Tick;}
    }; }
    public static void Run() { EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");SessionState.SetBool("BossMapTest",true);EditorApplication.EnterPlaymode(); }
    static IEnumerator Test()
    {
        yield return null;yield return null;
        var boss=Object.FindAnyObjectByType<MonsterAI>();var agent=boss.GetComponent<NavMeshAgent>();
        NoiseManager.Instance.debugLogging=false;
        foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
        var player=Object.FindAnyObjectByType<PlayerMotor>();player.enabled=false;player.GetComponent<PlayerLook>().enabled=false;
        var health=player.GetComponent<PlayerHealth>();if(!health.IsInvulnerable)health.ToggleInvulnerability();
        boss.config=Object.Instantiate(boss.config);boss.config.sightRange=0;
        boss.enabled=false;boss.enabled=true;
        NoiseManager.EmitNoise(BossMapAccessSetup.Destinations[7],0,NoiseSource.Footstep);
        if(boss.CurrentState!=MonsterState.Patrol)throw new Exception("Silent event attracted boss");
        NoiseManager.EmitNoise(BossMapAccessSetup.Destinations[7],.05f,NoiseSource.Footstep);
        if(boss.CurrentState!=MonsterState.Investigate || Vector3.Distance(boss.LastNoisePosition,BossMapAccessSetup.Destinations[7])>.01f)throw new Exception("Far quiet noise not investigated");
        report.AppendLine("PASS global quiet noise from upper east hallway, distance="+Vector3.Distance(boss.transform.position,boss.LastNoisePosition)+"m; silent event ignored");
        boss.enabled=false;
        var guard=boss.GetComponent<MonsterDoorNavigation>();guard.enabled=false;guard.enabled=true;
        var doors=Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&d.requiredKeyId=="silver_key").ToArray();
        var initialLocks=doors.Select(d=>d.isLocked).ToArray();
        agent.speed=6.3f;agent.acceleration=28;agent.SetDestination(BossMapAccessSetup.Destinations[1]);agent.isStopped=false;
        Time.timeScale=4;Time.maximumDeltaTime=.05f;
        double until=EditorApplication.timeSinceStartup+8;bool lockedStopped=false;
        while(EditorApplication.timeSinceStartup<until) {if(guard.Tick(agent)&&agent.isStopped&&doors.Any(d=>d.isLocked&&Vector3.Distance(d.NearestPoint(agent.transform.position+Vector3.up),agent.transform.position+Vector3.up)<2f)){lockedStopped=true;break;}yield return null;}
        if(!lockedStopped || doors.Where((d,i)=>d.isLocked!=initialLocks[i]).Any())throw new Exception("Boss bypassed locked room door");
        report.AppendLine("PASS boss waits at locked room door; silver-key locks retained");
        yield return null;yield return null;
        boss.enabled=true;
        NoiseManager.EmitNoise(BossMapAccessSetup.Destinations[7],.05f,NoiseSource.Footstep);
        if(boss.CurrentState!=MonsterState.Investigate)throw new Exception("Noise behind carved locked door ignored");
        boss.enabled=false;
        report.AppendLine("PASS noise behind locked door remains audible; investigation approaches reachable side");
        File.WriteAllText("RecoveryReports/boss-map-access-check.txt",report.ToString());
        // Unlock only in this test session, as if the player had used the key. Boss opens the leaves itself.
        foreach(var d in doors)d.UnlockDoor("silver_key");
        guard.enabled=false;guard.enabled=true;
        var barricade=Object.FindObjectsByType<Transform>().Single(t=>t.name=="1stFloorBarricade (2)");
        int besideWoodSamples=0;
        for(int i=1;i<boss.waypoints.Length;i++)
        {
            agent.SetDestination(boss.waypoints[i].position);agent.isStopped=false;
            until=EditorApplication.timeSinceStartup+60;
            while(EditorApplication.timeSinceStartup<until)
            {
                guard.Tick(agent);
                if(i==4)
                {
                    var p=agent.transform.position;
                    foreach(var c in Physics.OverlapCapsule(p+Vector3.up*(agent.radius+.08f),p+Vector3.up*(agent.height-agent.radius),agent.radius-.025f,~0,QueryTriggerInteraction.Ignore))
                        if(c.transform.IsChildOf(barricade))throw new Exception("Boss penetrated barricade "+c.name+" at "+p);
                    if(p.x>26&&p.x<30)besideWoodSamples++;
                }
                if(!agent.pathPending && Vector3.Distance(agent.transform.position,boss.waypoints[i].position)<.45f)break;
                yield return null;
            }
            if(Vector3.Distance(agent.transform.position,boss.waypoints[i].position)>.45f)
            {
                report.AppendLine("AGENT rollback="+guard.LastMovementBlocker+" stopped="+agent.isStopped+" velocity="+agent.velocity+" steering="+agent.steeringTarget+" remaining="+agent.remainingDistance+" corners="+string.Join(";",agent.path.corners.Select(p=>p.ToString("F2"))));
                foreach(var d in doors.Where(d=>Vector3.Distance(d.transform.position,agent.transform.position)<8)) report.AppendLine("NEAR DOOR "+MapCollisionAudit.PathOf(d.transform)+" angle="+d.CurrentAngle+" blocked="+d.BlockingCollider+" obstacles="+d.GetComponentsInChildren<NavMeshObstacle>().Length);
                foreach(var o in Object.FindObjectsByType<NavMeshObstacle>())report.AppendLine("OBSTACLE "+MapCollisionAudit.PathOf(o.transform)+" enabled="+o.enabled+" carve="+o.carving+" center="+o.transform.TransformPoint(o.center)+" size="+Vector3.Scale(o.size,o.transform.lossyScale));
                var dir=Vector3.ProjectOnPlane(agent.steeringTarget-agent.transform.position,Vector3.up);
                foreach(var hit in Physics.CapsuleCastAll(agent.transform.position+Vector3.up*(agent.radius+.08f),agent.transform.position+Vector3.up*(agent.height-agent.radius),agent.radius,dir.normalized,Mathf.Min(dir.magnitude,1.6f),~0,QueryTriggerInteraction.Ignore))report.AppendLine("CAST "+MapCollisionAudit.PathOf(hit.transform)+" type="+hit.collider.GetType()+" bounds="+hit.collider.bounds+" distance="+hit.distance);
                throw new Exception("Traversal failed target="+i+" position="+agent.transform.position+" target="+boss.waypoints[i].position+" path="+agent.pathStatus+" door="+guard.OperatingDoor+" angle="+(guard.OperatingDoor?guard.OperatingDoor.CurrentAngle:0)+" blocked="+(guard.OperatingDoor&&guard.OperatingDoor.BlockingCollider?MapCollisionAudit.PathOf(guard.OperatingDoor.BlockingCollider.transform):"none"));
            }
            report.AppendLine("PASS actual boss agent traverses patrol "+i+" at "+agent.transform.position);
            File.WriteAllText("RecoveryReports/boss-map-access-check.txt",report.ToString());
        }
        if(besideWoodSamples==0)throw new Exception("Barricade passage not sampled");
        report.AppendLine("PASS side gap of 1stFloorBarricade (2): "+besideWoodSamples+" capsule samples without wood overlap");
        boss.config.patrolSpeed=6.3f;boss.enabled=true;
        foreach(int goal in new[]{0,10})
        {
            boss.enabled=false;boss.enabled=true;
            float volume=.1f;float nextNoise=Time.time+1;
            NoiseManager.EmitNoise(boss.waypoints[goal].position,volume,NoiseSource.Footstep);
            until=EditorApplication.timeSinceStartup+60;
            while(EditorApplication.timeSinceStartup<until && Vector3.Distance(boss.transform.position,boss.waypoints[goal].position)>.5f)
            {
                if(Time.time>=nextNoise){nextNoise=Time.time+1;volume=Mathf.Min(1,volume+.02f);NoiseManager.EmitNoise(boss.waypoints[goal].position,volume,NoiseSource.Footstep);}
                yield return null;
            }
            if(Vector3.Distance(boss.transform.position,boss.waypoints[goal].position)>.5f)throw new Exception("Full AI investigation failed goal="+goal+" at="+boss.transform.position+" state="+boss.CurrentState+" link="+agent.isOnOffMeshLink);
            report.AppendLine("PASS full MonsterAI follows repeated noises across floors to patrol "+goal);
            File.WriteAllText("RecoveryReports/boss-map-access-check.txt",report.ToString());
        }
        boss.enabled=false;
        // Verify the short link spans also physically admit the full-sized boss capsule.
        foreach(var passage in Object.FindAnyObjectByType<MonsterStairLinks>().passages)
        {
            var go=new GameObject("Stair clearance probe");go.transform.position=passage.start+Vector3.up*.04f;
            var cc=go.AddComponent<CharacterController>();cc.height=agent.height;cc.radius=agent.radius;cc.center=Vector3.up*agent.height*.5f;cc.stepOffset=.3f;cc.skinWidth=.025f;
            for(int i=0;i<300;i++){var delta=Vector3.ProjectOnPlane(passage.end-go.transform.position,Vector3.up);if(delta.magnitude<.08f)break;cc.Move(Vector3.ClampMagnitude(delta,.03f)+Vector3.down*.03f);}
            var error=Vector3.Distance(go.transform.position,passage.end);
            Object.DestroyImmediate(go);if(error>.3f)throw new Exception("Stair link physical clearance failed "+error);
            report.AppendLine("PASS full boss capsule crosses stair seam "+passage.start+" to "+passage.end);
        }
        report.AppendLine("No scene saved. Door unlocks and speed/time test changes are runtime only.");
    }
    static void Tick(){try{if(test.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
    static void Finish(int code){Time.timeScale=1;EditorApplication.update-=Tick;SessionState.SetBool("BossMapTest",false);File.WriteAllText("RecoveryReports/boss-map-access-check.txt",report.ToString());EditorApplication.Exit(code);}
}
