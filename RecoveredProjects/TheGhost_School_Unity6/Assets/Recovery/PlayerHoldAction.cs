using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50), DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMotor), typeof(PlayerInteractor))]
public sealed class PlayerHoldAction : MonoBehaviour
{
    public HoldNoiseAction ActiveAction { get; private set; }
    public HoldNoiseAction ViewedAction { get; private set; }
    public bool IsWorking => ActiveAction;
    PlayerMotor motor;
    PlayerInteractor interactor;
    bool releaseRequired;
    Vector3 standingPosition;
    void Awake() { motor = GetComponent<PlayerMotor>(); interactor = GetComponent<PlayerInteractor>(); }
    void OnEnable() { MonsterAI.OnAnyAttackHit += Attacked; }
    void OnDisable() { MonsterAI.OnAnyAttackHit -= Attacked; StopWork(); ViewedAction = null; }
    void Attacked(PlayerMotor victim) { if (victim == motor) Interrupt(); }
    public void Interrupt() { StopWork(); releaseRequired = true; }
    void Update()
    {
        bool allowed = Application.isFocused && Cursor.lockState == CursorLockMode.Locked && !motor.InputBlocked && !interactor.InputBlocked;
        interactor.RefreshTarget();
        Tick(allowed && Keyboard.current != null && Keyboard.current.eKey.isPressed,
            allowed ? interactor.Target as HoldNoiseAction : null, Time.deltaTime);
    }
    public void Tick(bool held, HoldNoiseAction target, float deltaTime)
    {
        ViewedAction = target && target.isActiveAndEnabled ? target : null;
        if (!held) { releaseRequired = false; StopWork(); return; }
        if (releaseRequired || !ViewedAction || ViewedAction.IsComplete) { StopWork(); return; }
        if (ActiveAction && ActiveAction != ViewedAction) StopWork();
        if (ActiveAction && Vector3.ProjectOnPlane(transform.position - standingPosition, Vector3.up).sqrMagnitude > .0025f)
        { Interrupt(); return; }
        if (!ActiveAction) { ActiveAction = ViewedAction; standingPosition = transform.position; }
        motor.ActionMovementLocked = true;
        if (!ActiveAction.Advance(this, deltaTime) || !ActiveAction || ActiveAction.IsComplete) StopWork();
    }
    void StopWork()
    {
        if (ActiveAction) ActiveAction.Release(this);
        ActiveAction = null;
        if (motor) motor.ActionMovementLocked = false;
    }
}
