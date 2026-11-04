using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Collision-aware preview locomotion; the skeletal animation driver owns the visual pose.
[DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
public sealed class BossPrototypeMotion : MonoBehaviour
{
    public enum MotionState { Idle, Walk, Roar, Attack, Alert, Investigate, Run, Lunge, Search }
    public Transform visualPivot;
    public bool previewLoop = true;
    public bool enableTestKeys = true;
    public float walkSpeed = .65f;
    public float pacingDistance = 1.2f;
    public float lungeSpeed = 6f;
    [Min(.1f)] public float attackPlaybackSpeed = 1.6f;
    public float attackReach = 1.25f;
    public AudioSource voice;
    public AudioClip roarClip;
    public BossAnimationDriver animationDriver;
    public float investigateSpeed = .48f;
    public float runSpeed = 4.8f;
    public MotionState State { get; private set; }
    public event Action<MotionState> OnStateChanged;
    public event Action<CharacterController> OnAttackContact;
    CharacterController controller;
    Vector3 home, outward, visualHome, previous;
    Quaternion visualRotation;
    float timer, gravity, phase;
    int sequence;
    bool returning, contactChecked, initialized;
    static readonly MotionState[] PreviewActions = { MotionState.Walk, MotionState.Alert, MotionState.Investigate, MotionState.Run, MotionState.Lunge, MotionState.Search };

    void Awake() { Initialize(); }
    void Initialize()
    {
        if (initialized) return;
        controller = GetComponent<CharacterController>();
        home = transform.position; outward = transform.forward; previous = home;
        if (visualPivot) { visualHome = visualPivot.localPosition; visualRotation = visualPivot.localRotation; }
        initialized = true;
    }
    void Update()
    {
        if (enableTestKeys && Keyboard.current != null && Cursor.lockState == CursorLockMode.Locked)
        {
            if (Keyboard.current.f6Key.wasPressedThisFrame) { previewLoop = !previewLoop; SetState(MotionState.Idle); }
            if (Keyboard.current.f7Key.wasPressedThisFrame) PlayRoar();
            if (Keyboard.current.f8Key.wasPressedThisFrame) PlayAttack();
            if (Keyboard.current.f9Key.wasPressedThisFrame) { previewLoop = false; SetState(PreviewActions[sequence++ % PreviewActions.Length]); }
        }
        Tick(Time.deltaTime);
    }
    public void PlayRoar() { SetState(MotionState.Roar); }
    public void PlayAttack() { SetState(MotionState.Attack); }
    public void SetState(MotionState state)
    {
        Initialize(); State = state; timer = 0; contactChecked = false;
        if (animationDriver) animationDriver.Play(state.ToString());
        if (state == MotionState.Roar && voice && roarClip) voice.PlayOneShot(roarClip);
        OnStateChanged?.Invoke(state);
    }
    public void Tick(float dt)
    {
        Initialize(); if (!controller.enabled || dt <= 0) return;
        dt = Mathf.Min(dt, .05f);
        bool attacking = State == MotionState.Attack || State == MotionState.Lunge;
        timer += dt * (attacking ? attackPlaybackSpeed : 1);
        Vector3 movement = Vector3.zero;
        if (State == MotionState.Walk || State == MotionState.Investigate || State == MotionState.Run)
        {
            Vector3 target = returning ? home : home + outward * pacingDistance;
            Vector3 delta = Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
            if (delta.magnitude < .12f) { returning = !returning; SetState(MotionState.Idle); }
            else
            {
                float desiredSpeed = State == MotionState.Run ? runSpeed : State == MotionState.Investigate ? investigateSpeed : walkSpeed;
                if (State == MotionState.Investigate) desiredSpeed *= Mathf.Clamp01((Mathf.Abs(Mathf.Sin(timer * Mathf.PI / .8f)) - .15f) * 1.5f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(delta), 110 * dt);
                movement = transform.forward * Mathf.Min(desiredSpeed, delta.magnitude / dt);
            }
        }
        else if ((State == MotionState.Attack || State == MotionState.Lunge) && timer >= .3f && timer < .7f)
        {
            movement = transform.forward * lungeSpeed;
            if (!contactChecked) { contactChecked = true; CheckAttackContact(); }
        }
        gravity = controller.isGrounded && gravity < 0 ? -2 : Mathf.Max(gravity - 20 * dt, -25);
        controller.Move((movement + Vector3.up * gravity) * dt);
        float speed = Vector3.ProjectOnPlane(transform.position - previous, Vector3.up).magnitude / dt;
        previous = transform.position; phase += dt * (State == MotionState.Walk ? 7 : 1.7f);
        if (!animationDriver) AnimateVisual(dt, speed);
        if ((State == MotionState.Roar && timer >= 1.8f) || ((State == MotionState.Attack || State == MotionState.Lunge || State == MotionState.Alert) && timer >= 1f) || (State == MotionState.Search && timer >= 4)) SetState(MotionState.Idle);
        else if (previewLoop && State == MotionState.Idle && timer >= 2.5f)
        {
            SetState(PreviewActions[sequence++ % PreviewActions.Length]);
        }
        else if ((State == MotionState.Walk || State == MotionState.Investigate || State == MotionState.Run) && timer > 4) { returning = !returning; SetState(MotionState.Idle); }
    }
    void AnimateVisual(float dt, float speed)
    {
        if (!visualPivot) return;
        float pitch = 0, roll = 0, lift = Mathf.Sin(phase) * .008f;
        if (State == MotionState.Walk && speed > .03f) { lift = Mathf.Abs(Mathf.Sin(phase)) * .025f; pitch = 3; roll = Mathf.Sin(phase * .5f) * 2; }
        if (State == MotionState.Roar) { float envelope = Mathf.Sin(Mathf.Clamp01(timer / 1.8f) * Mathf.PI); pitch = -9 * envelope; roll = Mathf.Sin(timer * 24) * 1.2f * envelope; }
        if (State == MotionState.Attack) pitch = timer < .32f ? -8 * timer / .32f : 17 * (1 - Mathf.Clamp01((timer - .32f) / .83f));
        float blend = 1 - Mathf.Exp(-12 * dt);
        visualPivot.localPosition = Vector3.Lerp(visualPivot.localPosition, visualHome + Vector3.up * lift, blend);
        visualPivot.localRotation = Quaternion.Slerp(visualPivot.localRotation, visualRotation * Quaternion.Euler(pitch, 0, roll), blend);
    }
    void CheckAttackContact()
    {
        Vector3 origin = transform.position + Vector3.up * 1.05f;
        foreach (var hit in Physics.OverlapSphere(origin + transform.forward * .65f, attackReach * .5f, ~0, QueryTriggerInteraction.Ignore))
        {
            var target = hit.GetComponent<CharacterController>();
            if (!target || target == controller) continue;
            Vector3 destination = target.bounds.center;
            // A wall or door between the actors blocks the contact event.
            var hits = Physics.RaycastAll(origin, destination - origin, Vector3.Distance(origin, destination), ~0, QueryTriggerInteraction.Ignore);
            bool blocked = false;
            foreach (var obstacle in hits) if (!obstacle.transform.IsChildOf(transform) && !obstacle.transform.IsChildOf(target.transform)) { blocked = true; break; }
            if (!blocked) OnAttackContact?.Invoke(target);
        }
    }
    void OnDisable()
    {
        if (visualPivot && initialized) { visualPivot.localPosition = visualHome; visualPivot.localRotation = visualRotation; }
    }
}
