using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class FpsInputPlayCheck
{
    static int stage;
    static double started;
    static Vector3 position;
    static Quaternion rotation;
    static FpsInputPlayCheck() { EditorApplication.update += Check; }
    [MenuItem("Tools/FPS/Check input in Play Mode")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) return;
        SessionState.SetBool("FpsInputPlayCheck", true);
        EditorApplication.isPlaying = true;
    }
    static void Check()
    {
        if (!SessionState.GetBool("FpsInputPlayCheck", false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        var player = UnityEngine.Object.FindFirstObjectByType<SchoolExplorer>();
        if (!player || !player.GetComponent<PlayerMotor>()) return;
        try
        {
            if (stage == 0)
            {
                if (Keyboard.current == null || Mouse.current == null) throw new Exception("Native keyboard/mouse missing. Restart Editor after changing Active Input Handling.");
                var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                EditorWindow.GetWindow(gameView).Focus();
                Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
                position = player.transform.position; rotation = player.transform.rotation;
                started = EditorApplication.timeSinceStartup; stage = 1;
                return;
            }
            if (stage == 1)
            {
                if (!Application.isFocused) throw new Exception("Game view is not focused");
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.A));
                InputSystem.QueueDeltaStateEvent(Mouse.current.delta, new Vector2(3, 1));
                if (EditorApplication.timeSinceStartup - started < .35) return;
                stage = 2; return;
            }
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            float distance = Vector3.ProjectOnPlane(player.transform.position - position, Vector3.up).magnitude;
            float angle = Quaternion.Angle(rotation, player.transform.rotation);
            if (distance < .05f || angle < .1f) throw new Exception("Update input failed: distance="+distance+", yaw="+angle);
            File.WriteAllText("RecoveryReports/fps-input-play-check.txt", "PASS: native keyboard and mouse present; queued Input System events consumed by normal PlayerMotor/PlayerLook Update in School_Test. Movement="+distance+"m, yaw="+angle+" degrees.");
            Finish();
        }
        catch (Exception e) { File.WriteAllText("RecoveryReports/fps-input-play-check.txt", "FAIL: "+e); Finish(); }
    }
    static void Finish()
    {
        if (Keyboard.current != null) InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
        SessionState.SetBool("FpsInputPlayCheck", false); stage=0; EditorApplication.isPlaying=false;
    }
}
