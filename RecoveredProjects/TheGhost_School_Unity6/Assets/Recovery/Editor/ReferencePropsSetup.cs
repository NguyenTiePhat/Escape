using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Imports the eight photo-reference props and updates existing pickup visuals only.</summary>
public static class ReferencePropsSetup
{
    const string Root = "Assets/Recovery/ReferenceProps";
    const string Existing = "Assets/Recovery/M6";
    const string Reports = "RecoveryReports/ReferenceProps";
    static readonly string[] Models = { "MetalBlank", "GoldKey", "SilverKey", "SawBlade", "SawHandle", "Screws", "AssembledSaw", "KeyMold" };
    static readonly string[] Ids = { "metal_blank", "main_exit_key", "silver_key", "saw_blade", "saw_handle", "screws", "assembled_saw", "key_frame" };
    static readonly string[] Materials = { "RP_Steel", "RP_Silver", "RP_Gold", "RP_Wood", "RP_Dark", "RP_Brass" };
    static readonly float[] Metallic = { .60f, .68f, .64f, 0f, .32f, .55f };
    static readonly float[] Smoothness = { .31f, .40f, .43f, .12f, .17f, .34f };
    static readonly Dictionary<string, Material> Palette = new Dictionary<string, Material>();

    [MenuItem("Tools/The Ghost/Reference Props/Import and apply eight models")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before updating the asset library.");
        if (!Application.isBatchMode && string.IsNullOrEmpty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path))
            throw new InvalidOperationException("Save the current untitled scene before using this import command.");
        foreach (string folder in new[] { "Materials", "Prefabs", "Icons", "Scenes" }) Directory.CreateDirectory(Root + "/" + folder);
        Directory.CreateDirectory(Reports + "/Before");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Palette.Clear();
        var shader = Shader.Find("Standard");
        if (!shader) throw new Exception("This project requires the built-in Standard shader.");
        for (int i = 0; i < Materials.Length; i++)
        {
            string texPath = Root + "/Textures/" + Materials[i] + "_Albedo.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(texPath);
            importer.sRGBTexture = true; importer.mipmapEnabled = true;
            importer.maxTextureSize = 512; importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            string path = Root + "/Materials/" + Materials[i] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
            material.shader = shader; material.color = Color.white;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            material.SetFloat("_Metallic", Metallic[i]); material.SetFloat("_Glossiness", Smoothness[i]);
            EditorUtility.SetDirty(material); Palette[Materials[i]] = material;
        }

