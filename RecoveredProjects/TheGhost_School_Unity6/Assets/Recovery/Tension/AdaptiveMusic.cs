using UnityEngine;

public sealed class AdaptiveMusic : MonoBehaviour
{
    public TensionManager tension;
    public AudioSource ambient, tense, chase;
    [Range(0,1)] public float masterVolume=.35f;
    void Start()
    {
        double start=AudioSettings.dspTime+.15;
        foreach(var source in new[]{ambient,tense,chase}) if(source&&source.clip){source.loop=true;source.volume=0;source.PlayScheduled(start);}
    }
    public static Vector3 Weights(float value)
    {
        float t=Mathf.Clamp01(value);
        return new Vector3(Mathf.Clamp01(1-t*2),1-Mathf.Abs(t*2-1),Mathf.Clamp01(t*2-1));
    }
    void Update()
    {
        Vector3 weight=Weights(tension?tension.Tension:0);
        if(!tension||!tension.ActiveRound)weight=Vector3.zero;
        float blend=1-Mathf.Exp(-2*Time.unscaledDeltaTime);
        if(ambient)ambient.volume=Mathf.Lerp(ambient.volume,weight.x*masterVolume,blend);
        if(tense)tense.volume=Mathf.Lerp(tense.volume,weight.y*masterVolume,blend);
        if(chase)chase.volume=Mathf.Lerp(chase.volume,weight.z*masterVolume,blend);
    }
    void OnDisable(){foreach(var source in new[]{ambient,tense,chase})if(source)source.Stop();}
}
