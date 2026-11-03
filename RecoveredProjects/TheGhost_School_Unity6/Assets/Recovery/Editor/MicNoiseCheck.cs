using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class MicNoiseCheck
{
    const string Prefix="M3.AutomatedTest.";
    static IEnumerator routine; static double started; static MicrophoneNoiseDetector waiting;
    static readonly List<GameObject> objects=new List<GameObject>();
    static readonly List<NoiseEvent> events=new List<NoiseEvent>();
    static MicNoiseCheck(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("MicNoiseCheck",false)){Application.runInBackground=true;Time.timeScale=1;EditorApplication.isPaused=false;routine=Tests();started=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}};}
    public static void Run(){Application.runInBackground=true;EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Show();EditorApplication.isPaused=false;ClearPrefs();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);SessionState.SetBool("MicNoiseCheck",true);EditorApplication.isPlaying=true;}
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Tick()
    {
        try{EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.isPlaying){EditorApplication.isPaused=true;EditorApplication.Step();}if(EditorApplication.timeSinceStartup-started>45)throw new Exception("Microphone test timeout: "+(waiting?waiting.Status+" active="+waiting.isActiveAndEnabled+" time="+Time.time:"none"));if(!routine.MoveNext())Finish(null);}
        catch(Exception e){Finish(e);}
    }
    static GameObject Create(string name){var go=new GameObject(name);objects.Add(go);return go;}
    static MicrophoneNoiseDetector Detector(FakeInput fake)
    {
        var go=Create("Fake mic player");go.SetActive(false);var detector=go.AddComponent<MicrophoneNoiseDetector>();detector.prefsPrefix=Prefix;detector.SetInputBackend(fake);go.SetActive(true);return detector;
    }
    static float[] Samples(float db){var data=new float[800];float value=Mathf.Pow(10,db/20);for(int i=0;i<data.Length;i++)data[i]=i%2==0?value:-value;return data;}
    static void Heard(NoiseEvent e){events.Add(e);}
    static IEnumerator Tests()
    {
        NoiseManager.Instance.debugLogging=false;NoiseManager.OnNoiseEmitted+=Heard;
        var fake=new FakeInput();var detector=Detector(fake);
        waiting=detector;Debug.Log("Mic test state: application="+Application.isPlaying+" editor="+EditorApplication.isPlaying+" frame="+Time.frameCount+" enabled="+detector.detectionEnabled);while(!detector.IsCapturing)yield return null;
        Require(fake.Loop&&fake.Length==1&&fake.Rate==16000,"loop1second16kHz capture contract");
        fake.Position=800;float now=Time.unscaledTime+.05f;detector.PollCapture(now);
        Require(fake.Offset==0&&fake.ReadLength==800,"latest800samples first window");
        fake.Position=15900;detector.PollCapture(now+.05f);Require(fake.Offset==15100,"newest window after delayed read");
        fake.Position=700;detector.PollCapture(now+.10f);Require(fake.Offset==15900&&fake.ReadLength==800,"ring buffer wrap offset");
        Require(Mathf.Abs(MicrophoneNoiseDetector.SamplesToDb(Samples(-40))+40)<.001f,"RMS to dB");
        Require(MicrophoneNoiseDetector.SamplesToDb(new float[800])==-120,"silence finite");
        detector.enabled=false;events.Clear();float t=10;
        for(int i=0;i<12;i++){detector.ProcessSamples(Samples(-40),.05f,t);t+=.05f;}
        float whisper=detector.Level;Require(whisper>.20f&&whisper<.30f,"quiet voice low normalized loudness");
        for(int i=0;i<12;i++){detector.ProcessSamples(Samples(-10),.05f,t);t+=.05f;}
        float shout=detector.Level;Require(shout>.99f&&shout>whisper,"loud voice high normalized loudness");
        float before=detector.SmoothedDb;detector.ProcessSamples(Samples(-80),.05f,t);t+=.05f;Require(detector.SmoothedDb>before-15,"slow release");
        int count=events.Count;for(int i=0;i<30;i++){detector.ProcessSamples(Samples(-60),.05f,t);t+=.05f;}Require(events.Count==count,"noise below floor never emits during release");
        events.Clear();t=20;for(int i=0;i<20;i++){detector.ProcessSamples(Samples(-10),.05f,t);t+=.05f;}
        Require(events.Count>=4&&events.Count<=5,"voice rate limited to5persecond");
        foreach(var e in events)Require(e.source==NoiseSource.Voice&&e.owner==detector.gameObject&&e.loudness>=0&&e.loudness<=1,"voice payload");
        detector.SuppressEmission=true;count=events.Count;detector.ProcessSamples(Samples(-5),.5f,30);Require(events.Count==count,"calibration suppresses gameplay voice");detector.SuppressEmission=false;
        var silence=new List<float>();var speech=new List<float>();for(int i=0;i<60;i++){silence.Add(-60+(i%5)*.5f);speech.Add(-28+(i%7)*.5f);}
        Require(MicCalibration.TryCalculate(silence,speech,out float floor,out float max,out float sensitivity),"two-phase calibration");
        Require(floor>-58&&floor<-50,"ambient safety margin");Require(Mathf.Abs(MicrophoneNoiseDetector.NormalizeDb(-26,floor,max,sensitivity)-.6f)<.05f,"normal voice target around60percent");
        detector.noiseFloorDb=floor;detector.maxDb=max;detector.sensitivity=sensitivity;events.Clear();
        for(int i=0;i<60;i++)detector.ProcessSamples(Samples(-57),.05f,40+i*.05f);Require(events.Count==0,"calibrated room/fan noise filtered");
        Require(!MicCalibration.TryCalculate(silence,silence,out _,out _,out _),"silent speech phase rejected");
        detector.selectedDevice="Fake B";detector.detectionEnabled=false;detector.sensitivity=1.7f;detector.SaveSettings();
        var restored=Detector(new FakeInput());Require(!restored.detectionEnabled&&restored.selectedDevice=="Fake B"&&Mathf.Abs(restored.sensitivity-1.7f)<.001f&&restored.noiseFloorDb==floor&&restored.maxDb==max,"PlayerPrefs reload all settings");restored.enabled=false;
        ClearPrefs();
        var none=Detector(new FakeInput{Names=Array.Empty<string>()});for(int i=0;i<6;i++)yield return null;Require(!none.IsCapturing&&none.Status.Contains("Không có"),"no microphone safe warning");none.enabled=false;
        var denied=Detector(new FakeInput{Permission=false});for(int i=0;i<8;i++)yield return null;Require(!denied.IsCapturing&&denied.Status.Contains("từ chối"),"permission denial safe warning");denied.enabled=false;
        var unplugInput=new FakeInput();var unplug=Detector(unplugInput);waiting=unplug;while(!unplug.IsCapturing)yield return null;
        unplugInput.Names=Array.Empty<string>();unplug.RefreshDevices();Require(!unplug.IsCapturing&&unplug.Level==0&&unplugInput.EndCount>0,"unplug stops and clears level");
        unplugInput.Names=new[]{"Fake A","Fake B"};unplug.RefreshDevices();waiting=unplug;while(!unplug.IsCapturing)yield return null;
        unplugInput.ThrowRead=true;unplugInput.Position=800;unplug.PollCapture(Time.unscaledTime+.05f);Require(!unplug.IsCapturing&&unplug.Status.Contains("gặp lỗi"),"device API exception handled");unplug.enabled=false;
        var stalled=Detector(new FakeInput());waiting=stalled;while(!stalled.IsCapturing)yield return null;stalled.PollCapture(Time.unscaledTime+3);Require(!stalled.IsCapturing,"stalled hardware timeout");stalled.enabled=false;
        var badStart=Detector(new FakeInput{NullStart=true});for(int i=0;i<6;i++)yield return null;Require(!badStart.IsCapturing,"null capture clip safe");badStart.enabled=false;
        // Run the actual two timed calibration phases, using a fake live stream.
        var calibratedInput=new FakeInput{AutoAdvance=true,Amplitude=.001f};var calibrated=Detector(calibratedInput);waiting=calibrated;while(!calibrated.IsCapturing)yield return null;
        var panel=calibrated.gameObject.AddComponent<MicSettingsPanel>();panel.BeginCalibration();
        Require(calibrated.SuppressEmission,"panel starts calibration suppression");
        float calibrationStart=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-calibrationStart<3.15f)yield return null;
        calibratedInput.Amplitude=Mathf.Pow(10,-26f/20);
        while(calibrated.SuppressEmission)yield return null;
        Require(Mathf.Abs(calibrated.noiseFloorDb+54)<.5f,"actual3second ambient phase applied floor");
        Require(Mathf.Abs(MicrophoneNoiseDetector.NormalizeDb(-26,calibrated.noiseFloorDb,calibrated.maxDb,calibrated.sensitivity)-.6f)<.03f,"actual3second speech phase target");
        Require(PlayerPrefs.HasKey(Prefix+"floor"),"calibration saved preferences");
        panel.BeginCalibration();calibratedInput.Names=Array.Empty<string>();calibrated.RefreshDevices();Require(!calibrated.SuppressEmission&&!calibrated.IsCapturing,"unplug cancels active calibration");calibrated.enabled=false;panel.enabled=false;
        var meter=Create("Noise meter").AddComponent<NoiseMeterUI>();meter.player=detector.gameObject;
        NoiseManager.EmitNoise(Vector3.zero,.8f,NoiseSource.Footstep,detector.gameObject);Require(meter.RecentPeak==.8f&&meter.CurrentColor==meter.dangerColor,"own footsteps event and danger color");
        NoiseManager.EmitNoise(Vector3.zero,1,NoiseSource.Voice,restored.gameObject);NoiseManager.EmitNoise(Vector3.zero,1,NoiseSource.Door,detector.gameObject);Require(meter.RecentPeak==.8f,"ignore other owners and door noise");
        meter.enabled=false;meter.enabled=true;meter.Record(.4f,100);Require(meter.CurrentColor==meter.warningColor,"warning color");meter.Record(.1f,100.2f);meter.Advance(100.49f,.1f);Require(meter.RecentPeak==.4f,"highest in recent half second");
        meter.Advance(100.51f,.05f);Require(meter.RecentPeak==.1f&&meter.DisplayedLevel>.1f&&meter.DisplayedLevel<.4f,"expired peak fades smoothly");meter.Advance(101,1);Require(meter.DisplayedLevel==0&&meter.CurrentColor==meter.quietColor,"idle decay and quiet color");
        NoiseManager.OnNoiseEmitted-=Heard;
        Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/mic-noise-check.txt",$"PASS: fake microphone loop1s/16kHz; newest800frames and ring wrap; RMS/dB; whisper={whisper:F3}, shout={shout:F3}; fast attack/slow release; gate and<=5Voice events/sec; owner/source; calibration suppression; actual3s+3s calibration coroutine, saved preferences, unplug cancellation; fan rejected; invalid calibration rejected; all PlayerPrefs fields reloaded; no device/permission denied/unplug/reconnect/API exception/null clip/stall safe; HUD own Footstep/Voice filtering, half-second maximum, fade and3colors. Real microphone hardware was not recorded.\n");
    }
    static void ClearPrefs(){foreach(string key in new[]{"enabled","device","floor","max","sensitivity"})PlayerPrefs.DeleteKey(Prefix+key);PlayerPrefs.Save();}
    static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;SessionState.SetBool("MicNoiseCheck",false);NoiseManager.OnNoiseEmitted-=Heard;
        foreach(var go in objects)if(go)Object.DestroyImmediate(go);objects.Clear();ClearPrefs();
        if(error!=null){Debug.LogException(error);Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/mic-noise-check.txt","FAIL: "+error);}
        EditorApplication.Exit(error==null?0:1);
    }
    sealed class FakeInput:IMicrophoneInput
    {
        public bool Permission=true,ThrowRead,NullStart,Loop,AutoAdvance;public float Amplitude=.001f;public int Length,Rate,Position,Offset,ReadLength,EndCount;
        public string[] Names={"Fake A","Fake B"};
        public bool HasPermission=>Permission;
        public IEnumerator RequestPermission(){yield return null;}
        public string[] Devices=>Names;
        public AudioClip Start(string device,bool loop,int lengthSeconds,int frequency){Loop=loop;Length=lengthSeconds;Rate=frequency;return NullStart?null:AudioClip.Create("Fake mic",16000,1,16000,false);}
        public void End(string device){EndCount++;}
        public bool IsRecording(string device)=>true;
        public int GetPosition(string device)=>AutoAdvance?(int)(Time.realtimeSinceStartup*16000)%16000:Position;
        public bool Read(AudioClip clip,float[] samples,int offset){if(ThrowRead)throw new InvalidOperationException("device removed");Offset=offset;ReadLength=samples.Length;for(int i=0;i<samples.Length;i++)samples[i]=Amplitude;return true;}
    }
}
