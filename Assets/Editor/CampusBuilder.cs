using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CampusBuilder {
    [MenuItem("The Ghost/Build IUH Preview")]
    public static void Build() {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var data = JsonUtility.FromJson<CampusData>(Resources.Load<TextAsset>("campus").text);
        Directory.CreateDirectory("Assets/Generated/Materials");
        Directory.CreateDirectory("Assets/Scenes");
        var root = new GameObject("IUH — Fictional abandoned teaching block");
        var mats = new Dictionary<string, Material>();
        foreach (var part in data.parts) {
            var kind = part.shape == "sphere" ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(kind);
            go.name = part.name;
            go.transform.SetParent(root.transform);
            go.transform.position = V(part.p);
            go.transform.localScale = V(part.s);
            string key = string.Join("_", System.Array.ConvertAll(part.color, c => Mathf.RoundToInt(c * 255).ToString()));
            if (!mats.TryGetValue(key, out var mat)) {
                string path = "Assets/Generated/Materials/" + key + ".mat";
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!mat) { mat = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(mat, path); }
                mat.color = new Color(part.color[0],part.color[1],part.color[2]);
                mat.SetFloat("_Glossiness", .12f);
                mats.Add(key,mat);
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!part.solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = true;
        }
        foreach(var label in data.labels) {
            var go = new GameObject("Sign " + label.text);
            go.transform.SetParent(root.transform); go.transform.position = V(label.p);
            go.transform.rotation = Quaternion.Euler(0,180,0);
            var txt = go.AddComponent<TextMesh>(); txt.text = label.text; txt.anchor = TextAnchor.MiddleCenter;
            txt.characterSize = .075f; txt.fontSize = 64; txt.color = new Color(.76f,.85f,.83f);
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.48f,.56f,.62f);
        RenderSettings.ambientEquatorColor = new Color(.34f,.39f,.40f);
        RenderSettings.ambientGroundColor = new Color(.20f,.23f,.22f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.31f,.39f,.42f); RenderSettings.fogDensity = .006f;
        var sun = new GameObject("Overcast evening").AddComponent<Light>(); sun.type = LightType.Directional;
        sun.intensity = .85f; sun.color = new Color(.72f,.81f,1); sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(48,-32,0);
        for(int floor=0;floor<2;floor++) for(int i=0;i<6;i++) {
            var light = new GameObject("Gallery fluorescent").AddComponent<Light>();
            light.type = LightType.Point; light.range = 7; light.intensity = .7f;
            light.color = new Color(.69f,.86f,.78f); light.transform.position = new Vector3(-20+i*8,3.3f+floor*4.2f,0);
            var roomLight = new GameObject("Room inspection lighting").AddComponent<Light>();
            roomLight.type = LightType.Point; roomLight.range = 10; roomLight.intensity = 1;
            roomLight.color = new Color(.72f,.80f,.76f); roomLight.transform.position = new Vector3(-20+i*8,3.3f+floor*4.2f,7);
        }
        var player = new GameObject("Player"); player.transform.position = new Vector3(0,.15f,-25);
        var controller = player.AddComponent<CharacterController>(); controller.height = 1.8f;
        controller.center = new Vector3(0,.9f,0); controller.radius = .28f; controller.stepOffset = .32f;
        var eyes = new GameObject("Eyes").AddComponent<Camera>(); eyes.tag = "MainCamera";
        eyes.transform.SetParent(player.transform,false); eyes.transform.localPosition = new Vector3(0,1.65f,0);
        eyes.fieldOfView = 75; eyes.nearClipPlane = .05f; eyes.farClipPlane = 220;
        eyes.clearFlags = CameraClearFlags.SolidColor; eyes.backgroundColor = RenderSettings.fogColor;
        var torch = eyes.gameObject.AddComponent<Light>(); torch.type = LightType.Spot;
        torch.range = 24; torch.spotAngle = 65; torch.intensity = 2; torch.enabled = false;
        player.AddComponent<CampusWalk>().eyes = eyes;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/IUH_Preview.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/IUH_Preview.unity",true) };
        AssetDatabase.SaveAssets(); Selection.activeGameObject = root;
    }
    static Vector3 V(float[] v) => new Vector3(v[0],v[1],v[2]);
}
