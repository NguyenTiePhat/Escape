using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterAI))]
public sealed class MonsterAIEditor : Editor
{
    void OnSceneGUI()
    {
        var ai = (MonsterAI)target;
        string detail = ai.IsWaiting ? " — waiting " + ai.WaitRemaining.ToString("F1") + "s" : "";
        if (ai.IsLookingAround) detail = " — looking " + ai.LookRemaining.ToString("F1") + "s";
        Handles.Label(ai.transform.position + Vector3.up * 2.5f, ai.CurrentState + detail + " awareness=" + ai.Awareness.ToString("F2"));
        if (ai.waypoints == null) return;
        Handles.color = new Color(1, .75f, .15f);
        for (int i = 0; i < ai.waypoints.Length; i++)
        {
            var point = ai.waypoints[i]; if (!point) continue;
            Handles.Label(point.position + Vector3.up * .15f, "Patrol " + (i + 1));
            Handles.DrawWireDisc(point.position, Vector3.up, .2f);
        }
        if (Application.isPlaying) SceneView.RepaintAll();
    }
}
