using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class SewerEnemyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;

    [Header("Distances")]
    public float triggerDistance = 20f;
    public float attackDistance = 6.0f;

    [Header("Movement")]
    public float moveSpeed = 4.0f;
    public float turnSpeed = 10.0f;

    [Header("Attack Launch")]
    public float attackLaunchSpeed = 150.0f;   // Speed of the lunge to match moveSpeed
    public float attackLaunchDuration = 0.25f; // How long the lunge lasts
    public float attackLaunchDelayNorm = 0.47f; // Delay before lunge starts (0.47 = ~frame 38 of 81)
    public float attackAimOffset = 0f; // Manual rotation offset (try -10 or 10)
    public float sideCorrection = 1.2f; // How much we pull him to the left while flying
    [Range(5f, 100f)] public float attackKnockback = 35.0f; // How much the player is pushed back
    [Range(0f, 10f)] public float attackVerticalLift = 3.0f; // Upward pop on hit
    [Range(0f, 2f)] public float attackStunDuration = 0.5f; // How long the player loses control
    public float knockbackDistance = 1.8f; // Distance to trigger the push
    [Range(0f, 100f)] public float attackDamage = 25f; // HP removed from RatDamageEffect on impact

    [Header("Ground Snap")]
    public float groundOffset = 0.05f;
    
    private Vector3 _attackLaunchDirection;
    private float _attackLaunchTimer;
    private bool _isLaunching;
    private bool _hasHitPlayer; // Track if we already pushed the player this attack
    private bool _hasLaunchedThisAttack; // Track if the lunge started

    private enum Phase
    {
        WaitingToStandUp,
        StandingUp,
        Running,
        Attacking,
        Recovering,
        FightingIdle
    }

    private Animator _anim;
    private NavMeshAgent _agent;
    private Phase _phase = Phase.WaitingToStandUp;

    private static readonly int ParamIsRunning = Animator.StringToHash("IsRunning");
    private static readonly int ParamAttack = Animator.StringToHash("Attack");
    private static readonly int ParamStartRunning = Animator.StringToHash("StartRunning");

    private float _logTimer = 0f;
    private const float LOG_INTERVAL = 0.4f;

    void Awake()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        foreach (var l in listeners)
        {
            if (l.gameObject.tag != "MainCamera") l.enabled = false;
        }
    }

    void Start()
    {
        _anim = GetComponent<Animator>();
        _anim.applyRootMotion = false;

        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
            _agent = gameObject.AddComponent<NavMeshAgent>();
        
        _agent.enabled = false;
        
        if (player == null)
        {
            if (Camera.main != null)
            {
                // Try to find the root player body (the one with the CharacterController or Receiver)
                PlayerKnockbackReceiver receiver = FindAnyObjectByType<PlayerKnockbackReceiver>();
                if (receiver != null)
                {
                    player = receiver.transform;
                }
                else
                {
                    CharacterController cc = FindAnyObjectByType<CharacterController>();
                    if (cc != null) player = cc.transform;
                    else player = Camera.main.transform;
                }
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 p1 = player.position;
        Vector3 p2 = transform.position;
        p1.y = 0;
        p2.y = 0;
        float dist = Vector3.Distance(p1, p2);

        // MANUALLY HANDLE THE ATTACK LUNGE
        if (_isLaunching)
        {
            _attackLaunchTimer += Time.deltaTime;
            if (_attackLaunchTimer >= attackLaunchDuration)
            {
                _isLaunching = false;
            }

            // Check for impact with player
            if (!_hasHitPlayer && dist <= knockbackDistance)
            {
                _hasHitPlayer = true;
                ApplyKnockbackToPlayer();
            }

            // Stop moving forward if we hit the player (adds impact weight)
            if (!_hasHitPlayer || _attackLaunchTimer < attackLaunchDuration * 0.4f)
            {
                // Move Forward
                Vector3 forwardMove = _attackLaunchDirection * attackLaunchSpeed * Time.deltaTime;
                
                // Pull Left (Side Correction)
                Vector3 sideDir = Vector3.Cross(Vector3.up, _attackLaunchDirection); // This is his Right
                Vector3 sideMove = -sideDir * sideCorrection * Time.deltaTime; // Negative is Left
                
                Vector3 totalMove = forwardMove + sideMove;
                
                // Apply to Transform
                transform.position += totalMove;
                
                // Also update Agent's internal position so it stays in sync
                if (_agent.enabled)
                {
                    _agent.nextPosition = transform.position;
                }
            }

            Physics.SyncTransforms();
        }

        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);

        // Simple Logging only for phase changes
        _logTimer += Time.deltaTime;
        if (_logTimer >= LOG_INTERVAL)
        {
            _logTimer = 0f;
            Debug.Log($"[{Time.time:F2}] Phase:{_phase}, Dist:{dist:F2}");
        }

        switch (_phase)
        {
            case Phase.WaitingToStandUp:
                if (dist <= triggerDistance)
                {
                    _anim.SetTrigger(ParamStartRunning);
                    _phase = Phase.StandingUp;
                }
                break;

            case Phase.StandingUp:
                if (stateInfo.IsName("Standing Up") && stateInfo.normalizedTime >= 0.9f)
                {
                    _agent.enabled = true;
                    _agent.updatePosition = true;
                    _agent.updateRotation = true;
                    _agent.speed = moveSpeed;
                    _agent.Warp(transform.position);
                    _agent.SetDestination(player.position);
                    _anim.SetBool(ParamIsRunning, true);
                    _phase = Phase.Running;
                }
                break;

            case Phase.Running:
                if (dist <= attackDistance)
                {
                    StartAttackLunge();
                }
                else if (_agent.isOnNavMesh)
                {
                    _agent.SetDestination(player.position);
                }
                break;

            case Phase.Attacking:
                // Start the lunge movement after the wind-up (frame 38 / 0.47 normalized time)
                if (!_hasLaunchedThisAttack && stateInfo.IsName("Flying Knee Kick") && stateInfo.normalizedTime >= attackLaunchDelayNorm)
                {
                    _isLaunching = true;
                    _hasLaunchedThisAttack = true;
                    Debug.Log($"[ATTACK] Lunge launched at normalizedTime: {stateInfo.normalizedTime:F2}");
                }

                // Wait for the kick to finish
                if (stateInfo.IsName("Flying Knee Kick") && stateInfo.normalizedTime >= 0.95f)
                {
                    StartRecovery();
                }
                break;

            case Phase.Recovering:
                // Just force Y position relative to the ground under him
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit rHit, 3.0f, NavMesh.AllAreas))
                {
                    Vector3 p = transform.position;
                    p.y = rHit.position.y + groundOffset;
                    transform.position = p;
                }

                // Wait for the recovery animation to finish
                if (stateInfo.IsName("Standing Interpolation") && stateInfo.normalizedTime >= 0.95f)
                {
                    _phase = Phase.FightingIdle;
                }
                break;

            case Phase.FightingIdle:
                // Keep him on the invisible floor even while idling
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit fHit, 3.0f, NavMesh.AllAreas))
                {
                    Vector3 p = transform.position;
                    p.y = fHit.position.y + groundOffset;
                    transform.position = p;
                }
                FacePlayer();

                // Loop back into combat
                if (dist <= attackDistance)
                {
                    StartAttackLunge();
                }
                else
                {
                    _agent.enabled = true;
                    _agent.isStopped = false;
                    _agent.updatePosition = true;
                    _agent.updateRotation = true;
                    _agent.speed = moveSpeed;
                    _agent.Warp(transform.position);
                    if (_agent.isOnNavMesh) _agent.SetDestination(player.position);
                    _anim.SetBool(ParamIsRunning, true);
                    _phase = Phase.Running;
                }
                break;
        }
    }

    void StartAttackLunge()
    {
        _phase = Phase.Attacking;

        // Snap rotation
        FacePlayer();

        // Capture Direction
        _attackLaunchDirection = (player.position - transform.position).normalized;
        _attackLaunchDirection.y = 0;
        
        _attackLaunchTimer = 0f;
        _isLaunching = false;
        _hasLaunchedThisAttack = false;
        _hasHitPlayer = false; // Reset hit flag

        // FULLY DISABLE AGENT so it doesn't fight our manual position updates
        _agent.isStopped = true;
        _agent.enabled = false; 

        // Animation
        _anim.SetBool(ParamIsRunning, false);
        _anim.applyRootMotion = false; 
        _anim.CrossFadeInFixedTime("Flying Knee Kick", 0.05f);
    }

    void StartRecovery()
    {
        _phase = Phase.Recovering;
        _isLaunching = false;
        _anim.applyRootMotion = false;

        // Play the specific recovery animation you found
        _anim.CrossFadeInFixedTime("Standing Interpolation", 0.1f);

        // Re-enable agent but keep it stopped
        _agent.enabled = true;
        _agent.isStopped = true;
        _agent.updatePosition = true;

        // Find the floor and lift him slightly so he doesn't phase through
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            Vector3 groundPos = hit.position;
            groundPos.y += 0.05f; // Tiny lift to prevent phasing
            _agent.Warp(groundPos);
        }
        else
        {
            _agent.Warp(transform.position);
        }
    }

    void ApplyKnockbackToPlayer()
    {
        if (player == null) return;

        // Direction away from the enemy at the moment of impact
        Vector3 pushDir = (player.position - transform.position).normalized;
        pushDir.y = 0; // Keep horizontal push flat
        
        if (pushDir == Vector3.zero) 
            pushDir = transform.forward; // Fallback if exactly overlapping

        // Red-screen damage
        if (RatDamageEffect.Instance != null)
            RatDamageEffect.Instance.TakeDamage(attackDamage);

        // Pass knockback down to player receiver
        if (player.TryGetComponent<PlayerKnockbackReceiver>(out PlayerKnockbackReceiver receiver))
        {
            receiver.ApplyKnockback(pushDir, attackKnockback, attackVerticalLift, attackStunDuration);
            Debug.Log($"[IMPACT] Sent knockback to player receiver! Str: {attackKnockback}, Lift: {attackVerticalLift}");
        }
        else
        {
            // Fallback for missing receiver (direct impulse)
            if (player.TryGetComponent<CharacterController>(out CharacterController cc))
            {
                cc.Move((pushDir * attackKnockback + Vector3.up * attackVerticalLift) * 0.05f); // Simple jitter
            }
            else
            {
                player.position += (pushDir * attackKnockback + Vector3.up * attackVerticalLift) * 0.05f;
            }
            Debug.LogWarning("[IMPACT] Player missing PlayerKnockbackReceiver. Using fallback jitter.");
        }
    }

    void FacePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = rot * Quaternion.Euler(0, attackAimOffset, 0);
        }
    }
}
