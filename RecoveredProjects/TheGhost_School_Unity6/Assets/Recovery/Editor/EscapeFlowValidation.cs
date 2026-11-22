using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class EscapeFlowValidation
{
    static IEnumerator test;
    static double deadline;
    static readonly StringBuilder report = new StringBuilder();
    static EscapeFlowValidation() { EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("EscapeFlowTest", false)) { test = Test(); deadline = EditorApplication.timeSinceStartup + 150; EditorApplication.update += Tick; } }; }
    public static void Run() { EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity"); SessionState.SetBool("EscapeFlowTest", true); EditorApplication.EnterPlaymode(); }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); report.AppendLine("PASS " + message); }
    static void Select(Inventory bag, string id) { for (int i = 0; i < Inventory.Capacity; i++) if (bag.GetSlot(i).Item && bag.GetSlot(i).Item.id == id) { bag.SelectSlot(i); return; } throw new Exception("Item missing: " + id); }
    static IEnumerator Test()
    {
        yield return null; yield return null;
        foreach (var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>()) mic.enabled = false;
        foreach (var boss in Object.FindObjectsByType<MonsterAI>()) boss.enabled = false;
        var player = Object.FindAnyObjectByType<PlayerHealth>();
        var motor = player.GetComponent<PlayerMotor>(); motor.enabled = false;
        player.GetComponent<PlayerLook>().enabled = false;
        var bag = player.GetComponent<Inventory>(); bag.room = null;
        var hold = player.GetComponent<PlayerHoldAction>(); hold.enabled = false;
        var spawner = Object.FindAnyObjectByType<EscapeItemSpawner>();
        var saw = Object.FindAnyObjectByType<SawAssemblyStation>();
        var key = Object.FindAnyObjectByType<KeyCraftingStation>();
        var barrier = Object.FindAnyObjectByType<SawWoodBarrier>();
        var game = GameStateManager.Instance;
        var zone = Object.FindAnyObjectByType<PlayerEscapeZone>();
        Check(spawner.Spawned.Count == 6 && game.State == GameState.Playing, "fresh round has six random pickups");
        Check(!zone.exit.TryOpenWithKey("silver_key"), "silver key cannot skip gold-key objective");
        Check(!key.GetComponent<HoldNoiseAction>().Advance(hold, 1), "machine rejects missing ingredients");
        Check(!barrier.GetComponent<HoldNoiseAction>().Advance(hold, 1), "wood barrier rejects missing saw");
        var silver = spawner.Spawned.Single(p => p.item.id == "silver_key");
        Check(silver.TryPickup(bag), "random silver key collected");
        var door = Object.FindObjectsByType<Door>(FindObjectsSortMode.None).First(d => d.isLocked && d.requiredKeyId == "silver_key");
        Select(bag, "silver_key"); Check(door.UnlockDoor(bag.Selected.Item.id) && bag.HasItem("silver_key"), "silver key unlocks ordinary door and remains reusable");
        foreach (var id in new[] { "saw_blade", "saw_handle", "screws" }) {
            var pickup = spawner.Spawned.Single(p => p && p.item.id == id);
            Check(pickup.TryPickup(bag), "collect " + id); Select(bag, id);
            Check(saw.TryInstall(bag) && !bag.HasItem(id), "install and consume " + id);
        }
        Check(saw.IsComplete && saw.Output, "three parts produce assembled saw");
        var spawnedAt = saw.Output.transform.position;
        double until = EditorApplication.timeSinceStartup + 2;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(saw.Output && Vector3.Distance(saw.Output.transform.position, spawnedAt) < 1, "crafted saw remains near output after physics settles");
        Check(saw.Output.TryPickup(bag), "crafted ReferenceProps saw is collectible"); Select(bag, "assembled_saw");
        var cutting = barrier.GetComponent<HoldNoiseAction>();
        for (int i = 0; i < 2000 && !cutting.IsComplete; i++) cutting.Advance(hold, .05f);
        Check(cutting.IsComplete && !barrier.boards.activeSelf && !barrier.door.isLocked, "selected saw clears boards and unlocks back door");
        foreach (var id in new[] { "key_frame", "metal_blank" }) Check(spawner.Spawned.Single(p => p && p.item.id == id).TryPickup(bag), "collect " + id);
        var crafting = key.GetComponent<HoldNoiseAction>();
        for (int i = 0; i < 2000 && !crafting.IsComplete; i++) crafting.Advance(hold, .05f);
        Check(crafting.IsComplete && key.Output && !bag.HasItem("key_frame") && !bag.HasItem("metal_blank"), "machine consumes ingredients and produces gold key");
        Check(key.Output.TryPickup(bag) && !crafting.Advance(hold, 1), "gold key collectible; completed machine cannot duplicate output");
        Check(bag.HasItem("main_exit_key") && zone.exit.TryOpenWithKey("main_exit_key"), "crafted gold key authorizes main exit");
        var leaf = zone.exit.GetComponent<Door>();
        for (int i = 0; i < 240; i++) { leaf.Tick(1f / 60); zone.exit.partner.Tick(1f / 60); }
        Check(zone.exit.IsExitReady && game.State == GameState.Playing, "opening main door alone does not win");
        var cc = player.GetComponent<CharacterController>(); var normal = zone.transform.forward;
        var start = zone.transform.position - normal * 1.5f; start.y = zone.transform.position.y - cc.center.y;
        cc.enabled = false; player.transform.position = start; cc.enabled = true; Physics.SyncTransforms();
        for (int i = 0; i < 100 && game.State == GameState.Playing; i++) { cc.Move(normal * .025f); yield return null; }
        Check(player.HasEscaped && game.State == GameState.Won, "physical doorway traversal wins after random pickups and crafting");
        game.Restart();
        while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        yield return null;
        Check(Object.FindAnyObjectByType<EscapeItemSpawner>().Spawned.Count == 6 && !Object.FindAnyObjectByType<SawAssemblyStation>().IsComplete && !Object.FindAnyObjectByType<KeyCraftingStation>().Output, "restart resets pickups and both crafting stations");
        report.AppendLine("Integration test invokes gameplay APIs; travel between pickups/stations and keyboard input are not simulated. Main doorway traversal uses CharacterController. No scene saved.");
    }
    static void Tick() { try { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout"); if (test.MoveNext()) return; Finish(0); } catch (Exception e) { report.AppendLine("FAIL " + e); Finish(1); } }
    static void Finish(int code) { EditorApplication.update -= Tick; SessionState.SetBool("EscapeFlowTest", false); File.WriteAllText("RecoveryReports/escape-flow-check.txt", report.ToString()); EditorApplication.Exit(code); }
}

