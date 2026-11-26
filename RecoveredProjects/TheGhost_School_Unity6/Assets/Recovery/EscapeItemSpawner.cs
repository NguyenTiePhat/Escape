using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EscapeItemSpawner : MonoBehaviour
{
    [Serializable] public sealed class Entry { public ItemData item; public Transform[] points; }
    public Entry[] entries;
    public readonly List<PickupItem> Spawned = new List<PickupItem>();
    bool spawned;
    GameStateManager round;
    void Start()
    {
        round = GameStateManager.Instance;
        if (round) { round.OnStateChanged += StateChanged; StateChanged(round.State); }
        else Spawn();
    }
    void StateChanged(GameState state) { if (state == GameState.Playing) Spawn(); }
    void OnDestroy() { if (round) round.OnStateChanged -= StateChanged; }
    public void Spawn()
    {
        if (spawned || entries == null) return;
        foreach (var entry in entries) if (entry == null || !entry.item || !entry.item.pickupPrefab)
        { Debug.LogError("Missing escape item prefab.", this); return; }
        var assignment = new Transform[entries.Length];
        if (!Choose(0, assignment, new HashSet<Transform>()))
        { Debug.LogError("Escape item spawn points are incomplete or overlap.", this); return; }
        spawned = true;
        for (int i = 0; i < entries.Length; i++)
        {
            var prop = Instantiate(entries[i].item.pickupPrefab, assignment[i].position, assignment[i].rotation, transform);
            prop.item = entries[i].item;
            prop.quantity = 1;
            var body = prop.GetComponent<Rigidbody>(); if (body) { body.isKinematic = true; body.useGravity = false; }
            Spawned.Add(prop);
        }
    }
    bool Choose(int index, Transform[] assignment, HashSet<Transform> used)
    {
        if (index == entries.Length) return true;
        if (entries[index].points == null) return false;
        var options = new List<Transform>(entries[index].points);
        for (int i = options.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var value = options[i]; options[i] = options[j]; options[j] = value; }
        foreach (var point in options)
        {
            if (!point || used.Contains(point)) continue;
            bool overlap = false;
            var location = point.GetComponent<EscapeSpawnLocation>();
            for (int j = 0; j < index; j++)
            {
                if (Vector3.Distance(point.position, assignment[j].position) < .6f) overlap = true;
                var previous = assignment[j].GetComponent<EscapeSpawnLocation>();
                if (location && previous && !string.IsNullOrEmpty(location.region) && location.region == previous.region) overlap = true;
                // The two ingredients for the main-exit key must require exploring
                // different floors and distant parts of the school in every round.
                if (location && previous && IsKeyIngredient(entries[index].item.id) && IsKeyIngredient(entries[j].item.id))
                {
                    if (Mathf.Abs(point.position.y - assignment[j].position.y) < 5f ||
                        Vector3.ProjectOnPlane(point.position - assignment[j].position, Vector3.up).magnitude < 40f) overlap = true;
                }
            }
            if (overlap) continue;
            used.Add(point); assignment[index] = point;
            if (Choose(index + 1, assignment, used)) return true;
            used.Remove(point);
        }
        return false;
    }
    static bool IsKeyIngredient(string id) => id == "key_frame" || id == "metal_blank";
}
