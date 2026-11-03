using UnityEngine;

public sealed class NoiseTestListener : MonoBehaviour, INoiseListener
{
    public int HeardCount { get; private set; }
    public float LastPerceivedLoudness { get; private set; }
    public NoiseEvent LastEvent { get; private set; }
    void OnEnable() => NoiseManager.RegisterListener(this, transform);
    void OnDisable() => NoiseManager.UnregisterListener(this);
    public void OnNoiseHeard(NoiseEvent e, float perceivedLoudness)
    {
        HeardCount++;
        LastEvent = e;
        LastPerceivedLoudness = perceivedLoudness;
    }
}
