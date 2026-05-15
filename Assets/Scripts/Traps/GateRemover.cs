using UnityEngine;

// Attach this directly to any blocker (e.g. SerwersP_013 (1)).
// Every frame it checks both conditions — boss dead AND all circuits fixed.
// The moment both are true, the GameObject is destroyed. No coroutines,
// no references to wire, no FindObjectByType timing issues.
public class GateRemover : MonoBehaviour
{
    [Tooltip("Optional: explicit refs. If empty, finds them at runtime.")]
    public BossHealth boss;
    public MasterPowerSystem power;

    [Tooltip("Set true to remove on boss death alone (skip the circuits check).")]
    public bool ignoreCircuits = false;

    private bool _bossWarned, _powerWarned;
    private float _statusTimer;

    void Start()
    {
        Debug.Log($"<color=cyan>[GateRemover:{name}] Start at {transform.position}</color>");
    }

    void Update()
    {
        if (boss  == null) boss  = Object.FindFirstObjectByType<BossHealth>();
        if (power == null) power = Object.FindFirstObjectByType<MasterPowerSystem>();

        if (boss == null && !_bossWarned)
        {
            Debug.LogWarning($"<color=yellow>[GateRemover:{name}] No BossHealth in scene. Gate will never auto-remove.</color>");
            _bossWarned = true;
        }
        if (power == null && !_powerWarned && !ignoreCircuits)
        {
            Debug.LogWarning($"<color=yellow>[GateRemover:{name}] No MasterPowerSystem in scene. Either bake one or tick 'Ignore Circuits'.</color>");
            _powerWarned = true;
        }

        bool bossDead     = boss != null && boss.IsDead;
        bool circuitsDone = ignoreCircuits || (power != null && power.fixedCount >= power.targetCount);

        // Status heartbeat once a second so the user can watch the conditions tick over.
        _statusTimer += Time.deltaTime;
        if (_statusTimer >= 1f)
        {
            _statusTimer = 0f;
            string fx = power != null ? $"{power.fixedCount}/{power.targetCount}" : "n/a";
            Debug.Log($"[GateRemover:{name}] bossDead={bossDead} circuits={fx} (ignoreCircuits={ignoreCircuits})");
        }

        if (bossDead && circuitsDone)
        {
            Debug.Log($"<color=lime>[GateRemover:{name}] BOTH conditions met → destroying gate now</color>");
            Destroy(gameObject);
        }
    }
}
