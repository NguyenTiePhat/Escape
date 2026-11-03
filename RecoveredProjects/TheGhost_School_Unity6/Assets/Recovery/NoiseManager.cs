using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NoiseManager : MonoBehaviour
{
    public const float MaximumHearingDistance = 25f;
    public const float GizmoDuration = 1f;
    public bool debugLogging;
    public bool drawGizmos = true;
    public static event Action<NoiseEvent> OnNoiseEmitted;
    static NoiseManager instance;
    static readonly Dictionary<INoiseListener, Transform> listeners = new Dictionary<INoiseListener, Transform>();
    NoiseEvent latest;
    float emittedAt = float.NegativeInfinity;
    public NoiseEvent LatestNoise => latest;
    public bool HasRecentNoise => Time.unscaledTime - emittedAt < GizmoDuration;
    public static NoiseManager Instance
    {
        get
        {
            if (!instance) instance = FindAnyObjectByType<NoiseManager>();
            if (!instance) instance = new GameObject("Noise Manager").AddComponent<NoiseManager>();
            return instance;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; listeners.Clear(); OnNoiseEmitted = null; }
    void Awake()
    {
        if (instance && instance != this) { Destroy(this); return; }
        instance = this;
    }
    void OnDestroy() { if (instance == this) instance = null; }

    // Register in OnEnable, unregister in OnDisable. The transform is the hearing position.
    public static void RegisterListener(INoiseListener listener, Transform hearingPoint)
    {
        if (listener == null || !hearingPoint) throw new ArgumentNullException("listener/hearingPoint");
        listeners[listener] = hearingPoint;
    }
    public static void UnregisterListener(INoiseListener listener)
    {
        if (listener != null) listeners.Remove(listener);
    }
    public static void EmitNoise(Vector3 position, float loudness, NoiseSource source, GameObject owner = null)
    {
        if (!float.IsFinite(loudness) || !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z)) return;
        Instance.Dispatch(new NoiseEvent(position, loudness, source, owner));
    }
    void Dispatch(NoiseEvent e)
    {
        latest = e;
        emittedAt = Time.unscaledTime;
        if (OnNoiseEmitted != null)
            foreach (Action<NoiseEvent> callback in OnNoiseEmitted.GetInvocationList())
                try { callback(e); } catch (Exception exception) { Debug.LogException(exception, this); }
        float radius = e.loudness * MaximumHearingDistance;
        if (debugLogging) Debug.Log($"[Noise] {e.source} position={e.position} loudness={e.loudness:F2} radius={radius:F2}m owner={(e.owner ? e.owner.name : "none")}", this);
        if (radius <= 0) return;
        // A snapshot permits listeners to register, disable or emit another event in their callback.
        foreach (var entry in new List<KeyValuePair<INoiseListener, Transform>>(listeners))
        {
            if (!entry.Value || (entry.Key is UnityEngine.Object obj && !obj)) { listeners.Remove(entry.Key); continue; }
            if (!listeners.TryGetValue(entry.Key, out var current) || current != entry.Value) continue;
            if (!current.gameObject.activeInHierarchy || (entry.Key is Behaviour behaviour && !behaviour.isActiveAndEnabled)) continue;
            float distance = Vector3.Distance(e.position, current.position);
            if (distance > radius) continue;
            float perceived = e.loudness * Mathf.Clamp01(1f - distance / radius);
            if (debugLogging) Debug.Log($"[Noise heard] {current.name}: distance={distance:F2}m perceived={perceived:F3}", current);
            try { entry.Key.OnNoiseHeard(e, perceived); }
            catch (Exception exception) { Debug.LogException(exception, current); }
        }
    }
    void OnDrawGizmos()
    {
        if (!drawGizmos || !Application.isPlaying || !HasRecentNoise) return;
        Gizmos.color = new Color(1f, .65f, .1f, 1f);
        Gizmos.DrawWireSphere(latest.position, latest.loudness * MaximumHearingDistance);
    }
}
