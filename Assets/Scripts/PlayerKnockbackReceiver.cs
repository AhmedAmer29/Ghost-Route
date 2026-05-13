using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class PlayerKnockbackReceiver : MonoBehaviour
{
    [Header("Knockback")]
    [Range(1f, 20f)] public float knockbackDecay = 6f;
    [Range(0.1f, 2f)] public float minKnockbackSpeed = 0.3f;

    [Header("Camera Shake")]
    [Range(0f, 0.5f)] public float shakeIntensity = 0.12f;
    [Range(0f, 1f)] public float shakeDuration = 0.25f;

    [Header("Input Lock")]
    public MonoBehaviour[] componentsToDisable;

    [Header("Events")]
    public UnityEvent OnKnockbackStart;
    public UnityEvent OnKnockbackEnd;
    public UnityEvent OnParried;

    public bool IsStunned { get; private set; }
    public bool isParrying;

    private CharacterController _cc;
    private Vector3 _knockbackVelocity;
    private float _stunTimer;
    private Camera _playerCamera;
    private Coroutine _knockbackRoutine;
    private Coroutine _shakeRoutine;

    private Vector3 _shakeOrigPos;
    private Vector3 _shakeOffset;
    private bool _isShaking;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (_cc == null) _cc = GetComponentInParent<CharacterController>();

        _playerCamera = GetComponentInChildren<Camera>();
        if (_playerCamera == null) _playerCamera = Camera.main;

        if (_playerCamera != null)
            _shakeOrigPos = _playerCamera.transform.localPosition;
    }

    void LateUpdate()
    {
        if (!_isShaking || _playerCamera == null) return;
        _playerCamera.transform.localPosition = _shakeOrigPos + _shakeOffset;
    }

    public void ApplyKnockback(Vector3 direction, float strength, float verticalLift, float stunDuration)
    {
        if (isParrying)
        {
            OnParried?.Invoke();
            return;
        }

        _knockbackVelocity = direction * strength + Vector3.up * verticalLift;
        _stunTimer = stunDuration;

        if (_knockbackRoutine != null)
            StopCoroutine(_knockbackRoutine);

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);
        _shakeOffset = Vector3.zero;
        _isShaking = false;

        if (!IsStunned)
        {
            IsStunned = true;
            SetComponentsEnabled(false);
            OnKnockbackStart?.Invoke();
        }

        _knockbackRoutine = StartCoroutine(KnockbackRoutine());
    }

    IEnumerator KnockbackRoutine()
    {
        if (_playerCamera != null)
            _shakeRoutine = StartCoroutine(CameraShake());

        while (_stunTimer > 0f && _knockbackVelocity.magnitude > minKnockbackSpeed)
        {
            _stunTimer -= Time.deltaTime;

            if (_cc != null)
            {
                _cc.Move(_knockbackVelocity * Time.deltaTime);
            }
            else
            {
                transform.position += _knockbackVelocity * Time.deltaTime;
            }

            _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
            yield return null;
        }

        IsStunned = false;
        _knockbackVelocity = Vector3.zero;
        _knockbackRoutine = null;
        SetComponentsEnabled(true);
        OnKnockbackEnd?.Invoke();
    }

    IEnumerator CameraShake()
    {
        if (_playerCamera == null) yield break;

        float elapsed = 0f;
        _isShaking = true;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float decay = 1f - (elapsed / shakeDuration);
            _shakeOffset = Random.insideUnitSphere * shakeIntensity * decay;
            yield return null;
        }

        _shakeOffset = Vector3.zero;
        _isShaking = false;
        _playerCamera.transform.localPosition = _shakeOrigPos;
    }

    void SetComponentsEnabled(bool state)
    {
        if (componentsToDisable == null) return;
        foreach (var comp in componentsToDisable)
        {
            if (comp != null) comp.enabled = state;
        }
    }
}