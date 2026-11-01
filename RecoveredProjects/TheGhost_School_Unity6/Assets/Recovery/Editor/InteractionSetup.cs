using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class InteractionSetup
{
    static InteractionSetup(){EditorApplication.update+=ContinueSetup;}
    static void ContinueSetup(){if(!SessionState.GetBool("InteractionSetupPending",false)||EditorApplication.isCompiling||EditorApplication.isUpdating)return; if(!AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"))return; SessionState.SetBool("InteractionSetupPending",false);Run();}
    public static void RunBatch(){EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");Run();}
    [MenuItem("Tools/FPS/Set up interaction in current scene")]
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before setting up interaction.");
        // TMP ships inside uGUI. Import its shader/font resources once.
        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro/Resources"))
        {
            var package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            SessionState.SetBool("InteractionSetupPending",true);
            AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath,"Package Resources/TMP Essential Resources.unitypackage"),false);
            return;
        }
        var player=UnityEngine.Object.FindFirstObjectByType<SchoolExplorer>();
        if(!player || !player.view) throw new InvalidOperationException("Scene needs the existing SchoolExplorer and camera.");
        int layer=LayerMask.NameToLayer("Interactable");
        if(layer<0) throw new InvalidOperationException("Missing Interactable layer.");
        var interactor=player.GetComponent<PlayerInteractor>();
        if(!interactor) interactor=Undo.AddComponent<PlayerInteractor>(player.gameObject);
        interactor.view=player.view; interactor.range=2.5f; interactor.interactableMask=1<<layer;
        foreach(var item in UnityEngine.Object.FindObjectsByType<StartRoomItem>(FindObjectsSortMode.None))
            foreach(var collider in item.GetComponentsInChildren<Collider>(true)) { Undo.RecordObject(collider.gameObject,"Set interaction layer");collider.gameObject.layer=layer; }
        var canvasObject=GameObject.Find("Interaction UI");
        if(!canvasObject)
        {
            canvasObject=new GameObject("Interaction UI",typeof(Canvas),typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasObject,"Create interaction UI");
        }
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
        var text=canvasObject.GetComponentInChildren<TextMeshProUGUI>(true);
        if(!text)
        {
            var label=new GameObject("Interaction prompt",typeof(RectTransform));label.transform.SetParent(canvasObject.transform,false);
            text=label.AddComponent<TextMeshProUGUI>();
        }
        text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        // Dynamic population supports Vietnamese prompts using the supplied source font.
        if(text.font) {text.font.atlasPopulationMode=AtlasPopulationMode.Dynamic;EditorUtility.SetDirty(text.font);}
        text.fontSize=30;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;text.text="";text.enabled=false;
        text.rectTransform.anchorMin=text.rectTransform.anchorMax=text.rectTransform.pivot=new Vector2(.5f,.5f);
        text.rectTransform.sizeDelta=new Vector2(650,60);text.rectTransform.anchoredPosition=new Vector2(0,-45);
        var prompt=canvasObject.GetComponent<InteractionPromptUI>();if(!prompt)prompt=canvasObject.AddComponent<InteractionPromptUI>();
        prompt.interactor=interactor;prompt.label=text;
        if(!GameObject.Find("Interaction Test Cube"))
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Interaction Test Cube";Undo.RegisterCreatedObjectUndo(cube,"Create interaction test cube");
            cube.transform.position=player.view.transform.position+player.view.transform.forward*1.5f;cube.transform.localScale=Vector3.one*.25f;
            cube.layer=layer;cube.AddComponent<TestInteractable>();
        }
        var testCube = GameObject.Find("Interaction Test Cube").GetComponent<TestInteractable>();
        var cubeData = new SerializedObject(testCube);
        if (cubeData.FindProperty("prompt").stringValue == "Interact with test cube")
        { cubeData.FindProperty("prompt").stringValue = "Tương tác khối thử"; cubeData.ApplyModifiedProperties(); }
        EditorUtility.SetDirty(interactor);EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
        File.WriteAllText("RecoveryReports/interaction-setup.txt","PASS: current scene configured with PlayerInteractor (2.5m, Interactable), TMP centered prompt, existing pickups and test cube.");
        if(Application.isBatchMode)EditorApplication.Exit(0);
    }
}
