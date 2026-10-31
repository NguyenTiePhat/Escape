using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FpsControllerCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        GameObject player = null, floor = null, ceiling = null;
        try
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, -.5f, 0); floor.transform.localScale = new Vector3(100, 1, 100);
            player = new GameObject("FPS test");
            var body = player.AddComponent<CharacterController>(); body.height = 1.8f; body.radius = .3f; body.center = Vector3.up * .9f;
            var camera = new GameObject("Camera"); camera.transform.SetParent(player.transform); camera.transform.localPosition = Vector3.up * 1.6f;
            var motor = player.AddComponent<PlayerMotor>(); motor.view = camera.transform;
            // Edit-mode batch execution explicitly invokes lifecycle initialization.
            typeof(PlayerMotor).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(motor, null); typeof(PlayerMotor).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(motor, null); Physics.SyncTransforms();
            for (int i=0;i<30;i++) motor.Tick(Vector2.zero,false,1f/60);
            Require(body.isGrounded,"Gravity/grounding failed");
            var initial=player.transform.position;
            for(int i=0;i<60;i++) motor.Tick(Vector2.up,false,1f/60);
            Require(Mathf.Abs((player.transform.position-initial).z-3.5f)<.02f,"Walk speed");
            initial=player.transform.position;
            for(int i=0;i<60;i++) motor.Tick(Vector2.one,false,1f/60);
            Require(Mathf.Abs(Vector3.ProjectOnPlane(player.transform.position-initial,Vector3.up).magnitude-3.5f)<.02f,"Diagonal normalization");
            initial=player.transform.position;
            for(int i=0;i<60;i++) motor.Tick(Vector2.up,true,1f/60);
            Require(Mathf.Abs((player.transform.position-initial).z-6)<.02f,"Sprint speed");
            Require(Mathf.Abs(motor.Stamina-80)<.01f,"Sprint drain");
            motor.UpdateStamina(true,4.1f); Require(motor.Exhausted && motor.Stamina==0,"Exhaustion");
            motor.UpdateStamina(true,.5f); Require(!motor.IsSprinting && motor.Stamina==0,"Lockout / delay");
            motor.UpdateStamina(false,.5f); Require(motor.Stamina==0,"One second delay");
            motor.UpdateStamina(false,2); Require(Mathf.Abs(motor.Stamina-24)<.01f && motor.Exhausted,"Recovery / threshold");
            motor.UpdateStamina(true,.2f); Require(!motor.Exhausted && !motor.IsSprinting,"Unlock above 25");
            motor.UpdateStamina(true,.1f); Require(motor.IsSprinting,"Sprint resumes");
            float feet=player.transform.position.y+body.center.y-body.height*.5f;
            Require(motor.SetCrouching(true),"Crouch rejected");
            motor.Tick(Vector2.zero,false,1f/60);
            Require(body.height>1.1f && body.height<1.8f,"Capsule must lerp");
            for(int i=0;i<60;i++) motor.Tick(Vector2.zero,false,1f/60);
            Require(Mathf.Abs(body.height-1.1f)<.001f && Mathf.Abs(player.transform.position.y+body.center.y-body.height*.5f-feet)<.02f,"Crouch / fixed feet");
            Require(Mathf.Abs(camera.transform.localPosition.y-.9f)<.001f,"Camera lowering");
            initial=player.transform.position;
            for(int i=0;i<60;i++) motor.Tick(Vector2.up,true,1f/60);
            Require(Mathf.Abs((player.transform.position-initial).z-1.8f)<.02f && !motor.IsSprinting,"Crouch speed / no sprint");
            ceiling=GameObject.CreatePrimitive(PrimitiveType.Cube); ceiling.transform.position=player.transform.position+Vector3.up*1.5f; ceiling.transform.localScale=new Vector3(2,.2f,2); Physics.SyncTransforms();
            Require(!motor.SetCrouching(false) && motor.IsCrouching,"Stood through ceiling");
            Object.DestroyImmediate(ceiling); ceiling=null; Physics.SyncTransforms();
            Require(motor.SetCrouching(false),"Clear standing rejected");
            for(int i=0;i<60;i++) motor.Tick(Vector2.zero,false,1f/60);
            Require(Mathf.Abs(body.height-1.8f)<.001f,"Standing height");
            var restView = camera.transform.localPosition;
            float walkBob = MeasureBob(motor, camera.transform, restView, false);
            Require(walkBob > .001f && walkBob < .02f, "Walking head bob missing or excessive");
            for(int i=0;i<90;i++) motor.Tick(Vector2.zero,false,1f/60);
            Require(Vector3.Distance(camera.transform.localPosition,restView)<.0001f,"Idle camera drift");
            motor.headBobIntensity=.5f;
            float halfBob=MeasureBob(motor,camera.transform,restView,false);
            Require(Mathf.Abs(halfBob/walkBob-.5f)<.03f,"Intensity does not scale bob");
            motor.headBobEnabled=false;
            for(int i=0;i<90;i++) motor.Tick(Vector2.up,false,1f/60);
            Require(Vector3.Distance(camera.transform.localPosition,restView)<.0001f,"Disabled bob did not settle while moving");
            motor.headBobEnabled=true; motor.headBobIntensity=0;
            Require(MeasureBob(motor,camera.transform,restView,true)<.0001f,"Zero intensity still bobs");
            motor.headBobIntensity=1;
            motor.UpdateStamina(false,10);
            float runBob=MeasureBob(motor,camera.transform,restView,true);
            Require(runBob>walkBob && runBob<.03f,"Sprint bob amplitude");
            var look=player.AddComponent<PlayerLook>(); look.view=camera.transform; typeof(PlayerLook).GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(look, null);
            look.ApplyLook(new Vector2(0,10000)); Require(Mathf.Abs(Mathf.DeltaAngle(0,camera.transform.localEulerAngles.x)+80)<.01f,"Pitch min");
            look.ApplyLook(new Vector2(0,-10000)); Require(Mathf.Abs(Mathf.DeltaAngle(0,camera.transform.localEulerAngles.x)-80)<.01f,"Pitch max");
            File.WriteAllText("RecoveryReports/fps-controller-check.txt","PASS: gravity, walk 3.5, normalized diagonal, sprint 6, crouch 1.8, capsule/camera lerp, fixed feet, ceiling blocks standing, stamina drain/delay/recovery/exhaustion/unlock, pitch -80/+80; head bob walking/sprint, idle return without drift, half intensity, disabled and zero intensity. Walk bob="+walkBob+", sprint bob="+runBob);
            EditorApplication.Exit(0);
        }
        catch(Exception e) { File.WriteAllText("RecoveryReports/fps-controller-check.txt","FAIL: "+e); Debug.LogException(e); EditorApplication.Exit(1); }
        finally { if(player)Object.DestroyImmediate(player); if(floor)Object.DestroyImmediate(floor); if(ceiling)Object.DestroyImmediate(ceiling); }
    }
    static float MeasureBob(PlayerMotor motor, Transform camera, Vector3 origin, bool sprint)
    {
        float peak=0;
        for(int i=0;i<60;i++)
        {
            motor.Tick(Vector2.up,sprint,1f/60);
            peak=Mathf.Max(peak,Vector3.Distance(camera.localPosition,origin));
        }
        return peak;
    }
}
