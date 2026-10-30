using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class HandsPreview {
 public static void Run(){
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity");
 var player=Object.FindFirstObjectByType<SchoolExplorer>();
 if(!player.view.GetComponent<FirstPersonHands>())player.view.gameObject.AddComponent<FirstPersonHands>();
 EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
 player.view.GetComponent<FirstPersonHands>().Build();
 var cam=player.view;var rt=new RenderTexture(1280,720,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();System.IO.File.WriteAllBytes("RecoveryReports/first-person-hands.png",tex.EncodeToPNG());cam.targetTexture=null;RenderTexture.active=null;
 EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Explore.unity");
 if(!Application.isBatchMode)EditorApplication.isPlaying=true;
 }
}
