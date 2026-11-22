using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

public static class EscapeRandomSetup
{
    const string Root = "Assets/Recovery/";
    static readonly string[] ids = { "silver_key", "key_frame", "metal_blank", "saw_blade", "saw_handle", "screws", "main_exit_key", "assembled_saw" };
    static readonly string[] models = { "SilverKey", "KeyMold", "MetalBlank", "SawBlade", "SawHandle", "Screws", "GoldKey", "AssembledSaw" };
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(Root + "Scenes/School_Test.unity");
        var spawner = Object.FindAnyObjectByType<EscapeItemSpawner>();
        if (!spawner) throw new Exception("Missing existing escape spawner");
        var origin = Object.FindAnyObjectByType<PlayerMotor>().transform.position;
        if (!NavMesh.SamplePosition(origin, out var start, 3, NavMesh.AllAreas)) throw new Exception("Missing player NavMesh");
        var candidates = spawner.entries.SelectMany(e => e.points).Where(t => t).Select(t => t.position).Distinct().ToList();
        int originalCandidateCount = candidates.Count;
        var fixedKeys = Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None).Where(p => p.item && p.item.id == "silver_key").ToArray();
        foreach (var key in fixedKeys) for (int i = -2; i <= 2; i++) candidates.Add(key.transform.position + Vector3.right * i * .25f);
        foreach (var key in fixedKeys) key.gameObject.SetActive(false);
        Physics.SyncTransforms();
        foreach (var key in fixedKeys) for (float x = -2; x <= 2; x += .3f) for (float z = -2; z <= 2; z += .3f) {
            var top = key.transform.position + new Vector3(x, .7f, z);
            if (Physics.Raycast(top, Vector3.down, out var hit, 1.5f, ~0, QueryTriggerInteraction.Ignore) && hit.normal.y > .95f && hit.point.y > start.position.y + .4f) candidates.Add(hit.point + Vector3.up * .018f);
        }
        var items = new ItemData[ids.Length];
        var report = new System.Text.StringBuilder();
        var entries = new EscapeItemSpawner.Entry[6];
        var group = new GameObject("Random escape pickup locations").transform; group.SetParent(spawner.transform);
        for (int i = 0; i < ids.Length; i++)
        {
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(Root + "M6/Items/" + ids[i] + ".asset");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "ReferenceProps/Prefabs/" + models[i] + ".prefab");
            if (!item || !prefab || prefab.GetComponent<PickupItem>().item != item) throw new Exception("Invalid reference model " + ids[i]);
            items[i] = item;
            if (i >= 6) continue;
            var box = prefab.GetComponent<BoxCollider>();
            var valid = new List<Transform>();
            foreach (var pos in i == 0 ? candidates : candidates.Take(originalCandidateCount))
            {
                if (!Reachable(pos, start.position, i != 0)) continue;
                Vector3 center = pos + box.center;
                var ext = Vector3.Max(box.size * .48f, Vector3.one * .003f);
                if (Physics.OverlapBox(center + Vector3.up * .012f, ext, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Any(c => !(c is CharacterController))) continue;
                if (!Physics.Raycast(pos + Vector3.up * .03f, Vector3.down, .13f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var t = new GameObject(ids[i] + " candidate " + valid.Count).transform; t.SetParent(group); t.position = pos; valid.Add(t);
            }
            if (valid.Count < 2) throw new Exception("Insufficient accessible locations for " + ids[i] + ": " + valid.Count);
            entries[i] = new EscapeItemSpawner.Entry { item = item, points = valid.ToArray() };
            report.AppendLine(ids[i] + ": count=1, accessible candidates=" + valid.Count);
        }
        // Mutate saved assets only after every item has valid candidate locations.
        for (int i = 0; i < ids.Length; i++) { items[i].pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "ReferenceProps/Prefabs/" + models[i] + ".prefab").GetComponent<PickupItem>(); EditorUtility.SetDirty(items[i]); }
        spawner.entries = entries;
        foreach (var key in fixedKeys) Object.DestroyImmediate(key.gameObject);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        report.AppendLine("GoldKey and AssembledSaw remain crafting outputs; no pre-spawned shortcuts. All six pickups use ReferenceProps prefabs. Paths checked against locked doors.");
        File.WriteAllText("RecoveryReports/escape-random-setup.txt", report.ToString());
    }
    public static bool Reachable(Vector3 target, Vector3 origin, bool hasSilverKey = false)
    {
        for (int k = 0; k < 16; k++)
        {
            Vector3 near = target + Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward * .9f; near.y = origin.y;
            if (!NavMesh.SamplePosition(near, out var stand, .45f, NavMesh.AllAreas)) continue;
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(origin, stand.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
            bool locked = false;
            for (int n = 1; n < path.corners.Length; n++)
            {
                Vector3 a = path.corners[n - 1] + Vector3.up * .8f, delta = path.corners[n] - path.corners[n - 1];
                if (Physics.SphereCastAll(a, .25f, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore).Any(h => { var d = h.collider.GetComponentInParent<Door>(); return d && d.isLocked && !(hasSilverKey && d.requiredKeyId == "silver_key"); })) { locked = true; break; }
            }
            if (locked) continue;
            if (Physics.OverlapCapsule(stand.position + Vector3.up * .4f, stand.position + Vector3.up * 1.5f, .25f, ~0, QueryTriggerInteraction.Ignore).Any(c => !(c is CharacterController))) continue;
            var eye = stand.position + Vector3.up * 1.6f; var sight = target + Vector3.up * .03f - eye;
            if (sight.magnitude <= 2.5f && !Physics.Raycast(eye, sight.normalized, sight.magnitude - .05f, ~0, QueryTriggerInteraction.Ignore)) return true;
        }
        return false;
    }
}



