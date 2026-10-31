using System.Linq;
using UnityEngine;

public sealed class StartRoomItem : MonoBehaviour
{
    public bool flashlight;
    void Awake()
    {
        // The recovered paper's writing was stored as siblings on the table.
        // Keep those details with the paper when it is picked up.
        if (flashlight || !transform.parent) return;
        foreach (var child in transform.parent.Cast<Transform>().Where(t => t.name == "Dòng chữ").ToArray())
            child.SetParent(transform, true);
    }
}
