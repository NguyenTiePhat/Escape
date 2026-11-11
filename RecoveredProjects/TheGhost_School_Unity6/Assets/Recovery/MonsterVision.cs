using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterVision : MonoBehaviour
{
    public Transform eyes;
    public PlayerMotor player;
    public LayerMask obstacles = ~0;
    [Tooltip("Gameplay darkness flag supplied by room/lighting logic; not inferred from screen brightness.")]
    public bool playerInCompleteDarkness;
    public float EffectiveRange { get; private set; }
    CharacterController body;
    Light beam;
    PlayerMotor cachedPlayer;
    public bool CanSee(MonsterConfig config) => CanSee(config, player);
    public bool CanSee(MonsterConfig config, PlayerMotor player)
    {
        if (!config || !player || !player.gameObject.activeInHierarchy) return false;
        if (cachedPlayer != player) { cachedPlayer = player; body = player.GetComponent<CharacterController>(); beam = null; }
        if (!beam)
        {
            var t = player.transform.Find("First person arms/Flashlight beam"); if (t) beam = t.GetComponent<Light>();
        }
        bool flashlight = beam && beam.isActiveAndEnabled && beam.intensity > 0;
        bool dark = playerInCompleteDarkness || MonsterDarknessZone.Contains(player.transform.position);
        EffectiveRange = Mathf.Max(0, config.sightRange) * (player.IsCrouching ? .6f : 1) * (flashlight ? 1.5f : dark ? .4f : 1);
        Vector3 origin = eyes ? eyes.position : transform.position + Vector3.up * 1.9f;
        // Bone orientation is import-dependent; the actor's forward defines its cone.
        Vector3 torso = body ? body.bounds.center : player.transform.position + Vector3.up;
        Vector3 head = player.view ? player.view.position : torso + Vector3.up * .6f;
        return VisiblePoint(origin, head, config.sightAngle, player) || VisiblePoint(origin, torso, config.sightAngle, player);
    }
    bool VisiblePoint(Vector3 origin, Vector3 point, float angle, PlayerMotor player)
    {
        Vector3 delta = point - origin; float distance = delta.magnitude;
        if (distance > EffectiveRange || EffectiveRange <= 0) return false;
        if (distance > .001f && Vector3.Angle(transform.forward, delta) > Mathf.Clamp(angle, 0, 360) * .5f) return false;
        foreach (var hit in Physics.RaycastAll(origin, delta.normalized, distance, obstacles, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(player.transform)) continue;
            return false;
        }
        return true;
    }
}
