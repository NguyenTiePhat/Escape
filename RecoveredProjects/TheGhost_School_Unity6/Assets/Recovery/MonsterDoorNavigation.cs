using UnityEngine;
using UnityEngine.AI;

// Stops the agent before a leaf: NavMeshAgent alone does not collide with door colliders.
[DisallowMultipleComponent]
public sealed class MonsterDoorNavigation : MonoBehaviour
{
    Door waitingDoor;
    public Door OperatingDoor => waitingDoor;
    float waitUntil, retryAt;
    Vector3 previousPosition;
    bool hasPrevious;
    NavMeshAgent ownerAgent;

    public bool Tick(NavMeshAgent agent)
    {
        ownerAgent = agent;
        if (!agent || !agent.enabled || !agent.isOnNavMesh) return false;
        // Catch a large frame step before it can carry the agent through a door.
        if (hasPrevious)
        {
            Vector3 travelled = transform.position - previousPosition;
            if (travelled.sqrMagnitude > .0001f && travelled.magnitude < 10f &&
                FindDoor(previousPosition, travelled.normalized, travelled.magnitude, agent))
            {
                var target = agent.destination;
                agent.Warp(previousPosition);
                agent.SetDestination(target);
            }
        }
        previousPosition = transform.position; hasPrevious = true;
        if (waitingDoor)
        {
            if (!waitingDoor.isActiveAndEnabled || waitingDoor.IsFullyOpen)
            {
                if (Time.time < waitUntil && waitingDoor.isActiveAndEnabled) return Pause(agent);
                waitingDoor = null; agent.isStopped = false;
            }
            else
            {
                if (waitingDoor.isLocked || waitingDoor.decorative) return Blocked(agent, waitingDoor);
                // Retry a door whose first sweep was obstructed, without repeated toggle events.
                if (!waitingDoor.IsOpen && Time.time >= retryAt)
                {
                    retryAt = Time.time + .5f;
                    if (waitingDoor.Open(1f)) waitUntil = Time.time + 1f;
                }
                return Pause(agent);
            }
        }
        if (!agent.hasPath || agent.pathPending) return false;
        Vector3 direction = Vector3.ProjectOnPlane(agent.steeringTarget - transform.position, Vector3.up);
        if (direction.sqrMagnitude < .001f) return false;
        var door = FindDoor(transform.position, direction.normalized,
            Mathf.Min(direction.magnitude, Mathf.Max(1.6f, agent.velocity.magnitude * Time.deltaTime + .5f)), agent);
        if (!door) { agent.isStopped = false; return false; }
        if (door.isLocked || door.decorative) return Blocked(agent, door);
        if (door.IsFullyOpen) return Pause(agent); // The open leaf still physically obstructs this route.
        waitingDoor = door; retryAt = Time.time + .5f;
        if (!door.GetComponent<DoorNoise>()) door.gameObject.AddComponent<DoorNoise>();
        if (!door.IsOpen) door.Open(1f);
        waitUntil = Time.time + 1f;
        return Pause(agent);
    }
    bool Blocked(NavMeshAgent agent, Door door)
    {
        if (!door.GetComponent<DoorNavBlocker>()) door.gameObject.AddComponent<DoorNavBlocker>();
        if (Time.time >= retryAt)
        {
            retryAt = Time.time + .5f;
            var path = new NavMeshPath();
            if (agent.CalculatePath(agent.destination, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                // Wait until carving produces a route that no longer approaches this leaf.
                var direction = Vector3.ProjectOnPlane(agent.steeringTarget - transform.position, Vector3.up);
                if (direction.sqrMagnitude > .001f && !FindDoor(transform.position, direction.normalized,
                    Mathf.Min(1.6f, direction.magnitude), agent))
                {
                    waitingDoor = null; agent.isStopped = false; return false;
                }
            }
        }
        return Pause(agent);
    }
    bool Pause(NavMeshAgent agent) { agent.isStopped = true; agent.velocity = Vector3.zero; return true; }
    Door FindDoor(Vector3 position, Vector3 direction, float distance, NavMeshAgent agent)
    {
        float radius = Mathf.Max(.1f, agent.radius);
        Vector3 bottom = position + Vector3.up * (radius + .08f);
        Vector3 top = position + Vector3.up * Mathf.Max(radius + .08f, agent.height - radius);
        Door closest = null; float nearest = float.PositiveInfinity;
        foreach (var hit in Physics.CapsuleCastAll(bottom, top, radius, direction, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) continue;
            var door = hit.collider.GetComponentInParent<Door>();
            if (door && hit.distance < nearest) { closest = door; nearest = hit.distance; }
        }
        return closest;
    }
    void OnDisable()
    {
        waitingDoor = null; hasPrevious = false;
        if (ownerAgent && ownerAgent.enabled && ownerAgent.isOnNavMesh) ownerAgent.isStopped = false;
    }
}
