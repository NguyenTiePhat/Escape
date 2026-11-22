using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Built-in camera overlay. The scene depth and stencil keep the outline off walls and item interiors.
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class PickupProximityOutline : MonoBehaviour
{
    [Min(.1f)] public float distance = 3f;
    [Range(1, 5)] public float width = 2.5f;
    public int HighlightedCount { get; private set; }
    Camera view;
    Material material;
    CommandBuffer commands;
    readonly List<Renderer> visible = new List<Renderer>();
    void OnEnable()
    {
        view = GetComponent<Camera>();
        var shader = Resources.Load<Shader>("PickupOutline");
        if (!shader || !shader.isSupported) { Debug.LogError("Pickup outline shader is unavailable.", this); return; }
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        commands = new CommandBuffer { name = "Nearby pickup white outlines" };
        view.AddCommandBuffer(CameraEvent.AfterForwardAlpha, commands);
    }
    void OnPreCull()
    {
        if (commands == null) return;
        commands.Clear(); visible.Clear(); HighlightedCount = 0;
        var game = GameStateManager.Instance;
        var health = GetComponentInParent<PlayerHealth>();
        if ((game && game.State != GameState.Playing) || (health && !health.CanParticipate)) return;
        material.SetFloat("_OutlinePixels", width);
        foreach (var pickup in PickupItem.ActiveItems)
        {
            if (!pickup || !pickup.isActiveAndEnabled || !pickup.item) continue;
            var bounds = pickup.GetComponent<Collider>().bounds;
            if ((bounds.ClosestPoint(view.transform.position) - view.transform.position).sqrMagnitude > distance * distance) continue;
            int before = visible.Count;
            foreach (var renderer in pickup.VisualRenderers)
                if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy && (view.cullingMask & (1 << renderer.gameObject.layer)) != 0)
                    visible.Add(renderer);
            if (visible.Count > before) HighlightedCount++;
        }
        // Finish all masks before drawing any outlines, including multi-part screw/handle models.
        for (int pass = 0; pass < 2; pass++)
            foreach (var renderer in visible)
            {
                var mesh = renderer.GetComponent<MeshFilter>();
                int submeshes = mesh && mesh.sharedMesh ? mesh.sharedMesh.subMeshCount : renderer is SkinnedMeshRenderer skin && skin.sharedMesh ? skin.sharedMesh.subMeshCount : 0;
                for (int submesh = 0; submesh < submeshes; submesh++) commands.DrawRenderer(renderer, material, submesh, pass);
            }
    }
    void OnDisable()
    {
        HighlightedCount = 0;
        if (commands != null) { if (view) view.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, commands); commands.Release(); commands = null; }
        if (material) Destroy(material);
    }
}
