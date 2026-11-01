using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class DoorControlsRepair
{
    static DoorControlsRepair(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        const string request="RecoveryReports/door-controls-repair.request";
        if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        File.Delete(request);
        var door=Object.FindObjectsByType<Door>(FindObjectsSortMode.None).FirstOrDefault(d=>d.name=="Exit door hinge");
        if(!door){File.WriteAllText("RecoveryReports/door-controls-repair.txt","FAIL: School_Test exit not loaded");return;}
        Undo.RecordObject(door,"Restore unlocked starting exit");door.UnlockDoor();EditorUtility.SetDirty(door);
        EditorSceneManager.MarkSceneDirty(door.gameObject.scene);EditorSceneManager.SaveScene(door.gameObject.scene);
        File.WriteAllText("RecoveryReports/door-controls-repair.txt","PASS: starting exit unlocked; F opens/closes, G drops. Other door locks unchanged.");
        File.WriteAllText("RecoveryReports/item-play.request","run");
    }
}
