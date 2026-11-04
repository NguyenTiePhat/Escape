using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BossPrototypeSetup
{
    const string Folder = "Assets/Recovery/Boss/";
    static BossPrototypeSetup() { EditorApplication.update += CheckRequest; }
    static void CheckRequest()
    {
        if (!File.Exists("boss-prototype.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
        File.Delete("boss-prototype.request"); Run();
    }
    public static void RunBatch() { Run(); EditorApplication.Exit(File.ReadAllText("RecoveryReports/boss-prototype-check.txt").StartsWith("PASS") ? 0 : 1); }
    [MenuItem("Recovery/Set up M4 boss prototype")]
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Recovery/Scenes/School_Test.unity") throw new Exception("Open School_Test before setup; no other scene will be overwritten.");
            if (!File.Exists("RecoveryReports/School_Test-before-boss-prototype.unity"))
                EditorSceneManager.SaveScene(scene, "RecoveryReports/School_Test-before-boss-prototype.unity", true);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "M4Boss.fbx");
            importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None;
            importer.generateMeshLods = true; importer.maximumMeshLod = 3; importer.SaveAndReimport();
            ConfigureTexture("BaseColor", true, false); ConfigureTexture("Normal", false, true);
            ConfigureTexture("Roughness", false, false); ConfigureTexture("Metallic", false, false);
            // Pack from the original linear images, not from color-managed GPU samples.
            var rough = LoadRaw("Roughness"); var metal = LoadRaw("Metallic");
            if (rough.width != metal.width || rough.height != metal.height) throw new Exception("PBR map sizes differ");
            var pixels = metal.GetPixels32(); var roughPixels = rough.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(pixels[i].r, 255, 0, (byte)(255 - roughPixels[i].r));
            var packed = new Texture2D(metal.width, metal.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(pixels); packed.Apply(); File.WriteAllBytes(Folder + "Textures/MetallicSmoothness.png", packed.EncodeToPNG());
            Object.DestroyImmediate(rough); Object.DestroyImmediate(metal); Object.DestroyImmediate(packed);
            AssetDatabase.ImportAsset(Folder + "Textures/MetallicSmoothness.png"); ConfigureTexture("MetallicSmoothness", false, false);
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            var shader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
            if (!shader) throw new Exception("Active pipeline shader unavailable");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "BossPBR.mat");
            if (!material) { material = new Material(shader); AssetDatabase.CreateAsset(material, Folder + "BossPBR.mat"); }
            material.shader = shader; material.color = Color.white;
            material.SetTexture(urp ? "_BaseMap" : "_MainTex", Texture("BaseColor"));
            material.SetTexture("_BumpMap", Texture("Normal")); material.SetFloat("_BumpScale", .8f);
            material.SetTexture("_MetallicGlossMap", Texture("MetallicSmoothness"));
            material.SetFloat("_Metallic", 1); material.SetFloat(urp ? "_Smoothness" : "_GlossMapScale", 1);
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICGLOSSMAP");
            if (urp) material.EnableKeyword("_METALLICSPECGLOSSMAP"); EditorUtility.SetDirty(material);
            var root = GameObject.Find("M4 Boss"); if (!root) throw new Exception("M4 Boss missing");
            foreach (var child in root.GetComponentsInChildren<Transform>(true).Where(t => t.parent == root.transform).ToArray()) Object.DestroyImmediate(child.gameObject);
            root.transform.localScale = Vector3.one;
            var pivot = new GameObject("VisualPivot").transform; pivot.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "M4Boss.fbx"));
            model.transform.SetParent(pivot, false);
            // Normalize using bounds while root is temporarily at the origin.
            Vector3 position = root.transform.position; Quaternion rotation = root.transform.rotation;
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var renderers = model.GetComponentsInChildren<Renderer>(); Bounds bounds = BoundsOf(renderers);
            model.transform.localScale *= 2.2f / bounds.size.y; bounds = BoundsOf(renderers);
            model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in renderers) renderer.sharedMaterial = material;
            root.transform.SetPositionAndRotation(position, rotation);
            var cc = root.GetComponent<CharacterController>(); if (!cc) cc = root.AddComponent<CharacterController>();
            cc.height = 2.2f; cc.radius = .42f; cc.center = Vector3.up * 1.1f; cc.stepOffset = .22f; cc.skinWidth = .035f; cc.minMoveDistance = 0;
            var motion = root.GetComponent<BossPrototypeMotion>(); if (!motion) motion = root.AddComponent<BossPrototypeMotion>();
            motion.visualPivot = pivot; motion.previewLoop = true;
            var audio = root.GetComponent<AudioSource>(); if (!audio) audio = root.AddComponent<AudioSource>();
            audio.playOnAwake = false; audio.spatialBlend = 1; audio.minDistance = 2; audio.maxDistance = 18; audio.volume = .55f;
            motion.voice = audio; motion.roarClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/AudioClip/PumpkinGrowl.ogg");
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "M4Boss.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            var report = new StringBuilder("PASS: four embedded PBR images extracted, linear R metallic/A inverse roughness, normal map, 2K mipmaps.\n");
            report.AppendLine("Pipeline=" + (urp ? "URP" : "Built-in (existing map); packed textures also match URP Lit"));
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh; report.Append(mesh.name + " LOD triangles:");
                for (int lod = 0; lod < mesh.lodCount; lod++) { uint indices = 0; for (int sub = 0; sub < mesh.subMeshCount; sub++) indices += mesh.GetIndexCount(sub, lod); report.Append(" " + indices / 3); }
                report.AppendLine(); if (mesh.lodCount < 2) throw new Exception("No reduced LOD generated");
            }
            report.AppendLine("Height=" + BoundsOf(renderers).size.y + "; states Idle/Walk/Roar/Attack; CharacterController; F6 preview, F7 roar, F8 attack. No rig, chase AI or health damage yet.");
            CheckMotion(report);
            RenderPreview(root, "RecoveryReports/boss-prototype-preview.png");
            File.WriteAllText("RecoveryReports/boss-prototype-check.txt", report.ToString());
            Debug.Log(report);
        }
        catch (Exception e) { File.WriteAllText("RecoveryReports/boss-prototype-check.txt", "FAIL: " + e); Debug.LogException(e); }
    }
    static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Textures/" + name + ".png");
    static Texture2D LoadRaw(string name) { var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true); t.LoadImage(File.ReadAllBytes(Folder + "Textures/" + name + ".png")); return t; }
    static void ConfigureTexture(string name, bool srgb, bool normal)
    {
        string path = Folder + "Textures/" + name + ".png"; AssetDatabase.ImportAsset(path);
        var i = (TextureImporter)AssetImporter.GetAtPath(path); i.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        i.sRGBTexture = srgb; i.mipmapEnabled = true; i.maxTextureSize = 2048; i.isReadable = false;
        i.alphaSource = TextureImporterAlphaSource.FromInput; i.SaveAndReimport();
    }
    static Bounds BoundsOf(Renderer[] rs) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
    static void CheckMotion(StringBuilder report)
    {
        var testRoot = new GameObject("Boss motion validation"); testRoot.transform.position = new Vector3(1000, 0, 1000);
        try
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.transform.SetParent(testRoot.transform, false); ground.transform.localPosition = new Vector3(0, -.5f, 0); ground.transform.localScale = new Vector3(20, 1, 20);
            var actor = new GameObject("Actor"); actor.transform.SetParent(testRoot.transform, false); actor.transform.localPosition = Vector3.up * .03f;
            var cc = actor.AddComponent<CharacterController>(); cc.height = 2.2f; cc.center = Vector3.up * 1.1f; cc.radius = .42f;
            var pivot = new GameObject("Visual"); pivot.transform.SetParent(actor.transform, false);
            var motion = actor.AddComponent<BossPrototypeMotion>(); motion.visualPivot = pivot.transform; motion.previewLoop = false; motion.enableTestKeys = false;
            Physics.SyncTransforms(); motion.SetState(BossPrototypeMotion.MotionState.Walk);
            for (int i = 0; i < 40; i++) motion.Tick(.02f);
            if (actor.transform.localPosition.z < .3f) throw new Exception("Walk failed");
            motion.PlayRoar(); for (int i = 0; i < 40; i++) motion.Tick(.02f);
            if (Quaternion.Angle(pivot.transform.localRotation, Quaternion.identity) < 3) throw new Exception("Roar visual failed");
            for (int i = 0; i < 60; i++) motion.Tick(.02f);
            if (motion.State != BossPrototypeMotion.MotionState.Idle) throw new Exception("Roar did not end");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(testRoot.transform, false); wall.transform.localPosition = new Vector3(0, 1.5f, 1.15f); wall.transform.localScale = new Vector3(4, 3, .2f);
            Physics.SyncTransforms(); motion.PlayAttack(); for (int i = 0; i < 60; i++) motion.Tick(.02f);
            if (actor.transform.localPosition.z > .72f || motion.State != BossPrototypeMotion.MotionState.Idle) throw new Exception("Attack wall blocking/recovery failed");
            report.AppendLine("Motion checks PASS: actual CC displacement, roar pose/end, lunge blocked by wall, attack recovery.");
        }
        finally { Object.DestroyImmediate(testRoot); }
    }
    static void RenderPreview(GameObject root, string path)
    {
        var cameraObject = new GameObject("Boss preview"); var lightObject = new GameObject("Boss preview light");
        RenderTexture rt = null; Texture2D image = null; var previous = RenderTexture.active;
        var children = root.GetComponentsInChildren<Transform>(); var layers = children.Select(t => t.gameObject.layer).ToArray();
        try
        {
            foreach (var child in children) child.gameObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>(); camera.cullingMask = 1 << 31; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .14f, .16f);
            camera.transform.position = root.transform.position + root.transform.forward * 3.2f + root.transform.right * .65f + Vector3.up * 1.35f; camera.transform.LookAt(root.transform.position + Vector3.up * 1.1f); camera.nearClipPlane = .05f;
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.cullingMask = 1 << 31; light.intensity = 1.3f; light.transform.rotation = Quaternion.LookRotation(root.transform.position + Vector3.up - (camera.transform.position + Vector3.up));
            rt = new RenderTexture(800, 800, 24); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image = new Texture2D(800, 800, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 800, 800), 0, 0); image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally { for (int i = 0; i < children.Length; i++) children[i].gameObject.layer = layers[i]; RenderTexture.active = previous; Object.DestroyImmediate(cameraObject); Object.DestroyImmediate(lightObject); if (rt) Object.DestroyImmediate(rt); if (image) Object.DestroyImmediate(image); }
    }
}
