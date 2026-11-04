using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
public static class BossAggressionSetup
{
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
            if (!File.Exists("RecoveryReports/School_Test-before-faster-boss.unity")) File.Copy(scene.path, "RecoveryReports/School_Test-before-faster-boss.unity");
            var ai = UnityEngine.Object.FindAnyObjectByType<MonsterAI>(); ai.config.chaseSpeed = 4.8f; EditorUtility.SetDirty(ai.config);
            var motion = ai.GetComponent<BossPrototypeMotion>(); motion.runSpeed = 4.8f; motion.lungeSpeed = 6; motion.attackPlaybackSpeed = 1.6f;
            var agent = ai.GetComponent<NavMeshAgent>(); agent.acceleration = 18; agent.angularSpeed = 240;
            ai.animationDriver.blendSeconds = .07f;
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Recovery/Boss/Rigged/BossActions.controller");
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                var state = child.state;
                state.speed = state.name == "Run" ? 1.75f : state.name == "Lunge" ? 1.6f : state.name == "Investigate" ? 1.2f : state.name == "Alert" ? 1.25f : 1;
                EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(controller);
            var points = ai.waypoints; var player = ai.vision.player; ai.waypoints = Array.Empty<Transform>(); ai.vision.player = null;
            PrefabUtility.SaveAsPrefabAsset(ai.gameObject, "Assets/Recovery/Boss/M4Boss.prefab"); ai.waypoints = points; ai.vision.player = player;
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("RecoveryReports/boss-aggression-setup.txt", "PASS: chase4.8m/s, lunge6m/s; Run1.75x/Lunge1.6x, blend.07s, acceleration18/turn240. Chase uses visible or last-seen position; close-range lunge follows direct NavMesh path, no damage yet.");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { File.WriteAllText("RecoveryReports/boss-aggression-setup.txt", "FAIL: " + e); EditorApplication.Exit(1); }
    }
}
