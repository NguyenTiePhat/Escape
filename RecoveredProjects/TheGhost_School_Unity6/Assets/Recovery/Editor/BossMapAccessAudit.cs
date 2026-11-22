using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class BossMapAccessAudit
{
    public static void Rebuild()
    {
        M6NavigationSetup.Run();
        Run();
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        Physics.SyncTransforms();
        var report = new StringBuilder();
        var boss = Object.FindAnyObjectByType<MonsterAI>();
        report.AppendLine("BOSS " + boss.transform.position + " agent=" + boss.GetComponent<NavMeshAgent>().radius);
        for(float z=50;z<=54;z+=.25f)
        {
            var p=new Vector3(40.05f,8.5f,z);
            if(Physics.Raycast(p+Vector3.up,Vector3.down,out var floor,3,~0,QueryTriggerInteraction.Ignore))
            {
                bool ceiling=Physics.Raycast(floor.point+Vector3.up*.05f,Vector3.up,out var roof,3,~0,QueryTriggerInteraction.Ignore);
                bool nav=NavMesh.SamplePosition(floor.point,out var n,.2f,NavMesh.AllAreas);
                report.AppendLine("STAIR z="+z+" floor="+floor.point+" nav="+nav+" ceiling="+(ceiling?roof.distance.ToString():"clear")+" roof="+(ceiling?MapCollisionAudit.PathOf(roof.transform):"none"));
            }
        }
        foreach (var t in Object.FindObjectsByType<Transform>().Where(t => t.name == "Column (2)" || t.name == "1stFloorBarricade (2)"))
        {
            report.AppendLine(MapCollisionAudit.PathOf(t) + " position=" + t.position);
            foreach (var c in t.GetComponentsInChildren<Collider>()) report.AppendLine(" COLLIDER " + c.name + " " + c.bounds);
        }
        NavMesh.SamplePosition(boss.transform.position, out var start, 2, NavMesh.AllAreas);
        var targets = new[] { new Vector3(5,2,54), new Vector3(28,2,54), new Vector3(44.9f,2,52.15f), new Vector3(44.9f,5.38f,46.3f), new Vector3(40.05f,8.52f,54), new Vector3(68,8.52f,54), new Vector3(52,8.52f,44), new Vector3(35.3f,15.01f,52), new Vector3(3,2.12f,18) };
        foreach (var target in targets.Concat(boss.waypoints.Where(t => t).Select(t => t.position)))
        {
            var path = new NavMeshPath();
            bool sample = NavMesh.SamplePosition(target, out var end, 1, NavMesh.AllAreas);
            bool complete = sample && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
            report.AppendLine("TARGET " + target + " sample=" + sample + " complete=" + complete + " corners=" + string.Join(";", path.corners.Select(p => p.ToString("F2"))));
            if (!complete && path.corners.Length > 0)
                foreach(var c in Physics.OverlapSphere(path.corners.Last()+Vector3.up,1.2f,~0,QueryTriggerInteraction.Ignore)) report.AppendLine(" END " + MapCollisionAudit.PathOf(c.transform) + " " + c.bounds);
        }
        foreach(var door in Object.FindObjectsByType<Door>().Where(d => !d.decorative)) report.AppendLine("DOOR " + MapCollisionAudit.PathOf(door.transform) + " pos="+door.transform.position+" locked="+door.isLocked+" key="+door.requiredKeyId);
        File.WriteAllText("RecoveryReports/boss-map-access-audit.txt", report.ToString());
        EditorApplication.Exit(0);
    }
}
