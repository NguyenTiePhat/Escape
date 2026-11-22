using UnityEngine;

[CreateAssetMenu(menuName = "The Ghost/Monster Config", fileName = "MonsterConfig")]
public sealed class MonsterConfig : ScriptableObject
{
    [Min(0)] public float patrolSpeed = .3f;
    [Min(0)] public float chaseSpeed = 4.8f;
    [Range(0, 1)] public float hearingThreshold = .15f;
    [Tooltip("Hear emitted gameplay noises across the entire map without distance attenuation.")]
    public bool hearAcrossMap = true;
    [Min(0)] public float investigateSpeed = 3.8f;
    [Min(0)] public float noisePrioritySeconds = .6f;
    [Min(0)] public float sightRange = 14;
    [Range(0, 360)] public float sightAngle = 100;
    [Min(0)] public float searchDuration = 8;
    [Min(0)] public float roarCooldown = 6f;
    [Min(.01f)] public float awarenessGainPerSecond = .5f;
    [Min(0)] public float awarenessLossPerSecond = .3f;
    [Min(0)] public float chaseMemorySeconds = 4f;
    [Min(1)] public float chaseAcceleration = 28f;
    [Min(1)] public float chaseTurnSpeed = 360f;
    [Min(.1f)] public float attackRange = 1.8f;
    [Min(.1f)] public float attackHitDelay = .42f;
    [Min(.1f)] public float attackRecovery = .75f;
    [Min(0)] public float attackCooldownSeconds = .3f;
}
