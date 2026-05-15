using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Punch Hit")]
    public float punchRange = 8f;
    public float punchCooldown = 0.35f;

    [Header("Kick Hit")]
    public float kickRange = 8f;
    public float kickCooldown = 0.45f;

    private float _nextPunchTime;
    private float _nextKickTime;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        if (animator == null) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= _nextPunchTime)
        {
            _nextPunchTime = Time.time + punchCooldown;
            animator.ResetTrigger("Punch");
            animator.SetTrigger("Punch");
            Debug.Log("[PlayerCombat] PUNCH click");
            ApplyHit(punchRange);
        }

        if (Input.GetKeyDown(KeyCode.F) && Time.time >= _nextKickTime)
        {
            _nextKickTime = Time.time + kickCooldown;
            animator.ResetTrigger("Kick");
            animator.SetTrigger("Kick");
            Debug.Log("[PlayerCombat] KICK key");
            ApplyHit(kickRange);
        }
    }

    void ApplyHit(float range)
    {
        var bosses = Object.FindObjectsByType<BossHealth>(FindObjectsSortMode.None);
        Vector3 origin = transform.position;

        BossHealth best = null;
        float bestDist = range;
        foreach (var b in bosses)
        {
            if (b == null || b.IsDead || !b.gameObject.activeInHierarchy) continue;
            Vector3 to = b.transform.position - origin;
            to.y = 0f;
            float d = to.magnitude;
            if (d <= bestDist) { bestDist = d; best = b; }
        }

        if (best != null)
        {
            Debug.Log($"[PlayerCombat] Hit {best.name} at dist {bestDist:F2}");
            best.TakeHit();
        }
        else
        {
            Debug.Log($"[PlayerCombat] No boss within {range}m");
        }
    }
}
