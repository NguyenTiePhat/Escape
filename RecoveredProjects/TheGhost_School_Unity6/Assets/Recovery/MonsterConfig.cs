using UnityEngine;

[CreateAssetMenu(menuName = "The Ghost/Monster Config", fileName = "MonsterConfig")]
public sealed class MonsterConfig : ScriptableObject
{
    [Min(0)] public float patrolSpeed = .3f;
    [Min(0)] public float chaseSpeed = 4.8f;
    [Range(0, 1)] public float hearingThreshold = .15f;
    [Min(0)] public float sightRange = 14;
    [Range(0, 360)] public float sightAngle = 100;
    [Min(0)] public float searchDuration = 8;
    [Min(0)] public float roarCooldown = 6f;
    [Min(.01f)] public float awarenessGainPerSecond = .5f;
    [Min(0)] public float awarenessLossPerSecond = .3f;
}
