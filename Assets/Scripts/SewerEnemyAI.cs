using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class SewerEnemyAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform player;
    
    [Header("Distances")]
    public float triggerDistance = 10f; // How close before he wakes up
    public float attackDistance = 1.5f; // How close before he punches
    
    [Header("Movement")]
    public float moveSpeed = 4.0f;
    public float turnSpeed = 5.0f;

    [Header("Timing")]
    public float standUpDuration = 1.0f;
    public float attackCooldown = 1.2f;
    
    private Animator animator;
    private NavMeshAgent agent;
    private bool hasWokenUp = false;
    private bool isAttacking = false;
    private bool canChase = false;
    private float lockedY;
    private float attackTimer;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        
        animator.applyRootMotion = false;
        lockedY = transform.position.y;
        
        // Configure Agent
        agent.speed = moveSpeed;
        agent.angularSpeed = turnSpeed * 100f; // Agent uses degrees per second
        agent.stoppingDistance = attackDistance;
        
        // Auto-find player if you forget to assign it
        if (player == null && Camera.main != null)
        {
            player = Camera.main.transform;
        }
    }
    
    void Update()
    {
        if (player == null) return;

        // Keep him from floating away by forcing his height
        Vector3 currentPos = transform.position;
        currentPos.y = lockedY;
        agent.Warp(currentPos); // Use Warp to move NavMeshAgents directly without breaking pathing

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
        
        float distance = Vector3.Distance(transform.position, player.position);
        
        // 1. Wake up if player gets close!
        if (!hasWokenUp && distance <= triggerDistance)
        {
            hasWokenUp = true;
            animator.SetTrigger("WakeUp"); // Sitting -> Stand Up
            Invoke(nameof(BeginChase), standUpDuration);
        }
        
        // 2. Chase and Attack logic
        if (hasWokenUp && canChase && !isAttacking)
        {
            if (distance > attackDistance)
            {
                // Chase the player using the NavMesh! (Avoids walls)
                agent.isStopped = false;
                agent.SetDestination(player.position);
                animator.SetBool("IsRunning", true); // Run
            }
            else if (attackTimer <= 0f)
            {
                // 3. Attack! (Flying Knee Punch)
                agent.isStopped = true; // Stop moving
                
                // Force rotation to face player when attacking
                Vector3 direction = (player.position - transform.position).normalized;
                direction.y = 0;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                animator.SetTrigger("Attack"); 
                animator.SetBool("IsRunning", false);
                isAttacking = true;
                attackTimer = attackCooldown;
                
                // Reset back to idle/chase after the punch
                Invoke(nameof(ResetAttack), 0.6f);
            }
        }
    }

    void BeginChase()
    {
        canChase = true;
        animator.SetBool("IsRunning", true);
    }
    
    void ResetAttack()
    {
        isAttacking = false;
        // The script loops back to Update. If distance > attackDistance, he runs again.
        // If distance <= attackDistance, he stays in Fighting Idle until attackCooldown is up.
    }
}
