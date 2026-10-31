using UnityEngine;

// Compatibility bridge for existing scene, inventory and door references.
[DefaultExecutionOrder(-100)]
public sealed class SchoolExplorer : MonoBehaviour
{
    public Camera view;
    public Vector3 home;
    public bool IsCrouching => motor && motor.IsCrouching;
    PlayerMotor motor;
    PlayerLook look;
    StartRoom startRoom;
    void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        if (!motor) motor = gameObject.AddComponent<PlayerMotor>();
        look = GetComponent<PlayerLook>();
        if (!look) look = gameObject.AddComponent<PlayerLook>();
        if (!view) view = GetComponentInChildren<Camera>();
        motor.view = look.view = view ? view.transform : null;
        startRoom = FindFirstObjectByType<StartRoom>();
        if (view && !view.GetComponent<FirstPersonHands>()) view.gameObject.AddComponent<FirstPersonHands>();
    }
    void Update() { motor.InputBlocked = look.InputBlocked = startRoom && startRoom.Reading; }
    void OnEnable() { if (motor) motor.enabled = true; if (look) look.enabled = true; }
    void OnDisable() { if (motor) motor.enabled = false; if (look) look.enabled = false; }
    public bool SetCrouching(bool value) => motor.SetCrouching(value);
    public void MoveGrounded(Vector3 velocity, bool jump, float deltaTime) => motor.MoveGrounded(velocity, deltaTime);
}
