using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class TensionSetup
{
    const string Root="Assets/Recovery/Tension";
    public static void Run()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        if(GameObject.Find("Tension and scares"))throw new Exception("Already installed");
        File.Copy(scene.path,"RecoveryReports/School_Test-before-tension.unity",true);
        for(int i=0;i<4;i++)WriteAudio(i);
        AssetDatabase.Refresh();
        var player=Object.FindAnyObjectByType<PlayerHealth>();if(!player)throw new Exception("Player missing");
        var root=new GameObject("Tension and scares");
        var tension=root.AddComponent<TensionManager>();tension.player=player;
        root.AddComponent<TensionSettings>();
        var music=root.AddComponent<AdaptiveMusic>();music.tension=tension;
        music.ambient=Audio(root.transform,"Ambient",Root+"/Ambient.wav",false);
        music.tense=Audio(root.transform,"Tense",Root+"/Tense.wav",false);
        music.chase=Audio(root.transform,"Chase",Root+"/Chase.wav",false);
        var heart=root.AddComponent<HeartbeatController>();heart.tension=tension;heart.heartbeat=Audio(root.transform,"Heartbeat",Root+"/HeartbeatPulse.wav",false);
        var camera=player.GetComponentInChildren<Camera>();var visuals=camera.gameObject.AddComponent<TensionVisuals>();visuals.tension=tension;visuals.effectShader=Shader.Find("Hidden/School/TensionEffects");
        if(!visuals.effectShader)throw new Exception("Effect shader missing");
        var director=root.AddComponent<ScareDirector>();director.tension=tension;director.globalCooldown=90;
        var sound=Event(root.transform,"Corridor knock",ScareKind.Sound,new Vector3(14.5f,2.1f,55));
        sound.sound=Audio(sound.transform,"Distant knock","Assets/AudioClip/WoodenDoorClose.ogg",true);sound.sound.transform.localPosition=new Vector3(0,1,4);
        var slam=Event(root.transform,"Door behind player",ScareKind.DoorSlam,new Vector3(18.5f,2.1f,60));
        var doors=Object.FindObjectsByType<Door>().Where(d=>!d.decorative&&!d.GetComponent<MainExitDoor>()&&Vector3.Distance(d.transform.position,player.transform.position)<10).ToArray();
        if(doors.Length==0)throw new Exception("No nearby gameplay door");
        slam.door=doors.OrderBy(d=>Vector3.Distance(d.transform.position,player.transform.position)).First();
        slam.sound=Audio(slam.transform,"Door slam","Assets/AudioClip/WoodenDoorClose.ogg",true);
        slam.sound.transform.position=slam.door.transform.position+Vector3.up;
        Vector3 away=Vector3.ProjectOnPlane(slam.door.transform.position-player.transform.position,Vector3.up).normalized;
        slam.transform.position=Ground(slam.door.transform.position+away*1.6f)+Vector3.up;
        var shadow=Event(root.transform,"End of corridor silhouette",ScareKind.Shadow,new Vector3(5,2.1f,60));
        shadow.shadowStart=new GameObject("Shadow start").transform;shadow.shadowStart.SetParent(shadow.transform);shadow.shadowStart.position=Ground(new Vector3(4,2.1f,65));
        shadow.shadowEnd=new GameObject("Shadow end").transform;shadow.shadowEnd.SetParent(shadow.transform);shadow.shadowEnd.position=shadow.shadowStart.position+Vector3.right*2.2f;
        shadow.shadow=new GameObject("Passing silhouette").transform;shadow.shadow.SetParent(shadow.transform);shadow.shadow.position=shadow.shadowStart.position;
        var silhouette=new Material(Shader.Find("Unlit/Color"));silhouette.color=new Color(.025f,.025f,.025f);AssetDatabase.CreateAsset(silhouette,Root+"/Silhouette.mat");
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(shadow.shadow,false);body.transform.localPosition=Vector3.up*.9f;body.transform.localScale=new Vector3(.38f,.85f,.22f);Object.DestroyImmediate(body.GetComponent<Collider>());body.GetComponent<Renderer>().sharedMaterial=silhouette;
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(shadow.shadow,false);head.transform.localPosition=Vector3.up*1.75f;head.transform.localScale=Vector3.one*.24f;Object.DestroyImmediate(head.GetComponent<Collider>());head.GetComponent<Renderer>().sharedMaterial=silhouette;
        shadow.shadow.gameObject.SetActive(false);director.events=new[]{sound,slam,shadow};
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("RecoveryReports/tension-setup.txt","Built-in effects installed; three continuous music layers; heartbeat pulse; three scare events.\n"+string.Join("\n",director.events.Select(e=>e.eventId+" at "+e.transform.position))+"\nSlam door="+slam.door.name);
        TensionValidation.Run();
    }
    static Vector3 Ground(Vector3 point){if(NavMesh.SamplePosition(point,out var hit,3,NavMesh.AllAreas))return hit.position;throw new Exception("Scare point not on navigation mesh "+point);}
    public static void FinishAudio()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Recovery/Scenes/School_Test.unity");
        var director=Object.FindAnyObjectByType<ScareDirector>();var slam=director.events.First(e=>e.kind==ScareKind.DoorSlam);
        if(!slam.sound){slam.sound=Audio(slam.transform,"Door slam","Assets/AudioClip/WoodenDoorClose.ogg",true);slam.sound.transform.position=slam.door.transform.position+Vector3.up;}
        if(!slam.sound.clip||slam.sound.spatialBlend!=1)throw new Exception("Invalid slam audio");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.AppendAllText("RecoveryReports/tension-integration-check.txt","PASS saved door-slam event has spatial closing audio\n");
        EditorApplication.Exit(0);
    }
    static ScareEvent Event(Transform parent,string name,ScareKind kind,Vector3 point)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=Ground(point)+Vector3.up;
        var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(2,3,2);
        var e=go.AddComponent<ScareEvent>();e.kind=kind;e.eventId=name;e.trigger=box;return e;
    }
    static AudioSource Audio(Transform parent,string name,string path,bool spatial)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);var s=go.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=spatial?1:0;s.volume=spatial?.4f:0;s.minDistance=1;s.maxDistance=18;s.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(path);if(!s.clip)throw new Exception("Missing audio "+path);return s;
    }
    static void WriteAudio(int layer)
    {
        string name=new[]{"Ambient","Tense","Chase","HeartbeatPulse"}[layer];const int rate=22050;int count=(int)((layer==3?.5:16)*rate);
        using(var w=new BinaryWriter(File.Create(Root+"/"+name+".wav")))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+count*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(count*2);
            for(int i=0;i<count;i++)
            {
                double t=(double)i/rate, value;
                if(layer==3){value=Pulse(t)+.65*Pulse(t-.17);}
                else {double pad=(Math.Sin(2*Math.PI*55*t)+.4*Math.Sin(2*Math.PI*65.375*t)+.3*Math.Sin(2*Math.PI*82.375*t))*.18;
                    double rhythm=layer==0?.55:layer==1?.5+.3*Math.Cos(2*Math.PI*t):.4+.55*Math.Pow(.5+.5*Math.Cos(4*Math.PI*t),4);
                    value=pad*rhythm+(layer==2?.025*Math.Sin(2*Math.PI*110*t):0);}
                w.Write((short)(Math.Max(-1,Math.Min(1,value))*32767));
            }
        }
    }
    static double Pulse(double t)=>t<0||t>.22?0:Math.Sin(2*Math.PI*65*t)*Math.Exp(-t*24)*Math.Min(1,t/.008)*.7;
}
