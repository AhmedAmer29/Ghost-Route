using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Positions")]
    public Transform menuPosition;
    public Transform gameplayPosition;

    [Header("Field of View")]
    public float menuFOV     = 70f;
    public float gameplayFOV = 55f;

    [Header("Transition")]
    [Tooltip("Lower = slower, more suspenseful zoom. 0.6 is cinematic.")]
    public float transitionSpeed = 0.6f;

    [Header("Mouse Parallax (menu only)")]
    public float parallaxAmount = 0.35f;
    public float parallaxSmooth = 4f;

    [Header("Head Tilt (after zoom-in)")]
    [Tooltip("How many degrees to tilt sideways")]
    public float tiltDegrees = 5f;
    [Tooltip("How slowly the tilt sways back and forth")]
    public float tiltSpeed   = 0.3f;

    [Header("Cube Focus")]
    [Tooltip("Turn this off if you don't want the camera to lean toward the cube")]
    public bool  cubeFocusEnabled  = true;
    public Transform cubeFocusTarget;
    [Tooltip("0 = ignore cube, 1 = fully locked on. Keep it low, e.g. 0.2")]
    [Range(0f, 1f)]
    public float cubeFocusStrength = 0.2f;
    [Tooltip("How far the FOV narrows when leaning toward the cube")]
    public float cubeFocusFOV      = 50f;
    public float cubeFocusSpeed    = 0.3f;

    // ── private ──────────────────────────────────────────────────────────────
    private Camera    cam;
    private bool      inMenu      = true;
    private bool      aliveActive = false;
    private Vector3   parallaxOffset;
    private float     tiltTime;
    private Quaternion cubeLeanRot;

    void Awake()
    {
        cam      = GetComponent<Camera>();
        tiltTime = Random.Range(0f, 6f);

        if (menuPosition != null)
        {
            transform.position = menuPosition.position;
            transform.rotation = menuPosition.rotation;
            cam.fieldOfView    = menuFOV;
        }

        cubeLeanRot = transform.rotation;
    }

    void Update()
    {
        Transform target = inMenu ? menuPosition : gameplayPosition;
        if (target == null) return;

        Vector3 desiredPos = target.position;

        // ── Menu: mouse parallax ───────────────────────────────────────────
        if (inMenu)
        {
            float mx = (Input.mousePosition.x / Screen.width  - 0.5f) * 2f;
            float my = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

            Vector3 desiredOffset =
                target.right * mx * parallaxAmount +
                target.up    * my * parallaxAmount;

            parallaxOffset = Vector3.Lerp(parallaxOffset, desiredOffset,
                                          Time.deltaTime * parallaxSmooth);
            desiredPos += parallaxOffset;
        }
        else
        {
            parallaxOffset = Vector3.Lerp(parallaxOffset, Vector3.zero,
                                          Time.deltaTime * parallaxSmooth);
        }

        // ── Slow zoom-in ───────────────────────────────────────────────────
        transform.position = Vector3.Lerp(transform.position, desiredPos,
                                          Time.deltaTime * transitionSpeed);

        // Activate alive feel once zoom has settled
        if (!inMenu && !aliveActive)
        {
            if (Vector3.Distance(transform.position, target.position) < 0.05f)
            {
                aliveActive = true;
                // Snap the lean base to the exact gameplay rotation so there's no drift
                cubeLeanRot = target.rotation;
                transform.rotation = target.rotation;
            }
        }

        // ── Alive: gentle side tilt only ──────────────────────────────────
        if (aliveActive)
        {
            tiltTime += Time.deltaTime;

            // Single-axis roll: slow sine wave, max tiltDegrees each way
            float roll  = Mathf.Sin(tiltTime * tiltSpeed) * tiltDegrees;
            Quaternion sideTilt = Quaternion.Euler(0f, 0f, roll);

            // Cube focus (optional)
            Quaternion baseRot = target.rotation;
            if (cubeFocusEnabled && cubeFocusTarget != null)
            {
                Vector3    dirToCube = cubeFocusTarget.position - transform.position;
                Quaternion lookAt    = Quaternion.LookRotation(dirToCube, transform.up);
                baseRot = Quaternion.Slerp(target.rotation, lookAt, cubeFocusStrength);
            }

            cubeLeanRot = Quaternion.Slerp(cubeLeanRot, baseRot,
                                           Time.deltaTime * cubeFocusSpeed);

            transform.rotation = cubeLeanRot * sideTilt;
        }
        else
        {
            // Still zooming in — just slerp toward target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation,
                                                   Time.deltaTime * transitionSpeed);
        }

        // ── FOV ───────────────────────────────────────────────────────────
        float targetFOV = inMenu ? menuFOV
                        : (aliveActive && cubeFocusEnabled && cubeFocusTarget != null)
                            ? cubeFocusFOV
                            : gameplayFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV,
                          Time.deltaTime * (aliveActive ? cubeFocusSpeed : transitionSpeed));
    }

    public void SetMenuState(bool menu)
    {
        inMenu      = menu;
        aliveActive = false;
        cubeLeanRot = transform.rotation;
    }
}
