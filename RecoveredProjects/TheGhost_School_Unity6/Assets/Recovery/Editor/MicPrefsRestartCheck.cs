using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class MicPrefsRestartCheck
{
    const string Prefix="M3.RestartCheck.";
    static MicrophoneNoiseDetector Create()
    {
        var go=new GameObject("Prefs restart test");go.SetActive(false);
        var detector=go.AddComponent<MicrophoneNoiseDetector>();detector.prefsPrefix=Prefix;return detector;
    }
    public static void Write()
    {
        var detector=Create();detector.detectionEnabled=false;detector.selectedDevice="Persisted test microphone";detector.noiseFloorDb=-57;detector.maxDb=-17;detector.sensitivity=1.6f;detector.SaveSettings();Object.DestroyImmediate(detector.gameObject);EditorApplication.Exit(0);
    }
    public static void Read()
    {
        var detector=Create();detector.LoadSettings();
        bool ok=!detector.detectionEnabled&&detector.selectedDevice=="Persisted test microphone"&&detector.noiseFloorDb==-57&&detector.maxDb==-17&&Mathf.Abs(detector.sensitivity-1.6f)<.0001f;
        Object.DestroyImmediate(detector.gameObject);
        foreach(string key in new[]{"enabled","device","floor","max","sensitivity"})PlayerPrefs.DeleteKey(Prefix+key);PlayerPrefs.Save();
        Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/mic-prefs-restart-check.txt",(ok?"PASS":"FAIL")+": settings written by one Unity process and reloaded by a fresh Unity process; enabled/device/floor/max/sensitivity preserved; test keys cleaned.");
        EditorApplication.Exit(ok?0:1);
    }
}
