using System;
using UnityEngine;

[DefaultExecutionOrder(-40), DisallowMultipleComponent]
public sealed class TensionManager : MonoBehaviour
{
    public PlayerHealth player;
    public float dangerRadius = 25, riseSpeed = 2, fallSpeed = .3f;
    public float Tension { get; private set; }
    public float NearestDistance { get; private set; } = float.PositiveInfinity;
    public bool IsChased { get; private set; }
    public float Darkness { get; private set; }
    public float PlayerNoise { get; private set; }
    public event Action<float> OnTensionChanged;
    MonsterAI[] monsters = Array.Empty<MonsterAI>();
    Light[] lights = Array.Empty<Light>();
    float refreshAt;
    public bool ActiveRound => player && player.CanParticipate && GameStateManager.Instance && GameStateManager.Instance.State == GameState.Playing;
    void OnEnable() { NoiseManager.OnNoiseEmitted += Heard; refreshAt = 0; }
    void OnDisable() { NoiseManager.OnNoiseEmitted -= Heard; }
    void Heard(NoiseEvent noise)
    {
        if (ActiveRound && noise.owner && noise.owner.transform.IsChildOf(player.transform)) PlayerNoise = Mathf.Max(PlayerNoise, noise.loudness);
    }
    void Update()
    {
        if (Time.time >= refreshAt)
        {
            monsters = FindObjectsByType<MonsterAI>(); lights = FindObjectsByType<Light>(); refreshAt = Time.time + 1;
        }
        NearestDistance = float.PositiveInfinity; IsChased = false;
        float stateThreat = 0;
        if (ActiveRound)
        {
            foreach (var monster in monsters)
            {
                if (!monster || !monster.isActiveAndEnabled) continue;
                float distance = Vector3.Distance(player.transform.position, monster.transform.position);
                NearestDistance = Mathf.Min(NearestDistance, distance);
                bool chase = monster.CurrentState == MonsterState.Chase || monster.CurrentState == MonsterState.Attack;
                // Any chase suppresses scares, including one directed at a teammate.
                IsChased |= chase;
                if (chase) stateThreat = 1;
                else if (monster.CurrentState == MonsterState.Search || monster.CurrentState == MonsterState.Investigate)
                    stateThreat = Mathf.Max(stateThreat, .3f * Mathf.Clamp01(1-distance/dangerRadius));
            }
            Darkness = SampleDarkness();
        }
        PlayerNoise = Mathf.MoveTowards(PlayerNoise, 0, Time.deltaTime * .3f);
        Tick(ActiveRound ? Calculate(NearestDistance, stateThreat, PlayerNoise, Darkness, dangerRadius) : 0, Time.unscaledDeltaTime);
    }
    public static float Calculate(float distance, float state, float noise, float darkness, float radius = 25)
    {
        float proximity = Mathf.Clamp01(1-distance/Mathf.Max(1,radius));
        return Mathf.Clamp01(Mathf.Max(state, proximity*.75f + Mathf.Clamp01(noise)*.15f + Mathf.Clamp01(darkness)*.1f));
    }
    public void Tick(float target, float dt)
    {
        float previous = Tension;
        Tension = Mathf.Lerp(Tension, Mathf.Clamp01(target), 1-Mathf.Exp(-Mathf.Max(0,dt)*(target>Tension?riseSpeed:fallSpeed)));
        if (Mathf.Abs(Tension-previous)>.00001f) OnTensionChanged?.Invoke(Tension);
    }
    float SampleDarkness()
    {
        if (MonsterDarknessZone.Contains(player.transform.position)) return 1;
        var camera = player.GetComponentInChildren<Camera>(); Vector3 point = camera ? camera.transform.position : player.transform.position+Vector3.up;
        UnityEngine.Rendering.SphericalHarmonicsL2 probe; LightProbes.GetInterpolatedProbe(point,null,out probe);
        var directions = new[]{Vector3.up,Vector3.forward,Vector3.right}; var colors = new Color[3]; probe.Evaluate(directions,colors);
        float illumination = Mathf.Max(colors[0].grayscale,colors[1].grayscale,colors[2].grayscale);
        foreach (var light in lights)
        {
            if (!light || !light.isActiveAndEnabled || light.intensity<=0 || light.lightmapBakeType==LightmapBakeType.Baked) continue;
            Vector3 delta = light.transform.position-point; float distance = delta.magnitude;
            if (light.type != LightType.Point && light.type != LightType.Spot) continue;
            if (distance>=light.range) continue;
            if (light.type==LightType.Spot && Vector3.Angle(light.transform.forward,-delta)>light.spotAngle*.5f) continue;
            bool blocked=false;
            foreach(var hit in Physics.RaycastAll(point,delta.normalized,distance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                if(!hit.transform.IsChildOf(player.transform)&&!hit.transform.IsChildOf(light.transform)){blocked=true;break;}
            if(!blocked) illumination+=light.intensity*Mathf.Pow(1-distance/light.range,2)*light.color.grayscale;
        }
        return 1-Mathf.Clamp01(illumination);
    }
}
