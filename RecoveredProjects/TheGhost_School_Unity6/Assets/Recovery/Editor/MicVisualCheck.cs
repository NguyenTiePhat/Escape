using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MicVisualCheck
{
    static int stage;static double started;static float captureAfter;static MicrophoneNoiseDetector detector;static MicSettingsPanel panel;
    const string Prefix="M3.VisualCheck.";
    static MicVisualCheck()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MicVisualCheck",false))
            {
                Application.runInBackground=true;Time.timeScale=1;
                detector=Object.FindAnyObjectByType<MicrophoneNoiseDetector>();detector.enabled=false;detector.SetInputBackend(new FakeMic());detector.detectionEnabled=true;detector.enabled=true;
                panel=detector.GetComponent<MicSettingsPanel>();panel.Open();stage=0;started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;
            }
        };
    }
    public static void Run()
    {
        ClearPrefs();EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        // Temporary in-memory overrides ensure no physical microphone is opened during the visual test.
        var mic=Object.FindAnyObjectByType<MicrophoneNoiseDetector>();mic.detectionEnabled=false;mic.prefsPrefix=Prefix;
        foreach(string file in new[]{"mic-settings-preview.png","noise-meter-preview.png"})if(File.Exists("RecoveryReports/"+file))File.Delete("RecoveryReports/"+file);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Show();SessionState.SetBool("MicVisualCheck",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.isPlaying){EditorApplication.isPaused=true;EditorApplication.Step();}
        try
        {
            if(EditorApplication.timeSinceStartup-started>25)throw new Exception("Visual test timeout at stage "+stage);
            if(stage==0)
            {
                if(!detector.IsCapturing||detector.Level<.4f)return;
                var explorer=detector.GetComponent<SchoolExplorer>();var look=detector.GetComponent<PlayerLook>();var motor=detector.GetComponent<PlayerMotor>();
                if(!look.InputBlocked||!motor.InputBlocked) return;
                if(!panel.IsOpen||Cursor.lockState!=CursorLockMode.None)throw new Exception("Microphone panel did not release/block player input");
                ScreenCapture.CaptureScreenshot("RecoveryReports/mic-settings-preview.png");stage=1;
            }
            else if(stage==1)
            {
                if(!File.Exists("RecoveryReports/mic-settings-preview.png"))return;
                panel.Close();Cursor.lockState=CursorLockMode.None;NoiseManager.EmitNoise(detector.transform.position,.8f,NoiseSource.Footstep,detector.gameObject);
                stage=2;captureAfter=Time.realtimeSinceStartup+.2f;
            }
            else if(stage==2)
            {
                NoiseManager.EmitNoise(detector.transform.position,.8f,NoiseSource.Footstep,detector.gameObject);
                if(Time.realtimeSinceStartup<captureAfter)return;
                var meter=Object.FindAnyObjectByType<NoiseMeterUI>();
                if(meter.DisplayedLevel<.79f)throw new Exception("HUD did not receive footstep event");
                ScreenCapture.CaptureScreenshot("RecoveryReports/noise-meter-preview.png");stage=3;
            }
            else
            {
                NoiseManager.EmitNoise(detector.transform.position,.8f,NoiseSource.Footstep,detector.gameObject);
                if(!File.Exists("RecoveryReports/noise-meter-preview.png"))return;
                File.WriteAllText("RecoveryReports/mic-visual-check.txt","PASS: main School_Test microphone panel opens; cursor released; look/movement blocked; fake live input meter; HUD screenshots captured. Real microphone was never opened.\n");Finish(0);
            }
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("RecoveryReports/mic-visual-check.txt","FAIL: "+e);Finish(1);}
    }
    static void Finish(int code)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("MicVisualCheck",false);
        if(panel)panel.enabled=false;if(detector){detector.enabled=false;Object.DestroyImmediate(detector.gameObject);}
        ClearPrefs();EditorApplication.Exit(code);
    }
    static void ClearPrefs(){foreach(string key in new[]{"enabled","device","floor","max","sensitivity"})PlayerPrefs.DeleteKey(Prefix+key);PlayerPrefs.Save();}
    sealed class FakeMic:IMicrophoneInput
    {
        public bool HasPermission=>true;public IEnumerator RequestPermission(){yield break;}
        public string[] Devices=>new[]{"Microphone thử nghiệm (giả lập)"};
        public AudioClip Start(string device,bool loop,int seconds,int rate)=>AudioClip.Create("Visual fake mic",16000,1,16000,false);
        public void End(string device){}public bool IsRecording(string device)=>true;
        public int GetPosition(string device)=>(int)(Time.realtimeSinceStartup*16000)%16000;
        public bool Read(AudioClip clip,float[] samples,int offset){for(int i=0;i<samples.Length;i++)samples[i]=.032f;return true;}
    }
}
