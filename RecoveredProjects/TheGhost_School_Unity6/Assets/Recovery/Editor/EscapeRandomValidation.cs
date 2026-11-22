using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class EscapeRandomValidation
{
    static IEnumerator test;
    static double deadline;
    static EscapeRandomValidation()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("EscapeRandomTest", false)) {
                test = Test(); deadline = EditorApplication.timeSinceStartup + 90; EditorApplication.update += Tick;
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        var source = Object.FindAnyObjectByType<EscapeItemSpawner>();
        var layouts = new HashSet<string>(); var previous = UnityEngine.Random.state;
        try {
            for (int seed = 0; seed < 50; seed++) {
                UnityEngine.Random.InitState(seed);
                var root = new GameObject("Spawn validation");
                try {
                    var sample = root.AddComponent<EscapeItemSpawner>(); sample.entries = source.entries; sample.Spawn();
                    Check(sample); layouts.Add(string.Join("|", sample.Spawned.Select(p => p.transform.position.ToString("F3"))));
                    sample.Spawn(); Check(sample);
                } finally { Object.DestroyImmediate(root); }
            }
        } finally { UnityEngine.Random.state = previous; }
        if (layouts.Count < 40) throw new Exception("Insufficient randomized layout diversity: " + layouts.Count);
        File.WriteAllText("RecoveryReports/escape-random-check.txt", "PASS 50 seeded rounds: exact six unique ingredients, no overlapping positions, no duplicate spawn; layouts=" + layouts.Count + "\n");
        SessionState.SetBool("EscapeRandomTest", true); EditorApplication.EnterPlaymode();
    }
    static void Check(EscapeItemSpawner spawner)
    {
        if (spawner.Spawned.Count != 6 || spawner.Spawned.Select(p => p.item.id).Distinct().Count() != 6) throw new Exception("Wrong item counts");
        foreach (var p in spawner.Spawned) {
            if (!p || p.quantity != 1 || p.GetComponentsInChildren<MeshRenderer>().Length == 0) throw new Exception("Missing prop");
            if (!AssetDatabase.GetAssetPath(p.item.pickupPrefab).Contains("ReferenceProps/Prefabs")) throw new Exception("Wrong model source");
            foreach (var other in spawner.Spawned) if (other != p && Vector3.Distance(p.transform.position, other.transform.position) < .6f) throw new Exception("Overlap");
        }
    }
    static IEnumerator Test()
    {
        yield return null; yield return null;
        for (int round = 0; round < 2; round++) {
            var spawner = Object.FindAnyObjectByType<EscapeItemSpawner>(); Check(spawner);
            foreach (var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>()) mic.enabled = false;
            var health = Object.FindAnyObjectByType<PlayerHealth>(); if (!health.IsInvulnerable) health.ToggleInvulnerability();
            var all = Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
            foreach (var entry in spawner.entries) if (all.Count(p => p.item == entry.item) != 1) throw new Exception("Duplicate static pickup " + entry.item.id);
            var bag = new GameObject("Pickup test bag").AddComponent<Inventory>();
            foreach (var pickup in spawner.Spawned) {
                var id = pickup.item.id;
                if (!pickup.TryPickup(bag) || bag.CountItem(id) != 1 || pickup.TryPickup(bag)) throw new Exception("Pickup failed: " + id);
                bag.RemoveItem(id);
            }
            Object.Destroy(bag.gameObject);
            File.AppendAllText("RecoveryReports/escape-random-check.txt", "PASS Play round " + round + ": start spawn, six interactive ReferenceProps, no fixed duplicates, pickup once.\n");
            if (round == 0) {
                var operation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path);
                while (!operation.isDone) yield return null;
                yield return null; yield return null;
            }
        }
    }
    static void Tick()
    {
        try { if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout"); if (test.MoveNext()) return; Finish(0); }
        catch (Exception e) { File.AppendAllText("RecoveryReports/escape-random-check.txt", "FAIL " + e); Finish(1); }
    }
    static void Finish(int code) { EditorApplication.update -= Tick; SessionState.SetBool("EscapeRandomTest", false); EditorApplication.Exit(code); }
}
