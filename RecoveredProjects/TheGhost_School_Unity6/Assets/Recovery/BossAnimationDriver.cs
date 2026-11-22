using UnityEngine;
using System.Collections.Generic;

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
    readonly Dictionary<string,Transform> joints=new Dictionary<string,Transform>();
    readonly Dictionary<Transform,Quaternion> offsets=new Dictionary<Transform,Quaternion>();
    readonly Dictionary<Transform,Quaternion> results=new Dictionary<Transform,Quaternion>();
    public int ExpressiveJointCount => joints.Count;
    void Awake()
    {
        string[] names={"Spine","Chest","Head","UpperArm.L","UpperArm.R","Forearm.L","Forearm.R","Hand.L","Hand.R","Thigh.L","Thigh.R","Shin.L","Shin.R"};
        foreach(var bone in GetComponentsInChildren<Transform>(true))foreach(var name in names)if(bone.name==name)joints[name]=bone;
    }
    void Update(){RestoreOffsets();}
    void RestoreOffsets()
    {
        foreach(var pair in offsets)if(pair.Key&&results.TryGetValue(pair.Key,out var last)&&Quaternion.Angle(pair.Key.localRotation,last)<.01f)pair.Key.localRotation*=Quaternion.Inverse(pair.Value);
        offsets.Clear();results.Clear();
    }
    void AddPose(string name,Vector3 degrees)
    {
        if(!joints.TryGetValue(name,out var bone)||!bone)return;
        var q=Quaternion.Euler(degrees);bone.localRotation*=q;offsets[bone]=q;results[bone]=bone.localRotation;
    }
    void FerociousPose()
    {
        if(!animator||!animator.enabled||Time.timeScale<=0)return;
        bool run=CurrentAction=="Run",lunge=CurrentAction=="Lunge";
        animator.speed=lunge?1.4f:run?1.3f:1;
        if(!run&&!lunge)return;
        float phase=Time.time*18, left=Mathf.Sin(phase),right=Mathf.Sin(phase+2.5f);
        AddPose("Spine",new Vector3(7,0,3));AddPose("Chest",new Vector3(5,Mathf.Sin(phase)*4,-5));
        AddPose("Head",new Vector3(-5,Mathf.Sin(phase*.43f)*5,9));
        AddPose("UpperArm.L",new Vector3(left*18,4,-12));AddPose("UpperArm.R",new Vector3(right*23,-7,17));
        AddPose("Forearm.L",new Vector3(12+left*12,0,-8));AddPose("Forearm.R",new Vector3(20+right*15,0,9));
        AddPose("Hand.L",new Vector3(15,0,-12));AddPose("Hand.R",new Vector3(-12,0,17));
        if(run){AddPose("Thigh.L",new Vector3(left*7,0,-3));AddPose("Thigh.R",new Vector3(right*9,0,4));AddPose("Shin.L",new Vector3(Mathf.Max(0,-left)*12,0,0));AddPose("Shin.R",new Vector3(Mathf.Max(0,-right)*15,0,0));}
    }
    void OnDisable(){RestoreOffsets();if(animator&&Time.timeScale>0)animator.speed=1;}

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
        FerociousPose();
        bool looking = hasAttention && (CurrentAction == "Alert" || CurrentAction == "Investigate" || CurrentAction == "Run" || CurrentAction == "Search");
        attentionWeight = Mathf.MoveTowards(attentionWeight, looking ? 1 : 0, Time.deltaTime * (CurrentAction == "Alert" ? 12 : 4));
        if (!head || attentionWeight <= 0 || !animator || !animator.enabled) return;
        Vector3 direction = Vector3.ProjectOnPlane(attentionPoint - head.position, Vector3.up);
        if (direction.sqrMagnitude < .001f) return;
        float yaw = Mathf.Clamp(Vector3.SignedAngle(transform.forward, direction, Vector3.up), -40, 40);
        head.rotation = Quaternion.AngleAxis(yaw * attentionWeight, Vector3.up) * head.rotation;
    }
}
