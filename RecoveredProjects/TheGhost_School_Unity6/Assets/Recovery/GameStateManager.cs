using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public enum GameState { MainMenu, Loading, Playing, Won, Lost }

[DefaultExecutionOrder(-200), DisallowMultipleComponent]
public sealed class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState State { get; private set; } = GameState.MainMenu;
    public event Action<GameState> OnStateChanged;
    public bool startPlayingOnLoad = true;
    public bool IsRestarting { get; private set; }
    public IReadOnlyList<PlayerHealth> Players => players;
    readonly List<PlayerHealth> players = new List<PlayerHealth>();
    float previousTimeScale = 1, endedAt;
    public bool IsFinished => State == GameState.Won || State == GameState.Lost;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(this); return; }
        Instance = this; previousTimeScale = Time.timeScale;
    }
    void Start()
    {
        foreach (var player in FindObjectsByType<PlayerHealth>()) Register(player);
        if (startPlayingOnLoad) BeginPlaying();
    }
    public void Register(PlayerHealth player) { if (player && !players.Contains(player)) players.Add(player); }
    public void BeginPlaying()
    {
        if (IsFinished || IsRestarting || State == GameState.Playing) return;
        Time.timeScale = 1; SetState(GameState.Playing); EvaluateOutcome();
    }
    public void EvaluateOutcome()
    {
        if (State != GameState.Playing || players.Count == 0) return;
        int alive = 0, escaped = 0;
        foreach (var player in players)
        {
            if (!player || player.IsDead) continue;
            alive++; if (player.HasEscaped) escaped++;
        }
        if (alive == 0) Finish(GameState.Lost);
        else if (escaped == alive) Finish(GameState.Won);
    }
    void Finish(GameState outcome)
    {
        if (State != GameState.Playing) return;
        endedAt = Time.unscaledTime;
        foreach (var player in players) if (player) player.LockControls();
        foreach (var room in FindObjectsByType<StartRoom>()) room.enabled = false;
        foreach (var ui in FindObjectsByType<InventoryUI>()) ui.enabled = false;
        foreach (var panel in FindObjectsByType<MicSettingsPanel>()) panel.enabled = false;
        foreach (var mic in FindObjectsByType<MicrophoneNoiseDetector>()) mic.enabled = false;
        foreach (var monster in FindObjectsByType<MonsterAI>())
        {
            monster.enabled = false;
            var agent = monster.GetComponent<NavMeshAgent>();
            if (agent && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero; }
            var animator = monster.GetComponentInChildren<Animator>(); if (animator) animator.speed = 0;
        }
        foreach (var canvas in FindObjectsByType<Canvas>()) canvas.enabled = false;
        Time.timeScale = 0; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        SetState(outcome);
    }
    void SetState(GameState state) { if (State == state) return; State = state; OnStateChanged?.Invoke(state); }
    public void Restart()
    {
        if (!IsFinished || IsRestarting) return;
        IsRestarting = true; SetState(GameState.Loading); Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path, LoadSceneMode.Single);
    }
    void OnDestroy() { if (Instance != this) return; Instance = null; if (!IsRestarting) Time.timeScale = previousTimeScale; }
    void OnGUI()
    {
        if (!IsFinished && !IsRestarting) return;
        GUI.depth = -1000; Color old = GUI.color;
        // Unscaled fade leaves the camera fall visible while gameplay is stopped.
        float fade = Mathf.Clamp01((Time.unscaledTime - endedAt - .7f) / .7f);
        GUI.color = new Color(.015f, .018f, .018f, Mathf.Lerp(.16f, .86f, fade));
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
        var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.width / 32, 24, 42), alignment = TextAnchor.MiddleCenter, wordWrap = true };
        string text = IsRestarting ? "Đang tải..." : State == GameState.Won ? "Bạn đã thoát khỏi trường" : "Bạn đã thua";
        GUI.Label(new Rect(20, Screen.height * .32f, Screen.width - 40, 100), text, title);
        var button = new GUIStyle(GUI.skin.button) { fontSize = 22 }; GUI.enabled = !IsRestarting;
        if (GUI.Button(new Rect(Screen.width * .5f - 125, Screen.height * .58f, 250, 56), "Chơi lại", button)) Restart();
        GUI.enabled = true; GUI.color = old;
    }
}
