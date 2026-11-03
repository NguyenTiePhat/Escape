using UnityEngine;

public enum FootstepSurface { Tile, Wood, Metal, Carpet }

[DisallowMultipleComponent, RequireComponent(typeof(PlayerMotor), typeof(CharacterController), typeof(AudioSource))]
public sealed class FootstepNoise : MonoBehaviour
{
    public float walkDistance = 1.7f, sprintDistance = 2.3f, crouchDistance = 1.2f;
    [Range(0, 1)] public float walkLoudness = .35f, sprintLoudness = .8f, crouchLoudness = .1f;
    public LayerMask surfaceMask = Physics.DefaultRaycastLayers;
    public AudioClip[] tileClips, woodClips, metalClips, carpetClips;
    public AudioSource audioSource;
    public FootstepSurface LastSurface { get; private set; }
    public AudioClip LastClip { get; private set; }
    public int StepCount { get; private set; }
    PlayerMotor motor;
    CharacterController body;
    Vector3 previous;
    float distance;
    int previousState;

    void Awake() { motor = GetComponent<PlayerMotor>(); body = GetComponent<CharacterController>(); if (!audioSource) audioSource = GetComponent<AudioSource>(); }
    void OnEnable() => ResetTracking();
    public void ResetTracking() { previous = transform.position; distance = 0; previousState = -1; }
    void LateUpdate() => SampleMovement(Time.deltaTime);
    public void SampleMovement(float deltaTime)
    {
        if (!motor || !body) Awake();
        Vector3 start = previous;
        Vector3 delta = Vector3.ProjectOnPlane(transform.position - previous, Vector3.up);
        previous = transform.position;
        if (deltaTime <= 0 || !motor.enabled || motor.InputBlocked || !body.enabled || !body.isGrounded)
        { distance = 0; return; }
        float travelled = delta.magnitude;
        // Teleports/reset-position commands are not footsteps.
        if (travelled > Mathf.Max(3f, motor.sprintSpeed * deltaTime * 2f)) { distance = 0; return; }
        int state = motor.IsCrouching ? 2 : motor.IsSprinting ? 1 : 0;
        if (state != previousState) { distance = 0; previousState = state; }
        if (travelled < .0001f) return;
        if (!TryGetSurface(out var hit)) { distance = 0; return; }
        float stride = Mathf.Max(.1f, state == 2 ? crouchDistance : state == 1 ? sprintDistance : walkDistance);
        float baseLoudness = state == 2 ? crouchLoudness : state == 1 ? sprintLoudness : walkLoudness;
        float before = distance;
        distance += travelled;
        int count = Mathf.FloorToInt((distance + .00001f) / stride);
        for (int i = 0; i < count; i++)
        {
            float fraction = Mathf.Clamp01((stride - before + i * stride) / travelled);
            Vector3 position = Vector3.Lerp(start, transform.position, fraction);
            position.y = hit.point.y;
            EmitStep(position, ResolveSurface(hit.collider), baseLoudness);
        }
        distance = Mathf.Max(0, distance - count * stride);
    }
    bool TryGetSurface(out RaycastHit nearest)
    {
        nearest = default;
        Vector3 origin = new Vector3(body.bounds.center.x, body.bounds.min.y + .15f, body.bounds.center.z);
        float closest = float.PositiveInfinity;
        foreach (var hit in Physics.RaycastAll(origin, Vector3.down, .5f, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform.IsChildOf(transform) || hit.normal.y < .3f || hit.distance >= closest) continue;
            nearest = hit; closest = hit.distance;
        }
        return closest < float.PositiveInfinity;
    }
    public static float SurfaceMultiplier(FootstepSurface surface) => surface == FootstepSurface.Metal ? 1.3f : surface == FootstepSurface.Carpet ? .6f : 1f;
    public static FootstepSurface ResolveSurface(Collider collider)
    {
        if (!collider) return FootstepSurface.Tile;
        for (Transform t = collider.transform; t; t = t.parent)
            if (TrySurface(t.tag, out var surface)) return surface;
        if (collider.sharedMaterial && TrySurface(collider.sharedMaterial.name.Replace(" (Instance)", ""), out var materialSurface)) return materialSurface;
        return FootstepSurface.Tile;
    }
    static bool TrySurface(string name, out FootstepSurface surface)
    {
        switch (name.ToLowerInvariant())
        {
            case "tile": case "gạch": surface = FootstepSurface.Tile; return true;
            case "wood": case "gỗ": surface = FootstepSurface.Wood; return true;
            case "metal": case "kim loại": surface = FootstepSurface.Metal; return true;
            case "carpet": case "thảm": surface = FootstepSurface.Carpet; return true;
            default: surface = FootstepSurface.Tile; return false;
        }
    }
    void EmitStep(Vector3 position, FootstepSurface surface, float baseLoudness)
    {
        LastSurface = surface; StepCount++;
        float loudness = Mathf.Clamp01(baseLoudness * SurfaceMultiplier(surface));
        NoiseManager.EmitNoise(position, loudness, NoiseSource.Footstep, gameObject);
        var clips = surface == FootstepSurface.Wood ? woodClips : surface == FootstepSurface.Metal ? metalClips : surface == FootstepSurface.Carpet ? carpetClips : tileClips;
        LastClip = clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
        if (LastClip && audioSource && audioSource.isActiveAndEnabled) audioSource.PlayOneShot(LastClip, loudness);
    }
}
