using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Door))]
public sealed class DoorNoise : MonoBehaviour
{
    Door door;
    void OnEnable() { door = GetComponent<Door>(); door.OnDoorToggled += OnToggled; }
    void OnDisable() { if (door) door.OnDoorToggled -= OnToggled; }
    public static float CalculateLoudness(float angularSpeed) => Mathf.Clamp(.1f + Mathf.Abs(angularSpeed) * .004f, .1f, 1f);
    void OnToggled(Door sender, bool open, float speed)
    {
        if (sender.decorative) return;
        var panel = sender.GetComponentInChildren<Collider>();
        NoiseManager.EmitNoise(panel ? panel.bounds.center : sender.transform.position, CalculateLoudness(speed), NoiseSource.Door, sender.gameObject);
    }
}
