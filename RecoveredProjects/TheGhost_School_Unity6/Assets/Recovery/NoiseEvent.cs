using UnityEngine;

public enum NoiseSource { Footstep, Door, Drop, Voice, Impact }

public struct NoiseEvent
{
    public Vector3 position;
    public float loudness;
    public NoiseSource source;
    public GameObject owner;

    public NoiseEvent(Vector3 position, float loudness, NoiseSource source, GameObject owner)
    {
        this.position = position;
        this.loudness = Mathf.Clamp01(loudness);
        this.source = source;
        this.owner = owner;
    }
}
