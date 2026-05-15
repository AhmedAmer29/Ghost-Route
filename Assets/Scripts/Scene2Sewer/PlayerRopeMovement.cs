using UnityEngine;

/// <summary>
/// PlayerRopeMovement.cs
/// Bridges VerletRope and RopeCrossingController.
/// Attach to the Player GameObject.
/// Requires a Rigidbody on the Player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerRopeMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

    [Header("Required References")]
    [Tooltip("The VerletRope that spans the chasm.")]
    public VerletRope verletRope;

    [Tooltip("The RopeCrossingController managing the QTE logic.")]
    public RopeCrossingController controller;

    [Header("Player Positioning")]
    [Tooltip("Vertical offset from the rope node so the character stands on top of it.")]
    public float heightOffset = 0.9f;

    [Tooltip("How fast the player position lerps to the rope node (lower = smoother).")]
    public float trackingSpeed = 12f;

    [Header("Fall Physics")]
    [Tooltip("Minimum random horizontal force applied when the player tumbles off the rope.")]
    public float minFallForce = 2f;

    [Tooltip("Maximum random horizontal force applied when the player tumbles off the rope.")]
    public float maxFallForce = 5f;

    [Tooltip("Upward kick force applied when the player falls (for a more dramatic tumble).")]
    public float fallUpwardKick = 1.5f;

    // ─────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private Rigidbody _rb;

    /// <summary>Tracks whether we've already applied the fall impulse this run.</summary>
    private bool _fallApplied = false;

    /// <summary>Tracks whether we're currently locked to the rope.</summary>
    private bool _isOnRope = false;

    // Cache the previous state to detect transitions
    private RopeCrossingController.CrossingState _prevState = RopeCrossingController.CrossingState.Idle;

    // ─────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        if (controller == null) return;
        // Subscribe to events from the QTE controller
        controller.OnFall    += HandleFall;
        controller.OnSuccess += HandleSuccess;
    }

    void OnDisable()
    {
        if (controller == null) return;
        controller.OnFall    -= HandleFall;
        controller.OnSuccess -= HandleSuccess;
    }

    void Update()
    {
        if (controller == null || verletRope == null) return;

        RopeCrossingController.CrossingState state = controller.currentState;

        // ── Transition INTO Crossing ──────────────────────────────────
        if (state == RopeCrossingController.CrossingState.Crossing && !_isOnRope)
        {
            AttachToRope();
        }

        // ── While Crossing: lock player to the rope ───────────────────
        if (state == RopeCrossingController.CrossingState.Crossing && _isOnRope)
        {
            LockPlayerToRope();
        }

        _prevState = state;
    }

    // ─────────────────────────────────────────────────────────────
    // ROPE ATTACHMENT
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Switches the player from normal physics to kinematic rope-riding.
    /// Called once when the Crossing state begins.
    /// </summary>
    private void AttachToRope()
    {
        _isOnRope    = true;
        _fallApplied = false;

        // Disable gravity / physics so Verlet controls the position
        _rb.isKinematic = true;
        _rb.velocity        = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        Debug.Log("[PlayerRopeMovement] Attached to rope.");
    }

    /// <summary>
    /// Each frame while on the rope: update the player's world position to follow
    /// the rope node that corresponds to the current QTE progress (0-100).
    /// </summary>
    private void LockPlayerToRope()
    {
        // Get the world-space position along the rope at the player's current progress
        Vector3 ropePos = verletRope.GetPositionAtProgress(controller.progress);

        // Add the height offset so the character appears to stand on the rope
        Vector3 targetPos = ropePos + Vector3.up * heightOffset;

        // Smooth tracking prevents instant snapping (feels more physical)
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * trackingSpeed);

        // Keep the player facing forward along the rope direction
        if (controller.progress < 99f)
        {
            Vector3 nextPos = verletRope.GetPositionAtProgress(controller.progress + 1f);
            Vector3 forward = (nextPos - ropePos).normalized;
            if (forward != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // FALL HANDLING
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribed to <see cref="RopeCrossingController.OnFall"/>.
    /// Re-enables physics, detaches from rope, and launches the player off sideways.
    /// </summary>
    private void HandleFall()
    {
        if (_fallApplied) return;   // Safety: only apply once
        _fallApplied = true;
        _isOnRope    = false;

        // Re-enable full Rigidbody physics
        _rb.isKinematic = false;

        // Random horizontal tumble direction (left or right off the rope)
        float horizontalSign  = (Random.value > 0.5f) ? 1f : -1f;
        float horizontalForce = Random.Range(minFallForce, maxFallForce);

        // Build the impulse: sideways + a small upward kick for a dramatic tumble
        Vector3 impulse = (transform.right * horizontalSign * horizontalForce)
                        + (Vector3.up * fallUpwardKick);

        _rb.AddForce(impulse, ForceMode.Impulse);

        Debug.Log("[PlayerRopeMovement] Player tumbled off the rope! Impulse: " + impulse);

        // Optional: notify PlayerState to trigger death/animation sequence
        PlayerState ps = GetComponent<PlayerState>();
        if (ps != null) ps.TriggerDeath("Fell from rope into the chasm");
    }

    // ─────────────────────────────────────────────────────────────
    // SUCCESS HANDLING
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribed to <see cref="RopeCrossingController.OnSuccess"/>.
    /// Detaches from rope and restores normal player movement.
    /// </summary>
    private void HandleSuccess()
    {
        _isOnRope = false;

        // Return full physics control to the player
        _rb.isKinematic = false;

        Debug.Log("[PlayerRopeMovement] Player crossed successfully — control returned.");

        // You can trigger the next scene event, open a door, play a cutscene, etc. here.
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC UTILITIES
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Manually begin the crossing sequence from an external trigger (e.g. a trigger volume).
    /// </summary>
    public void BeginCrossing()
    {
        if (controller != null && verletRope != null)
            controller.StartCrossing();
    }
}
