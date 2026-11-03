using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

public static class MicNoiseSetup
{
    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
            if(!File.Exists("RecoveryReports/School_Test-before-microphone.unity"))File.Copy("Assets/Recovery/Scenes/School_Test.unity","RecoveryReports/School_Test-before-microphone.unity");
            var player=Object.FindAnyObjectByType<SchoolExplorer>();
            var detector=player.GetComponent<MicrophoneNoiseDetector>();if(!detector)detector=player.gameObject.AddComponent<MicrophoneNoiseDetector>();detector.player=player.transform;
            var settings=player.GetComponent<MicSettingsPanel>();if(!settings)settings=player.gameObject.AddComponent<MicSettingsPanel>();settings.detector=detector;
            var root=GameObject.Find("Noise HUD");if(!root)root=new GameObject("Noise HUD",typeof(Canvas),typeof(CanvasScaler));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=21;settings.noiseHud=canvas;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            var background=root.transform.Find("Meter background");if(!background){background=new GameObject("Meter background",typeof(RectTransform),typeof(Image)).transform;background.SetParent(root.transform,false);}
            var rect=(RectTransform)background;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-48);rect.sizeDelta=new Vector2(250,14);
            background.GetComponent<Image>().color=new Color(.08f,.10f,.12f,.9f);background.GetComponent<Image>().raycastTarget=false;
            var fillObject=background.Find("Fill");if(!fillObject){fillObject=new GameObject("Fill",typeof(RectTransform),typeof(Image)).transform;fillObject.SetParent(background,false);}
            var fill=fillObject.GetComponent<Image>();var fillRect=fill.rectTransform;fillRect.anchorMin=Vector2.zero;fillRect.anchorMax=Vector2.one;fillRect.offsetMin=new Vector2(2,2);fillRect.offsetMax=new Vector2(-2,-2);
            // Image.Type.Filled requires a sprite; persist a simple white UI sprite.
            const string path="Assets/Recovery/Noise/MeterWhite.png";
            if(!File.Exists(path)){var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();}
            fill.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.fillAmount=0;fill.raycastTarget=false;
            var labelObject=root.transform.Find("Noise label");if(!labelObject){labelObject=new GameObject("Noise label",typeof(RectTransform),typeof(TextMeshProUGUI)).transform;labelObject.SetParent(root.transform,false);}
            var label=labelObject.GetComponent<TextMeshProUGUI>();label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Recovery/Items/InteractionFont.asset");label.fontSize=22;label.alignment=TextAlignmentOptions.Center;label.text="Tiếng ồn: Yên tĩnh";label.raycastTarget=false;
            var labelRect=label.rectTransform;labelRect.anchorMin=labelRect.anchorMax=new Vector2(.5f,1);labelRect.pivot=new Vector2(.5f,1);labelRect.anchoredPosition=new Vector2(0,-19);labelRect.sizeDelta=new Vector2(330,26);
            if(label.font){label.font.material.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader");EditorUtility.SetDirty(label.font.material);label.fontSharedMaterial=label.font.material;}
            if(label.font)label.font.HasCharacters("Tiếng ồn: Yên tĩnh Cảnh báo Nguy hiểm",out uint[] missing,false,true);
            var meter=root.GetComponent<NoiseMeterUI>();if(!meter)meter=root.AddComponent<NoiseMeterUI>();meter.player=player.gameObject;meter.fill=fill;meter.label=label;
            if(string.IsNullOrWhiteSpace(PlayerSettings.iOS.microphoneUsageDescription))PlayerSettings.iOS.microphoneUsageDescription="Micro được dùng để chuyển giọng nói thành tiếng ồn trong trò chơi.";
            EditorUtility.SetDirty(detector);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(meter);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);EditorSceneManager.SaveScene(player.gameObject.scene);AssetDatabase.SaveAssets();
            File.WriteAllText("RecoveryReports/mic-noise-setup.txt","PASS: Player microphone detector and F2 settings panel; top-center event-driven NoiseMeterUI with sprite-backed fill and Vietnamese label; iOS microphone usage description. No real microphone recording was started by setup.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
