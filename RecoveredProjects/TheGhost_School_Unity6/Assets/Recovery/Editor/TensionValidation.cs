using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class TensionValidation
{
    static IEnumerator routine;
    static readonly StringBuilder report=new StringBuilder();
    static double deadline;
    static bool previousReduced;
    static TensionValidation(){EditorApplication.playModeStateChanged+=Changed;}
    public static void Run(){SessionState.SetBool("TensionValidation",true);EditorApplication.EnterPlaymode();}
    public static void Integration()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        SessionState.SetBool("TensionIntegration",true);Run();
    }
    static void Changed(PlayModeStateChange s)
    {
        if(s!=PlayModeStateChange.EnteredPlayMode||!SessionState.GetBool("TensionValidation",false))return;
        previousReduced=TensionSettings.Reduced;routine=SessionState.GetBool("TensionIntegration",false)?IntegrationTest():Test();deadline=EditorApplication.timeSinceStartup+150;EditorApplication.update+=Tick;
    }
    static void Tick(){try{if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Timeout");if(routine.MoveNext())return;Finish(0);}catch(Exception e){report.AppendLine("FAIL "+e);Finish(1);}}
    static void Finish(int code){TensionSettings.SetReduced(previousReduced);PlayerPrefs.Save();EditorApplication.update-=Tick;SessionState.SetBool("TensionValidation",false);bool integration=SessionState.GetBool("TensionIntegration",false);SessionState.SetBool("TensionIntegration",false);File.WriteAllText(integration?"RecoveryReports/tension-integration-check.txt":"RecoveryReports/tension-check.txt",report.ToString());EditorApplication.Exit(code);}
    static void Check(bool b,string text){if(!b)throw new Exception(text);report.AppendLine("PASS "+text);}
    static IEnumerator IntegrationTest()
    {
        yield return null;yield return null;
        TensionSettings.SetReduced(false);
        var tension=Object.FindAnyObjectByType<TensionManager>();var director=Object.FindAnyObjectByType<ScareDirector>();
        foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
        var monsters=Object.FindObjectsByType<MonsterAI>();foreach(var m in monsters)m.enabled=false;
        tension.enabled=false;
        var monster=monsters[0];var aiType=typeof(MonsterAI);
        // Keep this real monster as a sensor target, but stop its autonomous Update during these synchronous samples.
        monster.enabled=true;
        var state=aiType.GetProperty("CurrentState");state.SetValue(monster,MonsterState.Patrol);
        var motor=tension.player.GetComponent<PlayerMotor>();motor.enabled=false;
        var body=tension.player.GetComponent<CharacterController>();body.enabled=false;
        Vector3 home=tension.player.transform.position;
        var update=typeof(TensionManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
        tension.player.transform.position=monster.transform.position+Vector3.right*20;update.Invoke(tension,null);float far=tension.NearestDistance;
        tension.player.transform.position=monster.transform.position+Vector3.right*3;update.Invoke(tension,null);Check(tension.NearestDistance<far&&Mathf.Abs(tension.NearestDistance-3)<.05f,"real nearest-monster distance tracks approach");
        state.SetValue(monster,MonsterState.Chase);update.Invoke(tension,null);Check(tension.IsChased,"real monster chase state reaches manager");
        Check(!director.TryTrigger(director.events[0],1000),"actual chase blocks scare activation");
        state.SetValue(monster,MonsterState.Patrol);update.Invoke(tension,null);monster.enabled=false;
        var heart=Object.FindAnyObjectByType<HeartbeatController>();
        var hu=typeof(HeartbeatController).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
        float distant=HeartbeatController.Proximity(11),close=HeartbeatController.Proximity(3);Check(close>distant,"heartbeat strength increases with measured approach");
        var environmental=new GameObject("Validation environmental sound").AddComponent<AudioSource>();environmental.spatialBlend=1;
        typeof(HeartbeatController).GetMethod("Discover",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(heart,null);
        for(int i=0;i<30;i++)hu.Invoke(heart,null);
        var filter=environmental.GetComponent<AudioLowPassFilter>();Check(filter&&filter.cutoffFrequency<22000,"environmental sound gets proximity low-pass");
        TensionSettings.SetReduced(true);hu.Invoke(heart,null);Check(Mathf.Abs(filter.cutoffFrequency-22000)<1,"reduced effects restores environmental cutoff");
        TensionSettings.SetReduced(false);tension.player.transform.position=home;body.enabled=true;Physics.SyncTransforms();
        var camera=tension.player.GetComponentInChildren<Camera>();tension.Tick(1,100);
        Capture(camera,"RecoveryReports/tension-high.png");
        TensionSettings.SetReduced(true);Capture(camera,"RecoveryReports/tension-reduced.png");
        TensionSettings.SetReduced(false);tension.Tick(0,100);
        var slam=director.events[1];
        slam.door.UnlockDoor();slam.door.SetImmediate(true);Physics.SyncTransforms();
        Check(director.TryTrigger(slam,1000),"door-slam event starts on an open door at low tension");
        for(int i=0;i<120;i++)slam.door.Tick(1f/60);
        Check(slam.door.IsFullyClosed,"door slam completes through existing collision-safe door motion");
        report.AppendLine("High/reduced camera renders captured; no changes from validation saved to scene.");
    }
    static void Capture(Camera camera,string path)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(960,540,24);
        camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
        tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());
        camera.targetTexture=previous;RenderTexture.active=active;Object.Destroy(tex);rt.Release();Object.Destroy(rt);
    }
    static IEnumerator Test()
    {
        yield return null;yield return null;
        var tension=Object.FindAnyObjectByType<TensionManager>();var director=Object.FindAnyObjectByType<ScareDirector>();
        Check(tension&&tension.ActiveRound&&director&&director.events.Length==3,"scene has active tension and three authored scare events");
        foreach(var mic in Object.FindObjectsByType<MicrophoneNoiseDetector>())mic.enabled=false;
        foreach(var monster in Object.FindObjectsByType<MonsterAI>())monster.enabled=false;
        var method=typeof(TensionManager).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic);
        tension.enabled=false;
        float far=TensionManager.Calculate(40,0,0,0),near=TensionManager.Calculate(2,0,0,0);
        Check(near>far&&TensionManager.Calculate(2,1,1,1)==1,"proximity increases target; chase reaches maximum");
        Check(TensionManager.Calculate(30,0,1,1)>TensionManager.Calculate(30,0,0,0),"noise and darkness contribute");
        int changes=0;tension.OnTensionChanged+=_=>changes++;
        tension.Tick(1,1);float peak=tension.Tension;tension.Tick(0,.1f);
        Check(tension.Tension>0&&tension.Tension<peak&&changes==2,"Lerp decay is gradual and publishes events");
        var other=new GameObject("unrelated noise");NoiseManager.EmitNoise(tension.player.transform.position,1,NoiseSource.Voice,other);Check(tension.PlayerNoise==0,"unrelated noise ignored");
        // Re-enable only to restore the noise subscription, then stop automatic ticking.
        tension.enabled=true;NoiseManager.EmitNoise(tension.player.transform.position,1,NoiseSource.Voice,tension.player.gameObject);
        Check(tension.PlayerNoise>.9f,"player-owned noise reaches tension");tension.enabled=false;
        for(int i=0;i<=100;i++){var w=AdaptiveMusic.Weights(i/100f);Check(w.x>=0&&w.y>=0&&w.z>=0&&Mathf.Abs(w.x+w.y+w.z-1)<.001f,"music weights normalized "+i);}
        var music=Object.FindAnyObjectByType<AdaptiveMusic>();
        Check(music.ambient.clip&&music.tense.clip&&music.chase.clip&&music.ambient.loop&&music.chase.loop,"three continuous audio layers configured");
        double until=EditorApplication.timeSinceStartup+.4;while(EditorApplication.timeSinceStartup<until)yield return null;
        Check(music.ambient.isPlaying&&music.tense.isPlaying&&music.chase.isPlaying,"all layers play continuously even at zero gain");
        Check(HeartbeatController.Proximity(12)==0&&HeartbeatController.Proximity(5)>HeartbeatController.Proximity(10),"heartbeat starts below 12m and intensifies closer");
        var camera=tension.player.GetComponentInChildren<Camera>();var visuals=camera.GetComponent<TensionVisuals>();
        Check(visuals&&visuals.effectShader&&visuals.effectShader.isSupported&&!ShaderUtil.ShaderHasError(visuals.effectShader),"Built-in visual shader compiled and attached");
        TensionSettings.SetReduced(true);Check(TensionSettings.VisualStrength==0&&TensionSettings.ShakeStrength==0,"reduced mode removes visual effects and shake");
        Check(!director.TryTrigger(director.events[0],100),"reduced mode suppresses scares");TensionSettings.SetReduced(false);
        Check(!ScareDirector.Allowed(true,true,0,1,1000,0,90),"no scares during chase");
        Check(!ScareDirector.Allowed(true,false,0,1,89,0,0)&&ScareDirector.Allowed(true,false,0,1,90,0,0),"90-second minimum cannot be shortened in Inspector");
        Check(!ScareDirector.Allowed(true,false,.8f,.35f,1000,0,90),"high tension suppresses scares");
        tension.Tick(0,100);
        Check(director.TryTrigger(director.events[0],100),"sound event fires at low tension");
        Check(!director.TryTrigger(director.events[2],150),"global cooldown blocks another event");
        until=EditorApplication.timeSinceStartup+2.2;while(EditorApplication.timeSinceStartup<until)yield return null;
        Check(!director.TryTrigger(director.events[0],300),"same event cannot repeat this round");
        Check(director.TryTrigger(director.events[2],300),"different event can run after cooldown");
        TensionSettings.SetReduced(true);yield return null;
        Check(!director.events[2].shadow.gameObject.activeSelf,"active shadow is hidden when effects disabled");
        TensionSettings.SetReduced(false);
        var game=GameStateManager.Instance;tension.player.Escape();yield return null;
        Check(!tension.ActiveRound&&!director.TryTrigger(director.events[1],500),"round end suppresses tension gameplay and scares");
        report.AppendLine("Complete. Local Play Mode validation; audio gain/loop behaviour checked programmatically.");
    }
}
