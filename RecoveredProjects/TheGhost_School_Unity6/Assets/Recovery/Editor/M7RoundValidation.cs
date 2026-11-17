using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class M7RoundValidation
{
    static IEnumerator checks;
    static readonly StringBuilder report = new StringBuilder();
    static double deadline;
    static M7RoundValidation() { EditorApplication.playModeStateChanged += Changed; }
    public static void Run() { SessionState.SetBool("M7Validation", true); EditorApplication.EnterPlaymode(); }
    public static void GodMode()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        SessionState.SetBool("M7GodMode", true); Run();
    }
    public static void Screens()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        SessionState.SetBool("M7Screens", true); Run();
    }
    static void Changed(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("M7Validation", false)) return;
        checks = SessionState.GetBool("M7GodMode", false) ? TestGodMode() : SessionState.GetBool("M7Screens", false) ? Visuals() : Test(); deadline = EditorApplication.timeSinceStartup + 180;
        EditorApplication.update += Tick;
    }
    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Test timeout");
            if (checks.MoveNext()) return;
            Finish(0);
        }
        catch (Exception e) { report.AppendLine("FAIL " + e); Finish(1); }
    }
    static void Finish(int code)
    {
        EditorApplication.update -= Tick; SessionState.SetBool("M7Validation", false);
        bool screens = SessionState.GetBool("M7Screens", false); SessionState.SetBool("M7Screens", false);
        bool godMode = SessionState.GetBool("M7GodMode", false); SessionState.SetBool("M7GodMode", false);
        File.WriteAllText(godMode ? "RecoveryReports/m7-godmode-check.txt" : screens ? "RecoveryReports/m7-visual-check.txt" : "RecoveryReports/m7-round-check.txt", report.ToString());
        EditorApplication.Exit(code);
    }
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); report.AppendLine("PASS " + label); }
    static PlayerHealth Player => Object.FindFirstObjectByType<SchoolExplorer>().GetComponent<PlayerHealth>();
    static void Quiet()
    {
        foreach (var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>()) mic.enabled = false;
        foreach (var monster in Object.FindObjectsByType<MonsterAI>()) monster.enabled = false;
    }
    static PlayerHealth Extra()
    {
        var go = new GameObject("Validation second participant"); go.transform.position = new Vector3(0, 50, 0);
        go.AddComponent<CharacterController>(); go.AddComponent<PlayerMotor>().enabled = false;
        var health = go.AddComponent<PlayerHealth>(); GameStateManager.Instance.Register(health); return health;
    }
    static IEnumerator TestGodMode()
    {
        yield return null; yield return null; Quiet();
        var game = GameStateManager.Instance; var player = Player; int deaths = 0;
        player.OnPlayerDied += () => deaths++;
        Check(!player.IsInvulnerable, "invulnerability defaults off");
        player.ToggleInvulnerability(); player.Kill(); player.Kill();
        Check(player.IsInvulnerable && player.Health == 1 && deaths == 0 && game.State == GameState.Playing && player.GetComponent<PlayerMotor>().enabled, "invulnerability rejects repeated kills without locking controls or losing");
        player.ToggleInvulnerability(); player.Kill();
        Check(!player.IsInvulnerable && player.IsDead && deaths == 1 && game.State == GameState.Lost, "toggle off restores one-hit death");
        player.ToggleInvulnerability(); Check(!player.IsInvulnerable && player.IsDead, "toggle cannot revive after death");
        game.Restart(); while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        Quiet(); player = Player; game = GameStateManager.Instance;
        Check(!player.IsInvulnerable, "restart resets invulnerability");
        player.ToggleInvulnerability(); player.Escape();
        Check(player.HasEscaped && game.State == GameState.Won, "invulnerability does not block victory");
    }
    static IEnumerator Visuals()
    {
        yield return null; yield return null; Quiet();
        var window = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")); window.Focus();
        double until = EditorApplication.timeSinceStartup + 2;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Player.Kill(); until = EditorApplication.timeSinceStartup + .5;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("RecoveryReports/m7-falling.png"));
        until = EditorApplication.timeSinceStartup + 2;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("RecoveryReports/m7-lost.png"));
        until = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        var game = GameStateManager.Instance; game.Restart();
        while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        Quiet(); Player.Escape(); until = EditorApplication.timeSinceStartup + 2;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("RecoveryReports/m7-won.png"));
        until = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(File.Exists("RecoveryReports/m7-lost.png") && File.Exists("RecoveryReports/m7-won.png"), "result screenshots captured");
    }
    static IEnumerator Test()
    {
        yield return null; yield return null;
        Quiet(); var game = GameStateManager.Instance; var player = Player;
        Check(game && game.State == GameState.Playing && player.Health == 1, "saved scene starts Playing with one health");
        int deaths = 0, changes = 0; player.OnPlayerDied += () => deaths++; game.OnStateChanged += s => changes++;
        var motor = player.GetComponent<PlayerMotor>(); var camera = motor.view; Vector3 before = camera.position;
        var monster = Object.FindFirstObjectByType<MonsterAI>(); var agent = monster.GetComponent<NavMeshAgent>();
        var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
        player.transform.position = monster.transform.position + monster.transform.forward * .8f; cc.enabled = true;
        var vision = monster.GetComponent<MonsterVision>(); vision.obstacles = 0;
        monster.config = Object.Instantiate(monster.config); monster.config.sightAngle = 360; monster.config.sightRange = 100;
        var type = typeof(MonsterAI); const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        type.GetField("attackVictim", flags).SetValue(monster, motor);
        type.GetField("attackStarted", flags).SetValue(monster, Time.time - .65f);
        type.GetField("attackLaunched", flags).SetValue(monster, false);
        Physics.SyncTransforms(); before = camera.position;
        type.GetMethod("UpdateAttack", flags).Invoke(monster, null);
        Check(player.IsDead && game.State == GameState.Lost, "real monster attack hit invokes Kill and Lost");
        player.Kill(); player.Escape();
        Check(deaths == 1 && changes == 1 && !player.HasEscaped, "death/result events occur once; dead player cannot escape");
        Check(!motor.enabled && motor.InputBlocked && !player.GetComponent<PlayerLook>().enabled && !player.GetComponent<InventoryInput>().enabled, "death locks movement, look and inventory");
        Check(Time.timeScale == 0 && Cursor.lockState == CursorLockMode.None, "result freezes game and releases cursor");
        double until = EditorApplication.timeSinceStartup + 1.1;
        while (EditorApplication.timeSinceStartup < until) yield return null;
        Check(player.DeathAnimationComplete && camera.position.y < before.y - .5f && Quaternion.Angle(camera.rotation, Quaternion.Euler(12, camera.eulerAngles.y, 78)) < 1, "camera falls and rolls using unscaled time");
        ScreenCapture.CaptureScreenshot("RecoveryReports/m7-lost.png");
        until = EditorApplication.timeSinceStartup + .3; while (EditorApplication.timeSinceStartup < until) yield return null;
        game.Restart(); Check(game.State == GameState.Loading, "restart announces Loading");
        while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        Quiet(); game = GameStateManager.Instance; player = Player;
        Check(Time.timeScale == 1 && !player.IsDead && player.GetComponent<PlayerMotor>().enabled, "restart restores fresh living player and controls");
        var second = Extra(); player.Kill();
        Check(game.State == GameState.Playing, "one dead with another alive does not lose");
        second.Escape(); Check(game.State == GameState.Won, "all surviving players escaped wins even with a dead teammate");
        second.Kill(); Check(!second.IsDead, "escaped player cannot be killed");
        game.Restart(); while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        Quiet(); game = GameStateManager.Instance; player = Player; second = Extra();
        player.Kill(); second.Kill(); Check(game.State == GameState.Lost, "all players dead loses");
        game.Restart(); while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        Quiet(); game = GameStateManager.Instance; player = Player; second = Extra();
        player.Escape(); Check(game.State == GameState.Playing, "one escaped while another remains does not win");
        second.Escape(); Check(game.State == GameState.Won, "all living players escaped wins");
        game.Restart(); while (!GameStateManager.Instance || GameStateManager.Instance == game || GameStateManager.Instance.State != GameState.Playing) yield return null;
        Quiet(); game = GameStateManager.Instance; player = Player;
        var zone = Object.FindFirstObjectByType<PlayerEscapeZone>(); var exit = zone.exit;
        Check(!exit.TryOpenWithKey("silver_key"), "silver key cannot authorize main exit");
        Check(exit.TryOpenWithKey("main_exit_key"), "gold key authorizes exit");
        var leaf = exit.GetComponent<Door>();
        for (int i = 0; i < 240; i++) { leaf.Tick(1f / 60); exit.partner.Tick(1f / 60); }
        Check(exit.IsExitReady && game.State == GameState.Playing, "fully opened doors alone do not win");
        cc = player.GetComponent<CharacterController>(); motor = player.GetComponent<PlayerMotor>();
        var box = zone.GetComponent<BoxCollider>(); Vector3 normal = zone.transform.forward;
        Vector3 start = zone.transform.position - normal * 1.5f;
        start.y = zone.transform.position.y - cc.center.y;
        cc.enabled = false; player.transform.position = start; cc.enabled = true; motor.enabled = false;
        Physics.SyncTransforms();
        for (int i = 0; i < 100 && game.State == GameState.Playing; i++) { cc.Move(normal * .025f); yield return null; }
        Check(player.HasEscaped && game.State == GameState.Won, "CharacterController walks through actual main doorway into outside zone and wins");
        ScreenCapture.CaptureScreenshot("RecoveryReports/m7-won.png");
        until = EditorApplication.timeSinceStartup + .4; while (EditorApplication.timeSinceStartup < until) yield return null;
        report.AppendLine("All M7 step 1 checks complete.");
    }
}
