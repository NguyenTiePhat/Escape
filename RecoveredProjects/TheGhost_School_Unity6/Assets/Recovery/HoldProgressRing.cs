using UnityEngine;
using UnityEngine.UI;

// Procedural annulus: a transparent centre leaves the existing crosshair visible.
public sealed class HoldProgressRing : MaskableGraphic
{
    public PlayerHoldAction player;
    float progress = -1;
    bool shown;
    protected override void Awake() { base.Awake(); raycastTarget = false; }
    void LateUpdate()
    {
        var action = player ? player.ViewedAction : null;
        bool visible = action && action.isActiveAndEnabled;
        float value = visible ? action.Progress : 0;
        if (visible == shown && Mathf.Approximately(value, progress)) return;
        shown = visible; progress = value; SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); if (!shown) return;
        float outer = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
        AddArc(mesh, outer, outer - 5f, 1, new Color(0, 0, 0, .5f));
        AddArc(mesh, outer, outer - 5f, progress, Color.white);
    }
    static void AddArc(VertexHelper mesh, float outer, float inner, float fill, Color color)
    {
        if (fill <= 0) return;
        int segments = Mathf.Max(1, Mathf.CeilToInt(96 * fill));
        int start = mesh.currentVertCount;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * .5f - Mathf.PI * 2 * fill * i / segments;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            mesh.AddVert(direction * outer, color, Vector2.zero);
            mesh.AddVert(direction * inner, color, Vector2.zero);
            if (i == 0) continue;
            int index = start + i * 2;
            mesh.AddTriangle(index - 2, index, index - 1); mesh.AddTriangle(index, index + 1, index - 1);
        }
    }
}
