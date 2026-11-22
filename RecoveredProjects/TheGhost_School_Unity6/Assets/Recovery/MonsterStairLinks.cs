using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Short, continuous passages across recovered stair/floor seams, not teleports between floors.
[ExecuteAlways, DefaultExecutionOrder(-150)]
public sealed class MonsterStairLinks : MonoBehaviour
{
    [Serializable] public struct Passage { public Vector3 start, end; }
    public Passage[] passages = Array.Empty<Passage>();
    readonly List<NavMeshLinkInstance> links = new List<NavMeshLinkInstance>();
    void OnEnable()
    {
        foreach (var p in passages)
        {
            var link = NavMesh.AddLink(new NavMeshLinkData {
                startPosition = p.start, endPosition = p.end, width = .3f,
                bidirectional = true, agentTypeID = 0, area = 0, costModifier = -1
            });
            if (link.valid) links.Add(link);
        }
    }
    void OnDisable() { foreach (var link in links) if (link.valid) link.Remove(); links.Clear(); }
}
