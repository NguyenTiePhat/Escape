using UnityEngine;
using UnityEngine.AI;

// Created only for a locked/decorative leaf encountered by a monster.
// Carving lets all agents route around it; the physical guard also covers carving latency.
[DisallowMultipleComponent, RequireComponent(typeof(Door))]
public sealed class DoorNavBlocker : MonoBehaviour
{
    Door door;
    NavMeshObstacle[] obstacles;
    void Awake()
    {
        door = GetComponent<Door>();
        var panels = GetComponentsInChildren<BoxCollider>();
        obstacles = new NavMeshObstacle[panels.Length];
        for (int i = 0; i < panels.Length; i++)
        {
            var helper = new GameObject("Locked door navigation blocker");
            helper.transform.SetParent(panels[i].transform, false);
            var obstacle = helper.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = panels[i].center;
            obstacle.size = panels[i].size;
            obstacle.carving = true; obstacle.carveOnlyStationary = false;
            obstacles[i] = obstacle;
        }
        Update();
    }
    void Update()
    {
        bool blocking = door && door.isActiveAndEnabled && (door.isLocked || door.decorative);
        foreach (var obstacle in obstacles) if (obstacle) obstacle.enabled = blocking;
    }
    void OnDisable() { if (obstacles != null) foreach (var obstacle in obstacles) if (obstacle) obstacle.enabled = false; }
    void OnDestroy() { if (obstacles != null) foreach (var obstacle in obstacles) if (obstacle) Destroy(obstacle.gameObject); }
}
