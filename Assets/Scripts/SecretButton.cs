using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SecretButton : MonoBehaviour
{
    [Tooltip("How close the player must be to see the prompt.")]
    public float interactDistance = 2.5f;

    GameObject      _promptContainer; // toggle this — hides both background AND label
    TextMeshProUGUI _promptLabel;
    bool            _promptVisible;
    Transform       _player;

    void Start()
    {
        _player = FindPlayer();
        BuildPromptUI();
        SetPromptVisible(false);
    }

    void Update()
    {
        bool inRange = IsPlayerNearby();

        if (inRange != _promptVisible)
            SetPromptVisible(inRange);

        if (inRange && !UVRevealController.RevealUsed && Input.GetKeyDown(KeyCode.E))
        {
            SetPromptVisible(false);

            if (UVRevealController.Instance != null)
                UVRevealController.Instance.StartReveal();
            else
                Debug.LogWarning("SecretButton: No UVRevealController in scene.");
        }
    }

    bool IsPlayerNearby()
    {
        if (_player == null) return false;
        return Vector3.Distance(transform.position, _player.position) <= interactDistance;
    }

    Transform FindPlayer()
    {
        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null) return tagged.transform;
        var mv = FindObjectOfType<PlayerMovement>();
        return mv != null ? mv.transform : null;
    }

    void SetPromptVisible(bool visible)
    {
        _promptVisible = visible;
        if (_promptContainer != null)
            _promptContainer.SetActive(visible); // hides the whole panel including background
    }

    void BuildPromptUI()
    {
        GameObject canvasGO = new GameObject("SecretButtonCanvas");
        DontDestroyOnLoad(canvasGO);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Background panel — this is what we toggle
        _promptContainer = new GameObject("PromptContainer");
        _promptContainer.transform.SetParent(canvasGO.transform, false);

        Image bg = _promptContainer.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);

        RectTransform bgRt = bg.rectTransform;
        bgRt.anchorMin        = new Vector2(0.5f, 0f);
        bgRt.anchorMax        = new Vector2(0.5f, 0f);
        bgRt.pivot            = new Vector2(0.5f, 0f);
        bgRt.anchoredPosition = new Vector2(0f, 80f);
        bgRt.sizeDelta        = new Vector2(328f, 44f);

        // Label inside the panel
        GameObject labelGO = new GameObject("PromptLabel");
        labelGO.transform.SetParent(_promptContainer.transform, false);

        _promptLabel = labelGO.AddComponent<TextMeshProUGUI>();
        _promptLabel.text      = "Press <b>E</b> to reveal";
        _promptLabel.fontSize  = 20f;
        _promptLabel.alignment = TextAlignmentOptions.Center;
        _promptLabel.color     = new Color(1f, 1f, 1f, 0.95f);

        RectTransform labelRt = _promptLabel.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(14f, 8f);
        labelRt.offsetMax = new Vector2(-14f, -8f);
    }
}