        var report = new StringBuilder("Reference props import and saved-asset validation\n");
        for (int i = 0; i < Models.Length; i++)
        {
            string modelPath = Root + "/Models/" + Models[i] + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.isReadable = false; importer.addCollider = false; importer.globalScale = 1;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            var item = AssetDatabase.LoadAssetAtPath<ItemData>(Existing + "/Items/" + Ids[i] + ".asset");
            if (!item || !item.pickupPrefab) throw new Exception("Missing existing item " + Ids[i]);
            string pickupPath = AssetDatabase.GetAssetPath(item.pickupPrefab);
            Backup(pickupPath); Backup(AssetDatabase.GetAssetPath(item));
            string oldIconPath = AssetDatabase.GetAssetPath(item.icon);
            if (!string.IsNullOrEmpty(oldIconPath)) Backup(oldIconPath);
            var root = PrefabUtility.LoadPrefabContents(pickupPath);
            try
            {
                // Keep the root, PickupItem, Rigidbody and their file IDs used by ItemData and scenes.
                foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), root.transform);
                visual.name = Models[i] + " visual";
                // A single-mesh FBX stores the metre and axis conversion on its root.
                // Preserve that imported rotation/scale (multi-mesh FBXs have an identity wrapper).
                visual.transform.localPosition = Vector3.zero;
                foreach (var renderer in visual.GetComponentsInChildren<MeshRenderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Palette[m.name.Split('.')[0]]).ToArray();
                var b = BoundsOf(root);
                visual.transform.localPosition -= Vector3.up * (b.min.y - root.transform.position.y);
                b = BoundsOf(root);
                var box = root.GetComponent<BoxCollider>();
                if (!box) box = root.AddComponent<BoxCollider>();
                box.center = root.transform.InverseTransformPoint(b.center);
                box.size = Vector3.Max(b.size, new Vector3(.008f, .008f, .008f));
                var pickup = root.GetComponent<PickupItem>();
                pickup.dropRadius = Mathf.Max(.06f, b.extents.magnitude); pickup.item = item;
                PrefabUtility.SaveAsPrefabAsset(root, pickupPath);
                PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + Models[i] + ".prefab");
                string iconPath = string.IsNullOrEmpty(oldIconPath) ? Root + "/Icons/" + Ids[i] + ".png" : oldIconPath;
                RenderIcon(root, iconPath, Models[i]);
                AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceSynchronousImport);
                var iconImporter = (TextureImporter)AssetImporter.GetAtPath(iconPath);
                iconImporter.textureType = TextureImporterType.Sprite;
                iconImporter.spriteImportMode = SpriteImportMode.Single;
                iconImporter.alphaIsTransparency = true; iconImporter.mipmapEnabled = false;
                iconImporter.textureCompression = TextureImporterCompression.Uncompressed;
                iconImporter.SaveAndReimport();
                item.icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                EditorUtility.SetDirty(item);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(pickupPath);
            if (item.pickupPrefab != saved.GetComponent<PickupItem>()) throw new Exception("Pickup file ID changed: " + Ids[i]);
            if (saved.GetComponent<PickupItem>().item != item) throw new Exception("Wrong ItemData binding.");
            if (!item.icon || !saved.GetComponent<Collider>() || !saved.GetComponent<Rigidbody>()) throw new Exception("Incomplete pickup.");
            int triangles = 0;
            foreach (var f in saved.GetComponentsInChildren<MeshFilter>())
            {
                if (!f.sharedMesh || f.sharedMesh.vertexCount < 3) throw new Exception("Missing mesh.");
                for (int sm = 0; sm < f.sharedMesh.subMeshCount; sm++) triangles += (int)f.sharedMesh.GetIndexCount(sm) / 3;
                if (f.GetComponent<Renderer>().sharedMaterials.Any(m => !m || !m.mainTexture || m.shader != shader)) throw new Exception("Invalid material.");
            }
            var bounds = BoundsOf(saved);
            if (bounds.size.magnitude > 1 || bounds.size.magnitude < .04f) throw new Exception("Unexpected metre scale: " + Models[i]);
            report.AppendLine("PASS " + Models[i] + ": " + triangles + " triangles; metres=" + bounds.size.ToString("F4") + "; item=" + item.id + "; saved mesh/material/texture/icon/collider/rigidbody and original pickup reference valid.");
        }
        CreateShowcase();
        AssetDatabase.SaveAssets();
        report.AppendLine("PASS: eight prefabs and showcase saved. School_Test scene and runtime scripts not edited.");
        File.WriteAllText(Reports + "/validation.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    static void Backup(string path)
    {
        string dest = Reports + "/Before/" + Path.GetFileName(path);
        if (!File.Exists(dest)) File.Copy(path, dest);
        if (File.Exists(path + ".meta") && !File.Exists(dest + ".meta")) File.Copy(path + ".meta", dest + ".meta");
    }

    static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>();
        if (renderers.Length == 0) throw new Exception("Empty model: " + root.name);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    static void RenderIcon(GameObject source, string path, string name)
    {
        // Prefab preview scenes are excluded from normal camera culling. Render a temporary
        // clone in the active scene, far away from existing content, then clean it up.
        var root = Object.Instantiate(source);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        root.transform.position = Vector3.one * 1000;
        root.transform.rotation = Quaternion.Euler(0, 180, 0) * source.transform.rotation;
        var oldAmbientMode = RenderSettings.ambientMode;
        var oldAmbient = RenderSettings.ambientLight;
        var oldReflection = RenderSettings.reflectionIntensity;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.22f, .23f, .22f);
        RenderSettings.reflectionIntensity = .15f;
        var b = BoundsOf(root);
        var cameraObject = new GameObject("Reference icon camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, root.scene);
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f, .095f, .087f, 0);
        camera.orthographic = true; camera.nearClipPlane = .01f; camera.farClipPlane = 10;
        camera.transform.position = b.center + new Vector3(.9f, 2.5f, -1.45f);
        camera.transform.LookAt(b.center);
        float extent = 0;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
        {
            var mb = f.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var p = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                p = camera.transform.InverseTransformPoint(f.transform.TransformPoint(p));
                extent = Mathf.Max(extent, Mathf.Abs(p.x), Mathf.Abs(p.y));
            }
        }
        camera.orthographicSize = extent * 1.22f;
        var lightObject = new GameObject("Reference soft light");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject, root.scene);
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
        light.intensity = .85f; light.transform.rotation = Quaternion.Euler(45, -35, 0);
        var fillObject = new GameObject("Reference fill");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject, root.scene);
        var fill = fillObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.intensity = .30f;
        fill.transform.rotation = Quaternion.Euler(65, 145, 0);
        var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
        var old = RenderTexture.active;
        var image = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            File.WriteAllBytes(Reports + "/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = old; rt.Release();
            Object.DestroyImmediate(rt); Object.DestroyImmediate(image);
            Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); Object.DestroyImmediate(fillObject);
            Object.DestroyImmediate(root);
            RenderSettings.ambientMode = oldAmbientMode;
            RenderSettings.ambientLight = oldAmbient;
            RenderSettings.reflectionIntensity = oldReflection;
        }
    }

    static void CreateShowcase()
    {
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        // The isolated batch process starts with an untitled scene; additive creation is
        // forbidden in that state. In the interactive editor, preserve the saved user scene.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,
            Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        for (int i = 0; i < Models.Length; i++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + Models[i] + ".prefab"), scene);
            go.name = (i + 1).ToString("00") + " " + Models[i];
            go.transform.position = new Vector3((i % 4 - 1.5f) * .75f, 0, -(i / 4) * .48f);
            go.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        var cam = new GameObject("Main Camera").AddComponent<Camera>();cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0, 3.2f, -2.2f); cam.transform.LookAt(new Vector3(0, 0, -.24f));
        cam.orthographic = true; cam.orthographicSize = 1.05f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.075f,.095f,.087f);
        var light = new GameObject("Soft key light").AddComponent<Light>();light.type = LightType.Directional;
        light.intensity = 1.2f;light.transform.rotation = Quaternion.Euler(45, -30, 0);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.38f,.40f,.41f);
        EditorSceneManager.SaveScene(scene, Root + "/Scenes/ReferenceProps_Showcase.unity");
        if (!Application.isBatchMode)
        {
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        }
    }
}
