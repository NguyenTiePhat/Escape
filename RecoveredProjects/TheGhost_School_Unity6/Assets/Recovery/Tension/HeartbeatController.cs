using System.Collections.Generic;
using UnityEngine;

public sealed class HeartbeatController : MonoBehaviour
{
    public TensionManager tension;
    public AudioSource heartbeat;
    public float BeatsPerMinute { get; private set; }
    float nextBeat, strength, refreshAt;
    readonly Dictionary<AudioLowPassFilter,float> filters=new Dictionary<AudioLowPassFilter,float>();
    readonly List<AudioLowPassFilter> owned=new List<AudioLowPassFilter>();
    public static float Proximity(float distance)=>Mathf.Clamp01(1-distance/12f);
    void Update()
    {
        float target=tension&&tension.ActiveRound?Proximity(tension.NearestDistance):0;
        strength=Mathf.Lerp(strength,target,1-Mathf.Exp(-3*Time.unscaledDeltaTime));
        BeatsPerMinute=Mathf.Lerp(65,145,strength);
        if(heartbeat){heartbeat.volume=strength*.4f*TensionSettings.Heartbeat*(TensionSettings.Reduced?.1f:1);
            if(target>0&&Time.time>=nextBeat){if(heartbeat.clip)heartbeat.PlayOneShot(heartbeat.clip);nextBeat=Time.time+60/BeatsPerMinute;}}
        if(Time.unscaledTime>=refreshAt){Discover();refreshAt=Time.unscaledTime+2;}
        float amount=TensionSettings.Muffle&&!TensionSettings.Reduced?strength:0;
        foreach(var pair in filters)if(pair.Key)pair.Key.cutoffFrequency=Mathf.Lerp(pair.Value,Mathf.Min(pair.Value,3800),amount*.65f);
    }
    void Discover()
    {
        foreach(var source in FindObjectsByType<AudioSource>())
        {
            // Only environmental 3D sounds; never the heartbeat or music layers.
            if(source.spatialBlend<.5f||source==heartbeat)continue;
            var filter=source.GetComponent<AudioLowPassFilter>();
            if(!filter){filter=source.gameObject.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=22000;owned.Add(filter);}
            if(!filters.ContainsKey(filter))filters.Add(filter,filter.cutoffFrequency);
        }
    }
    void OnDisable(){if(heartbeat)heartbeat.Stop();foreach(var pair in filters)if(pair.Key)pair.Key.cutoffFrequency=pair.Value;foreach(var filter in owned)if(filter)Destroy(filter);filters.Clear();owned.Clear();}
}
