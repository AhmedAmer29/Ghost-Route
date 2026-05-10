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
    public float attackAimOffset = 0f; // Manual rotation offset (try -10 or 10)
    public float sideCorrection = 1.2f; // How much we pull him to the left while flying
    
    private Vector3 _attackLaunchDirection;
    private float _attackLaunchTimer;
    private bool _isLaunching;

    private enum Phase
    {
        WaitingToStandUp,
        StandingUp,
        Running,
        Attacking,
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
        
        if (player == null && Camera.main != null)
            player = Camera.main.transform;
    }

    void Update()
    {
        if (player == null) return;

        // MANUALLY HANDLE THE ATTACK LUNGE
        if (_isLaunching)
        {
            _attackLaunchTimer += Time.deltaTime;
            if (_attackLaunchTimer >= attackLaunchDuration)
            {
                _isLaunching = false;
            }
            else
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

                Physics.SyncTransforms();
            }
        }

        Vector3 playerPos = player.position;
        Vector3 myPos = transform.position;
        playerPos.y = 0;
        myPos.y = 0;
        float dist = Vector3.Distance(myPos, playerPos);
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);

        // Logging
        _logTimer += Time.deltaTime;
        if (_logTimer >= LOG_INTERVAL || _phase == Phase.Attacking) // Log EVERY frame during attack
        {
            if (_phase == Phase.Attacking)
            {
                Vector3 toPlayer = (player.position - transform.position).normalized;
                toPlayer.y = 0;
                float angleError = Vector3.Angle(transform.forward, toPlayer);
                Debug.Log($"[ATTACK DIAGNOSTIC] Time:{Time.time:F2}, Pos:{transform.position}, Dist:{dist:F2}, AngleToPlayer:{angleError:F2}, Launching:{_isLaunching}, LaunchDir:{_attackLaunchDirection}");
            }
            else if (_logTimer >= LOG_INTERVAL)
            {
                _logTimer = 0f;
                Debug.Log($"[{Time.time:F2}] Phase:{_phase}, Dist:{dist:F2}");
            }
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
                if (stateInfo.IsName("Fighting Idle"))
                {
                    FinishAttack();
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
        _isLaunching = true;

        // FULLY DISABLE AGENT so it doesn't fight our manual position updates
        _agent.isStopped = true;
        _agent.enabled = false; 

        // Animation
        _anim.SetBool(ParamIsRunning, false);
        _anim.applyRootMotion = false; 
        _anim.CrossFadeInFixedTime("Flying Knee Kick", 0.05f);
    }

    void FinishAttack()
    {
        _phase = Phase.FightingIdle;
        _isLaunching = false;
        _anim.applyRootMotion = false;

        // Re-enable agent
        _agent.enabled = true;
        _agent.isStopped = false;
        _agent.updatePosition = true;
        _agent.Warp(transform.position);
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
