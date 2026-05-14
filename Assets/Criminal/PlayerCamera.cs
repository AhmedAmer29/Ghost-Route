using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform target;
    public float     mouseSensitivity = 2f;
    public float     verticalClamp   = 80f;

    [Header("Eye Position")]
    [Tooltip("Height from feet to eyes while standing. Tune in Play mode.")]
    public float eyeHeight     = 1.6f;
    [Tooltip("Pushes camera to the front of the head. Raise until camera sits outside the face mesh.")]
    public float forwardOffset = 0.2f;

    [Header("Head Bob")]
    public bool  enableBob          = true;
    public float bobFrequency       = 5f;
    public float bobAmplitude       = 0.028f;
    public float bobRunMultiplier   = 1.8f;   // faster + bigger bob while sprinting
    public float bobCrouchMultiplier = 0.5f;  // slower + smaller while crouching

    [Header("Breathing")]
    public bool  enableBreathing = true;
    [Tooltip("Breaths per second at full rest.")]
    public float breathFreqRest = 0.28f;
    [Tooltip("Breaths per second at maximum exertion.")]
    public float breathFreqMax  = 1.5f;
    [Tooltip("Vertical camera shift at rest (metres).")]
    public float breathAmpRest  = 0.003f;
    [Tooltip("Vertical camera shift at max exertion (metres).")]
    public float breathAmpMax   = 0.02f;
    [Tooltip("How much breathing nudges camera pitch (degrees).")]
    public float breathPitchMax = 0.7f;

    [Header("Wall Collision")]
    [Tooltip("What the camera treats as solid walls. Exclude PlayerBody and the Criminal's own layer.")]
    public LayerMask wallMask = ~0;
    [Tooltip("Extra padding kept between the camera and any wall (metres).")]
    public float wallPadding = 0.12f;

    [Header("Body Rendering")]
    [Tooltip("Layer assigned to the character mesh renderers.")]
    public string bodyLayerName = "PlayerBody";
    [Tooltip("Near clip plane of the body-only camera. 0.4 clips close torso interior; legs at 0.8m+ still show.")]
    public float bodyCameraNearClip = 0.4f;
    [Tooltip("How far down the player can look (degrees). 60 stops the worst torso-interior angles.")]
    public float downwardClamp = 60f;

    private float          _xRotation;
    private float          _eyeBase;
    private float          _currentEyeH;
    private float          _bobTimer;
    private float          _bobOffsetY;
    private float          _bobOffsetX;
    private float          _breathTimer;
    private PlayerMovement _movement;
    private Camera         _bodyCam;
    private Camera         _mainCam;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        _xRotation       = 0f;

        // Ensure only this camera's AudioListener is active (scene Main Camera often has one too)
        foreach (var al in FindObjectsOfType<AudioListener>())
            if (al.gameObject != gameObject) al.enabled = false;

        float scale  = target != null ? target.lossyScale.y : 1f;
        _eyeBase     = eyeHeight * scale;
        _currentEyeH = _eyeBase;

        _movement = target != null ? target.GetComponent<PlayerMovement>() : null;
        _mainCam  = GetComponent<Camera>();

        SetupBodyCamera();
        DisableBodyFrustumCulling();
    }

    void SetupBodyCamera()
    {
        _mainCam.nearClipPlane = 0.15f;

        int bodyLayer = LayerMask.NameToLayer(bodyLayerName);
        if (bodyLayer == -1)
        {
            Debug.LogWarning("[PlayerCamera] 'PlayerBody' layer not found — create it in Project Settings → Tags & Layers.");
            return;
        }

        // Exclude body from main camera
        _mainCam.cullingMask &= ~(1 << bodyLayer);

        // Destroy any leftover body cam from a previous Play session before creating a new one
        Transform existing = transform.Find("PlayerBodyCamera");
        if (existing != null) Destroy(existing.gameObject);

        GameObject go  = new GameObject("PlayerBodyCamera");
        go.transform.SetParent(transform, false);
        _bodyCam              = go.AddComponent<Camera>();
        _bodyCam.clearFlags   = CameraClearFlags.Depth;
        _bodyCam.cullingMask  = 1 << bodyLayer;
        // Higher nearClip hides the torso interior (~0.1-0.3m away) but legs sit
        // 0.8-1.6m out so they still render. Tunable via bodyCameraNearClip.
        _bodyCam.nearClipPlane = bodyCameraNearClip;
        _bodyCam.farClipPlane  = _mainCam.farClipPlane;
        _bodyCam.fieldOfView   = _mainCam.fieldOfView;
        _bodyCam.depth         = _mainCam.depth + 1;
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

        // --- Mouse look ---
        // Clamp per-frame delta to avoid the spike Unity generates when clicking with a locked cursor
        float mouseX = Mathf.Clamp(Input.GetAxis("Mouse X") * mouseSensitivity, -10f, 10f);
        float mouseY = Mathf.Clamp(Input.GetAxis("Mouse Y") * mouseSensitivity, -10f, 10f);

        _xRotation -= mouseY;
        _xRotation  = Mathf.Clamp(_xRotation, -downwardClamp, verticalClamp);
        target.Rotate(Vector3.up * mouseX);

        // --- State reads ---
        bool  crouching = _movement != null && _movement.isCrouching;
        bool  running   = _movement != null && _movement.isRunning;
        float speed     = _movement != null ? _movement.currentSpeed : 0f;
        float exertion  = _movement != null ? _movement.exertion    : 0f;

        // --- Crouch camera drop ---
        float targetEyeH = crouching ? _eyeBase * 0.55f : _eyeBase;
        _currentEyeH = Mathf.Lerp(_currentEyeH, targetEyeH, Time.deltaTime * 12f);

        // --- Head bob ---
        // Vertical bounce + lateral sway form a subtle figure-8 that reads as natural walking weight
        if (enableBob && speed > 0.1f)
        {
            float mult = crouching ? bobCrouchMultiplier : (running ? bobRunMultiplier : 1f);
            float freq = bobFrequency * mult;
            float amp  = bobAmplitude * mult;

            // Scale amplitude with how fast we're actually moving so the bob eases in/out
            float maxSpd    = running ? _movement.runSpeed : (crouching ? _movement.crouchSpeed : _movement.walkSpeed);
            float speedRatio = Mathf.Clamp01(speed / Mathf.Max(maxSpd, 0.01f));

            _bobTimer   += Time.deltaTime * freq;
            _bobOffsetY  = Mathf.Sin(_bobTimer)        * amp * speedRatio;
            _bobOffsetX  = Mathf.Sin(_bobTimer * 0.5f) * amp * 0.4f * speedRatio;
        }
        else
        {
            // Smoothly kill the bob when stopping
            _bobOffsetY = Mathf.Lerp(_bobOffsetY, 0f, Time.deltaTime * 10f);
            _bobOffsetX = Mathf.Lerp(_bobOffsetX, 0f, Time.deltaTime * 10f);
        }

        // --- Breathing ---
        // Always present. Resting = barely noticeable. Sprinting / post-jump = heavy heaving.
        float breathY     = 0f;
        float breathPitch = 0f;

        if (enableBreathing)
        {
            float freq = Mathf.Lerp(breathFreqRest, breathFreqMax, exertion);
            float amp  = Mathf.Lerp(breathAmpRest,  breathAmpMax,  exertion);

            _breathTimer += Time.deltaTime * freq * Mathf.PI * 2f;

            breathY     = Mathf.Sin(_breathTimer) * amp;
            // Slight pitch nudge so your view dips very slightly with each exhale
            breathPitch = Mathf.Sin(_breathTimer) * Mathf.Lerp(0f, breathPitchMax, exertion);
        }

        // Keep body camera in sync with main camera FOV
        if (_bodyCam != null)
            _bodyCam.fieldOfView = _mainCam.fieldOfView;

        // --- Final position + rotation ---
        float   scale   = target.lossyScale.y;
        Vector3 basePos = target.position
                        + Vector3.up   * (_currentEyeH + _bobOffsetY + breathY)
                        + target.right * _bobOffsetX;

        // Cast a short ray in the camera's look direction AND straight forward from the head.
        // Whichever wall is closest wins — this is the "camera hitbox" approach.
        float fwd = forwardOffset * scale;

        // Ray 1: straight forward from eye (catches walls the character faces)
        if (Physics.Raycast(basePos, target.forward, out RaycastHit fwdHit, fwd + wallPadding, wallMask, QueryTriggerInteraction.Ignore))
            fwd = Mathf.Max(0f, fwdHit.distance - wallPadding);

        // Ray 2: in the actual camera look direction (catches walls above/below when pitching)
        Vector3 lookDir = transform.rotation * Vector3.forward;
        if (Physics.Raycast(basePos, lookDir, out RaycastHit lookHit, fwd + wallPadding, wallMask, QueryTriggerInteraction.Ignore))
            fwd = Mathf.Max(0f, Mathf.Min(fwd, lookHit.distance - wallPadding));

        transform.position = basePos + target.forward * fwd;
        transform.rotation = Quaternion.Euler(
            _xRotation + breathPitch,
            target.eulerAngles.y,
            0f
        );
    }
}
