using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ScareDirector : MonoBehaviour
{
    public TensionManager tension;
    public ScareEvent[] events=Array.Empty<ScareEvent>();
    [Min(90)] public float globalCooldown=90;
    public float LastScareTime {get;private set;}=float.NegativeInfinity;
    readonly HashSet<string> played=new HashSet<string>();
    ScareEvent active;
    float beganAt;
    public static bool Allowed(bool playing,bool chased,float value,float threshold,float now,float last,float cooldown)
        => playing&&!chased&&value<=threshold&&now-last>=Mathf.Max(90,cooldown);
    void Start(){foreach(var e in events)if(e)e.ResetRound();}
    void Update()
    {
        bool allowed=tension&&tension.ActiveRound&&!tension.IsChased&&TensionSettings.Scares&&!TensionSettings.Reduced;
        // Recheck AI directly: never start during the frame a chase begins.
        if(allowed)foreach(var monster in FindObjectsByType<MonsterAI>())
            if(monster.isActiveAndEnabled&&(monster.CurrentState==MonsterState.Chase||monster.CurrentState==MonsterState.Attack)){allowed=false;break;}
        if(active)
        {
            float elapsed=Time.time-beganAt;
            if(!allowed||elapsed>2){active.Finish();active=null;}
            else if(active.kind==ScareKind.Shadow)active.shadow.position=Vector3.Lerp(active.shadowStart.position,active.shadowEnd.position,Mathf.Clamp01(elapsed/.8f));
        }
        if(!tension||!tension.player)return;
        foreach(var e in events)
        {
            if(!e)continue;
            bool entered=e.Entered(tension.player.transform.position);
            if(entered&&allowed&&!active)TryTrigger(e,Time.time);
        }
    }
    public bool TryTrigger(ScareEvent e,float now)
    {
        if(!e||!tension||!TensionSettings.Scares||TensionSettings.Reduced||active||played.Contains(e.eventId)||!e.Ready)return false;
        if(!Allowed(tension.ActiveRound,tension.IsChased,tension.Tension,e.maximumTension,now,LastScareTime,globalCooldown))return false;
        foreach(var monster in FindObjectsByType<MonsterAI>())if(monster.isActiveAndEnabled&&(monster.CurrentState==MonsterState.Chase||monster.CurrentState==MonsterState.Attack))return false;
        if(!e.Begin())return false;
        played.Add(e.eventId);LastScareTime=now;beganAt=Time.time;active=e;return true;
    }
    void OnDisable(){if(active)active.Finish();active=null;}
}
