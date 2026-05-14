using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed       = 2f;
    public float runSpeed        = 12f;
    public float crouchSpeed        = 1f;
    public float crouchSprintSpeed  = 1.6f;  // faster crouch walk when Shift is held
    public float acceleration    = 8f;
    public float deceleration    = 12f;
    public float sprintRampTime  = 0.45f;  // seconds to reach full sprint from walk

    [Header("Crouch")]
    public KeyCode crouchKey  = KeyCode.LeftControl;
    public float   crouchRatio = 0.5f;

    [Header("Jump")]
    public float jumpHeight            = 1.2f;
    public float landingFatigueDuration = 1.4f;  // seconds before speed fully recovers after landing
    public float landingFatigueAmount   = 0.28f; // peak speed penalty on landing (0–1)

    [Header("Gravity")]
    public float gravity             = -20f;
    public float groundCheckDistance = 0.25f;
    public LayerMask groundMask      = ~0;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 1.5f;

    // Read by AnimationMoving and PlayerCamera
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public bool  isGrounded;
    [HideInInspector] public bool  isCrouching;
    [HideInInspector] public bool  isRunning;
    [HideInInspector] public float currentStamina = 100f;

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
    private float   _staminaRegenTimer;

    void Start()
    {
        _controller  = GetComponent<CharacterController>();
        _velocity.y  = -2f;
        _standHeight = _controller.height;
        _standCenter = _controller.center;
        _wasGrounded = true;
        currentStamina = maxStamina;
    }

    void Update()
    {
        GroundCheck();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        HandleStamina();
        UpdateExertion();
        _wasGrounded = isGrounded;
    }

    void GroundCheck()
    {
        isGrounded = _controller.isGrounded;

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
        Vector3 direction = (transform.right * h + transform.forward * v).normalized;
        bool isMoving = direction.sqrMagnitude > 0.01f;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool wantsRun = shiftHeld && isMoving && !isCrouching && currentStamina > 0f;

        if (wantsRun)
            _sprintFactor = Mathf.MoveTowards(_sprintFactor, 1f, Time.deltaTime / sprintRampTime);
        else
            _sprintFactor = Mathf.MoveTowards(_sprintFactor, 0f, Time.deltaTime / (sprintRampTime * 0.5f));

        isRunning = wantsRun && _sprintFactor > 0.1f;

        float speed = isCrouching
            ? (shiftHeld ? crouchSprintSpeed : crouchSpeed)
            : Mathf.Lerp(walkSpeed, runSpeed, _sprintFactor);

        _landingFatigue = Mathf.MoveTowards(_landingFatigue, 0f, Time.deltaTime / landingFatigueDuration);
        speed *= (1f - _landingFatigue);

        _smoothMove = direction * speed;
        currentSpeed = _smoothMove.magnitude;

        if (Input.GetKeyDown(KeyCode.F3))
            Debug.Log($"[Sprint] shift={shiftHeld} moving={isMoving} stamina={currentStamina:F0} sprintFactor={_sprintFactor:F2} isRunning={isRunning} speed={speed:F1}");
    }

    void HandleJump()
    {
        if (isGrounded && !isCrouching && Input.GetButtonDown("Jump"))
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        
        Vector3 finalMove = _smoothMove;
        finalMove.y = _velocity.y;
        
        _controller.Move(finalMove * Time.deltaTime);
    }

    void HandleStamina()
    {
        if (isRunning && currentStamina > 0f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0f, currentStamina);
            _staminaRegenTimer = 0f;

            if (currentStamina <= 0f)
            {
                _sprintFactor = 0f;
                isRunning = false;
            }
        }
        else
        {
            _staminaRegenTimer += Time.deltaTime;
            if (_staminaRegenTimer >= staminaRegenDelay)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
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
