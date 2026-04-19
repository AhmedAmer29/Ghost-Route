using UnityEngine;

/// <summary>
/// Handles two camera "states":
///   1) Menu state  – camera pulled back a bit, with a soft mouse-follow parallax
///   2) Gameplay state – camera zoomed into the first-person POV
///
/// HOW TO USE:
///   - Attach this script to your Main Camera.
///   - Create two empty GameObjects in the scene:
///       * "CameraPos_Menu"     – place/rotate it where the menu shot should sit (slightly pulled back)
///       * "CameraPos_Gameplay" – place/rotate it where the first-person POV should sit
///   - Drag both into the "Menu Position" and "Gameplay Position" slots in the Inspector.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Positions (create empty GameObjects in the scene)")]
    public Transform menuPosition;
    public Transform gameplayPosition;

    [Header("Field of View")]
    public float menuFOV = 70f;
    public float gameplayFOV = 55f;

    [Header("Transition")]
    [Tooltip("Higher = faster zoom when you click Play")]
    public float transitionSpeed = 2.5f;

    [Header("Mouse Parallax (only active in Menu state)")]
    [Tooltip("How far the camera drifts with the mouse")]
    public float parallaxAmount = 0.35f;
    [Tooltip("How smoothly it eases toward the mouse")]
    public float parallaxSmooth = 4f;

    private Camera cam;
    private bool inMenu = true;
    private Vector3 parallaxOffset;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Snap to the menu position instantly when the game starts
        if (menuPosition != null)
        {
            transform.position = menuPosition.position;
            transform.rotation = menuPosition.rotation;
            cam.fieldOfView = menuFOV;
        }
    }

    void Update()
    {
        Transform target = inMenu ? menuPosition : gameplayPosition;
        if (target == null) return;

        // Base target position
        Vector3 desiredPos = target.position;

        // Mouse parallax, only in menu
        if (inMenu)
        {
            float mx = (Input.mousePosition.x / Screen.width  - 0.5f) * 2f; // -1..1
            float my = (Input.mousePosition.y / Screen.height - 0.5f) * 2f; // -1..1

            Vector3 desiredOffset =
                target.right * mx * parallaxAmount +
                target.up    * my * parallaxAmount;

            parallaxOffset = Vector3.Lerp(parallaxOffset, desiredOffset, Time.deltaTime * parallaxSmooth);
            desiredPos += parallaxOffset;
        }
        else
        {
            // Fade parallax out during the zoom-in
            parallaxOffset = Vector3.Lerp(parallaxOffset, Vector3.zero, Time.deltaTime * parallaxSmooth);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * transitionSpeed);

        float targetFOV = inMenu ? menuFOV : gameplayFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * transitionSpeed);
    }

    /// <summary>Switch between menu and gameplay camera state.</summary>
    public void SetMenuState(bool menu)
    {
        inMenu = menu;
    }
}
