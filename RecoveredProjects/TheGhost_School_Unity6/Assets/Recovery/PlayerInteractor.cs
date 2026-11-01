using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerInteractor : MonoBehaviour
{
    public Camera view;
    [Min(0)] public float range = 2.5f;
    public LayerMask interactableMask;
    public LayerMask obstructionMask = Physics.DefaultRaycastLayers;
    public bool InputBlocked { get; set; }
    public IInteractable Target { get; private set; }
    public event Action<IInteractable> OnTargetChanged;
    public event Action OnInteractWithoutTarget;

    void Awake()
    {
        if (!view) view = GetComponentInChildren<Camera>();
        if (interactableMask.value == 0) interactableMask = LayerMask.GetMask("Interactable");
    }
    void Update()
    {
        if (InputBlocked || !Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
        { ClearTarget(); return; }
        RefreshTarget();
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        HandleInteractionInput(keyboard.eKey.wasPressedThisFrame, keyboard.fKey.wasPressedThisFrame);
    }
    public void HandleInteractionInput(bool usePressed, bool doorPressed)
    {
        if (InputBlocked) return;
        RefreshTarget();
        if (Target is Door)
        {
            if (doorPressed) InteractWithTarget();
            else if (usePressed) OnInteractWithoutTarget?.Invoke();
        }
        else if (usePressed) InteractWithTarget();
    }
    public void RefreshTarget()
    {
        IInteractable nearest = null;
        float distance = float.PositiveInfinity;
        if (view && !InputBlocked)
        {
            var ray = new Ray(view.transform.position, view.transform.forward);
            foreach (var hit in Physics.RaycastAll(ray, range, interactableMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(transform) || hit.distance >= distance) continue;
                var candidate = hit.collider.GetComponentInParent<IInteractable>();
                if (!(candidate is MonoBehaviour behaviour) || !behaviour.isActiveAndEnabled) continue;
                nearest = candidate; distance = hit.distance;
            }
            // Only Interactable supplies targets; walls still occlude them.
            if (nearest != null)
                foreach (var hit in Physics.RaycastAll(ray, distance, obstructionMask.value & ~interactableMask.value, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(transform) && hit.distance < distance - .001f)
                    { nearest = null; break; }
        }
        SetTarget(nearest);
    }
    public void InteractWithTarget()
    {
        if (InputBlocked) { ClearTarget(); return; }
        RefreshTarget();
        if (Target is MonoBehaviour behaviour && behaviour && behaviour.isActiveAndEnabled)
        {
            Target.Interact();
            RefreshTarget(); // Pickups can deactivate themselves, clearing the prompt immediately.
        }
        else OnInteractWithoutTarget?.Invoke();
    }
    void SetTarget(IInteractable target)
    {
        if (ReferenceEquals(Target, target)) return;
        Target = target;
        OnTargetChanged?.Invoke(target);
    }
    public void ClearTarget() => SetTarget(null);
    void OnDisable() => ClearTarget();
}
