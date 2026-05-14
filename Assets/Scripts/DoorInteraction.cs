using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DoorInteraction : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] float openAngle = 90f;
    [SerializeField] float animationSpeed = 2.5f;
    [SerializeField] Vector3 rotationAxis = Vector3.up;

    [Header("Interaction")]
    [SerializeField] float interactDistance = 2.5f;

    [Header("Prompt UI (optional — auto-created if empty)")]
    [SerializeField] TextMeshProUGUI promptLabel;

    bool _isOpen;
    float _currentAngle;
    float _targetAngle;
    Quaternion _closedRotation;
    Camera _cam;
    bool _promptVisible;

    static Canvas _sharedCanvas;

    void Start()
    {
        _closedRotation = transform.localRotation;
        _cam = Camera.main;

        EnsurePromptUI();
        SetPromptVisible(false);
    }

    void Update()
    {
        bool inRange = CheckLookingAtDoor();

        if (inRange != _promptVisible)
            SetPromptVisible(inRange);

        if (inRange && Input.GetKeyDown(KeyCode.E))
            ToggleDoor();

        AnimateDoor();
    }

    void ToggleDoor()
    {
        _isOpen = !_isOpen;
        _targetAngle = _isOpen ? openAngle : 0f;

        if (promptLabel != null)
            promptLabel.text = _isOpen ? "Press <b>E</b> to close" : "Press <b>E</b> to open";
    }

    void AnimateDoor()
    {
        if (Mathf.Approximately(_currentAngle, _targetAngle)) return;

        _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, animationSpeed * openAngle * Time.deltaTime);
        transform.localRotation = _closedRotation * Quaternion.AngleAxis(_currentAngle, rotationAxis);
    }

    bool CheckLookingAtDoor()
    {
        if (_cam == null) return false;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactDistance, ~0, QueryTriggerInteraction.Ignore))
            return false;

        return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
    }

    void SetPromptVisible(bool visible)
    {
        _promptVisible = visible;
        if (promptLabel != null)
            promptLabel.gameObject.SetActive(visible);
    }

    void EnsurePromptUI()
    {
        if (promptLabel != null) return;

        if (_sharedCanvas == null)
            _sharedCanvas = CreateOverlayCanvas();

        GameObject go = new GameObject("DoorPrompt_" + gameObject.name);
        go.transform.SetParent(_sharedCanvas.transform, false);

        promptLabel = go.AddComponent<TextMeshProUGUI>();
        promptLabel.text = "Press <b>E</b> to open";
        promptLabel.fontSize = 20f;
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.color = new Color(1f, 1f, 1f, 0.92f);
        promptLabel.fontStyle = FontStyles.Normal;

        RectTransform rt = promptLabel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.sizeDelta = new Vector2(280f, 40f);

        AddPromptBackground(go, rt);
    }

    void AddPromptBackground(GameObject parent, RectTransform labelRT)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent.transform, false);
        bg.transform.SetSiblingIndex(0);

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform bgRT = img.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = new Vector2(-12f, -6f);
        bgRT.offsetMax = new Vector2(12f, 6f);
    }

    static Canvas CreateOverlayCanvas()
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
