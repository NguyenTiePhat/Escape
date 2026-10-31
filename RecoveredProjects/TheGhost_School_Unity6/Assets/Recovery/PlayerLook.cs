using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PlayerLook : MonoBehaviour
{
    public Transform view;
    [Min(0)] public float sensitivity = .12f;
    public bool InputBlocked { get; set; }
    float pitch;
    void Start()
    {
        if (!view) { var camera = GetComponentInChildren<Camera>(); if (camera) view = camera.transform; }
        if (view) pitch = Mathf.DeltaAngle(0, view.localEulerAngles.x);
    }
    void Update()
    {
        if (InputBlocked || !Application.isFocused) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return; }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            return;
        }
        if (Mouse.current != null) ApplyLook(Mouse.current.delta.ReadValue());
    }
    public void ApplyLook(Vector2 delta)
    {
        if (!view) return;
        // Mouse displacement is already per frame, so no deltaTime multiplier.
        transform.Rotate(0, delta.x * sensitivity, 0);
        pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -80, 80);
        view.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
}
