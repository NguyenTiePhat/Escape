using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class HoldActionSetup
{
    [MenuItem("Tools/FPS/Set up M6 hold action test")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        const string backup = "RecoveryReports/School_Test-before-M6-hold.unity";
        if (!File.Exists(backup)) EditorSceneManager.SaveScene(scene, backup, true);
        var motor = UnityEngine.Object.FindAnyObjectByType<PlayerMotor>();
        if (!motor) throw new Exception("Missing player");
        var controller = motor.GetComponent<PlayerHoldAction>(); if (!controller) controller = motor.gameObject.AddComponent<PlayerHoldAction>();
        var intro = UnityEngine.Object.FindAnyObjectByType<StartRoom>();
        var table = intro ? intro.transform.Find("Bàn hướng dẫn") : null;
        if (!table) throw new Exception("Starting-room instruction table missing");
        var station = GameObject.Find("M6 Hold Action Test");
        if (!station)
        {
            Vector3 position = table.position + new Vector3(-1.1f, .65f, -.15f);
            Physics.SyncTransforms();
            if (Physics.OverlapBox(position, new Vector3(.26f, .62f, .26f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Length != 0)
                throw new Exception("Chosen spot beside table is obstructed; scene not saved");
            station = GameObject.CreatePrimitive(PrimitiveType.Cube); station.name = "M6 Hold Action Test";
            station.transform.position = position; station.transform.localScale = new Vector3(.55f, 1.3f, .55f);
            station.layer = LayerMask.NameToLayer("Interactable");
            station.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovery/IntroMetal.mat");
            var action = station.AddComponent<HoldNoiseAction>(); action.duration = 10; action.loudness = .85f;
        }
        var hud = GameObject.Find("M6 Hold Progress HUD");
        if (!hud)
        {
            hud = new GameObject("M6 Hold Progress HUD", typeof(Canvas), typeof(CanvasScaler));
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 40;
            var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
            var ringObject = new GameObject("Progress donut", typeof(RectTransform), typeof(CanvasRenderer), typeof(HoldProgressRing));
            var rect = ringObject.GetComponent<RectTransform>(); rect.SetParent(hud.transform, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(64, 64);
            ringObject.GetComponent<HoldProgressRing>().player = controller;
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        File.WriteAllText("RecoveryReports/m6-hold-setup.txt", "PASS: School_Test starting room test station=" + station.transform.position + "; 10s, loudness .85; centre donut HUD; player controller assigned.");
    }
}
