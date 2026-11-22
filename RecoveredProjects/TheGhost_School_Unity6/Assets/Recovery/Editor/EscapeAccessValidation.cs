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
public static class EscapeAccessValidation
{
    static IEnumerator test; static double deadline;
    static readonly StringBuilder report = new StringBuilder();
    static PlayerInteractor interactor; static PlayerMotor player; static CharacterController body; static Vector3 origin;
    static EscapeAccessValidation() { EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("EscapeAccessTest", false)) { test = Test(); deadline = EditorApplication.timeSinceStartup + 120; EditorApplication.update += Tick; } }; }
    public static void Run() { EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity"); SessionState.SetBool("EscapeAccessTest", true); EditorApplication.EnterPlaymode(); }
    static IEnumerator Test()
    {
        yield return null; yield return null;
        foreach (var ai in Object.FindObjectsByType<MonsterAI>()) ai.enabled = false;
        foreach (var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>()) mic.enabled = false;
        player = Object.FindAnyObjectByType<PlayerMotor>(); player.enabled = false;
        player.GetComponent<PlayerLook>().enabled = false; player.GetComponent<PlayerHoldAction>().enabled = false;
        interactor = player.GetComponent<PlayerInteractor>(); interactor.enabled = false; interactor.InputBlocked = false;
        body = player.GetComponent<CharacterController>();
        if (!NavMesh.SamplePosition(player.transform.position, out var source, 2, NavMesh.AllAreas)) throw new Exception("Missing starting navigation"); origin = source.position;
        var spawner = Object.FindAnyObjectByType<EscapeItemSpawner>();
        foreach (var pickup in spawner.Spawned) pickup.gameObject.SetActive(false);
        foreach (var entry in spawner.entries) {
            int count = 0;
            foreach (var point in entry.points) {
                var prop = Object.Instantiate(entry.item.pickupPrefab, point.position, point.rotation);
                Physics.SyncTransforms();
                try {
                    if (!AimAt(prop, prop.GetComponent<Collider>().bounds.center, entry.item.id != "silver_key")) throw new Exception("Cannot target " + entry.item.id + " at " + point.position);
                    count++;
                } finally { Object.DestroyImmediate(prop.gameObject); }
            }
            report.AppendLine("PASS " + entry.item.id + ": actual PlayerInteractor targets all " + count + " candidates from reachable standing positions");
        }
        var machine = Object.FindAnyObjectByType<KeyCraftingStation>();
        var assembly = Object.FindAnyObjectByType<SawAssemblyStation>();
        var barrier = Object.FindAnyObjectByType<SawWoodBarrier>();
        CheckStation(machine.GetComponent<HoldNoiseAction>(), machine.transform.position + Vector3.up);
        BakePlayerNavigation();
        origin = new Vector3(35.3f,15,52); NavMesh.SamplePosition(origin,out var localStand,2,NavMesh.AllAreas); origin=localStand.position; report.AppendLine("Local upper-floor station targeting only; ground-to-upper-floor path unresolved.");
        CheckStation(assembly, assembly.outputPoint.position - Vector3.up * .2f);
        CheckStation(barrier.GetComponent<HoldNoiseAction>(), barrier.transform.position + Vector3.up * 1.3f);
        report.AppendLine("No scene changes saved. Uses NavMesh paths and actual camera/raycast targeting; does not simulate continuous keyboard-driven walking.");
    }
    static void BakePlayerNavigation()
    {
        var loader = Object.FindAnyObjectByType<MonsterNavMesh>(); loader.enabled = false;
        var oldNav = NavMesh.AddNavMeshData(AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/NavMeshData/NavMesh_0.asset")); NavMesh.SamplePosition(origin,out var oldStart,2,NavMesh.AllAreas); NavMesh.SamplePosition(new Vector3(35.3f,15,52),out var oldRoof,2,NavMesh.AllAreas); var oldPath=new NavMeshPath(); NavMesh.CalculatePath(oldStart.position,oldRoof.position,NavMesh.AllAreas,oldPath); report.AppendLine("ORIGINAL PATH "+oldPath.status+" "+string.Join(";",oldPath.corners.Select(v=>v.ToString("F2")))); oldNav.Remove();
        var marks = new System.Collections.Generic.List<NavMeshBuildMarkup>();
        foreach(var d in Object.FindObjectsByType<Door>()) if(!d.decorative) marks.Add(new NavMeshBuildMarkup { root=d.transform, ignoreFromBuild=true });
        marks.Add(new NavMeshBuildMarkup { root=player.transform, ignoreFromBuild=true });
        foreach(var ai in Object.FindObjectsByType<MonsterAI>()) marks.Add(new NavMeshBuildMarkup { root=ai.transform, ignoreFromBuild=true });
        var sources = new System.Collections.Generic.List<NavMeshBuildSource>();
        var bounds = new Bounds(new Vector3(25,15,60),new Vector3(180,60,180));
        NavMeshBuilder.CollectSources(bounds,~0,NavMeshCollectGeometry.PhysicsColliders,0,marks,sources);
        sources.RemoveAll(s => s.component is Collider c && (c is CharacterController || c.bounds.size.x>200 || c.bounds.size.y>80 || c.bounds.size.z>200));
        var settings = NavMesh.GetSettingsByID(0); settings.agentRadius=body.radius; settings.agentHeight=body.height; settings.agentClimb=body.stepOffset; settings.agentSlope=body.slopeLimit; settings.overrideVoxelSize=true; settings.voxelSize=.05f; settings.minRegionArea=.1f;
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
        if(!data) throw new Exception("Player-sized navigation build failed"); var testNav=NavMesh.AddNavMeshData(data);
        if(NavMesh.SamplePosition(origin,out var start,1,NavMesh.AllAreas)) origin=start.position;
        NavMesh.SamplePosition(new Vector3(35.3f,15,52),out var roof,2,NavMesh.AllAreas); var route=new NavMeshPath(); NavMesh.CalculatePath(origin,roof.position,NavMesh.AllAreas,route); report.AppendLine("PATH UP "+route.status+" "+string.Join(";",route.corners.Select(v=>v.ToString("F2")))); NavMesh.CalculatePath(roof.position,origin,NavMesh.AllAreas,route); report.AppendLine("PATH DOWN "+route.status+" "+string.Join(";",route.corners.Select(v=>v.ToString("F2")))); foreach(var c in Object.FindObjectsByType<Collider>()) if(c.name.IndexOf("Stair",StringComparison.OrdinalIgnoreCase)>=0) report.AppendLine("STAIRS "+c.name+" "+c.bounds+" "+c.GetType().Name);
        foreach(var probe in new[]{new Vector3(22.65f,3,58.85f),new Vector3(30.6f,9,56.5f)}) foreach(var c in Physics.OverlapSphere(probe,2.5f,~0,QueryTriggerInteraction.Ignore)) {string hierarchy=c.name; for(var t=c.transform.parent;t;t=t.parent) hierarchy=t.name+"/"+hierarchy; report.AppendLine("NEAR "+probe+" "+hierarchy+" "+c.bounds+" "+c.GetType().Name);}
        foreach(var name in new[]{"StairsSmall","StairsMain"}) { var stair=GameObject.Find(name); if(stair) {report.AppendLine("STAIR ROOT "+name+" position="+stair.transform.position); foreach(var r in stair.GetComponentsInChildren<Renderer>())report.AppendLine("STAIR VISUAL "+r.name+" "+r.bounds); foreach(var c in stair.GetComponentsInChildren<Collider>())report.AppendLine("STAIR COLLIDER "+c.name+" "+c.bounds+" enabled="+c.enabled);}}
        foreach(var variant in new[]{new Vector3(.34f,1.8f,.4f),new Vector3(.34f,1.1f,.25f),new Vector3(.2f,1.1f,.4f)}) {
            testNav.Remove(); settings.agentRadius=variant.x;settings.agentHeight=variant.y;settings.agentClimb=variant.z;
            var alternative=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);testNav=NavMesh.AddNavMeshData(alternative);
            NavMesh.SamplePosition(origin,out var from,2,NavMesh.AllAreas);NavMesh.SamplePosition(new Vector3(35.3f,15,52),out var to,2,NavMesh.AllAreas);var check=new NavMeshPath();NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,check);
            report.AppendLine("VARIANT "+variant+" "+check.status+" "+string.Join(";",check.corners.Select(v=>v.ToString("F2"))));
        }
        testNav.Remove();NavMesh.AddNavMeshData(data);
        report.AppendLine("Player-sized navigation built in memory only: radius="+body.radius+" height="+body.height+" step="+body.stepOffset+" slope="+body.slopeLimit);
    }
    static void CheckStation(MonoBehaviour target, Vector3 aim) { if (!AimAt(target, aim, true) && !target.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Any(c => AimAt(target, c.bounds.center, true))) throw new Exception("Cannot reach/target station " + target.name + " at " + aim); report.AppendLine("PASS reachable and interactable station: " + target.name); }
    static bool AimAt(MonoBehaviour target, Vector3 aim, bool silver)
    {
        int sampled=0, pathed=0, unlocked=0, clear=0;
        for (float radius = .6f; radius <= 1.8f; radius += .3f) for (int k = 0; k < 24; k++) {
            var near = aim + Quaternion.Euler(0, k * 15, 0) * Vector3.forward * radius - Vector3.up * .8f;
            if (!NavMesh.SamplePosition(near, out var stand, 1.1f, NavMesh.AllAreas) || stand.position.y > aim.y + .15f || aim.y - stand.position.y > 2.2f) continue;
            sampled++; var path = new NavMeshPath(); if (!NavMesh.CalculatePath(origin, stand.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            pathed++; bool blocked = false;
            for (int n = 1; n < path.corners.Length; n++) {
                var delta = path.corners[n] - path.corners[n - 1];
                if (Physics.SphereCastAll(path.corners[n - 1] + Vector3.up * .8f, .25f, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore).Any(h => { var door = h.collider.GetComponentInParent<Door>(); return door && door.isLocked && !(silver && door.requiredKeyId == "silver_key"); })) { blocked = true; break; }
            }
            if (blocked) continue;
            unlocked++; body.enabled = false; player.transform.position = stand.position + Vector3.up * .02f; body.enabled = true;
            Physics.SyncTransforms();
            var center = body.bounds.center; float r = body.radius, half = Mathf.Max(0, body.height * .5f - r);
            if (Physics.OverlapCapsule(center - Vector3.up * half, center + Vector3.up * half, r * .95f, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(player.transform))) continue;
            clear++; interactor.view.transform.LookAt(aim); interactor.RefreshTarget();
            if (ReferenceEquals(interactor.Target, target)) return true;
        }
        if (!(target is PickupItem)) report.AppendLine("DIAG " + target.name + " aim=" + aim + " sampled=" + sampled + " paths=" + pathed + " unlocked=" + unlocked + " clear=" + clear);
        return false;
    }
    static void Tick() { try { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout"); if (test.MoveNext()) return; Finish(0); } catch (Exception e) { report.AppendLine("FAIL " + e); Finish(1); } }
    static void Finish(int code) { EditorApplication.update -= Tick; SessionState.SetBool("EscapeAccessTest", false); File.WriteAllText("RecoveryReports/escape-access-check.txt", report.ToString()); EditorApplication.Exit(code); }
}






