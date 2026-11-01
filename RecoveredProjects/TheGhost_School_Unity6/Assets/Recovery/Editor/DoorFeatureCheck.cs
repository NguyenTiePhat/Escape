using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class DoorFeatureCheck
{
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static Door Create(float scale=1)
    {
        var root=new GameObject("Hinge");root.transform.localScale=Vector3.one*scale;
        var leaf=new GameObject("Leaf");leaf.transform.SetParent(root.transform,false);
        var box=leaf.AddComponent<BoxCollider>();box.center=new Vector3(.5f,1,0)/scale;box.size=new Vector3(1,2,.08f)/scale;
        var door=root.AddComponent<Door>();door.openAngle=90;door.Initialize();return door;
    }
    static GameObject PlayerAt(Vector3 p)
    {
        var g=new GameObject("Player blocker");g.transform.position=p;
        var cc=g.AddComponent<CharacterController>();cc.height=1.8f;cc.radius=.2f;cc.center=Vector3.zero;Physics.SyncTransforms();return g;
    }
    static void NoPenetration(Door door,Collider other)
    {
        Physics.SyncTransforms();foreach(var panel in door.GetComponentsInChildren<BoxCollider>())
            Require(!Physics.ComputePenetration(panel,panel.transform.position,panel.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth)||depth<.001f,"Door penetrates blocker: "+depth);
    }
    public static void Run()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var door=Create();int events=0;float lastSpeed=0;bool lastOpen=false;
            door.OnDoorToggled+=(sender,open,speed)=>{Require(sender==door,"Wrong event sender");events++;lastSpeed=speed;lastOpen=open;};
            door.requiredKeyId="school-key";door.LockDoor();Require(!door.Toggle(60)&&events==0,"Locked door toggled");
            Require(!door.UnlockDoor("wrong")&&door.isLocked,"Wrong key unlocked");Require(door.UnlockDoor("school-key")&&!door.isLocked,"Correct key failed");
            Require(door.Toggle(30)&&events==1&&lastSpeed==30&&lastOpen,"Slow open event");door.Tick(1);Require(Mathf.Abs(door.CurrentAngle-30)<.02f,"Slow angle/time");
            door.Tick(3);Require(door.IsFullyOpen,"Open endpoint");Require(door.Toggle(160)&&events==2&&!lastOpen&&lastSpeed==160,"Strong close event");door.Tick(1);Require(door.IsFullyClosed,"Close endpoint");
            Require(!door.Toggle(0)&&!door.Toggle(float.NaN)&&events==2,"Invalid speed accepted");
            var player=PlayerAt(new Vector3(.6f,1,-.6f));Require(door.Toggle(10000),"Begin opening failed");door.Tick(1);
            Require(door.IsBlocked&&door.CurrentAngle>0&&door.CurrentAngle<85,"High-speed opening crossed player");NoPenetration(door,player.GetComponent<Collider>());int blockedEvents=events;
            for(int i=0;i<10;i++)door.Tick(.1f);Require(events==blockedEvents,"Blocked retries emitted duplicate events");
            Object.DestroyImmediate(player);Physics.SyncTransforms();door.Tick(2);Require(door.IsFullyOpen,"Did not resume after clearance");
            player=PlayerAt(new Vector3(.6f,1,-.6f));Require(door.Toggle(10000),"Begin closing failed");door.Tick(1);Require(door.IsBlocked&&door.CurrentAngle>1,"Closing crossed player");NoPenetration(door,player.GetComponent<Collider>());
            Object.DestroyImmediate(player);door.Tick(2);Require(door.IsFullyClosed,"Close after clearance");
            var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.position=new Vector3(.6f,1,-.6f);blocker.transform.localScale=new Vector3(.04f,2,.04f);Physics.SyncTransforms();
            door.Toggle(10000);door.Tick(1);Require(door.IsBlocked,"Thin static obstacle missed");NoPenetration(door,blocker.GetComponent<Collider>());Object.DestroyImmediate(blocker);
            door.LockDoor();float stopped=door.CurrentAngle;door.Tick(2);Require(Mathf.Abs(door.CurrentAngle-stopped)<.001f,"Lock did not stop motion");door.UnlockDoor();
            Object.DestroyImmediate(door.gameObject);
            door=Create(106.6f);player=PlayerAt(new Vector3(.6f,1,-.6f));door.Toggle(160);door.Tick(2);Require(door.IsBlocked,"Scaled recovered hinge crossed player");NoPenetration(door,player.GetComponent<Collider>());Object.DestroyImmediate(player);door.SetImmediate(false);door.decorative=true;Require(!door.Toggle(60),"Decorative leaf interacted");door.SetImmediate(true);Require(door.IsFullyClosed,"Decorative leaf moved");
            File.WriteAllText("RecoveryReports/door-feature-check.txt","PASS: lock/unlock/key ID, slow/strong speeds and event payload/count, endpoints, high-speed/low-FPS opening and closing stop before CharacterController, no duplicate blocked events, resumes after clearance, thin static blocker, lock during motion, scale106.6, decorative leaf never moves.");EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText("RecoveryReports/door-feature-check.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
