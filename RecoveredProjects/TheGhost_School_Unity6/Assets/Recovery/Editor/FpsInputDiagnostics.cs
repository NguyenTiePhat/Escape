using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
[InitializeOnLoad]
public static class FpsInputDiagnostics
{
    static FpsInputDiagnostics() { EditorApplication.delayCall += Capture; }
    [MenuItem("Tools/FPS/Restart input backend")]
    public static void RestartBackend()
    {
        if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; EditorApplication.delayCall += RestartBackend; return; }
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes()) return;
        EditorApplication.OpenProject(Path.GetFullPath("."), "-force-d3d11");
    }
    [MenuItem("Tools/FPS/Capture input diagnostics")]
    public static void Capture()
    {
        var s = new StringBuilder();
        s.AppendLine("Playing="+Application.isPlaying+" Focus="+Application.isFocused+" Cursor="+Cursor.lockState+" Update="+InputSystem.settings.updateMode);
        foreach(var d in InputSystem.devices) s.AppendLine("Device="+d.name+" Enabled="+d.enabled+" Native="+d.native);
        var p=Object.FindFirstObjectByType<SchoolExplorer>();
        if(p) s.AppendLine("Player="+p.name+" enabled="+p.enabled+" look="+p.GetComponent<PlayerLook>()+" motor="+p.GetComponent<PlayerMotor>());
        File.WriteAllText("RecoveryReports/fps-input-diagnostics.txt",s.ToString());
    }
}

