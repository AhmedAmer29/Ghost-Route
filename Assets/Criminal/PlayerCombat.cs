using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private const string PunchState = "Punch";
    private const string KickState  = "Kick";

    private bool _attacking;

    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.applyRootMotion = false;
    }

    void Update()
    {
        if (animator == null || _attacking) return;

        if (Input.GetMouseButtonDown(0))
            StartCoroutine(Attack(PunchState, "Punch"));

        if (Input.GetKeyDown(KeyCode.F))
            StartCoroutine(Attack(KickState, "Kick"));
    }

    IEnumerator Attack(string stateName, string trigger)
    {
        _attacking = true;
        animator.applyRootMotion = false;
        animator.SetTrigger(trigger);

        // wait two frames for animator to enter the new state
        yield return null;
        yield return null;

        float length = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(length);

        _attacking = false;
    }
}
