using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JumpMotionValidation
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = Vector3.down * .5f;
        floor.transform.localScale = new Vector3(50,1,50);
        var go = new GameObject("Jump probe");
        go.transform.position = Vector3.up * .05f;
        var cc = go.AddComponent<CharacterController>();
        cc.height=1.8f; cc.center=Vector3.up*.9f; cc.skinWidth=.01f;
        var motor = go.AddComponent<PlayerMotor>(); motor.SendMessage("Awake");
        Physics.SyncTransforms();
        Action step = () => motor.MoveGrounded(Vector3.zero,.01f);
        for(int i=0;i<30;i++) step();
        int impacts=0; float noise=0;
        NoiseManager.OnNoiseEmitted += e => { if(e.source==NoiseSource.Impact){impacts++;noise=e.loudness;} };
        float start=go.transform.position.y, peak=start; int frames=0, peakFrame=0;
        motor.TryJump();
        do { step(); frames++; if(go.transform.position.y>peak){peak=go.transform.position.y;peakFrame=frames;} }
        while(!cc.isGrounded && frames<200);
        Check(peak-start>.8f && peak-start<.95f,"height");
        Check(frames<55 && frames-peakFrame<peakFrame,"fast descent");
        Check(impacts==1 && noise>0 && noise<=1,"landing noise");
        for(int i=0;i<20;i++) step(); Check(impacts==1,"no idle noise");
        motor.TryJump(); bool queued=false, landed=false, bounced=false;
        for(int i=0;i<100;i++) {
            step();
            if(i>30 && !queued && !cc.isGrounded && go.transform.position.y<start+.25f){motor.TryJump();queued=true;}
            if(queued && cc.isGrounded) landed=true;
            else if(landed && go.transform.position.y>start+.05f){bounced=true;break;}
        }
        Check(bounced,"jump buffer");
        for(int i=0;i<120;i++) step();
        floor.SetActive(false); Physics.SyncTransforms();
        for(int i=0;i<5;i++) step();
        float edgeY=go.transform.position.y; motor.TryJump(); step();
        Check(go.transform.position.y>edgeY,"coyote jump");
        for(int i=0;i<15;i++) step();
        float before=go.transform.position.y; motor.TryJump(); step();
        Check(go.transform.position.y-before<.04f,"no double jump");
        motor.InputBlocked=true; Check(!motor.TryJump(),"blocked input");
        File.WriteAllText("RecoveryReports/jump-motion.txt",$"PASS height={peak-start:F3}m, airtime={frames*.01f:F2}s, ascent={peakFrame*.01f:F2}s, descent={(frames-peakFrame)*.01f:F2}s; landing noise={noise:F3}; no idle noise; buffer; coyote; no double jump; input lock.\n");
        EditorApplication.Exit(0);
    }
    static void Check(bool ok,string name){if(!ok)throw new Exception("Jump validation: "+name);}
}
