using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class NoiseGameplayCheck
{
    static double started;
    static NoiseGameplayCheck()
    {
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("NoiseGameplayCheck",false)){started=EditorApplication.timeSinceStartup;EditorApplication.update+=Check;}};
    }
    public static void Run(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);SessionState.SetBool("NoiseGameplayCheck",true);EditorApplication.isPlaying=true;}
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Move(PlayerMotor motor,FootstepNoise steps,float metres,bool sprint=false)
    {
        float speed=motor.IsCrouching?motor.crouchSpeed:sprint?motor.sprintSpeed:motor.walkSpeed;
        while(metres>.00001f){float amount=Mathf.Min(metres,speed*.05f);float dt=amount/speed;motor.Tick(Vector2.up,sprint,dt);steps.SampleMovement(dt);metres-=amount;}
    }
    static void Check()
    {
        if(EditorApplication.timeSinceStartup-started<1)return;
        EditorApplication.update-=Check;
        try
        {
            var manager=NoiseManager.Instance;manager.debugLogging=false;
            var listener=new GameObject("Probe").AddComponent<NoiseTestListener>();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Floor";floor.transform.position=new Vector3(0,-.5f,0);floor.transform.localScale=new Vector3(200,1,200);
            var floorCollider=floor.GetComponent<Collider>();var material=new PhysicsMaterial("Tile");floorCollider.sharedMaterial=material;
            var player=new GameObject("Footstep player");var cc=player.AddComponent<CharacterController>();cc.height=1.8f;cc.center=new Vector3(0,.9f,0);cc.radius=.3f;player.transform.position=new Vector3(0,.05f,0);
            var motor=player.AddComponent<PlayerMotor>();var steps=player.AddComponent<FootstepNoise>();
            var tile=AudioClip.Create("Tile test",2205,1,22050,false);var wood=AudioClip.Create("Wood test",2205,1,22050,false);var metal=AudioClip.Create("Metal test",2205,1,22050,false);var carpet=AudioClip.Create("Carpet test",2205,1,22050,false);
            steps.tileClips=new[]{tile};steps.woodClips=new[]{wood};steps.metalClips=new[]{metal};steps.carpetClips=new[]{carpet};
            Physics.SyncTransforms();for(int i=0;i<10;i++)motor.Tick(Vector2.zero,false,.05f);Require(cc.isGrounded,"test player grounded");steps.ResetTracking();
            Move(motor,steps,1.69f);Require(steps.StepCount==0,"no walking step before1.7m");Move(motor,steps,.02f);Require(steps.StepCount==1,"walking step at1.7m");
            Require(Mathf.Abs(manager.LatestNoise.loudness-.35f)<.00001f&&steps.LastClip==tile,"tile walk noise/audio selection");
            int count=steps.StepCount;for(int i=0;i<20;i++){motor.Tick(Vector2.zero,false,.05f);steps.SampleMovement(.05f);}Require(steps.StepCount==count,"standing still silent");
            material.name="Metal";steps.ResetTracking();Move(motor,steps,2.29f,true);Require(steps.StepCount==count,"no sprint step before2.3m");Move(motor,steps,.02f,true);
            Require(steps.StepCount==++count && steps.LastSurface==FootstepSurface.Metal && steps.LastClip==metal,"metal ray material and audio");Require(manager.LatestNoise.loudness==1 && manager.LatestNoise.loudness*25==25,"sprint metal loudness1/radius25");
            material.name="Carpet";Require(motor.SetCrouching(true),"crouch");steps.ResetTracking();Move(motor,steps,1.19f);Require(steps.StepCount==count,"no crouch step before1.2m");Move(motor,steps,.02f);
            Require(steps.StepCount==++count && steps.LastClip==carpet,"carpet crouch audio");Require(Mathf.Abs(manager.LatestNoise.loudness-.06f)<.00001f && Mathf.Abs(manager.LatestNoise.loudness*25-1.5f)<.0001f,"carpet crouch loudness/radius");
            material.name="Wood";steps.ResetTracking();Move(motor,steps,1.21f);Require(steps.LastClip==wood&&Mathf.Abs(manager.LatestNoise.loudness-.1f)<.00001f,"wood surface/audio");
            // A real blocking collider prevents distance accumulation despite input.
            motor.SetCrouching(false);player.transform.position=new Vector3(20,.01f,0);steps.ResetTracking();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(20,1,.7f);wall.transform.localScale=new Vector3(5,2,.4f);Physics.SyncTransforms();
            count=steps.StepCount;Move(motor,steps,8);Require(steps.StepCount==count,"walking into wall silent");
            player.transform.position+=Vector3.up*10;steps.ResetTracking();motor.Tick(Vector2.up,false,.05f);steps.SampleMovement(.05f);Require(steps.StepCount==count,"airborne silent");
            Object.DestroyImmediate(player);Object.DestroyImmediate(wall);
            // Exercise the existing Door event, including locked/no-op and resubscribe behavior.
            var hinge=new GameObject("Noise door");hinge.transform.position=new Vector3(0,2,0);var leaf=new GameObject("Panel");leaf.transform.SetParent(hinge.transform,false);var box=leaf.AddComponent<BoxCollider>();box.center=new Vector3(.5f,0,0);box.size=new Vector3(1,1,.05f);
            var door=hinge.AddComponent<Door>();var bridge=hinge.AddComponent<DoorNoise>();
            int heard=listener.HeardCount;Require(door.Toggle(60),"slow door toggle");float slow=manager.LatestNoise.loudness;Require(listener.HeardCount==heard+1&&manager.LatestNoise.source==NoiseSource.Door,"door event emits once");door.Tick(2);
            bridge.enabled=false;bridge.enabled=true;heard=listener.HeardCount;Require(door.Toggle(160),"strong door toggle");float strong=manager.LatestNoise.loudness;Require(strong>slow && listener.HeardCount==heard+1,"stronger door louder, no duplicate subscription");door.LockDoor();heard=listener.HeardCount;Require(!door.Toggle(160)&&listener.HeardCount==heard,"locked door silent");Object.DestroyImmediate(hinge);
            // Release and impact are distinct events. Drop one real Inventory item.
            var inventoryObject=new GameObject("Inventory noise player");var inv=inventoryObject.AddComponent<Inventory>();inventoryObject.AddComponent<InventoryNoise>();var camera=new GameObject("Drop camera").AddComponent<Camera>();camera.transform.SetParent(inventoryObject.transform);camera.transform.position=new Vector3(0,3,0);inv.view=camera;
            var template=GameObject.CreatePrimitive(PrimitiveType.Cube);template.transform.position=new Vector3(80,10,80);template.transform.localScale=Vector3.one*.1f;template.AddComponent<Rigidbody>().isKinematic=true;var pickup=template.AddComponent<PickupItem>();var data=ScriptableObject.CreateInstance<ItemData>();data.id="noise-test";data.weight=.2f;data.pickupPrefab=pickup;pickup.item=data;
            Require(inv.AddItem(data)&&inv.Drop(),"inventory drop");Require(manager.LatestNoise.source==NoiseSource.Drop&&manager.LatestNoise.owner==inventoryObject,"release event source/owner");
            NoiseOnImpact dropped=null;foreach(var impact in Object.FindObjectsByType<NoiseOnImpact>(FindObjectsSortMode.None))if(impact.owner==inventoryObject)dropped=impact;
            Require(dropped,"dropped instance automatically has impact component");Object.DestroyImmediate(dropped.gameObject);Object.DestroyImmediate(template);Object.DestroyImmediate(inventoryObject);
            var oldMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            float bottleNoise=Impact(.15f,.35f,0,out int bottleCount);float crateNoise=Impact(8,2,7,out int crateCount);
            Physics.simulationMode=oldMode;
            Require(bottleCount>0&&crateCount>0,"actual collision callbacks fired");Require(crateNoise>bottleNoise&&bottleNoise>=.1f&&crateNoise<=1,"thrown crate louder than gently dropped bottle");
            Require(NoiseOnImpact.CalculateLoudness(100,100)==1,"impact clamp");
            Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/noise-gameplay-check.txt",$"PASS: real CharacterController distance thresholds walk1.7/sprint2.3/crouch1.2; idle/wall/air silent; surface ray/clip selection; sprint metal1/radius25; crouch carpet.06/radius1.5; Door event slow={slow:F3} strong={strong:F3}; locked silent; no duplicate subscription; Inventory release event and auto impact component; actual Physics collision bottle={bottleNoise:F3} crate={crateNoise:F3}; impact clamp.\n");
            Finish(0);
        }
        catch(Exception e){Debug.LogException(e);Directory.CreateDirectory("RecoveryReports");File.WriteAllText("RecoveryReports/noise-gameplay-check.txt","FAIL: "+e);Finish(1);}
    }
    static float Impact(float mass,float height,float speed,out int count)
    {
        var prop=GameObject.CreatePrimitive(PrimitiveType.Cube);prop.transform.position=new Vector3(0,height,0);prop.transform.localScale=Vector3.one*.2f;var body=prop.AddComponent<Rigidbody>();body.mass=mass;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=new Vector3(speed,-speed,0);var impact=prop.AddComponent<NoiseOnImpact>();
        Physics.SyncTransforms();float loudness=0;
        for(int i=0;i<160;i++){Physics.Simulate(.02f);if(impact.EmissionCount>0){loudness=NoiseManager.Instance.LatestNoise.loudness;break;}}
        count=impact.EmissionCount;Object.DestroyImmediate(prop);return loudness;
    }
    static void Finish(int code){SessionState.SetBool("NoiseGameplayCheck",false);EditorApplication.Exit(code);}
}
