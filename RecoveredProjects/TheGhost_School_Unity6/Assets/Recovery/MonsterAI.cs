using System;
using System.Collections.Generic;
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
    public AudioSource voice;
    public AudioClip roarClip;
    public AudioClip attackClip;
    public event Action<PlayerMotor> OnAttackHit;
    public static event Action<PlayerMotor> OnAnyAttackHit;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetAttackEvents() => OnAnyAttackHit = null;
    MonsterDoorNavigation doorNavigation;
    public PlayerMotor CurrentTarget { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public IReadOnlyList<Vector3> SearchPoints => searchPoints;
    public event Action OnChaseRoared;
    readonly List<Vector3> searchPoints = new List<Vector3>();
    PlayerMotor[] candidates = Array.Empty<PlayerMotor>();
    float candidatesRefreshAt, targetChangedAt, lastSightAt, searchStarted, heardUntil;
    float lastRoarAt = float.NegativeInfinity;
    int searchIndex;
    bool searchTravelling;
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
    PlayerMotor attackVictim;
    bool attackLaunched;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>(); path = new NavMeshPath();
        if (!animationDriver) animationDriver = GetComponent<BossAnimationDriver>();
        if (!vision) vision = GetComponent<MonsterVision>();
        // Only one component owns movement once AI is installed.
        var prototype = GetComponent<BossPrototypeMotion>(); if (prototype)
        {
            prototype.enabled = false;
            if (!voice) voice = prototype.voice;
            if (!roarClip) roarClip = prototype.roarClip;
        }
        if (!voice) voice = GetComponent<AudioSource>();
        doorNavigation = GetComponent<MonsterDoorNavigation>();
        if (!doorNavigation) doorNavigation = gameObject.AddComponent<MonsterDoorNavigation>();
        var controller = GetComponent<CharacterController>(); if (controller) controller.enabled = false;
    }
    void OnEnable()
    {
        CurrentState = MonsterState.Patrol; CurrentWaypointIndex = -1; IsWaiting = false;
        travelling = false; retryAt = 0; animationName = null;
        hasNoiseTarget = false; targetNoiseLoudness = 0; IsLookingAround = false; resumePatrolTarget = false;
        Awareness = 0; CanSeePlayer = false; CurrentTarget = null;
        candidatesRefreshAt = 0; chaseRefreshAt = 0; attackCooldown = 0;
        heardUntil = 0; searchPoints.Clear(); searchTravelling = false;
        NoiseManager.RegisterListener(this, transform);
    }

    public void OnNoiseHeard(NoiseEvent e, float perceivedLoudness)
    {
        if (!isActiveAndEnabled || !config || !agent || !agent.enabled || !agent.isOnNavMesh) return;
        if (!float.IsFinite(perceivedLoudness) || !(perceivedLoudness > config.hearingThreshold)) return;
        if (!float.IsFinite(e.position.x) || !float.IsFinite(e.position.y) || !float.IsFinite(e.position.z)) return;
        if (e.owner && e.owner.transform.IsChildOf(transform)) return;
        if (doorNavigation && e.owner && e.owner.GetComponent<Door>() == doorNavigation.OperatingDoor && doorNavigation.OperatingDoor) return;
        // Hearing remembers the emitted position, never the owner's live position through walls.
        if (CurrentState == MonsterState.Chase || CurrentState == MonsterState.Attack)
        {
            if (CanSeePlayer || (Time.time < heardUntil && perceivedLoudness <= targetNoiseLoudness)) return;
            var noisyPlayer = e.owner ? e.owner.GetComponentInParent<PlayerMotor>() : null;
            if (!noisyPlayer) return;
            CurrentTarget = noisyPlayer; targetChangedAt = Time.time;
            lastSeenPosition = e.position; LastKnownPosition = e.position;
            targetNoiseLoudness = perceivedLoudness; heardUntil = Time.time + 2f;
            return;
        }
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
        float movementSpeed = CurrentState == MonsterState.Attack ? 0 : CurrentState == MonsterState.Chase ? config.chaseSpeed : config.patrolSpeed;
        agent.speed = float.IsFinite(movementSpeed) ? Mathf.Max(0, movementSpeed) : 0;
        if (!agent.enabled || !agent.isOnNavMesh)
        {
            Animate(false); WarnOnce("Monster is not on a loaded NavMesh; patrol stopped."); return;
        }
        warning = null;
        UpdateVision();
        // Door waits must not freeze lost-sight or search timeout clocks.
        if (CurrentState == MonsterState.Chase && !CanSeePlayer && Time.time - lastSightAt > 1.5f) BeginSearch(lastSeenPosition);
        if (CurrentState == MonsterState.Search && Time.time - searchStarted >= Mathf.Max(0, config.searchDuration))
        { Awareness = 0; CurrentTarget = null; ResumePatrol(); }
        if (CurrentState != MonsterState.Attack && doorNavigation.Tick(agent)) { PlayAnimation("Idle"); return; }
        if (CurrentState == MonsterState.Chase) { UpdateChase(); return; }
        if (CurrentState == MonsterState.Attack) { UpdateAttack(); return; }
        if (CurrentState == MonsterState.Investigate) { UpdateInvestigation(); return; }
        if (CurrentState == MonsterState.Search) { UpdateSearch(); return; }
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
        if (Time.time >= candidatesRefreshAt)
        {
            candidates = FindObjectsByType<PlayerMotor>();
            candidatesRefreshAt = Time.time + .5f;
        }
        bool currentVisible = vision && CurrentTarget && vision.CanSee(config, CurrentTarget);
        PlayerMotor nearest = currentVisible ? CurrentTarget : null;
        float nearestDistance = nearest ? (nearest.transform.position - transform.position).sqrMagnitude : float.PositiveInfinity;
        foreach (var candidate in candidates)
        {
            if (!candidate || candidate == CurrentTarget || !vision) continue;
            float distance = (candidate.transform.position - transform.position).sqrMagnitude;
            if (distance >= nearestDistance || !vision.CanSee(config, candidate)) continue;
            nearest = candidate; nearestDistance = distance;
        }
        // Switch only for a target at least 25% closer, after holding for .75s.
        if (nearest && nearest != CurrentTarget && (!currentVisible ||
            (Time.time - targetChangedAt >= .75f && nearestDistance <
             (CurrentTarget.transform.position - transform.position).sqrMagnitude * .5625f)))
        {
            CurrentTarget = nearest; targetChangedAt = Time.time; currentVisible = true;
        }
        CanSeePlayer = currentVisible;
        if (CanSeePlayer)
        {
            lastSeenPosition = CurrentTarget.transform.position;
            LastKnownPosition = lastSeenPosition; lastSightAt = Time.time;
        }
        if (CurrentState == MonsterState.Chase || CurrentState == MonsterState.Attack) return;
        Awareness = Mathf.MoveTowards(Awareness, CanSeePlayer ? 1 : 0,
            Mathf.Max(0, CanSeePlayer ? config.awarenessGainPerSecond : config.awarenessLossPerSecond) * Time.deltaTime);
        if (CanSeePlayer && CurrentState == MonsterState.Search) Awareness = 1;
        if (!CanSeePlayer || Awareness < 1) return;
        BeginChase();
    }
    void BeginChase()
    {
        if (CurrentState == MonsterState.Patrol)
        {
            resumePatrolTarget = travelling; resumeWaypointIndex = CurrentWaypointIndex;
        }
        IsWaiting = false; IsLookingAround = false; hasNoiseTarget = false; targetNoiseLoudness = 0;
        if (animationDriver) animationDriver.ClearAttention();
        Stop(); chaseRefreshAt = 0; ChangeState(MonsterState.Chase);
        if (Time.time - lastRoarAt >= Mathf.Max(0, config.roarCooldown) && voice && roarClip)
        {
            lastRoarAt = Time.time; voice.PlayOneShot(roarClip); OnChaseRoared?.Invoke();
        }
    }
    void UpdateChase()
    {
        agent.speed = Mathf.Max(0, config.chaseSpeed);
        if (!CanSeePlayer && Time.time - lastSightAt > 1.5f)
        {
            BeginSearch(lastSeenPosition); return;
        }
        if (CanSeePlayer && Time.time >= attackCooldown && Vector3.Distance(CurrentTarget.transform.position, transform.position) < 1.5f)
        {
            attackVictim = CurrentTarget; attackStarted = Time.time; attackLaunched = false;
            Stop(); ChangeState(MonsterState.Attack); PlayAnimation("Attack");
            if (voice && (attackClip || roarClip)) voice.PlayOneShot(attackClip ? attackClip : roarClip);
            return;
        }
        if (Time.time >= chaseRefreshAt)
        {
            chaseRefreshAt = Time.time + .2f;
            if (!SetDestination(lastSeenPosition, 2.5f)) { Stop(); return; }
        }
        PlayAnimation(agent.velocity.sqrMagnitude > .01f ? "Run" : "Idle");
    }
    void UpdateAttack()
    {
        // Commit to the victim selected at windup; never transfer a hit to another player.
        agent.speed = 0; agent.isStopped = true; agent.velocity = Vector3.zero;
        float elapsed = Time.time - attackStarted;
        if (!attackLaunched && elapsed >= .6f)
        {
            attackLaunched = true;
            if (attackVictim && attackVictim.gameObject.activeInHierarchy &&
                Vector3.Distance(attackVictim.transform.position, transform.position) < 1.5f &&
                vision && vision.CanSee(config, attackVictim))
            {
                var health = attackVictim.GetComponent("PlayerHealth");
                if (health) health.SendMessage("Kill", SendMessageOptions.RequireReceiver);
                else Debug.Log("Monster Attack: Kill " + attackVictim.name + " (PlayerHealth not installed yet).", attackVictim);
                OnAttackHit?.Invoke(attackVictim);
                OnAnyAttackHit?.Invoke(attackVictim);
            }
        }
        if (elapsed >= .8f)
        {
            attackVictim = null; attackCooldown = Time.time + .65f;
            chaseRefreshAt = 0; ChangeState(MonsterState.Chase);
        }
    }
    void UpdateInvestigation()
    {
        if (IsLookingAround)
        {
            PlayAnimation("Search");
            if (Time.time >= lookUntil)
            {
                IsLookingAround = false; hasNoiseTarget = false; targetNoiseLoudness = 0;
                BeginSearch(lastNoisePosition);
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
    void BeginSearch(Vector3 center)
    {
        Stop(); LastKnownPosition = center; searchStarted = Time.time;
        IsWaiting = false; IsLookingAround = false; hasNoiseTarget = false; targetNoiseLoudness = 0;
        searchTravelling = false; searchIndex = 0; searchPoints.Clear();
        if (animationDriver) animationDriver.ClearAttention();
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        int count = UnityEngine.Random.Range(3, 6);
        var candidatePath = new NavMeshPath();
        for (int attempt = 0; attempt < 100 && searchPoints.Count < count; attempt++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 8f;
            Vector3 candidate = center + new Vector3(offset.x, 0, offset.y);
            if (!NavMesh.SamplePosition(candidate, out var hit, 2f, filter) ||
                Vector3.Distance(hit.position, center) > 8f) continue;
            if (searchPoints.Exists(point => (point - hit.position).sqrMagnitude < 1f)) continue;
            if (!NavMesh.CalculatePath(agent.nextPosition, hit.position, filter, candidatePath) ||
                candidatePath.status != NavMeshPathStatus.PathComplete) continue;
            searchPoints.Add(hit.position);
        }
        // Approach the remembered position before visiting the random search points.
        searchTravelling = SetDestination(center, 2.5f);
        ChangeState(MonsterState.Search);
    }
    void UpdateSearch()
    {
        if (Time.time - searchStarted >= Mathf.Max(0, config.searchDuration))
        {
            Awareness = 0; CurrentTarget = null; ResumePatrol(); return;
        }
        if (IsLookingAround)
        {
            PlayAnimation("Search");
            transform.Rotate(0, 65f * Time.deltaTime, 0);
            if (Time.time < lookUntil) return;
            IsLookingAround = false;
        }
        if (searchTravelling)
        {
            if (agent.pathPending) return;
            if (agent.pathStatus == NavMeshPathStatus.PathComplete && agent.hasPath &&
                agent.remainingDistance > agent.stoppingDistance + .08f)
            {
                PlayAnimation(agent.velocity.sqrMagnitude > .0025f ? "Investigate" : "Idle"); return;
            }
            Stop(); searchTravelling = false;
            IsLookingAround = true; lookUntil = Time.time + 1.2f; PlayAnimation("Search"); return;
        }
        while (searchIndex < searchPoints.Count)
        {
            if (SetDestination(searchPoints[searchIndex++])) { searchTravelling = true; return; }
        }
        IsLookingAround = true; lookUntil = Time.time + 1.2f; PlayAnimation("Search");
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
