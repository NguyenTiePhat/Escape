using System.Collections.Generic;
using UnityEngine;

// Author only rooms that are completely dark; disabled zones stop applying immediately.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class MonsterDarknessZone : MonoBehaviour
{
    static readonly HashSet<MonsterDarknessZone> active = new HashSet<MonsterDarknessZone>();
    BoxCollider volume;
    void Reset() { GetComponent<BoxCollider>().isTrigger = true; }
    void OnEnable() { volume = GetComponent<BoxCollider>(); active.Add(this); }
    void OnDisable() { active.Remove(this); }
    public static bool Contains(Vector3 position)
    {
        foreach (var zone in active)
        {
            if (!zone || !zone.volume || !zone.volume.enabled) continue;
            Vector3 p = zone.transform.InverseTransformPoint(position) - zone.volume.center;
            Vector3 half = zone.volume.size * .5f;
            if (Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z) return true;
        }
        return false;
    }
}
