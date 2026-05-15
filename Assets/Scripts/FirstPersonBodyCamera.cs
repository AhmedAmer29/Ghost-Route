using UnityEngine;

// Attach to the first-person camera.
//
// SETUP (do this once in the editor, NOT in Play mode):
//   1. Select each body part you want visible when looking down
//      (torso, legs, arms — NOT the head, NOT collider-only objects).
//   2. In the Inspector top, change their Layer dropdown to "CriminalBody" (layer 9).
//   3. Add this script to your first-person camera. Done.
//
// The script sets up a second camera that renders only layer 9 with a
// tiny near-clip so legs/hands show cleanly. The main camera is blocked
// from seeing layer 9, so you never clip inside the mesh.

[RequireComponent(typeof(Camera))]
public class FirstPersonBodyCamera : MonoBehaviour
{
    [Tooltip("Layer assigned to your body parts in the editor (default 9 = CriminalBody)")]
    [Range(8, 31)]
    public int bodyLayer = 9;

    private Camera _mainCam;
    private Camera _bodyCam;

    void Awake()
    {
        _mainCam = GetComponent<Camera>();

        // Block main camera from ever seeing the body layer
        _mainCam.cullingMask &= ~(1 << bodyLayer);

        // Child camera composites the body on top
        var bodyGO = new GameObject("_BodyCamera");
        bodyGO.transform.SetParent(transform, false);

        _bodyCam               = bodyGO.AddComponent<Camera>();
        _bodyCam.clearFlags    = CameraClearFlags.Depth;
        _bodyCam.cullingMask   = 1 << bodyLayer;
        _bodyCam.nearClipPlane = 0.01f;
        _bodyCam.farClipPlane  = _mainCam.farClipPlane;
        _bodyCam.depth         = _mainCam.depth + 1;
        _bodyCam.fieldOfView   = _mainCam.fieldOfView;
        _bodyCam.rect          = _mainCam.rect;
        _bodyCam.allowHDR      = _mainCam.allowHDR;
    }

    void LateUpdate()
    {
        _bodyCam.fieldOfView = _mainCam.fieldOfView;
    }
}
