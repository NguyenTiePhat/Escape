using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent, RequireComponent(typeof(PlayerMotor))]
public sealed class PlayerHealth : MonoBehaviour
{
    public bool IsDead { get; private set; }
    public bool HasEscaped { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public int Health => IsDead ? 0 : 1;
    public bool CanParticipate => !IsDead && !HasEscaped;
    public event Action OnPlayerDied;
    public event Action OnPlayerEscaped;
    public bool DeathAnimationComplete { get; private set; }
    [Min(.1f)] public float fallDuration = .9f;
    void Start() { if (GameStateManager.Instance) GameStateManager.Instance.Register(this); }
    void Update()
    {
        if (Application.isFocused && Cursor.lockState == CursorLockMode.Locked &&
            Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            ToggleInvulnerability();
    }
    public void ToggleInvulnerability()
    {
        var game = GameStateManager.Instance;
        if (!CanParticipate || !game || game.State != GameState.Playing) return;
        IsInvulnerable = !IsInvulnerable;
    }
    public void Kill()
    {
        var game = GameStateManager.Instance;
        if (IsInvulnerable || !CanParticipate || (game && game.State != GameState.Playing)) return;
        IsDead = true; LockControls(); StartCoroutine(Fall());
        if (game) { game.Register(this); game.EvaluateOutcome(); }
        OnPlayerDied?.Invoke();
    }
    public void Escape()
    {
        var game = GameStateManager.Instance;
        if (!CanParticipate || !game || game.State != GameState.Playing) return;
        HasEscaped = true; LockControls(); game.Register(this); game.EvaluateOutcome(); OnPlayerEscaped?.Invoke();
    }
    public void LockControls()
    {
        var hold = GetComponent<PlayerHoldAction>(); if (hold) { hold.Interrupt(); hold.enabled = false; }
        var bridge = GetComponent<SchoolExplorer>(); if (bridge) bridge.enabled = false;
        var motor = GetComponent<PlayerMotor>(); if (motor) { motor.InputBlocked = true; motor.enabled = false; }
        var look = GetComponent<PlayerLook>(); if (look) { look.InputBlocked = true; look.enabled = false; }
        var interaction = GetComponent<PlayerInteractor>(); if (interaction) { interaction.InputBlocked = true; interaction.ClearTarget(); interaction.enabled = false; }
        foreach (var input in GetComponentsInChildren<InventoryInput>(true)) input.enabled = false;
        foreach (var hands in GetComponentsInChildren<FirstPersonHands>(true)) hands.enabled = false;
        foreach (var mic in GetComponentsInChildren<MicrophoneNoiseDetector>(true)) mic.enabled = false;
        foreach (var panel in GetComponentsInChildren<MicSettingsPanel>(true)) panel.enabled = false;
    }
    IEnumerator Fall()
    {
        var motor = GetComponent<PlayerMotor>(); Transform view = motor ? motor.view : null;
        if (!view) { var camera = GetComponentInChildren<Camera>(); if (camera) view = camera.transform; }
        if (view)
        {
            Vector3 start = view.position; Quaternion rotation = view.rotation;
            var body = GetComponent<CharacterController>(); float floor = body ? body.bounds.min.y : transform.position.y;
            Vector3 end = new Vector3(start.x, Mathf.Min(start.y, floor + .22f), start.z);
            Quaternion target = Quaternion.Euler(12, view.eulerAngles.y, 78); float elapsed = 0;
            while (elapsed < fallDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / Mathf.Max(.1f, fallDuration));
                view.SetPositionAndRotation(Vector3.Lerp(start, end, t), Quaternion.Slerp(rotation, target, t)); yield return null;
            }
        }
        DeathAnimationComplete = true;
    }
}
