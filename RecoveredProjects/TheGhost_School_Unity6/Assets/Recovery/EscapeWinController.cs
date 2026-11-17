using System;
using UnityEngine;

// Retains serialized M6 references; M7 owns round results and presentation.
[DisallowMultipleComponent, RequireComponent(typeof(GameStateManager))]
public sealed class EscapeWinController : MonoBehaviour
{
    GameStateManager game;
    public bool HasWon => game && game.State == GameState.Won;
    public bool IsRestarting => game && game.IsRestarting;
    public event Action OnWon;
    void Awake() { game = GetComponent<GameStateManager>(); }
    void OnEnable() { if (!game) game = GetComponent<GameStateManager>(); game.OnStateChanged += StateChanged; }
    void OnDisable() { if (game) game.OnStateChanged -= StateChanged; }
    void StateChanged(GameState state) { if (state == GameState.Won) OnWon?.Invoke(); }
    public void Win() { if (game) game.EvaluateOutcome(); }
    public void Restart() { if (game) game.Restart(); }
}
