using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("Idle Switching")]
    public string idleIndexParam    = "IdleIndex";
    public float  idleSwitchMinTime = 4f;
    public float  idleSwitchMaxTime = 9f;

    [Header("Jump — tick when animation is ready")]
    public bool useJumpAnimation = false;

    private Animator        _animator;
    private PlayerMovement  _movement;
    private float           _idleTimer;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _movement = GetComponent<PlayerMovement>();
        _animator.applyRootMotion = false;
        _animator.SetInteger(idleIndexParam, 1);
        ResetIdleTimer();
    }

    void Update()
    {
        float speed = _movement.currentSpeed;

        _animator.SetFloat("Speed",      speed, 0f, Time.deltaTime);
        _animator.SetBool ("IsCrouching", _movement.isCrouching);

        // Randomly alternate between the two idle animations while standing still
        if (speed < 0.1f)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                int cur = _animator.GetInteger(idleIndexParam);
                _animator.SetInteger(idleIndexParam, cur == 1 ? 2 : 1);
                ResetIdleTimer();
            }
        }
        else
        {
            ResetIdleTimer();
        }

        if (useJumpAnimation)
            _animator.SetBool("IsJumping", !_movement.isGrounded);
    }

    void ResetIdleTimer()
    {
        _idleTimer = Random.Range(idleSwitchMinTime, idleSwitchMaxTime);
    }
}
