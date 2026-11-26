using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
public sealed class PlayerMotor : MonoBehaviour
{
    public Transform view;
    [Min(0)] public float walkSpeed = 3.5f, sprintSpeed = 6f, crouchSpeed = 1.8f;
    [Min(0)] public float crouchHeight = 1.1f, crouchLerpSpeed = 14f;
    [Header("Jump / landing")]
    [Tooltip("Downward acceleration in m/s². More negative gives a snappier jump.")]
    public float gravity = -25f;
    [Tooltip("Target jump height in metres."), Min(.1f)] public float jumpHeight = .9f;
    [Tooltip("Gravity multiplier while falling."), Min(1)] public float fallMultiplier = 1.8f;
    [Tooltip("Seconds after leaving an edge during which jumping is allowed."), Min(0)] public float coyoteTime = .1f;
    [Tooltip("Seconds to remember a jump pressed before landing."), Min(0)] public float jumpBuffer = .1f;
    [Tooltip("Landing noise per m/s of downward speed; total noise is capped at 1."), Min(0)] public float landingNoisePerSpeed = .08f;
    public LayerMask standingObstacles = ~0;
    [Header("Head bob / comfort")]
    public bool headBobEnabled = true;
    [Range(0, 1)] public float headBobIntensity = 1f;
    [SerializeField, Min(0)] float walkBobAmplitude = .012f;
    [SerializeField, Min(0)] float sprintBobAmplitude = .02f;
    [SerializeField, Min(0)] float walkBobFrequency = 1.8f;
    [SerializeField, Min(0)] float sprintBobFrequency = 2.6f;
    public bool IsSprinting { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool InputBlocked { get; set; }
    public bool ActionMovementLocked { get; set; }
    CharacterController body;
    float standingHeight, fallSpeed;
    float coyoteRemaining = -1, jumpRemaining = -1;
    Vector3 standingCenter, standingView;
    Vector3 cameraBasePosition, bobOffset;
    float bobPhase;
    void Awake()
    {
        body = GetComponent<CharacterController>();
        standingHeight = body.height;
        standingCenter = body.center;
    }
    void Start()
    {
        if (!view) { var camera = GetComponentInChildren<Camera>(); if (camera) view = camera.transform; }
        if (view) cameraBasePosition = standingView = view.localPosition;
    }
    void Update()
    {
        var keyboard = Keyboard.current;
        bool input = !InputBlocked && Application.isFocused && Cursor.lockState == CursorLockMode.Locked && keyboard != null;
        Vector2 move = Vector2.zero;
        if (input)
        {
            move = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
            if (keyboard.cKey.wasPressedThisFrame) SetCrouching(!IsCrouching);
            if (keyboard.spaceKey.wasPressedThisFrame) TryJump();
        }
        Tick(move, input && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed), Time.deltaTime);
    }
    public bool SetCrouching(bool crouching)
    {
        if (!crouching && !CanStand()) return false;
        IsCrouching = crouching;
        return true;
    }
    public bool TryJump()
    {
        if (!CanJump()) return false;
        // Accept the press now; consume it in the single movement update.
        jumpRemaining = jumpBuffer;
        return true;
    }
    bool CanJump()
    {
        if (!isActiveAndEnabled || InputBlocked || ActionMovementLocked || !body.enabled ||
            IsCrouching || body.height < standingHeight - .05f) return false;
        var game = GameStateManager.Instance;
        var health = GetComponent<PlayerHealth>();
        if ((game && game.State != GameState.Playing) || (health && !health.CanParticipate)) return false;
        return true;
    }
    public bool CanStand()
    {
        // Check the additional headroom, excluding floor contacts and our own colliders.
        float radius = body.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        Vector3 bottom = transform.TransformPoint(body.center) + transform.up * (body.height * scaleY * .5f - radius);
        Vector3 top = transform.TransformPoint(standingCenter) + transform.up * (standingHeight * scaleY * .5f - radius);
        foreach (var hit in Physics.OverlapCapsule(bottom, top, Mathf.Max(.001f, radius - .005f), standingObstacles, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(transform)) return false;
        return true;
    }
    public void Tick(Vector2 input, bool walkHeld, float deltaTime)
    {
        if (deltaTime <= 0 || !body.enabled) return;
        input = (InputBlocked || ActionMovementLocked) ? Vector2.zero : Vector2.ClampMagnitude(input, 1);
        if (!IsCrouching && body.height < standingHeight - .001f && !CanStand()) IsCrouching = true;
        float targetHeight = IsCrouching ? Mathf.Clamp(crouchHeight, body.radius * 2, standingHeight) : standingHeight;
        float blend = 1 - Mathf.Exp(-crouchLerpSpeed * deltaTime);
        body.height = Mathf.Lerp(body.height, targetHeight, blend);
        body.center = standingCenter - Vector3.up * ((standingHeight - body.height) * .5f);
        cameraBasePosition = Vector3.Lerp(cameraBasePosition, standingView - Vector3.up * (standingHeight - targetHeight), blend);
        IsSprinting = !walkHeld && input.sqrMagnitude > .001f && !IsCrouching;
        float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
        Vector3 previousPosition = transform.position;
        MoveGrounded((transform.right * input.x + transform.forward * input.y) * speed, deltaTime);
        float horizontalSpeed = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up).magnitude / deltaTime;
        UpdateHeadBob(horizontalSpeed, deltaTime);
    }
    void UpdateHeadBob(float horizontalSpeed, float deltaTime)
    {
        Vector3 target = Vector3.zero;
        bool moving = headBobEnabled && headBobIntensity > 0 && !InputBlocked && !IsCrouching
            && body.isGrounded && horizontalSpeed > .1f;
        if (moving)
        {
            float frequency = IsSprinting ? sprintBobFrequency : walkBobFrequency;
            float amplitude = (IsSprinting ? sprintBobAmplitude : walkBobAmplitude) * Mathf.Clamp01(headBobIntensity);
            bobPhase = Mathf.Repeat(bobPhase + deltaTime * frequency * Mathf.PI * 2, Mathf.PI * 2);
            target = new Vector3(Mathf.Sin(bobPhase) * amplitude * .5f, Mathf.Sin(bobPhase * 2) * amplitude, 0);
        }
        else bobPhase = 0;
        // Keep posture and bob separate so crouching/idle never accumulate camera drift.
        bobOffset = Vector3.Lerp(bobOffset, target, 1 - Mathf.Exp(-14 * deltaTime));
        if (!moving && bobOffset.sqrMagnitude < .00000001f) bobOffset = Vector3.zero;
        if (view) view.localPosition = cameraBasePosition + bobOffset;
    }
    public void MoveGrounded(Vector3 velocity, float deltaTime)
    {
        if (deltaTime <= 0 || !body.enabled) return;
        bool grounded = body.isGrounded && fallSpeed <= 0;
        if (grounded) { coyoteRemaining = coyoteTime; fallSpeed = -2; }
        if (!CanJump()) jumpRemaining = -1;
        if (jumpRemaining >= 0 && (grounded || coyoteRemaining > 0))
        {
            fallSpeed = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            jumpRemaining = coyoteRemaining = -1;
        }
        coyoteRemaining -= deltaTime;
        jumpRemaining -= deltaTime;
        fallSpeed -= Mathf.Abs(gravity) * (fallSpeed < 0 ? Mathf.Max(1, fallMultiplier) : 1) * deltaTime;
        float impactSpeed = Mathf.Max(0, -fallSpeed);
        velocity.y = fallSpeed;
        var flags = body.Move(velocity * deltaTime);
        if ((flags & CollisionFlags.Below) != 0)
        {
            if (!grounded && impactSpeed > 0)
                NoiseManager.EmitNoise(new Vector3(transform.position.x, body.bounds.min.y, transform.position.z),
                    Mathf.Clamp01(impactSpeed * landingNoisePerSpeed), NoiseSource.Impact, gameObject);
            fallSpeed = -2;
        }
        if ((flags & CollisionFlags.Above) != 0 && fallSpeed > 0) fallSpeed = 0;
    }
    void OnDisable() { jumpRemaining = coyoteRemaining = -1; }
}
