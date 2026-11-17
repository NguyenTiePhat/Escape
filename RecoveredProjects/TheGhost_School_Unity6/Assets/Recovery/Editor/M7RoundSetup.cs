using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class M7RoundSetup
{
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        const string backup = "RecoveryReports/School_Test-before-M7-round.unity";
        if (!File.Exists(backup)) File.Copy(scene.path, backup);
        var facade = Object.FindFirstObjectByType<EscapeWinController>();
        if (!facade) throw new Exception("Existing M6 game controller missing");
        var manager = facade.GetComponent<GameStateManager>();
        if (!manager) manager = facade.gameObject.AddComponent<GameStateManager>();
        var players = Object.FindObjectsByType<PlayerMotor>();
        if (players.Length == 0) throw new Exception("No scene players");
        foreach (var player in players) if (!player.GetComponent<PlayerHealth>()) player.gameObject.AddComponent<PlayerHealth>();
        var left = GameObject.Find("Gameplay/Doors/DoorMain_L").GetComponent<MainExitDoor>();
        var bounds = left.GetComponent<Collider>().bounds;
        bounds.Encapsulate(left.partner.GetComponent<Collider>().bounds);
        // Thin axis of the closed double door is the doorway normal.
        Vector3 normal = bounds.size.x < bounds.size.z ? Vector3.right : Vector3.forward;
        if (Vector3.Dot(normal, players[0].transform.position - bounds.center) > 0) normal = -normal;
        var existing = Object.FindFirstObjectByType<PlayerEscapeZone>();
        var zone = existing ? existing.gameObject : new GameObject("M7 Main exit - outside threshold");
        zone.transform.position = bounds.center + normal * .85f;
        zone.transform.rotation = Quaternion.LookRotation(normal);
        var box = zone.GetComponent<BoxCollider>(); if (!box) box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(Mathf.Max(bounds.size.x, bounds.size.z), Mathf.Max(2.2f, bounds.size.y), .9f);
        var escape = zone.GetComponent<PlayerEscapeZone>(); if (!escape) escape = zone.AddComponent<PlayerEscapeZone>();
        escape.exit = left;
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("RecoveryReports/m7-round-setup.txt", $"Players={players.Length}\nDoor bounds={bounds}\nOutside direction={normal}\nExit zone={zone.transform.position}; size={box.size}\n");
        M7RoundValidation.Run();
    }
}
