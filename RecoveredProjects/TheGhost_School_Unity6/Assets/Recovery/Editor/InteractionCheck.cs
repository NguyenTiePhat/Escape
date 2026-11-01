using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
public static class InteractionCheck
{
    static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
    public static void Run()
    {
        try
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var player=new GameObject("Test player"); var camera=new GameObject("Camera").AddComponent<Camera>();camera.transform.SetParent(player.transform,false);
            var interactor=player.AddComponent<PlayerInteractor>();interactor.view=camera;interactor.interactableMask=1<<13;
            var near=Cube("Near",new Vector3(0,0,1),13);var a=near.AddComponent<TestInteractable>();
            var far=Cube("Far",new Vector3(0,0,2),13);var b=far.AddComponent<TestInteractable>();
            int events=0;interactor.OnTargetChanged+=target=>events++;
            Physics.SyncTransforms();interactor.RefreshTarget();Require(ReferenceEquals(interactor.Target,a)&&events==1,"Nearest target/event");
            interactor.RefreshTarget();Require(events==1,"Unchanged target repeated event");
            interactor.InteractWithTarget();Require(a.InteractionCount==1&&b.InteractionCount==0,"Interact wrong target or duplicate");
            camera.transform.rotation=Quaternion.Euler(0,90,0);interactor.RefreshTarget();Require(interactor.Target==null&&events==2,"Look away clear");
            camera.transform.rotation=Quaternion.identity;near.layer=0;interactor.obstructionMask=0;Physics.SyncTransforms();interactor.RefreshTarget();Require(ReferenceEquals(interactor.Target,b),"Layer filtering");
            near.layer=13;Object.DestroyImmediate(a);Physics.SyncTransforms();interactor.RefreshTarget();Require(ReferenceEquals(interactor.Target,b),"Collider without interface masked valid target");
            interactor.obstructionMask=Physics.DefaultRaycastLayers;near.layer=0;Physics.SyncTransforms();interactor.RefreshTarget();Require(interactor.Target==null,"Wall occlusion");
            Object.DestroyImmediate(near);far.transform.position=new Vector3(0,0,2.7f);Physics.SyncTransforms();interactor.RefreshTarget();Require(interactor.Target==null,"Range must be 2.5m from camera");
            far.transform.position=new Vector3(0,0,2);Physics.SyncTransforms();interactor.RefreshTarget();Require(ReferenceEquals(interactor.Target,b),"Target reacquisition");
            far.SetActive(false);interactor.RefreshTarget();Require(interactor.Target==null,"Inactive target clear");
            far.SetActive(true);interactor.InputBlocked=true;interactor.InteractWithTarget();Require(interactor.Target==null&&b.InteractionCount==0,"Modal input blocked");
            File.WriteAllText("RecoveryReports/interaction-check.txt","PASS: nearest interface, change-only event, look-away clears, exactly one Interact, layer filtering, skips non-interactable collider, wall occlusion, camera-origin 2.5m range, inactive target, modal input blocked.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText("RecoveryReports/interaction-check.txt","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static GameObject Cube(string name,Vector3 position,int layer){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=position;g.transform.localScale=Vector3.one*.2f;g.layer=layer;return g;}
}
