using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class NoiseCoreCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var manager = NoiseManager.Instance;
            var owner = new GameObject("Noise owner");
            var near = new GameObject("Near 5m").AddComponent<NoiseTestListener>();
            var far = new GameObject("Far 15m").AddComponent<NoiseTestListener>();
            var edge = new GameObject("Edge 10m").AddComponent<NoiseTestListener>();
            near.transform.position = Vector3.right * 5;
            far.transform.position = Vector3.right * 15;
            edge.transform.position = Vector3.right * 10;
            NoiseManager.RegisterListener(near, near.transform);
            NoiseManager.RegisterListener(far, far.transform);
            NoiseManager.RegisterListener(edge, edge.transform);
            NoiseManager.EmitNoise(Vector3.zero, .4f, NoiseSource.Door, owner);
            Require(near.HeardCount == 1 && Mathf.Abs(near.LastPerceivedLoudness - .2f) < .00001f, "linear attenuation at 5m");
            Require(far.HeardCount == 0, "outside radius must not receive event");
            Require(edge.HeardCount == 1 && edge.LastPerceivedLoudness == 0, "inclusive boundary at 10m");
            Require(near.LastEvent.owner == owner && near.LastEvent.source == NoiseSource.Door && near.LastEvent.position == Vector3.zero, "event payload");
            NoiseManager.RegisterListener(near, near.transform);
            NoiseManager.EmitNoise(Vector3.zero, .4f, NoiseSource.Voice);
            Require(near.HeardCount == 2, "duplicate registration must not duplicate delivery");
            near.enabled = false;
            NoiseManager.EmitNoise(Vector3.zero, .4f, NoiseSource.Footstep);
            Require(near.HeardCount == 2, "disabled listener");
            near.enabled = true; NoiseManager.RegisterListener(near, near.transform);
            near.transform.position = Vector3.zero;
            NoiseManager.EmitNoise(Vector3.zero, 0, NoiseSource.Drop);
            Require(near.HeardCount == 2, "zero loudness is silent even at source");
            NoiseManager.EmitNoise(Vector3.zero, 2, NoiseSource.Impact);
            Require(near.HeardCount == 3 && near.LastPerceivedLoudness == 1 && manager.LatestNoise.loudness == 1, "clamp loudness");
            Require(far.HeardCount == 1 && Mathf.Abs(far.LastPerceivedLoudness - .4f) < .00001f, "25m maximum range");
            NoiseManager.UnregisterListener(near);
            NoiseManager.EmitNoise(Vector3.zero, .4f, NoiseSource.Door);
            Require(near.HeardCount == 3, "unregister");
            var emitter = owner.AddComponent<NoiseTestEmitter>(); emitter.nearListener = near; emitter.farListener = far;
            NoiseManager.RegisterListener(near, near.transform); manager.debugLogging = true;
            int farBefore = far.HeardCount; emitter.EmitTestNoise();
            Require(near.HeardCount == 4 && far.HeardCount == farBefore, "test key action and near/far setup");
            Require(manager.HasRecentNoise, "gizmo active immediately");
            // Inject age through serialization-independent reflection; no wall-clock busy wait.
            typeof(NoiseManager).GetField("emittedAt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(manager, Time.unscaledTime - 1.01f);
            Require(!manager.HasRecentNoise, "gizmo expires after one second");
            Object.DestroyImmediate(near.gameObject); Object.DestroyImmediate(far.gameObject); Object.DestroyImmediate(edge.gameObject);
            NoiseManager.EmitNoise(Vector3.zero, .4f, NoiseSource.Impact);
            Directory.CreateDirectory("RecoveryReports");
            File.WriteAllText("RecoveryReports/noise-core-check.txt", "PASS: linear attenuation; outside radius excluded; boundary zero; payload; duplicate registration; disabled/unregistered/destroyed listener; zero/clamped loudness; test emitter action; gizmo lifetime; optional logs exercised.");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); Directory.CreateDirectory("RecoveryReports"); File.WriteAllText("RecoveryReports/noise-core-check.txt", "FAIL: " + e); EditorApplication.Exit(1); }
    }
}
