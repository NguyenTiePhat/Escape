using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class IntroModelSwap
{
    const string Root = "Assets/Recovery/NewIntroModels/";
    const string ScenePath = "Assets/Recovery/Scenes/School_Test.unity";

    [MenuItem("Tools/Preview held note and hands", true)]
    static bool CanPreview() => EditorApplication.isPlaying;

    [MenuItem("Tools/Preview held note and hands")]
    static void PreviewHeldNote()
    {
        var hands = Object.FindFirstObjectByType<FirstPersonHands>();
        if (!hands) { Debug.LogError("FirstPersonHands missing during Play"); return; }
        hands.SetHeldItem(true, true);
        var rig = hands.transform.Find("First person arms");
        foreach (Transform child in rig)
            if (child.name == "Held note" || child.name == "Human arms") {
                var renderers = child.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) continue;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Debug.Log("Preview " + child.name + " active=" + child.gameObject.activeInHierarchy + " bounds=" + bounds + " position=" + child.localPosition + " scale=" + child.localScale);
            }
    }

    [MenuItem("Tools/Replace starting room models")]
    public static void Replace()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var room = Object.FindFirstObjectByType<StartRoom>();
        if (!room) throw new System.Exception("StartRoom missing");
        Transform table = null;
        foreach (Transform child in room.transform)
            if (child.name.StartsWith("Bàn hướng dẫn")) table = child;
        if (!table) throw new System.Exception("Starting table missing");
        var note = table.GetComponentInChildren<StartRoomItem>(true);
        var all = table.GetComponentsInChildren<StartRoomItem>(true);
        StartRoomItem light = null;
        foreach (var item in all) { if (item.flashlight) light = item; else note = item; }
        if (!note || !light) throw new System.Exception("Starting items missing");
        InstallVisual(table, "caiban", new Vector3(1.35f,.85f,.72f), new Vector3(0,.43f,0), true);
        InstallVisual(note.transform, "togiay", new Vector3(.32f,.015f,.38f), Vector3.zero, false);
        InstallVisual(light.transform, "denpin", new Vector3(.075f,.075f,.30f), new Vector3(0,-.03f,0), false);
        if (room.hands) {
            room.hands.customFlashlightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "denpin.fbx");
            room.hands.customFlashlightMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "denpin_scene.mat");
            room.hands.customNotePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "togiay.fbx");
            room.hands.customNoteMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "togiay_scene.mat");
            room.hands.customHandsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Hands/AnatomicalHand.fbx");
            var handsMaterialPath = Root + "Hands/arms_scene.mat";
            var handsMaterial = AssetDatabase.LoadAssetAtPath<Material>(handsMaterialPath);
            if (!handsMaterial) {
                handsMaterial = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(handsMaterial, handsMaterialPath);
            }
            handsMaterial.mainTexture = null;
            handsMaterial.color = new Color(.55f,.34f,.27f);
            EditorUtility.SetDirty(handsMaterial);
            room.hands.customHandsMaterial = handsMaterial;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Replaced starting table, paper, and flashlight visuals. Existing pickup colliders and flashlight beam remain.");
    }

    static void InstallVisual(Transform holder, string asset, Vector3 size, Vector3 offset, bool table)
    {
        var old = holder.Find("New " + asset);
        if (old) Object.DestroyImmediate(old.gameObject);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + asset + ".fbx");
        if (!prefab) throw new System.Exception("Model missing: " + asset);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
        model.name = "New " + asset;
        foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) throw new System.Exception("No mesh in " + asset);
        var t = model.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = table ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        var importedScale = t.localScale;
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        float scale = Mathf.Min(size.x / Mathf.Max(b.size.x,.0001f), size.y / Mathf.Max(b.size.y,.0001f), size.z / Mathf.Max(b.size.z,.0001f));
        t.localScale = Vector3.Scale(importedScale, new Vector3(scale, scale * (table ? 1.5f : 1f), scale));
        b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        t.position += holder.TransformVector(offset - holder.InverseTransformPoint(b.center));
        foreach (var r in renderers) { r.enabled = true; r.forceRenderingOff = false; r.gameObject.SetActive(true); }
        model.SetActive(true);
        var materialPath = Root + asset + "_scene.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material) {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.color = Color.white;
        var colorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + asset + "/texture_pbr_20250901.png");
        if (!colorTexture) throw new System.Exception("Original color texture missing: " + asset);
        material.mainTexture = colorTexture;
        var normalPath = Root + asset + "/texture_pbr_20250901_normal.png";
        var normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter && normalImporter.textureType != TextureImporterType.NormalMap) {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var normalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normalTexture) {
            material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_BumpMap", normalTexture);
        }
        material.SetFloat("_Glossiness", .25f);
        EditorUtility.SetDirty(material);
        foreach (var r in renderers) r.sharedMaterial = material;
        Bounds finalBounds = renderers[0].bounds;
        foreach (var r in renderers) finalBounds.Encapsulate(r.bounds);
        var mesh = renderers[0].GetComponent<MeshFilter>();
        Debug.Log(asset + " visual: local=" + t.localPosition + " scale=" + t.localScale + " bounds=" + finalBounds + " renderers=" + renderers.Length + " type=" + renderers[0].GetType().Name + " active=" + renderers[0].gameObject.activeInHierarchy + " enabled=" + renderers[0].enabled + " mesh=" + (mesh ? mesh.sharedMesh.name : "none") + " meshBounds=" + (mesh ? mesh.sharedMesh.bounds.ToString() : "none") + " material=" + renderers[0].sharedMaterial);
        foreach (var r in holder.GetComponentsInChildren<Renderer>(true))
            if (!r.transform.IsChildOf(t)) r.enabled = false;
        if (table) foreach (Transform child in holder) {
            if (child == t || child.GetComponent<StartRoomItem>()) continue;
            if (child.GetComponentInChildren<StartRoomItem>(true)) continue;
            foreach (var r in child.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        }
    }
}
