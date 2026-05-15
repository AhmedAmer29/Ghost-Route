using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraScene3 : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 2f;
    public float verticalClamp = 80f;

    [Header("Eye Position")]
    public float eyeHeight = 1.0f;
    public float forwardOffset = 0.2f;

    [Header("Head Bob")]
    public bool enableBob = true;
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.035f;
    public float bobRunMultiplier = 1.6f;
    public float bobCrouchMultiplier = 0.5f;

    [Header("Breathing")]
    public bool enableBreathing = true;
    public float breathFreqRest = 0.28f;
    public float breathFreqMax = 1.5f;
    public float breathAmpRest = 0.003f;
    public float breathAmpMax = 0.02f;
    public float breathPitchMax = 0.7f;

    [Header("Wall Collision")]
    public LayerMask wallMask = ~0;
    public float wallPadding = 0.2f;

    [Header("Body Rendering")]
    public string bodyLayerName = "PlayerBody";
    public float bodyCameraNearClip = 0.08f;
    public float downwardClamp = 45f;

    private float _xRotation;
    private float _eyeBase;
    private float _currentEyeH;
    private float _bobTimer;
    private float _bobOffsetY;
    private float _bobOffsetX;
    private float _breathTimer;
    private PlayerMovement _movement;
    private PlayerState _state;
    private Camera _bodyCam;
    private Camera _mainCam;

    void Start()
    {
        if (target == null)
        {
            PlayerMovement found = FindObjectOfType<PlayerMovement>();
            if (found != null) target = found.transform;
        }

        if (target == null)
        {
            Debug.LogError("[PlayerCameraScene3] No player found! Add a PlayerMovement component to player.");
            enabled = false;
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _xRotation = 0f;

        _movement = target.GetComponent<PlayerMovement>();
        _state    = target.GetComponent<PlayerState>();
        _mainCam  = GetComponent<Camera>();

        _eyeBase = ComputeEyeBase();
        _currentEyeH = _eyeBase;

        transform.SetParent(target);
        transform.localPosition = new Vector3(0f, _eyeBase, forwardOffset);
        transform.localRotation = Quaternion.identity;
    }

    float ComputeEyeBase()
    {
        float scale = target.lossyScale.y > 0.001f ? target.lossyScale.y : 1f;
        if (eyeHeight > 0.001f) return eyeHeight / scale;
        var cc = target.GetComponent<CharacterController>();
        if (cc != null) return cc.center.y + cc.height * 0.5f - 0.1f;
        return 1.7f / scale;

        SetupBodyCamera();
        DisableBodyFrustumCulling();
    }

    void SetupBodyCamera()
    {
        _mainCam.nearClipPlane = 0.15f;

        int bodyLayer = LayerMask.NameToLayer(bodyLayerName);
        if (bodyLayer == -1)
        {
            Debug.LogWarning("[PlayerCameraScene3] 'PlayerBody' layer not found — create it in Project Settings.");
            return;
        }

        _mainCam.cullingMask &= ~(1 << bodyLayer);

        Transform existing = transform.Find("PlayerBodyCamera");
        if (existing != null) Destroy(existing.gameObject);

        GameObject go = new GameObject("PlayerBodyCamera");
        go.transform.SetParent(transform, false);
        _bodyCam = go.AddComponent<Camera>();
        _bodyCam.clearFlags = CameraClearFlags.Depth;
        _bodyCam.cullingMask = 1 << bodyLayer;
        _bodyCam.nearClipPlane = bodyCameraNearClip;
        _bodyCam.farClipPlane = _mainCam.farClipPlane;
        _bodyCam.fieldOfView = _mainCam.fieldOfView;
        _bodyCam.depth = _mainCam.depth + 1;
    }

    void DisableBodyFrustumCulling()
    {
        if (target == null) return;
        foreach (var smr in target.GetComponentsInChildren<SkinnedMeshRenderer>())
            smr.updateWhenOffscreen = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Mathf.Clamp(Input.GetAxis("Mouse X") * mouseSensitivity, -10f, 10f);
        float mouseY = Mathf.Clamp(Input.GetAxis("Mouse Y") * mouseSensitivity, -10f, 10f);

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -downwardClamp, verticalClamp);
        target.Rotate(Vector3.up * mouseX);

        bool crouching = _movement != null && _movement.isCrouching;
        bool running = _movement != null && _movement.isRunning;
        float speed = _movement != null ? _movement.currentSpeed : 0f;
        float exertion = _movement != null ? _movement.exertion : 0f;

        _eyeBase = ComputeEyeBase();
        float targetEyeH = crouching ? _eyeBase * 0.55f : _eyeBase;
        _currentEyeH = Mathf.Lerp(_currentEyeH, targetEyeH, Time.deltaTime * 12f);

        if (enableBob && speed > 0.1f)
        {
            float mult = crouching ? bobCrouchMultiplier : (running ? bobRunMultiplier : 1f);
            float freq = bobFrequency * mult;
            float amp = bobAmplitude * mult;
            float maxSpd = running ? _movement.runSpeed : (crouching ? _movement.crouchSpeed : _movement.walkSpeed);
            float speedRatio = Mathf.Clamp01(speed / Mathf.Max(maxSpd, 0.01f));

            _bobTimer += Time.deltaTime * freq;
            _bobOffsetY = Mathf.Sin(_bobTimer) * amp * speedRatio;
            _bobOffsetX = Mathf.Sin(_bobTimer * 0.5f) * amp * 0.4f * speedRatio;
        }
        else
        {
            _bobOffsetY = Mathf.Lerp(_bobOffsetY, 0f, Time.deltaTime * 10f);
            _bobOffsetX = Mathf.Lerp(_bobOffsetX, 0f, Time.deltaTime * 10f);
        }

        float breathY = 0f;
        float breathPitch = 0f;
        if (enableBreathing)
        {
            float freq = Mathf.Lerp(breathFreqRest, breathFreqMax, exertion);
            float amp = Mathf.Lerp(breathAmpRest, breathAmpMax, exertion);
            _breathTimer += Time.deltaTime * freq * Mathf.PI * 2f;
            breathY = Mathf.Sin(_breathTimer) * amp;
            breathPitch = Mathf.Sin(_breathTimer) * Mathf.Lerp(0f, breathPitchMax, exertion);
        }

        if (_bodyCam != null)
            _bodyCam.fieldOfView = _mainCam.fieldOfView;

        float scale = target.lossyScale.y;

        Vector3 worldEye = target.position
                         + Vector3.up * (_currentEyeH * scale + _bobOffsetY * scale + breathY * scale)
                         + target.right * (_bobOffsetX * scale);

        float fwd = forwardOffset * scale;
        if (Physics.Raycast(worldEye, target.forward, out RaycastHit fwdHit, fwd + wallPadding, wallMask, QueryTriggerInteraction.Ignore))
            fwd = Mathf.Max(0f, fwdHit.distance - wallPadding);

        Vector3 lookDir = transform.rotation * Vector3.forward;
        if (Physics.Raycast(worldEye, lookDir, out RaycastHit lookHit, fwd + wallPadding, wallMask, QueryTriggerInteraction.Ignore))
            fwd = Mathf.Max(0f, Mathf.Min(fwd, lookHit.distance - wallPadding));

        float fwdLocal = scale > 0.001f ? fwd / scale : fwd;
        transform.localPosition = new Vector3(_bobOffsetX, _currentEyeH + _bobOffsetY + breathY, fwdLocal);
        transform.localRotation = Quaternion.Euler(_xRotation + breathPitch, 0f, 0f);
    }
}
