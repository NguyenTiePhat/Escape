using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class BossMapAccessSetup
{
    public static readonly Vector3[] Destinations = {
        new Vector3(-11,2,40.3f), new Vector3(3,2.12f,18), new Vector3(5,2,54),
        new Vector3(21,2,54.8f), new Vector3(31,2,55.5f), new Vector3(44.9f,2,52.15f),
        new Vector3(40.05f,8.52f,54), new Vector3(68,8.52f,54),
        new Vector3(52,8.52f,44), new Vector3(41,8.52f,60), new Vector3(35.3f,15.01f,52)
    };
    public static void Run() { Apply(); EditorApplication.Exit(0); }
    public static void Validate() { Apply(); BossMapAccessValidation.Run(); }
    static void Apply()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        var boss = Object.FindAnyObjectByType<MonsterAI>();
        var report = new StringBuilder();
        var links = Object.FindAnyObjectByType<MonsterStairLinks>();
        if (!links) links = new GameObject("Boss stair seam passages").AddComponent<MonsterStairLinks>();
        links.enabled = false;
        links.passages = new[] {
            Passage(new Vector3(40.05f,8.15f,51.1f),new Vector3(40.05f,8.52f,52.5f)),
            Passage(new Vector3(35.3f,14.65f,51.1f),new Vector3(35.3f,15.01f,52.5f))
        };
        links.enabled = true;
        if (!NavMesh.SamplePosition(Destinations[0], out var spawn, 1, NavMesh.AllAreas)) throw new Exception("Column room spawn missing NavMesh");
        var points = Destinations.Select(p => {
            if (!NavMesh.SamplePosition(p,out var hit,1,NavMesh.AllAreas)) throw new Exception("No NavMesh at "+p);
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(spawn.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                throw new Exception("Disconnected from Column room: "+p+" endpoint="+(path.corners.Length>0?path.corners.Last().ToString():"none"));
            report.AppendLine("PASS Column room route to "+hit.position);
            return hit.position;
        }).ToArray();
        var root = GameObject.Find("Boss whole school patrol") ?? new GameObject("Boss whole school patrol");
        var waypoints = new Transform[points.Length];
        for(int i=0;i<points.Length;i++) {
            var t=root.transform.Find("Patrol "+i);
            if(!t){t=new GameObject("Patrol "+i).transform;t.SetParent(root.transform);}
            t.position=points[i];waypoints[i]=t;
        }
        boss.transform.position=spawn.position;
        boss.GetComponent<NavMeshAgent>().autoTraverseOffMeshLink=true;
        boss.waypoints=waypoints;
        boss.config.hearAcrossMap=true;boss.config.hearingThreshold=0;
        EditorUtility.SetDirty(boss.config);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        report.AppendLine("Saved: spawn in Column (2) room, 11 patrol points, global positive-noise hearing. Locked doors and barricade colliders unchanged.");
        File.WriteAllText("RecoveryReports/boss-map-access-setup.txt",report.ToString());
    }
    static MonsterStairLinks.Passage Passage(Vector3 a,Vector3 b)
    {
        if(!NavMesh.SamplePosition(a,out var start,.6f,NavMesh.AllAreas)||!NavMesh.SamplePosition(b,out var end,.6f,NavMesh.AllAreas))
            throw new Exception("Stair seam endpoints missing: "+a+" / "+b);
        return new MonsterStairLinks.Passage{start=start.position,end=end.position};
    }
}
