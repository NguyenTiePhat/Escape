using UnityEngine;
using UnityEngine.AI;

// Owns a baked NavMeshData asset in the recovered scene (which has no NavMeshSurface).
[ExecuteAlways, DefaultExecutionOrder(-200), DisallowMultipleComponent]
public sealed class MonsterNavMesh : MonoBehaviour
{
    public NavMeshData data;
    NavMeshDataInstance instance;
    void OnEnable() { if (data && !instance.valid) instance = NavMesh.AddNavMeshData(data); }
    void OnDisable() { if (instance.valid) instance.Remove(); }
}
