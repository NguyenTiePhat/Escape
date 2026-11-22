using UnityEngine;
using UnityEngine.AI;

// Created for a locked leaf or an open leaf physically obstructing a monster's route.
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
            // Leave clearance for the physical capsule sweep and NavMesh voxel rounding.
            var scale = panels[i].transform.lossyScale;
            obstacle.size = panels[i].size + new Vector3(.16f / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                .02f / Mathf.Max(.001f, Mathf.Abs(scale.y)), .16f / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            // Recovered door panels float above the floor. Carving must reach the
            // navigation surface, otherwise the agent plans straight through the leaf.
            if (NavMesh.SamplePosition(panels[i].bounds.center, out var floor, 4f, NavMesh.AllAreas))
            {
                float bottom = panels[i].transform.InverseTransformPoint(new Vector3(
                    panels[i].bounds.center.x, floor.position.y - .15f, panels[i].bounds.center.z)).y;
                float top = obstacle.center.y + obstacle.size.y * .5f;
                bottom = Mathf.Min(bottom, obstacle.center.y - obstacle.size.y * .5f);
                var center = obstacle.center; center.y = (top + bottom) * .5f; obstacle.center = center;
                var size = obstacle.size; size.y = top - bottom; obstacle.size = size;
            }
            obstacle.carving = true; obstacle.carveOnlyStationary = false;
            obstacles[i] = obstacle;
        }
        Update();
    }
    void Update()
    {
        // Decorative leaves intentionally have their Door behaviour disabled,
        // but their solid colliders still require navigation clearance.
        bool blocking = door && door.gameObject.activeInHierarchy && (door.decorative ||
            (door.isActiveAndEnabled && (door.isLocked || door.IsFullyOpen || door.IsBlocked)));
        foreach (var obstacle in obstacles) if (obstacle) obstacle.enabled = blocking;
    }
    void OnDisable() { if (obstacles != null) foreach (var obstacle in obstacles) if (obstacle) obstacle.enabled = false; }
    void OnDestroy() { if (obstacles != null) foreach (var obstacle in obstacles) if (obstacle) Destroy(obstacle.gameObject); }
}
