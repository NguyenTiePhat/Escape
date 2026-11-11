using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Door : MonoBehaviour, IInteractable
{
    public float openAngle = 100f;
    [Min(1)] public float speed = 60f;
    [Min(1)] public float strongSpeed = 160f;
    public bool isLocked;
    public string requiredKeyId = "";
    public bool consumeKeyOnUnlock = true;
    [TextArea] public string lockedPrompt = "";
    public bool decorative;
    [Tooltip("Hinge pin offset in this leaf's local coordinates; the closed mesh stays in place.")]
    public Vector3 hingeOffset;
    public LayerMask obstacleMask = Physics.DefaultRaycastLayers;
    public event Action<Door, bool, float> OnDoorToggled;
    // IsOpen is the requested state; a blocked leaf can still be partway through its swing.
    public bool IsOpen { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsBlocked { get; private set; }
    public Collider BlockingCollider { get; private set; }
    public Collider BlockingPanel { get; private set; }
    public float CurrentAngle => angle;
    public float CurrentSpeed { get; private set; }
    public bool IsFullyOpen => Mathf.Abs(angle - openAngle) < .01f;
    public bool IsFullyClosed => Mathf.Abs(angle) < .01f;
    public string Prompt => isLocked ? (string.IsNullOrEmpty(lockedPrompt) ? "Cửa đã khóa (chọn chìa khóa, E: mở khóa)" : lockedPrompt) : IsOpen ? "Đóng cửa (Shift: đóng mạnh)" : "Mở cửa (Shift: mở mạnh)";

    Quaternion closed;
    Vector3 closedPosition, pivotInParent, scaledHingeOffset;
    float angle, stepDegrees;
    [NonSerialized] bool initialized;
    BoxCollider[] panels;
    readonly Dictionary<(BoxCollider, Collider), float> initialContacts = new Dictionary<(BoxCollider, Collider), float>();
    const float contactTolerance = .0005f;

    protected virtual void Awake() { Initialize(); }
    public void Initialize(bool resetClosedPose = false)
    {
        if (resetClosedPose)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Cannot reset the closed pose during Play Mode.");
            initialized = false; initialContacts.Clear(); angle = 0; IsOpen = false; IsMoving = false;
        }
        if (initialized) return;
        initialized = true;
        closed = transform.localRotation;
        closedPosition = transform.localPosition;
        scaledHingeOffset = Vector3.Scale(hingeOffset, transform.localScale);
        pivotInParent = closedPosition + closed * scaledHingeOffset;
        panels = GetComponentsInChildren<BoxCollider>();
        float radius = .1f;
        Physics.SyncTransforms();
        foreach (var panel in panels)
        {
            if (!panel.enabled || panel.isTrigger) continue;
            radius = Mathf.Max(radius, Vector3.Distance(transform.TransformPoint(hingeOffset), panel.bounds.center) + panel.bounds.extents.magnitude);
            // Recovered frames can already touch/overlap the closed leaf. Permit only that
            // original static penetration, never progressively larger overlaps or a player.
            foreach (var other in Physics.OverlapBox(panel.bounds.center, panel.bounds.extents, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (Ignore(other) || other is CharacterController || other.attachedRigidbody) continue;
                var otherDoor = other.GetComponentInParent<Door>();
                if (otherDoor && !otherDoor.decorative && otherDoor.enabled) continue;
                if (Physics.ComputePenetration(panel, panel.transform.position, panel.transform.rotation,
                    other, other.transform.position, other.transform.rotation, out _, out float depth))
                    initialContacts[(panel, other)] = depth;
            }
        }
        // At most 5 mm of travel at the furthest corner per collision sample, even at low FPS.
        stepDegrees = Mathf.Min(.5f, Mathf.Rad2Deg * .005f / radius);
    }
    public void Interact()
    {
        var keyboard = Keyboard.current;
        bool strong = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        Toggle(strong ? strongSpeed : speed);
    }
    // Monster interaction uses the same swept motion and noise event as player interaction.
    public bool Open(float duration = 1f)
    {
        if (!float.IsFinite(duration) || duration <= 0) return false;
        return TrySetOpen(true, Mathf.Max(1f, Mathf.Abs(openAngle - angle) / duration));
    }
    public void Toggle() => Toggle(speed);
    public bool Toggle(float angularSpeed) => TrySetOpen(!IsOpen, angularSpeed);
    public bool TrySetOpen(bool open, float angularSpeed)
    {
        Initialize();
        if (decorative || !isActiveAndEnabled || isLocked || !float.IsFinite(angularSpeed) || angularSpeed <= 0) return false;
        if (IsOpen == open) return false;
        Physics.SyncTransforms();
        float target = open ? openAngle : 0;
        float firstStep = Mathf.MoveTowards(angle, target, stepDegrees);
        if (!CanRotateTo(firstStep)) { IsBlocked = true; return false; }
        IsOpen = open;
        CurrentSpeed = angularSpeed;
        IsMoving = Mathf.Abs(angle - target) > .001f;
        IsBlocked = false;
        if (IsMoving) OnDoorToggled?.Invoke(this, open, CurrentSpeed);
        return true;
    }
    public void LockDoor() { isLocked = true; IsMoving = false; IsBlocked = false; }
    public void UnlockDoor() { isLocked = false; }
    public bool UnlockDoor(string keyId)
    {
        if (string.IsNullOrEmpty(requiredKeyId) || !string.Equals(requiredKeyId, keyId, StringComparison.Ordinal)) return false;
        UnlockDoor(); return true;
    }
    protected virtual void Update() => Tick(Time.deltaTime);
    public void Tick(float deltaTime)
    {
        if (!IsMoving || decorative || isLocked || deltaTime <= 0) return;
        Physics.SyncTransforms();
        float end = Mathf.MoveTowards(angle, IsOpen ? openAngle : 0, CurrentSpeed * deltaTime);
        IsBlocked = false; BlockingCollider = null;
        while (Mathf.Abs(end - angle) > .00001f)
        {
            float candidate = Mathf.MoveTowards(angle, end, stepDegrees);
            if (!CanRotateTo(candidate)) { IsBlocked = true; Physics.SyncTransforms(); return; }
            angle = candidate;
            ApplyAngle();
        }
        Physics.SyncTransforms();
        if (Mathf.Abs(angle - (IsOpen ? openAngle : 0)) < .001f) IsMoving = false;
    }
    bool Ignore(Collider other) => !other || !other.enabled || other.isTrigger || other.transform.IsChildOf(transform);
    void ApplyAngle()
    {
        transform.localRotation = closed * Quaternion.Euler(0, angle, 0);
        transform.localPosition = pivotInParent - transform.localRotation * scaledHingeOffset;
    }
    bool CanRotateTo(float candidate)
    {
        Quaternion localRotation = closed * Quaternion.Euler(0, candidate, 0);
        Quaternion worldRotation = (transform.parent ? transform.parent.rotation : Quaternion.identity) * localRotation;
        Vector3 localPosition = pivotInParent - localRotation * scaledHingeOffset;
        Vector3 rootPosition = transform.parent ? transform.parent.TransformPoint(localPosition) : localPosition;
        Quaternion delta = worldRotation * Quaternion.Inverse(transform.rotation);
        foreach (var panel in panels)
        {
            if (!panel || !panel.enabled || panel.isTrigger) continue;
            Vector3 position = rootPosition + delta * (panel.transform.position - transform.position);
            Quaternion rotation = delta * panel.transform.rotation;
            Vector3 center = rootPosition + delta * (panel.transform.TransformPoint(panel.center) - transform.position);
            Vector3 scale = panel.transform.lossyScale;
            Vector3 half = Vector3.Scale(panel.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            foreach (var other in Physics.OverlapBox(center, half, rotation, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (Ignore(other)) continue;
                if (!Physics.ComputePenetration(panel, position, rotation, other, other.transform.position, other.transform.rotation, out _, out float depth)) continue;
                initialContacts.TryGetValue((panel, other), out float originalDepth);
                if (depth > Mathf.Max(contactTolerance, originalDepth + .0001f)) { BlockingCollider = other; BlockingPanel = panel; return false; }
            }
        }
        return true;
    }
    // Editor repair/audit compatibility only. Runtime interactions always use the swept Tick path.
    public void SetImmediate(bool open)
    {
        if (Application.isPlaying) { TrySetOpen(open, speed); return; }
        Initialize();
        if (decorative && open) return;
        IsOpen = open; IsMoving = false; IsBlocked = false; angle = open ? openAngle : 0;
        ApplyAngle(); Physics.SyncTransforms();
    }
    public Vector3 NearestPoint(Vector3 point)
    {
        Initialize(); Vector3 nearest = transform.position; float distance = float.PositiveInfinity;
        foreach (var panel in panels)
        {
            if (!panel || !panel.enabled || panel.isTrigger) continue;
            var candidate = panel.ClosestPoint(point); float d = (candidate - point).sqrMagnitude;
            if (d < distance) { nearest = candidate; distance = d; }
        }
        return nearest;
    }
}
