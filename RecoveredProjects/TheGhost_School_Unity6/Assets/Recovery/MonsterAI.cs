using System;
using UnityEngine;
using UnityEngine.AI;

public enum MonsterState { Patrol, Investigate, Chase, Search, Attack }

[DefaultExecutionOrder(-100), DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
public sealed class MonsterAI : MonoBehaviour, INoiseListener
{
    public MonsterConfig config;
    public Transform[] waypoints = Array.Empty<Transform>();
    public BossAnimationDriver animationDriver;
    public MonsterVision vision;
    public float Awareness { get; private set; }
    public bool CanSeePlayer { get; private set; }
    public MonsterState CurrentState { get; private set; } = MonsterState.Patrol;
    public bool IsWaiting { get; private set; }
    public int CurrentWaypointIndex { get; private set; } = -1;
    public float WaitRemaining => IsWaiting ? Mathf.Max(0, waitUntil - Time.time) : 0;
    public event Action<int, float> OnWaypointReached;
    public event Action<MonsterState> OnStateChanged;
    public Vector3 LastNoisePosition => lastNoisePosition;
    public float TargetNoiseLoudness => targetNoiseLoudness;
    public bool IsLookingAround { get; private set; }
    public float LookRemaining => IsLookingAround ? Mathf.Max(0, lookUntil - Time.time) : 0;
    public const float InvestigationLookDuration = 3f;
    [SerializeField] Vector3 lastNoisePosition;
    NavMeshAgent agent;
    NavMeshPath path;
    bool travelling;
    float waitUntil, retryAt, refreshAt;
    Vector3 destination;
    string animationName;
    string warning;
    float targetNoiseLoudness, lookUntil;
    bool hasNoiseTarget, resumePatrolTarget;
    int resumeWaypointIndex;
    Vector3 noiseDestination, navigationDestination;
    Vector3 lastSeenPosition;
    float chaseRefreshAt;
    float attackStarted, attackCooldown;
    Vector3 attackTarget;
    bool attackLaunched;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>(); path = new NavMeshPath();
        if (!animationDriver) animationDriver = GetComponent<BossAnimationDriver>();
        if (!vision) vision = GetComponent<MonsterVision>();
        // Only one component owns movement once AI is installed.
        var prototype = GetComponent<BossPrototypeMotion>(); if (prototype) prototype.enabled = false;
        var controller = GetComponent<CharacterController>(); if (controller) controller.enabled = false;
    }
    void OnEnable()
    {
        CurrentState = MonsterState.Patrol; CurrentWaypointIndex = -1; IsWaiting = false;
        travelling = false; retryAt = 0; animationName = null;
        hasNoiseTarget = false; targetNoiseLoudness = 0; IsLookingAround = false; resumePatrolTarget = false;
        Awareness = 0; CanSeePlayer = false;
        NoiseManager.RegisterListener(this, transform);
    }

    public void OnNoiseHeard(NoiseEvent e, float perceivedLoudness)
    {
        if (!isActiveAndEnabled || !config || !agent || !agent.enabled || !agent.isOnNavMesh) return;
        if (!float.IsFinite(perceivedLoudness) || !(perceivedLoudness > config.hearingThreshold)) return;
        if (!float.IsFinite(e.position.x) || !float.IsFinite(e.position.y) || !float.IsFinite(e.position.z)) return;
        if (e.owner && e.owner.transform.IsChildOf(transform)) return;
        if (CurrentState != MonsterState.Patrol && CurrentState != MonsterState.Search && CurrentState != MonsterState.Investigate) return;
        // Compare what this monster heard, not the source's unattenuated loudness.
        if (hasNoiseTarget && perceivedLoudness <= targetNoiseLoudness) return;
        // Voice/door noise can originate above the floor. Project vertically while
        // keeping the destination within .6m horizontally of the actual sound.
        // An unreachable sound must not replace an existing reachable target.
        if (!SetDestination(e.position, 2.5f)) return;
        if (CurrentState == MonsterState.Patrol)
        {
            resumePatrolTarget = travelling; resumeWaypointIndex = CurrentWaypointIndex;
        }
        lastNoisePosition = e.position; noiseDestination = navigationDestination;
        targetNoiseLoudness = perceivedLoudness; hasNoiseTarget = true;
        IsWaiting = false; IsLookingAround = false; travelling = false;
        if (animationDriver) animationDriver.SetAttentionPoint(lastNoisePosition);
        ChangeState(MonsterState.Investigate); PlayAnimation("Investigate");
    }
    void Update()
    {
        if (!agent) return;
        if (!config)
        {
            Stop(); WarnOnce("MonsterConfig is missing; patrol stopped."); return;
        }
        // Read the shared asset live: Inspector edits take effect on the next frame.
        float movementSpeed = CurrentState == MonsterState.Attack ? 6 : CurrentState == MonsterState.Chase ? config.chaseSpeed : config.patrolSpeed;
        agent.speed = float.IsFinite(movementSpeed) ? Mathf.Max(0, movementSpeed) : 0;
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            Animate(false); WarnOnce("Monster is not on a loaded NavMesh; patrol stopped."); return;
        }
        warning = null;
        UpdateVision();
        if (CurrentState == MonsterState.Chase) { UpdateChase(); return; }
        if (CurrentState == MonsterState.Attack) { UpdateAttack(); return; }
        if (CurrentState == MonsterState.Investigate) { UpdateInvestigation(); return; }
        // Search behavior will be expanded in a later part. Keep a real transition
        // so a new sound heard during Search can begin another investigation.
        if (CurrentState == MonsterState.Search) { ResumePatrol(); return; }
        if (CurrentState != MonsterState.Patrol) { Stop(); return; }
        if (waypoints == null || waypoints.Length == 0) { Stop(); IsWaiting = false; return; }
        if (IsWaiting)
        {
            Animate(false);
            if (Time.time < waitUntil) return;
            IsWaiting = false; travelling = false;
        }
        if (!travelling)
        {
            if (Time.time < retryAt) { Animate(false); return; }
            if (!FindNextDestination()) { retryAt = Time.time + 1; Stop(); return; }
        }
        if (agent.pathPending) { Animate(agent.velocity.sqrMagnitude > .0025f); return; }
        if (CurrentWaypointIndex >= waypoints.Length || !waypoints[CurrentWaypointIndex])
        {
            travelling = false; Stop(); return;
        }
        // Re-evaluate a waypoint moved by the designer during Play Mode.
        if (Time.time >= refreshAt && Vector3.Distance(destination, waypoints[CurrentWaypointIndex].position) > .3f)
        {
            refreshAt = Time.time + .5f;
            if (!SetDestination(waypoints[CurrentWaypointIndex].position)) { travelling = false; Stop(); return; }
        }
        if (agent.pathStatus != NavMeshPathStatus.PathComplete) { travelling = false; Stop(); retryAt = Time.time + .5f; return; }
        if (agent.remainingDistance <= agent.stoppingDistance + .04f && agent.velocity.sqrMagnitude < .01f)
        {
            agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero; travelling = false; IsWaiting = true;
            float pause = UnityEngine.Random.Range(2f, 4f); waitUntil = Time.time + pause;
            Animate(false); OnWaypointReached?.Invoke(CurrentWaypointIndex, pause); return;
        }
        Animate(agent.velocity.sqrMagnitude > .0025f);
    }
    bool FindNextDestination()
    {
        if (waypoints == null || waypoints.Length == 0) return false;
        for (int attempt = 0; attempt < waypoints.Length; attempt++)
        {
            CurrentWaypointIndex = (CurrentWaypointIndex + 1) % waypoints.Length;
            var point = waypoints[CurrentWaypointIndex];
            if (point && SetDestination(point.position)) { travelling = true; return true; }
        }
        return false;
    }
    void UpdateVision()
    {
        CanSeePlayer = vision && vision.CanSee(config);
        if (CanSeePlayer) lastSeenPosition = vision.player.transform.position;
        if (CurrentState == MonsterState.Chase || CurrentState == MonsterState.Attack) return;
        Awareness = Mathf.MoveTowards(Awareness, CanSeePlayer ? 1 : 0,
            Mathf.Max(0, CanSeePlayer ? config.awarenessGainPerSecond : config.awarenessLossPerSecond) * Time.deltaTime);
        if (Awareness < 1) return;
        IsWaiting = false; IsLookingAround = false; hasNoiseTarget = false; targetNoiseLoudness = 0;
        if (animationDriver) animationDriver.ClearAttention();
        Stop(); ChangeState(MonsterState.Chase);
    }
    void UpdateChase()
    {
        if (CanSeePlayer && Time.time >= attackCooldown && Vector3.ProjectOnPlane(lastSeenPosition - transform.position, Vector3.up).magnitude <= 2)
        {
            attackTarget = lastSeenPosition; attackStarted = Time.time; attackLaunched = false;
            Stop(); ChangeState(MonsterState.Attack); PlayAnimation("Lunge"); return;
        }
        if (Time.time >= chaseRefreshAt)
        {
            chaseRefreshAt = Time.time + .15f;
            if (!SetDestination(lastSeenPosition, 2.5f)) { Stop(); return; }
        }
        PlayAnimation(agent.velocity.sqrMagnitude > .01f ? "Run" : "Idle");
        if (!CanSeePlayer && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + .1f)
        {
            Awareness = 0; ResumePatrol();
        }
    }
    void UpdateAttack()
    {
        float elapsed = Time.time - attackStarted;
        // Matches the Lunge state's 1.6x playback: windup .1875s, thrust .25s.
        if (!attackLaunched && elapsed >= .1875f)
        {
            attackLaunched = true;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(attackTarget, out var point, .6f, filter) &&
                !NavMesh.Raycast(agent.nextPosition, point.position, out _, filter)) SetDestination(point.position);
        }
        agent.speed = 6;
        if (elapsed >= .4375f && agent.hasPath) { agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero; }
        if (elapsed >= .625f) { attackCooldown = Time.time + .65f; chaseRefreshAt = 0; ChangeState(MonsterState.Chase); }
    }
    void UpdateInvestigation()
    {
        if (IsLookingAround)
        {
            PlayAnimation("Search");
            if (Time.time >= lookUntil)
            {
                IsLookingAround = false; hasNoiseTarget = false; targetNoiseLoudness = 0;
                ChangeState(MonsterState.Search);
            }
            return;
        }
        if (agent.pathPending) return;
        if (agent.pathStatus != NavMeshPathStatus.PathComplete ||
            (!agent.hasPath && Vector3.Distance(agent.nextPosition, noiseDestination) > agent.stoppingDistance + .1f))
        {
            ResumePatrol(); return;
        }
        if (agent.remainingDistance <= agent.stoppingDistance + .04f && agent.velocity.sqrMagnitude < .01f)
        {
            agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero;
            IsLookingAround = true; lookUntil = Time.time + InvestigationLookDuration;
            if (animationDriver) animationDriver.ClearAttention();
            PlayAnimation("Search"); return;
        }
        PlayAnimation(agent.velocity.sqrMagnitude > .0025f ? "Investigate" : "Idle");
    }
    void ResumePatrol()
    {
        Stop(); IsWaiting = false; IsLookingAround = false; hasNoiseTarget = false; targetNoiseLoudness = 0;
        if (resumePatrolTarget) CurrentWaypointIndex = resumeWaypointIndex - 1;
        resumePatrolTarget = false; retryAt = 0;
        if (animationDriver) animationDriver.ClearAttention();
        ChangeState(MonsterState.Patrol);
    }
    void ChangeState(MonsterState state)
    {
        if (CurrentState == state) return;
        CurrentState = state; OnStateChanged?.Invoke(state);
    }
    bool SetDestination(Vector3 position, float sampleDistance = .6f)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(position, out var hit, sampleDistance, filter)) return false;
        if (Vector3.ProjectOnPlane(hit.position - position, Vector3.up).magnitude > .6f) return false;
        if (!NavMesh.CalculatePath(agent.nextPosition, hit.position, filter, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        if (!agent.SetPath(path)) return false;
        destination = position; navigationDestination = hit.position; agent.isStopped = false;
        return true;
    }
    void Stop()
    {
        if (agent && agent.enabled && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero; }
        travelling = false; Animate(false);
    }
    void Animate(bool moving)
    {
        PlayAnimation(moving ? "Walk" : "Idle");
    }
    void PlayAnimation(string next)
    {
        if (animationName == next) return;
        animationName = next; if (animationDriver) animationDriver.Play(next);
    }
    void WarnOnce(string message) { if (warning == message) return; warning = message; Debug.LogWarning(message, this); }
    void OnDisable()
    {
        NoiseManager.UnregisterListener(this); Stop(); IsWaiting = false; IsLookingAround = false;
        hasNoiseTarget = false; targetNoiseLoudness = 0;
        if (animationDriver) animationDriver.ClearAttention();
    }
}
