using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DoorInteraction : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Degrees the door swings open (positive = one way, negative = other way)")]
    [SerializeField] float openAngle = 90f;
    [SerializeField] float animationSpeed = 2.5f;

    [Header("Hinge")]
    [Tooltip("Local-space offset from the door's center to the hinge edge.\n" +
             "X = half the door width (positive or negative depending on hinge side).\n" +
             "Use Scene view gizmo to verify. Default 0 = center pivot.")]
    [SerializeField] Vector3 hingeLocalOffset = new Vector3(-0.44f, 0f, 0f);

    [Header("Interaction")]
    [SerializeField] float interactDistance = 2.5f;

    [Header("Prompt UI (optional — auto-created if empty)")]
    [SerializeField] TextMeshProUGUI promptLabel;

    bool _isOpen;
    float _currentAngle;
    float _targetAngle;

    Vector3 _closedWorldPos;
    Quaternion _closedWorldRot;
    Vector3 _hingeAxis;

    Collider[] _doorColliders;
    Transform _player;
    Camera _cam;
    bool _promptVisible;

    void Start()
    {
        // Capture door's rest state in WORLD space so complex parent rotations don't interfere
        _closedWorldPos = transform.position;
        _closedWorldRot = transform.rotation;

        // The hinge axis is the door's own "up" direction in world space
        _hingeAxis = transform.up;

        _player = FindPlayer();
        _cam = FindGameCamera();

        EnsureCollider();

        // Collect only colliders on this door object and its children (not the parent/frame/wall)
        _doorColliders = GetComponentsInChildren<Collider>(true);

        EnsurePromptUI();
        SetPromptVisible(false);
    }

    void Update()
    {
        bool inRange = IsPlayerNearby();

        if (inRange != _promptVisible)
            SetPromptVisible(inRange);

        if (inRange && Input.GetKeyDown(KeyCode.E))
            ToggleDoor();

        AnimateDoor();
    }

    // ── Core ──────────────────────────────────────────────────────────────

    void ToggleDoor()
    {
        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? openAngle : 0f;

        // Immediately make the door passable when opening, solid when closing
        SetCollidersEnabled(!_isOpen);

        if (promptLabel != null)
            promptLabel.text = _isOpen ? "Press <b>E</b> to close" : "Press <b>E</b> to open";
    }

    void AnimateDoor()
    {
        if (Mathf.Approximately(_currentAngle, _targetAngle)) return;

        _currentAngle = Mathf.MoveTowards(
            _currentAngle, _targetAngle,
            animationSpeed * Mathf.Abs(openAngle) * Time.deltaTime);

        // World-space hinge point
        Vector3 hingeWorld = _closedWorldPos + _closedWorldRot * hingeLocalOffset;

        // Rotation of the door around the hinge axis
        Quaternion swing = Quaternion.AngleAxis(_currentAngle, _hingeAxis);

        transform.rotation = swing * _closedWorldRot;
        transform.position = hingeWorld + swing * (_closedWorldPos - hingeWorld);
    }

    void SetCollidersEnabled(bool solid)
    {
        if (_doorColliders == null) return;
        foreach (Collider col in _doorColliders)
            col.enabled = solid;
    }

    // ── Detection ─────────────────────────────────────────────────────────

    bool IsPlayerNearby()
    {
        if (_player == null) return false;
        return Vector3.Distance(transform.position, _player.position) <= interactDistance;
    }

    // ── Setup ─────────────────────────────────────────────────────────────

    Transform FindPlayer()
    {
        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null) return tagged.transform;

        var mv = FindObjectOfType<PlayerMovement>();
        return mv != null ? mv.transform : null;
    }

    Camera FindGameCamera()
    {
        var pc = FindObjectOfType<PlayerCamera>();
        if (pc != null) return pc.GetComponent<Camera>();
        return Camera.main;
    }

    void EnsureCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = transform.InverseTransformPoint(b.center);
        box.size = new Vector3(
            b.size.x / transform.lossyScale.x,
            b.size.y / transform.lossyScale.y,
            b.size.z / transform.lossyScale.z);
    }

    // ── UI ────────────────────────────────────────────────────────────────

    void SetPromptVisible(bool visible)
    {
        _promptVisible = visible;
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(visible);
    }

    void EnsurePromptUI()
    {
        if (promptLabel != null) return;

        Canvas canvas = BuildOverlayCanvas();

        GameObject go = new GameObject("DoorPrompt_" + gameObject.name);
        go.transform.SetParent(canvas.transform, false);

        AddBackground(go);

        promptLabel = go.AddComponent<TextMeshProUGUI>();
        promptLabel.text = "Press <b>E</b> to open";
        promptLabel.fontSize = 20f;
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.color = new Color(1f, 1f, 1f, 0.95f);

        RectTransform rt = promptLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.sizeDelta = new Vector2(300f, 44f);
    }

    void AddBackground(GameObject parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent.transform, false);

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-14f, -8f);
        rt.offsetMax = new Vector2(14f, 8f);
    }

    Canvas BuildOverlayCanvas()
    {
        GameObject go = new GameObject("DoorInteractionCanvas");
        DontDestroyOnLoad(go);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the hinge point so you can verify it in Scene view
        Vector3 hinge = transform.position + transform.rotation * hingeLocalOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hinge, 0.06f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(hinge, transform.up * 0.5f);
    }
}
