using UnityEngine;

// Animation-only boundary: navigation/perception can request actions without editing the rig.
[DisallowMultipleComponent]
public sealed class BossAnimationDriver : MonoBehaviour
{
    public Animator animator;
    public Transform head;
    public float blendSeconds = .12f;
    public string CurrentAction { get; private set; } = "Idle";
    Vector3 attentionPoint;
    bool hasAttention;
    float attentionWeight;

    public void Play(string action)
    {
        if (!animator || !animator.runtimeAnimatorController) return;
        if (action == "Attack") action = "Lunge";
        int hash = Animator.StringToHash("Base Layer." + action);
        if (!animator.HasState(0, hash)) { Debug.LogWarning("Missing boss animation: " + action, this); return; }
        CurrentAction = action;
        animator.CrossFadeInFixedTime(hash, action == "Alert" ? .04f : blendSeconds, 0, 0);
    }
    public void SetAttentionPoint(Vector3 position) { attentionPoint = position; hasAttention = true; }
    public void ClearAttention() { hasAttention = false; }
    public void AlertAt(Vector3 position)
    {
        SetAttentionPoint(position);
        var motion = GetComponent<BossPrototypeMotion>();
        if (motion) motion.SetState(BossPrototypeMotion.MotionState.Alert); else Play("Alert");
    }
    void LateUpdate()
    {
        bool looking = hasAttention && (CurrentAction == "Alert" || CurrentAction == "Investigate" || CurrentAction == "Run" || CurrentAction == "Search");
        attentionWeight = Mathf.MoveTowards(attentionWeight, looking ? 1 : 0, Time.deltaTime * (CurrentAction == "Alert" ? 12 : 4));
        if (!head || attentionWeight <= 0 || !animator || !animator.enabled) return;
        Vector3 direction = Vector3.ProjectOnPlane(attentionPoint - head.position, Vector3.up);
        if (direction.sqrMagnitude < .001f) return;
        float yaw = Mathf.Clamp(Vector3.SignedAngle(transform.forward, direction, Vector3.up), -40, 40);
        head.rotation = Quaternion.AngleAxis(yaw * attentionWeight, Vector3.up) * head.rotation;
    }
}
