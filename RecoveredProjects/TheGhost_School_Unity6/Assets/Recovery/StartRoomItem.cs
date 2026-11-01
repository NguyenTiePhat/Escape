using System.Linq;
using UnityEngine;

public sealed class StartRoomItem : MonoBehaviour, IInteractable
{
    public bool flashlight;
    public string Prompt => flashlight ? "Nhặt đèn pin" : "Nhặt tờ giấy";
    public void Interact() { var room = FindFirstObjectByType<StartRoom>(); if (room && !room.Reading) room.TryPickup(this); }
    void Awake()
    {
        // The recovered paper's writing was stored as siblings on the table.
        // Keep those details with the paper when it is picked up.
        if (!flashlight && transform.parent)
            foreach (var child in transform.parent.Cast<Transform>().Where(t => t.name == "Dòng chữ").ToArray())
                child.SetParent(transform, true);
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
            foreach (var collider in GetComponentsInChildren<Collider>(true)) collider.gameObject.layer = layer;
    }
}
