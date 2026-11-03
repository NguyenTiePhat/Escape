using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(Rigidbody))]
public sealed class NoiseOnImpact : MonoBehaviour
{
    [Min(0)] public float minimumSpeed = .2f;
    [Min(0)] public float cooldown = .12f;
    public GameObject owner;
    public int EmissionCount { get; private set; }
    Rigidbody body;
    float lastEmission = float.NegativeInfinity;
    void Awake() => body = GetComponent<Rigidbody>();
    void OnEnable() => lastEmission = float.NegativeInfinity;
    public static float CalculateLoudness(float mass, float relativeSpeed)
    {
        if (!float.IsFinite(mass) || !float.IsFinite(relativeSpeed)) return .1f;
        float energy = .5f * Mathf.Max(0, mass) * relativeSpeed * relativeSpeed;
        return Mathf.Clamp(.1f + .08f * Mathf.Sqrt(energy), .1f, 1f);
    }
    void OnCollisionEnter(Collision collision)
    {
        if (!body) body = GetComponent<Rigidbody>();
        if (!body || body.isKinematic || collision.relativeVelocity.magnitude < minimumSpeed || Time.time - lastEmission < cooldown) return;
        lastEmission = Time.time;
        var position = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
        NoiseManager.EmitNoise(position, CalculateLoudness(body.mass, collision.relativeVelocity.magnitude), NoiseSource.Impact, owner ? owner : gameObject);
        EmissionCount++;
    }
    public static void Prepare(Rigidbody body, GameObject owner)
    {
        var impact = body.GetComponent<NoiseOnImpact>();
        if (!impact) impact = body.gameObject.AddComponent<NoiseOnImpact>();
        impact.owner = owner;
    }
}
