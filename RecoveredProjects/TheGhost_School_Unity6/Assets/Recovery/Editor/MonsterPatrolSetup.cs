using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class MonsterPatrolSetup
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
            if (!File.Exists("RecoveryReports/School_Test-before-monster-patrol.unity")) File.Copy(scene.path, "RecoveryReports/School_Test-before-monster-patrol.unity");
            var root = GameObject.Find("M4 Boss"); if (!root) throw new Exception("Boss missing");
            var navigation = GameObject.Find("Monster Navigation"); if (!navigation) navigation = new GameObject("Monster Navigation");
            var loader = navigation.GetComponent<MonsterNavMesh>(); if (!loader) loader = navigation.AddComponent<MonsterNavMesh>();
            loader.enabled = false; loader.data = AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/NavMeshData/NavMesh_0.asset"); loader.enabled = true;
            if (!NavMesh.SamplePosition(root.transform.position, out var spawn, .75f, NavMesh.AllAreas)) throw new Exception("Boss spawn not on school NavMesh");
            root.transform.position = spawn.position;
            var prototype = root.GetComponent<BossPrototypeMotion>(); prototype.previewLoop = false; prototype.enableTestKeys = false; prototype.enabled = false;
            root.GetComponent<CharacterController>().enabled = false;
            var agent = root.GetComponent<NavMeshAgent>(); if (!agent) agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = 0; agent.radius = .42f; agent.height = 2.2f; agent.baseOffset = 0; agent.acceleration = 4;
            agent.angularSpeed = 120; agent.stoppingDistance = .18f; agent.autoBraking = true; agent.autoTraverseOffMeshLink = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            const string configPath = "Assets/Recovery/Boss/MonsterConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<MonsterConfig>(configPath);
            if (!config) { config = ScriptableObject.CreateInstance<MonsterConfig>(); AssetDatabase.CreateAsset(config, configPath); }
            agent.speed = config.patrolSpeed;
            var ai = root.GetComponent<MonsterAI>(); if (!ai) ai = root.AddComponent<MonsterAI>();
            ai.config = config; ai.animationDriver = root.GetComponent<BossAnimationDriver>();
            Physics.SyncTransforms();
            var candidates = new List<Vector3>();
            for (float x = spawn.position.x - 4; x <= spawn.position.x + 4; x += .75f)
                for (float z = spawn.position.z - 2; z <= spawn.position.z + 5; z += .75f)
                    if (NavMesh.SamplePosition(new Vector3(x, spawn.position.y, z), out var p, .3f, NavMesh.AllAreas) && Mathf.Abs(p.position.y - spawn.position.y) < .3f && ClearPoint(p.position, root.transform)) candidates.Add(p.position);
            var points = new List<Vector3> { spawn.position };
            for (int i = 0; i < 3; i++)
            {
                Vector3 preferred = spawn.position + new Vector3(i == 0 ? -2.5f : i == 1 ? -2.5f : 1, 0, i == 0 ? 0 : 3);
                var valid = candidates.Where(p => points.All(old => Vector3.Distance(old, p) > 1.3f) && ClearRoute(points.Last(), p, root.transform) && ClearRoute(p, points[0], root.transform)).OrderBy(p => Vector3.Distance(p, preferred)).ToArray();
                if (valid.Length == 0) break; points.Add(valid[0]);
            }
            if (points.Count < 3) throw new Exception("Not enough collision-free connected patrol points; found " + points.Count + " candidates=" + candidates.Count);
            var group = GameObject.Find("Monster Patrol Waypoints"); if (!group) group = new GameObject("Monster Patrol Waypoints");
            foreach (Transform child in group.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var waypoints = new Transform[points.Count];
            for (int i = 0; i < points.Count; i++) { var point = new GameObject("Patrol " + (i + 1)).transform; point.SetParent(group.transform); point.position = points[(i + 1) % points.Count]; waypoints[i] = point; }
            ai.waypoints = Array.Empty<Transform>(); PrefabUtility.SaveAsPrefabAsset(root, "Assets/Recovery/Boss/M4Boss.prefab"); ai.waypoints = waypoints;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var report = new StringBuilder("PASS: restored baked school NavMesh_0; MonsterAI + NavMeshAgent; prototype/CC disabled; shared MonsterConfig; selected label.\n");
            foreach (var point in waypoints) report.AppendLine(point.name + "=" + point.position);
            report.AppendLine("Every route complete and capsule-clear against current map colliders; verified radius .42m, height 2.2m. NavMesh triangles=" + NavMesh.CalculateTriangulation().indices.Length / 3);
            File.WriteAllText("RecoveryReports/monster-patrol-setup.txt", report.ToString()); EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText("RecoveryReports/monster-patrol-setup.txt", "FAIL: " + e); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    public static bool ClearPoint(Vector3 point, Transform ignored)
    {
        return !Physics.OverlapCapsule(point + Vector3.up * .52f, point + Vector3.up * 1.78f, .42f, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(ignored) && !c.GetComponent<CharacterController>());
    }
    public static bool ClearRoute(Vector3 from, Vector3 to, Transform ignored)
    {
        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        for (int i = 1; i < path.corners.Length; i++)
        {
            float length = Vector3.Distance(path.corners[i - 1], path.corners[i]);
            for (float d = 0; d <= length; d += .12f) if (!ClearPoint(Vector3.Lerp(path.corners[i - 1], path.corners[i], d / Mathf.Max(.001f, length)), ignored)) return false;
        }
        return ClearPoint(to, ignored);
    }
}
