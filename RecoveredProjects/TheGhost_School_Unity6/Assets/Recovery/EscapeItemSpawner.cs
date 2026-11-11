using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EscapeItemSpawner : MonoBehaviour
{
    [Serializable] public sealed class Entry { public ItemData item; public Transform[] points; }
    public Entry[] entries;
    public readonly List<PickupItem> Spawned = new List<PickupItem>();
    bool spawned;
    void Start() => Spawn();
    public void Spawn()
    {
        if (spawned || entries == null) return;
        // Build the entire assignment before spawning anything. Backtracking avoids random dead ends.
        var assignment = new Transform[entries.Length];
        if (!Choose(0, assignment, new HashSet<Transform>()))
        { Debug.LogError("Escape item spawn points are incomplete or overlap.", this); return; }
        foreach (var entry in entries) if (entry == null || !entry.item || !entry.item.pickupPrefab)
        { Debug.LogError("Missing escape item prefab.", this); return; }
        spawned = true;
        for (int i = 0; i < entries.Length; i++)
        {
            var prop = Instantiate(entries[i].item.pickupPrefab, assignment[i].position, assignment[i].rotation);
            prop.item = entries[i].item;
            var body = prop.GetComponent<Rigidbody>(); if (body) { body.isKinematic = true; body.useGravity = false; }
            Spawned.Add(prop);
        }
    }
    bool Choose(int index, Transform[] assignment, HashSet<Transform> used)
    {
        if (index == entries.Length) return true;
        if (entries[index] == null || entries[index].points == null) return false;
        var options = new List<Transform>(entries[index].points);
        for (int i = options.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var value = options[i]; options[i] = options[j]; options[j] = value; }
        foreach (var point in options)
        {
            if (!point || used.Contains(point)) continue;
            bool overlap = false;
            for (int j = 0; j < index; j++) if (Vector3.Distance(point.position, assignment[j].position) < .6f) overlap = true;
            if (overlap) continue;
            used.Add(point); assignment[index] = point;
            if (Choose(index + 1, assignment, used)) return true;
            used.Remove(point);
        }
        return false;
    }
}
