using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SchoolExplorer))]
public sealed class DoorInteraction : MonoBehaviour
{
    public float reach = 2.2f;
    SchoolExplorer player;
    StartRoom room;
    SchoolDoor target;
    void Start() { player = GetComponent<SchoolExplorer>(); room = FindFirstObjectByType<StartRoom>(); }
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
    void Update()
    {
        target = null;
        if (Cursor.lockState != CursorLockMode.Locked || (room && room.Reading)) return;
        target = FindNearby();
        if (target && Input.GetKeyDown(KeyCode.F)) target.Toggle();
    }
    void OnGUI()
    {
        if (target) GUI.Box(new Rect(Screen.width / 2 - 130, Screen.height * .8f, 260, 36), target.IsOpen ? "[F] Đóng cửa" : "[F] Mở cửa");
    }
}
