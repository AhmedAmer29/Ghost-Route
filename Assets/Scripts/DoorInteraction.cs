using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DoorInteraction : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] float openAngle = 90f;
    [SerializeField] float animationSpeed = 2.5f;

    [Header("Interaction")]
    [Tooltip("How close the player must be to see the prompt and press E")]
    [SerializeField] float interactDistance = 2.5f;

    [Header("Prompt UI (optional — auto-created if empty)")]
    [SerializeField] TextMeshProUGUI promptLabel;

    bool _isOpen;
    float _currentAngle;
    float _targetAngle;
    Quaternion _closedRotation;
    Transform _player;
    Camera _cam;
    bool _promptVisible;

    void Start()
    {
        _closedRotation = transform.localRotation;

        // Find player — works with or without "Player" tag
        _player = FindPlayer();

        // Find the actual game camera (PlayerCamera component beats Camera.main)
        _cam = FindGameCamera();

        EnsureCollider();
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

    // ── Helpers ────────────────────────────────────────────────────────────

    Transform FindPlayer()
    {
        // Prefer tag, fall back to PlayerMovement component
        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null) return tagged.transform;

        var movement = FindObjectOfType<PlayerMovement>();
        return movement != null ? movement.transform : null;
    }

    Camera FindGameCamera()
    {
        // Prefer the camera that has a PlayerCamera controller on it
        var playerCam = FindObjectOfType<PlayerCamera>();
        if (playerCam != null) return playerCam.GetComponent<Camera>();

        // Fall back to Camera.main (tagged MainCamera)
        return Camera.main;
    }

    bool IsPlayerNearby()
    {
        if (_player == null) return false;
        return Vector3.Distance(transform.position, _player.position) <= interactDistance;
    }

    bool IsLookingAtDoor()
    {
        if (_cam == null) return true; // no camera — allow interaction by proximity alone

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance + 1f, ~0, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform)
            || transform.IsChildOf(hit.collider.transform);
    }

    void ToggleDoor()
    {
        // Require the player to be roughly facing the door before opening
        if (!IsLookingAtDoor()) return;

        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? openAngle : 0f;

        if (promptLabel != null)
            promptLabel.text = _isOpen ? "Press <b>E</b> to close" : "Press <b>E</b> to open";
    }

    void AnimateDoor()
    {
        if (Mathf.Approximately(_currentAngle, _targetAngle)) return;

        _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, animationSpeed * openAngle * Time.deltaTime);
        transform.localRotation = _closedRotation * Quaternion.Euler(0f, _currentAngle, 0f);
    }

    // ── Collider ──────────────────────────────────────────────────────────

    void EnsureCollider()
    {
        // If this object or any child already has a collider, we're fine
        if (GetComponentInChildren<Collider>(true) != null) return;

        // Auto-size a box collider from the renderers in this object
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = transform.InverseTransformPoint(bounds.center);
        box.size = Vector3.Scale(bounds.size, new Vector3(
            1f / transform.lossyScale.x,
            1f / transform.lossyScale.y,
            1f / transform.lossyScale.z));
    }

    // ── Prompt UI ─────────────────────────────────────────────────────────

    void SetPromptVisible(bool visible)
    {
        _promptVisible = visible;
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(visible);
    }

    void EnsurePromptUI()
    {
        if (promptLabel != null) return;

        Canvas canvas = CreateOverlayCanvas();

        GameObject go = new GameObject("DoorPrompt_" + gameObject.name);
        go.transform.SetParent(canvas.transform, false);

        AddPromptBackground(go);

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

    void AddPromptBackground(GameObject parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent.transform, false);

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.5f);

        RectTransform bgRT = img.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(-14f, -8f);
        bgRT.offsetMax = new Vector2(14f, 8f);
    }

    Canvas CreateOverlayCanvas()
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
}
