using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class FinishOriginalWall {
 static FinishOriginalWall(){EditorApplication.update+=Poll;}
 static void Poll(){var p="RecoveryReports/finish-original-wall.request";if(!File.Exists(p)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying)return;File.Delete(p);Run();}
 static void Run(){var root=GameObject.Find("START ROOM — original room corrections");if(!root)return;
 foreach(string n in new[]{"West south corner joint","West north corner joint"}){var old=root.transform.Find(n);if(old)Undo.DestroyObjectImmediate(old.gameObject);}
 var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="West wall - continuous repaired face";g.transform.SetParent(root.transform);g.transform.position=new Vector3(17.02f,4.575f,59.7f);g.transform.localScale=new Vector3(.26f,5.14f,5.65f);g.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recovery/OriginalRoomWall.mat");
 EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
 var cam=Object.FindFirstObjectByType<SchoolExplorer>().view;var pos=cam.transform.position;var rot=cam.transform.rotation;var target=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(960,540,24);var tex=new Texture2D(960,540,TextureFormat.RGB24,false);try{cam.transform.position=new Vector3(21.6f,3.65f,59.6f);cam.transform.rotation=Quaternion.Euler(10,-90,0);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes("RecoveryReports/original-west-final.png",tex.EncodeToPNG());}finally{cam.transform.position=pos;cam.transform.rotation=rot;cam.targetTexture=target;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(tex);}
 File.WriteAllText("RecoveryReports/original-wall-final.txt","West wall face aligned to original boundary; protruding corner patches removed. Scene saved.");}
}
