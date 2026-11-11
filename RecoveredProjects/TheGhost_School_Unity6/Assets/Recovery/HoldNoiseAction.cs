using System;
using UnityEngine;
using UnityEngine.Events;

public interface IHoldActionGate
{
    string Prompt { get; }
    bool CanAdvance(PlayerHoldAction player);
    bool Complete(PlayerHoldAction player);
}

[DisallowMultipleComponent]
public sealed class HoldNoiseAction : MonoBehaviour, IInteractable
{
    [Min(.01f)] public float duration = 10f;
    [Range(0, 1)] public float loudness = .85f;
    public Transform noiseOrigin;
    public UnityEvent onCompleted = new UnityEvent();
    public float Elapsed { get; private set; }
    public float Progress => Mathf.Clamp01(Elapsed / Mathf.Max(.01f, duration));
    public bool IsComplete { get; private set; }
    public string Prompt => IsComplete ? "Hoàn tất" : (GetComponent<IHoldActionGate>()?.Prompt ?? "Giữ E để thao tác");
    public event Action<float> OnProgressChanged;
    public event Action OnCompleted;
    PlayerHoldAction operatorPlayer;
    float noiseTime;
    public void Interact() { } // Holding is owned by PlayerHoldAction; a tap never completes the action.
    public bool Advance(PlayerHoldAction player, float deltaTime)
    {
        if (!isActiveAndEnabled || IsComplete || !player || deltaTime <= 0 || !float.IsFinite(deltaTime)) return false;
        if (operatorPlayer && operatorPlayer != player) return false;
        var gate = GetComponent<IHoldActionGate>();
        if (gate != null && !gate.CanAdvance(player)) return false;
        if (!operatorPlayer) { operatorPlayer = player; noiseTime = 0; }
        // Repeated impulses form a continuous audible action for the hearing system.
        noiseTime -= deltaTime;
        if (noiseTime <= 0)
        {
            noiseTime = .1f;
            if (loudness > 0) NoiseManager.EmitNoise(noiseOrigin ? noiseOrigin.position : transform.position, loudness, NoiseSource.Action, player.gameObject);
        }
        Elapsed = Mathf.Min(Mathf.Max(.01f, duration), Elapsed + deltaTime);
        IsComplete = Elapsed >= Mathf.Max(.01f, duration);
        if (IsComplete && gate != null && !gate.Complete(player)) IsComplete = false;
        OnProgressChanged?.Invoke(Progress);
        if (IsComplete) { operatorPlayer = null; OnCompleted?.Invoke(); onCompleted.Invoke(); }
        return true;
    }
    public void ResetProgress()
    {
        if (operatorPlayer) operatorPlayer.Interrupt();
        operatorPlayer = null; Elapsed = 0; IsComplete = false; noiseTime = 0;
        OnProgressChanged?.Invoke(0);
    }
    public void Release(PlayerHoldAction player) { if (operatorPlayer == player) operatorPlayer = null; }
    void OnDisable() { if (operatorPlayer) operatorPlayer.Interrupt(); operatorPlayer = null; }
}
