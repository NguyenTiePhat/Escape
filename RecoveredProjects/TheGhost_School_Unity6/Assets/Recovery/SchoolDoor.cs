using System.Collections.Generic;
using UnityEngine;

public sealed class SchoolDoor : MonoBehaviour
{
    public static readonly HashSet<SchoolDoor> Active = new HashSet<SchoolDoor>();
    public float openAngle = 100f;
    public float speed = 160f;
    public bool IsOpen { get; private set; }
    Quaternion closed;
    float angle;
    Collider[] panels;
    void Awake() { Initialize(); }
    public void Initialize() { closed = transform.localRotation; panels = GetComponentsInChildren<Collider>(); }
    void OnEnable() { Active.Add(this); }
    void OnDisable() { Active.Remove(this); }
    public void Toggle() { IsOpen = !IsOpen; }
    public void SetImmediate(bool open)
    {
        IsOpen = open;
        angle = open ? openAngle : 0;
        transform.localRotation = closed * Quaternion.Euler(0, angle, 0);
        Physics.SyncTransforms();
    }
    void Update()
    {
        angle = Mathf.MoveTowards(angle, IsOpen ? openAngle : 0, speed * Time.deltaTime);
        transform.localRotation = closed * Quaternion.Euler(0, angle, 0);
    }
    public Vector3 NearestPoint(Vector3 point)
    {
        Vector3 nearest = transform.position;
        float distance = float.PositiveInfinity;
        if (panels == null) panels = GetComponentsInChildren<Collider>();
        foreach (var panel in panels)
        {
            if (!panel.enabled || panel.isTrigger) continue;
            var candidate = panel.ClosestPoint(point);
            float d = (candidate - point).sqrMagnitude;
            if (d < distance) { distance = d; nearest = candidate; }
        }
        return nearest;
    }
}

