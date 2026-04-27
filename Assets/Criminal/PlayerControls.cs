using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed       = 2f;
    public float runSpeed        = 3.5f;
    public float crouchSpeed        = 1f;
    public float crouchSprintSpeed  = 1.6f;  // faster crouch walk when Shift is held
    public float acceleration    = 8f;
    public float deceleration    = 12f;
    public float sprintRampTime  = 0.45f;  // seconds to reach full sprint from walk

    [Header("Crouch")]
    public KeyCode crouchKey  = KeyCode.LeftControl;
    public float   crouchRatio = 0.5f;

    [Header("Jump")]
    public float jumpHeight            = 0.7f;
    public float landingFatigueDuration = 1.4f;  // seconds before speed fully recovers after landing
    public float landingFatigueAmount   = 0.28f; // peak speed penalty on landing (0–1)

    [Header("Gravity")]
    public float gravity             = -20f;
    public float groundCheckDistance = 0.25f;
    public LayerMask groundMask      = ~0;

    // Read by AnimationMoving and PlayerCamera
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public bool  isGrounded;
    [HideInInspector] public bool  isCrouching;
    [HideInInspector] public bool  isRunning;

    // 0 = fully rested, 1 = maximum effort — drives camera breathing intensity
    [HideInInspector] public float exertion;

    private CharacterController _controller;
    private Vector3 _velocity;
    private Vector3 _smoothMove;
    private Vector3 _moveDamp;
    private float   _standHeight;
    private Vector3 _standCenter;
    private float   _sprintFactor;   // 0–1, ramps up when Shift held
    private float   _landingFatigue; // 0–1, spikes on landing and fades
    private bool    _wasGrounded;

    void Start()
    {
        _controller  = GetComponent<CharacterController>();
        _velocity.y  = -2f;
        _standHeight = _controller.height;
        _standCenter = _controller.center;
        _wasGrounded = true;
    }

    void Update()
    {
        GroundCheck();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        UpdateExertion();
        _wasGrounded = isGrounded;
    }

    void GroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * _controller.radius;
        isGrounded = Physics.SphereCast(
            origin, _controller.radius - 0.01f, Vector3.down, out _,
            _controller.radius + groundCheckDistance, groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        // Spike landing fatigue the moment feet touch the ground
        if (isGrounded && !_wasGrounded)
            _landingFatigue = landingFatigueAmount;
    }

    void HandleCrouch()
    {
        bool want = Input.GetKey(crouchKey);

        if (!want && isCrouching)
        {
            Vector3 top = transform.position + Vector3.up * (_standHeight - _controller.radius);
            if (Physics.SphereCast(top, _controller.radius, Vector3.up, out _, 0.15f, groundMask))
                want = true;
        }

        isCrouching = want;

        float targetH = _standHeight * (isCrouching ? crouchRatio : 1f);
        _controller.height = Mathf.Lerp(_controller.height, targetH, Time.deltaTime * 12f);
        _controller.center = Vector3.Lerp(
            _controller.center,
            _standCenter * (_controller.height / _standHeight),
            Time.deltaTime * 12f
        );
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 raw = (transform.right * h + transform.forward * v).normalized;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) && raw.magnitude > 0.1f;
        bool wantsRun  = shiftHeld && !isCrouching;

        // Sprint ramps up over sprintRampTime so you feel the push into a run
        _sprintFactor = wantsRun
            ? Mathf.MoveTowards(_sprintFactor, 1f, Time.deltaTime / sprintRampTime)
            : Mathf.MoveTowards(_sprintFactor, 0f, Time.deltaTime / (sprintRampTime * 0.5f));

        isRunning = wantsRun && _sprintFactor > 0.1f;

        // Crouch sprint: Shift while crouching gives a faster crouch walk, no full stand-up
        float baseSpeed = isCrouching
            ? (shiftHeld ? crouchSprintSpeed : crouchSpeed)
            : Mathf.Lerp(walkSpeed, runSpeed, _sprintFactor);

        // Landing fatigue briefly slows you after a jump landing
        _landingFatigue = Mathf.MoveTowards(_landingFatigue, 0f, Time.deltaTime / landingFatigueDuration);
        float targetSpeed = baseSpeed * (1f - _landingFatigue);

        float rate = raw.magnitude > 0.1f ? acceleration : deceleration;
        _smoothMove  = Vector3.SmoothDamp(_smoothMove, raw * targetSpeed, ref _moveDamp, 1f / rate);
        currentSpeed = _smoothMove.magnitude;

        _controller.Move(_smoothMove * Time.deltaTime);
    }

    void HandleJump()
    {
        if (isGrounded && !isCrouching && Input.GetButtonDown("Jump"))
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    void UpdateExertion()
    {
        float target = 0f;

        bool crouchSprinting = isCrouching && Input.GetKey(KeyCode.LeftShift) && currentSpeed > 0.1f;

        if      (isRunning)               target = 0.55f + _sprintFactor * 0.45f;
        else if (crouchSprinting)         target = 0.3f;
        else if (currentSpeed > 0.1f)     target = 0.18f;
        else if (isCrouching)             target = 0.12f;

        // Landing impact also drives exertion (heavy breath when you hit the ground)
        target = Mathf.Max(target, _landingFatigue * 0.9f);

        // Exertion rises fast while running, drops off slowly during recovery
        float rate = (target > exertion) ? 3f : 0.35f;
        exertion = Mathf.MoveTowards(exertion, target, Time.deltaTime * rate);
    }
}
