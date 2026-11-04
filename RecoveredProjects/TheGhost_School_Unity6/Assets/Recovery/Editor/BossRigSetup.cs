using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BossRigSetup
{
    const string Folder = "Assets/Recovery/Boss/Rigged/";
    static readonly string[] Actions = { "Idle", "Walk", "Alert", "Investigate", "Run", "Lunge", "Search", "Roar" };
    static BossRigSetup() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if (!File.Exists("boss-rig.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying = false; return; }
        File.Delete("boss-rig.request"); Run();
    }
    [MenuItem("Recovery/Apply skeletal boss animations")]
    public static void Run()
    {
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Recovery/Scenes/School_Test.unity") throw new Exception("School_Test must be open");
            if (!File.Exists("RecoveryReports/School_Test-before-boss-rig.unity")) EditorSceneManager.SaveScene(scene, "RecoveryReports/School_Test-before-boss-rig.unity", true);
            string path = Folder + "M4BossRigged.fbx"; AssetDatabase.ImportAsset(path);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic; importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true; importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.generateMeshLods = false; importer.isReadable = true; importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var takes = importer.defaultClipAnimations;
            foreach (var take in takes)
            {
                string name = Actions.FirstOrDefault(a => take.name == a || take.name.EndsWith("|" + a) || take.name.EndsWith("_" + a));
                if (name == null) throw new Exception("Unknown take: " + take.name);
                take.name = name; take.loopTime = name != "Alert" && name != "Lunge" && name != "Roar";
                take.loopPose = false; take.lockRootPositionXZ = true; take.lockRootHeightY = true; take.lockRootRotation = true;
                take.keepOriginalPositionXZ = true; take.keepOriginalPositionY = true; take.keepOriginalOrientation = true;
            }
            importer.clipAnimations = takes; importer.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            foreach (string name in Actions) if (!clips.Any(c => c.name == name)) throw new Exception("Missing clip " + name + ": " + string.Join(",", clips.Select(c => c.name)));
            string controllerPath = Folder + "BossActions.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            foreach (string name in Actions)
            {
                var state = machine.AddState(name); state.motion = clips.First(c => c.name == name); state.writeDefaultValues = true;
                if (name == "Idle") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            var root = GameObject.Find("M4 Boss"); if (!root) throw new Exception("M4 Boss missing");
            foreach (var child in root.GetComponentsInChildren<Transform>(true).Where(t => t.parent == root.transform).ToArray()) Object.DestroyImmediate(child.gameObject);
            var pivot = new GameObject("VisualPivot").transform; pivot.SetParent(root.transform, false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)); model.name = "RiggedVisual"; model.transform.SetParent(pivot, false);
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderBy(r => r.name).ToArray();
            if (skins.Length != 3) throw new Exception("Expected 3 skinned LODs, got " + skins.Length);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovery/Boss/BossPBR.mat");
            foreach (var skin in skins) { skin.gameObject.SetActive(true); skin.enabled = true; skin.sharedMaterial = material; skin.quality = SkinQuality.Bone4; skin.localBounds = new Bounds(new Vector3(0, 1.1f, 0), new Vector3(3.8f, 3.5f, 3.8f)); }
            var lod = model.GetComponent<LODGroup>(); if (!lod) lod = model.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.3f, new Renderer[] { skins[0] }), new LOD(.12f, new Renderer[] { skins[1] }), new LOD(0, new Renderer[] { skins[2] }) });
            lod.fadeMode = LODFadeMode.None; lod.localReferencePoint = Vector3.up * 1.1f; lod.size = 2.2f;
            var animator = model.GetComponent<Animator>(); if (!animator) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var driver = root.GetComponent<BossAnimationDriver>(); if (!driver) driver = root.AddComponent<BossAnimationDriver>();
            driver.animator = animator; driver.head = model.GetComponentsInChildren<Transform>().First(t => t.name == "Head");
            var motion = root.GetComponent<BossPrototypeMotion>(); motion.animationDriver = driver; motion.visualPivot = pivot; motion.walkSpeed = .3f; motion.pacingDistance = 1.5f; motion.previewLoop = true;
            var report = new StringBuilder("PASS: skeletal boss with 7 requested clips + retained Roar.\n");
            Validate(model, skins, clips, report);
            // Persist nested FBX instance overrides before saving the scene/prefab.
            // Reimporting here would reset the freshly assigned controller on the instance.
            foreach (var component in model.GetComponentsInChildren<Component>(true))
                if (component && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Recovery/Boss/M4Boss.prefab");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("RecoveryReports/boss-rig-check.txt", report.ToString()); Debug.Log(report);
        }
        catch (Exception e) { File.WriteAllText("RecoveryReports/boss-rig-check.txt", "FAIL: " + e); Debug.LogException(e); }
    }
    static void Validate(GameObject source, SkinnedMeshRenderer[] sourceSkins, AnimationClip[] clips, StringBuilder report)
    {
        var model = Object.Instantiate(source); model.name = "Rig validation"; model.transform.SetPositionAndRotation(new Vector3(1000, 0, 1000), Quaternion.identity);
        try
        {
            var animator = model.GetComponent<Animator>(); animator.enabled = false;
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderBy(r => r.name).ToArray();
            foreach (var skin in skins)
            {
                if (skin.bones.Length < 15 || skin.sharedMesh.bindposes.Length < 15) throw new Exception("Missing bone binding " + skin.name);
                foreach (var weight in skin.sharedMesh.boneWeights)
                    if (Mathf.Abs(weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3 - 1) > .01f) throw new Exception("Unweighted vertex");
                report.AppendLine(skin.name + ": " + skin.sharedMesh.triangles.Length / 3 + " triangles, " + skin.bones.Length + " bound bones");
            }
            var baked = new Mesh(); var skin0 = skins[0];
            try
            {
                foreach (var clip in clips)
                {
                    clip.SampleAnimation(model, 0); skin0.BakeMesh(baked); var first = baked.vertices;
                    clip.SampleAnimation(model, clip.length * .27f); skin0.BakeMesh(baked); var next = baked.vertices;
                    float max = 0; for (int i = 0; i < first.Length; i++) { if (!float.IsFinite(next[i].x + next[i].y + next[i].z)) throw new Exception("Nonfinite skin"); max = Mathf.Max(max, Vector3.Distance(first[i], next[i])); }
                    if (max < .002f || max > 2) throw new Exception("Invalid deformation " + clip.name + " " + max);
                    float seam = 0;
                    if (clip.isLooping)
                    {
                        clip.SampleAnimation(model, clip.length); skin0.BakeMesh(baked); var end = baked.vertices;
                        for (int i = 0; i < first.Length; i++) seam = Mathf.Max(seam, Vector3.Distance(first[i], end[i]));
                        if (seam > .015f) throw new Exception("Loop seam " + clip.name + " " + seam);
                    }
                    report.AppendLine(clip.name + ": duration=" + clip.length.ToString("F3") + "s loop=" + clip.isLooping + " mesh movement=" + max.ToString("F3") + "m seam=" + seam.ToString("F4") + "m");
                }
            }
            finally { Object.DestroyImmediate(baked); }
        }
        finally { Object.DestroyImmediate(model); }
    }
}
