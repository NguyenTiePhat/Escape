using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class EscapeWinController : MonoBehaviour
{
    public bool HasWon { get; private set; }
    public bool IsRestarting { get; private set; }
    public event Action OnWon;
    float previousTimeScale = 1;
    public void Win()
    {
        if (HasWon) return;
        HasWon = true; previousTimeScale = Time.timeScale;
        foreach (var hold in FindObjectsByType<PlayerHoldAction>()) { hold.Interrupt(); hold.enabled = false; }
        foreach (var bridge in FindObjectsByType<SchoolExplorer>()) bridge.enabled = false;
        foreach (var motor in FindObjectsByType<PlayerMotor>()) { motor.InputBlocked = true; motor.enabled = false; }
        foreach (var look in FindObjectsByType<PlayerLook>()) { look.InputBlocked = true; look.enabled = false; }
        foreach (var interaction in FindObjectsByType<PlayerInteractor>()) { interaction.InputBlocked = true; interaction.ClearTarget(); interaction.enabled = false; }
        foreach (var input in FindObjectsByType<InventoryInput>()) input.enabled = false;
        foreach (var room in FindObjectsByType<StartRoom>()) room.enabled = false;
        foreach (var inventoryUI in FindObjectsByType<InventoryUI>()) inventoryUI.enabled = false;
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
        OnWon?.Invoke();
    }
    public void Restart()
    {
        if (!HasWon || IsRestarting) return;
        IsRestarting = true; Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path, LoadSceneMode.Single);
    }
    void OnDestroy() { if (HasWon && !IsRestarting) Time.timeScale = previousTimeScale; }
    void OnGUI()
    {
        if (!HasWon) return;
        GUI.depth = -1000;
        var old = GUI.color; GUI.color = new Color(.015f, .02f, .018f, .98f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = Color.white;
        var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.width / 32, 24, 42), alignment = TextAnchor.MiddleCenter, wordWrap = true };
        GUI.Label(new Rect(20, Screen.height * .35f, Screen.width - 40, 100), "Bạn đã thoát khỏi trường", title);
        var button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
        GUI.enabled = !IsRestarting;
        if (GUI.Button(new Rect(Screen.width * .5f - 125, Screen.height * .58f, 250, 56), IsRestarting ? "Đang tải..." : "Chơi lại", button)) Restart();
        GUI.enabled = true; GUI.color = old;
    }
}
