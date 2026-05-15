using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class SwarmRatHallRunner : MonoBehaviour
{
    public string runStateName;
    public string roamTriggerName = "RatSwarm_Trigger";
    public float moveSpeed = 2.2f;
    public float turnSpeed = 220f;
    public float destinationReachDistance = 0.75f;
    public float boundaryPadding = 0.1f;
    public float curvedPathSpeed = 0.7f;
    public float laneJitter = 0.22f;
    public float groundSnapDistance = 5f;
    public float separationRadius = 1.25f;
    public float separationWeight = 1.5f;
    public float obstacleAvoidanceDistance = 1.4f;
    public float obstacleAvoidanceWeight = 2.0f;
    public LayerMask obstacleMask = ~0;

    [Header("Damage")]
    public bool  isAttackingRat = true;  // Only check this for Black Rats!
    public float damagePerSecond = 8f;   
    public string playerTag = "Player";

    private static readonly List<SwarmRatHallRunner> ActiveRunners = new List<SwarmRatHallRunner>();

    private Animator _animator;
    private NavMeshAgent _agent;
    private BoxCollider _roamBox;
    private Vector3 _destination;
    private bool _hasDestination;
    private float _pathPhase;
    private float _pathDirection = 1f;
    private float _laneScaleX = 1f;
    private float _laneScaleZ = 1f;
    private Vector3 _smoothSteeringDir;
    private float _steeringVelocity;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
    }

    void OnEnable()
    {
        if (!ActiveRunners.Contains(this))
        {
            ActiveRunners.Add(this);
        }
    }

    void OnDisable()
    {
        ActiveRunners.Remove(this);
    }

    void Start()
    {
        Debug.Log($"[SwarmRatHallRunner] Initializing rat: {name}");
        FindRoamBox();
        DisableAgentForBoxRoaming();
        ConfigureCurvedLane();
        PickNewDestination();

        if (_animator != null && !string.IsNullOrEmpty(runStateName))
        {
            _animator.Play(runStateName, 0, Random.value);
        }
    }

    void Update()
    {
        SnapToGround();

        if (_roamBox == null)
        {
            FindRoamBox(); // Keep trying to find it if it was missing
            if (_roamBox == null) 
            {
                MoveFallback();
                return;
            }
        }

        // Random Wandering Logic
        if (!_hasDestination || HasReachedDestination() || IsBlockedAhead())
        {
            PickNewDestination();
        }

        // Check if we are heading out of bounds and force a new destination if so
        if (WouldLeaveRoamBox())
        {
            PickNewDestination();
        }

        MoveRandomSmooth();
        SnapToGround();
        
        // Custom Damage Check (More reliable than OnTriggerStay)
        if (isAttackingRat)
        {
            CheckForPlayerDamage();
        }
    }

    private void CheckForPlayerDamage()
    {
        // Find all colliders within 0.5m of the rat
        Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (Collider c in hits)
        {
            if (c.CompareTag(playerTag))
            {
                // Auto-create damage effect if it doesn't exist
                if (RatDamageEffect.Instance == null)
                {
                    GameObject go = new GameObject("RatDamageEffect_Auto");
                    go.AddComponent<RatDamageEffect>();
                    Debug.Log("<color=orange>[SwarmRat] Auto-created RatDamageEffect because it was missing from the scene!</color>");
                }

                RatDamageEffect.Instance.TakeDamage(damagePerSecond * Time.deltaTime);
                return; // Only damage once per frame
            }
        }
    }

    public void Configure(string stateName)
    {
        runStateName = stateName;
    }

    private void FindRoamBox()
    {
        GameObject trigger = GameObject.Find(roamTriggerName);
        if (trigger != null)
        {
            _roamBox = trigger.GetComponent<BoxCollider>();
            if (_roamBox != null)
            {
                Debug.Log($"[SwarmRatHallRunner] Found roam box '{roamTriggerName}' for {name}. Size: {_roamBox.size}");
            }
        }

        if (_roamBox == null)
        {
            RatSwarm swarm = GameObject.FindObjectOfType<RatSwarm>();
            if (swarm != null)
            {
                _roamBox = swarm.GetComponent<BoxCollider>();
                if (_roamBox != null)
                {
                    Debug.Log($"[SwarmRatHallRunner] Fallback: Found roam box on RatSwarm for {name}. Size: {_roamBox.size}");
                }
            }
        }

        if (_roamBox == null)
        {
            Debug.LogWarning($"[SwarmRatHallRunner] {name} could NOT find a roam box! Using fallback destination logic.");
        }
    }

    private void DisableAgentForBoxRoaming()
    {
        if (_agent != null)
        {
            _agent.enabled = false;
        }
    }

    private void ConfigureCurvedLane()
    {
        int seed = Mathf.Abs(name.GetHashCode());
        _pathPhase = (seed % 1000) / 1000f * Mathf.PI * 2f;
        _pathDirection = (seed % 2 == 0) ? 1f : -1f;
        _laneScaleX = Mathf.Clamp01(0.58f + ((seed % 37) / 36f) * laneJitter);
        _laneScaleZ = Mathf.Clamp01(0.58f + (((seed / 37) % 37) / 36f) * laneJitter);
    }

    private void PickNewDestination()
    {
        if (_roamBox == null)
        {
            PickFallbackDestination();
            return;
        }

        // Get world-space dimensions
        Vector3 worldScale = _roamBox.transform.lossyScale;
        float realWidth = _roamBox.size.x * worldScale.x;
        float realLength = _roamBox.size.z * worldScale.z;

        float hW = Mathf.Max(0.5f, realWidth * 0.5f - boundaryPadding);
        float hL = Mathf.Max(0.5f, realLength * 0.5f - boundaryPadding);

        // Pick a random point in local space relative to the box center
        Vector3 randomLocal = new Vector3(
            Random.Range(-hW, hW),
            0f,
            Random.Range(-hL, hL)
        );

        // Transform that local point to world space
        // We account for rotation here
        _destination = _roamBox.transform.position + (_roamBox.transform.rotation * randomLocal);
        _destination.y = transform.position.y;
        _hasDestination = true;
    }

    private void PickFallbackDestination()
    {
        Vector3 randomDirection = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
        _destination = transform.position + randomDirection * 6f;
        _hasDestination = true;
    }

    private bool HasReachedDestination()
    {
        Vector3 flatDelta = _destination - transform.position;
        flatDelta.y = 0f;
        return flatDelta.magnitude <= destinationReachDistance;
    }

    private bool IsNearBoundary()
    {
        if (_roamBox == null)
        {
            return false;
        }

        Vector3 local = _roamBox.transform.InverseTransformPoint(transform.position) - _roamBox.center;
        Vector3 halfSize = _roamBox.size * 0.5f;
        return Mathf.Abs(local.x) >= halfSize.x - boundaryPadding ||
               Mathf.Abs(local.z) >= halfSize.z - boundaryPadding;
    }

    private bool WouldLeaveRoamBox()
    {
        if (_roamBox == null) return false;

        // Predict position 1 second ahead
        Vector3 predicted = transform.position + transform.forward * moveSpeed * 1.0f;
        
        // Convert to local space of the box to check boundaries
        Vector3 local = Quaternion.Inverse(_roamBox.transform.rotation) * (predicted - _roamBox.transform.position);
        
        Vector3 worldScale = _roamBox.transform.lossyScale;
        float hW = (_roamBox.size.x * worldScale.x) * 0.5f;
        float hL = (_roamBox.size.z * worldScale.z) * 0.5f;

        return Mathf.Abs(local.x) > hW || Mathf.Abs(local.z) > hL;
    }

    private void MoveRandomSmooth()
    {
        Vector3 desiredDirection = (_destination - transform.position);
        desiredDirection.y = 0f;

        if (desiredDirection.sqrMagnitude < 0.01f) return;
        desiredDirection.Normalize();

        // Steering Smoothing
        if (_smoothSteeringDir == Vector3.zero) _smoothSteeringDir = transform.forward;
        _smoothSteeringDir = Vector3.RotateTowards(_smoothSteeringDir, desiredDirection, 150f * Mathf.Deg2Rad * Time.deltaTime, 0f);
        
        Vector3 finalDirection = _smoothSteeringDir;
        finalDirection += GetSeparationDirection() * separationWeight;
        finalDirection += GetObstacleAvoidanceDirection() * obstacleAvoidanceWeight;
        finalDirection.y = 0f;
        finalDirection.Normalize();

        Quaternion targetRotation = Quaternion.LookRotation(finalDirection, Vector3.up);
        
        // Parabolic Turn Logic (x^2)
        float angle = Vector3.Angle(transform.forward, finalDirection);
        float turnFactor = Mathf.Clamp01(angle / 90f); 
        float turnSmooth = turnFactor * turnFactor; // This is the x^2 curve
        
        float currentTurnSpeed = turnSpeed * Mathf.Lerp(0.6f, 1.4f, turnSmooth);
        float currentMoveSpeed = moveSpeed * Mathf.Lerp(1.0f, 0.35f, turnSmooth);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentTurnSpeed * Time.deltaTime);
        transform.position += transform.forward * currentMoveSpeed * Time.deltaTime;
    }

    private void MoveFallback()
    {
        // Simple forward movement if no box found
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        if (Time.frameCount % 100 == 0) transform.Rotate(0, Random.Range(-30, 30), 0);
    }



    private Vector3 GetSeparationDirection()
    {
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        foreach (SwarmRatHallRunner other in ActiveRunners)
        {
            if (other == null || other == this)
            {
                continue;
            }

            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float distance = away.magnitude;

            if (distance <= 0.001f || distance > separationRadius)
            {
                continue;
            }

            separation += away.normalized * (1f - distance / separationRadius);
            neighborCount++;
        }

        if (neighborCount == 0)
        {
            return Vector3.zero;
        }

        return separation.normalized;
    }

    private Vector3 GetObstacleAvoidanceDirection()
    {
        Vector3 avoidance = Vector3.zero;
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        Vector3[] feelers =
        {
            transform.forward,
            Quaternion.Euler(0f, 35f, 0f) * transform.forward,
            Quaternion.Euler(0f, -35f, 0f) * transform.forward
        };

        foreach (Vector3 feeler in feelers)
        {
            RaycastHit hit;
            if (Physics.Raycast(origin, feeler, out hit, obstacleAvoidanceDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                avoidance += hit.normal * (1f - hit.distance / obstacleAvoidanceDistance);
            }
        }

        avoidance.y = 0f;
        return avoidance.sqrMagnitude > 0.001f ? avoidance.normalized : Vector3.zero;
    }

    private bool IsBlockedAhead()
    {
        Vector3 origin = transform.position + Vector3.up * 0.25f;
        RaycastHit hit;
        if (!Physics.Raycast(origin, transform.forward, out hit, obstacleAvoidanceDistance * 0.6f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return hit.transform != transform && !hit.transform.IsChildOf(transform);
    }

    private void ClampInsideRoamBox()
    {
        if (_roamBox == null) return;

        Vector3 local = _roamBox.transform.InverseTransformPoint(transform.position);
        Vector3 halfSize = _roamBox.size * 0.5f;
        Vector3 relative = local - _roamBox.center;

        // Ensure clamp matches the same safe padding as the movement logic
        float safePadding = Mathf.Min(boundaryPadding, Mathf.Min(halfSize.x, halfSize.z) * 0.2f);

        relative.x = Mathf.Clamp(relative.x, -halfSize.x + safePadding, halfSize.x - safePadding);
        relative.z = Mathf.Clamp(relative.z, -halfSize.z + safePadding, halfSize.z - safePadding);

        Vector3 clamped = _roamBox.center + relative;
        transform.position = _roamBox.transform.TransformPoint(clamped);
    }

    private void SnapToGround()
    {
        RaycastHit hit;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out hit, groundSnapDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point;
        }
    }

    // Removed OnTriggerStay in favor of CheckForPlayerDamage in Update

    void OnDrawGizmosSelected()
    {
        if (_roamBox == null) return;

        Vector3 worldCenter = _roamBox.transform.TransformPoint(_roamBox.center);
        Vector3 worldScale = _roamBox.transform.lossyScale;
        Vector3 realSize = Vector3.Scale(_roamBox.size, worldScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldCenter, realSize);

        // Draw destination
        if (_hasDestination)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_destination, 0.2f);
            Gizmos.DrawLine(transform.position, _destination);
        }
    }
}
