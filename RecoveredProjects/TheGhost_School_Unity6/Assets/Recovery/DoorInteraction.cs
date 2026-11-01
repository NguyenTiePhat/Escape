using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SchoolExplorer))]
public sealed class DoorInteraction : MonoBehaviour
{
    public float reach = 2.2f;
    SchoolExplorer player;


    void Start() { player = GetComponent<SchoolExplorer>(); }
    public SchoolDoor FindNearby()
    {
        var origin = player.view.transform.position;
        SchoolDoor best = null;
        float closest = reach;
        foreach (var door in SchoolDoor.Active)
        {
            if (!door || !door.isActiveAndEnabled) continue;
            var point = door.NearestPoint(origin);
            var offset = point - origin;
            float distance = offset.magnitude;
            if (distance > closest) continue;
            var hits = Physics.RaycastAll(origin, offset.normalized, distance + .05f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !h.collider.GetComponentInParent<SchoolExplorer>()).OrderBy(h => h.distance).ToArray();
            if (hits.Length > 0 && hits[0].collider.GetComponentInParent<SchoolDoor>() != door) continue;
            closest = distance;
            best = door;
        }
        return best;
    }
    // Legacy proximity query retained for old editor audits. Runtime input is PlayerInteractor/E.
}
