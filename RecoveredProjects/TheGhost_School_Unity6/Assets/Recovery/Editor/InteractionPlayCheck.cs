using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class InteractionPlayCheck
{
    static int stage;
    static double next;
    static int initialCount;
    static InteractionPlayCheck(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/FPS/Check interaction in Play Mode")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)return;
        EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        SessionState.SetBool("InteractionPlayCheck",true);EditorApplication.isPlaying=true;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool("InteractionPlayCheck",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(EditorApplication.timeSinceStartup<next)return;
        try
        {
            var player=Object.FindFirstObjectByType<SchoolExplorer>();var cube=Object.FindFirstObjectByType<TestInteractable>();var ui=Object.FindFirstObjectByType<InteractionPromptUI>();
            if(!player||!cube||!ui)return;
            var interactor=player.GetComponent<PlayerInteractor>();
            if(stage==0)
            {
                EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                player.GetComponent<PlayerMotor>().enabled=false;player.GetComponent<PlayerLook>().enabled=false;
                player.view.transform.LookAt(cube.transform.position);Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
                if(Keyboard.current!=null)InputSystem.ResetDevice(Keyboard.current);
                initialCount=cube.InteractionCount;stage=1;next=EditorApplication.timeSinceStartup+1;return;
            }
            if(stage==1)
            {
                interactor.RefreshTarget();
                Require(ReferenceEquals(interactor.Target,cube),"Camera did not acquire cube");
                Require(ui.label.enabled && ui.label.text.Contains(cube.Prompt),"TMP prompt missing");
                ui.label.ForceMeshUpdate();Require(ui.label.textInfo.characterCount>0,"TMP generated no glyphs");
                ScreenCapture.CaptureScreenshot(Path.GetFullPath("RecoveryReports/interaction-prompt.png"));
                interactor.InteractWithTarget();
                stage=2;next=EditorApplication.timeSinceStartup+.3;return;
            }
            if(stage==2)
            {
                Require(cube.InteractionCount==initialCount+1,"Interaction handler failed or called repeatedly: count="+cube.InteractionCount+", initial="+initialCount+", focus="+Application.isFocused);
                InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());
                player.view.transform.Rotate(0,90,0,Space.World);stage=3;next=EditorApplication.timeSinceStartup+.3;return;
            }
            var room=Object.FindFirstObjectByType<StartRoom>();
            if(stage==3)
            {
                interactor.RefreshTarget();
                Require(interactor.Target==null&&!ui.label.enabled&&ui.label.text=="","Looking away did not clear prompt");
                StartRoomItem note=null;
                foreach(var item in Object.FindObjectsByType<StartRoomItem>(FindObjectsSortMode.None))
                {
                    foreach(var collider in item.GetComponentsInChildren<Collider>())
                        Require(collider.gameObject.layer==LayerMask.NameToLayer("Interactable"),"Pickup collider has wrong layer");
                    if(!item.flashlight)note=item;
                }
                Require(note!=null,"Missing note");
                note.Interact();Require(room.inventory.Count==1&&!note.gameObject.activeSelf,"Interface pickup failed");
                interactor.InteractWithTarget();Require(room.Reading,"Empty-target E fallback no longer uses equipped note");
                stage=4;next=EditorApplication.timeSinceStartup+.3;return;
            }
            Require(room.Reading&&!ui.label.enabled,"Note closed immediately or prompt remained during modal");
            room.CloseNote();
            File.WriteAllText("RecoveryReports/interaction-play-check.txt","PASS: School_Test component pipeline acquires cube; centered TMP prompt has glyphs; InteractWithTarget invokes exactly one Interact; looking away clears target/text; pickup layers, interface pickup, equipped-note fallback and modal blocking pass. Screenshot: interaction-prompt.png.");Finish();
        }
        catch(Exception e){File.WriteAllText("RecoveryReports/interaction-play-check.txt","FAIL: "+e);Debug.LogException(e);Finish();}
    }
    static void Finish(){if(Keyboard.current!=null)InputSystem.ResetDevice(Keyboard.current);SessionState.SetBool("InteractionPlayCheck",false);stage=0;EditorApplication.isPlaying=false;}
}
