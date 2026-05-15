using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PCInteraction : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag PCScreenMain here so the desktop image is applied to it")]
    public Renderer pcScreenRenderer;
    [Tooltip("The Windows 7 desktop screenshot texture")]
    public Texture2D desktopTexture;

    [Header("Settings")]
    public float interactDistance = 3f;

    static readonly string MESSAGE =
        "Inside an old downtown bank, the main doors unlock at <b>nine</b> in the morning, " +
        "the security room monitors <b>six</b> active cameras across the hall, " +
        "the front desk is always staffed by <b>four</b> tellers during working hours, " +
        "and the final safety system is protected by <b>three</b> independent alarm checks before closing.";

    Transform       _player;
    TextMeshProUGUI _promptText;
    GameObject      _screenPanel;
    bool            _open;
    bool            _animating;
    CanvasGroup     _group;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;

        // Apply desktop texture to the 3D screen in the scene
        if (pcScreenRenderer != null && desktopTexture != null)
            pcScreenRenderer.material.mainTexture = desktopTexture;

        AddScreenLight();

        BuildUI();
    }

    void Update()
    {
        if (_animating) return;

        float dist = _player != null
            ? Vector3.Distance(transform.position, _player.position)
            : float.MaxValue;

        if (!_open)
        {
            _promptText.text = dist <= interactDistance ? "Press  E  to interact" : "";
            if (dist <= interactDistance && Input.GetKeyDown(KeyCode.E))
                StartCoroutine(OpenScreen());
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.Escape)) StartCoroutine(CloseScreen());
        }
    }

    // ── Open / Close ──────────────────────────────────────────────────────────

    IEnumerator OpenScreen()
    {
        _animating = true;
        _promptText.text = "";
        _screenPanel.SetActive(true);
        _open = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        // Fade in
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.3f;
            _group.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        _group.alpha = 1f;
        _animating = false;
    }

    IEnumerator CloseScreen()
    {
        _animating = true;

        // Fade out
        float t = 1f;
        while (t > 0f)
        {
            t -= Time.deltaTime / 0.3f;
            _group.alpha = Mathf.Clamp01(t);
            yield return null;
        }
        _group.alpha = 0f;

        _screenPanel.SetActive(false);
        _open = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        _animating = false;
    }

    // ── Screen light ──────────────────────────────────────────────────────────

    void AddScreenLight()
    {
        if (pcScreenRenderer == null) return;

        var lightGo = new GameObject("ScreenLight");
        lightGo.transform.SetParent(pcScreenRenderer.transform);
        lightGo.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        lightGo.transform.localRotation = Quaternion.identity;

        var lt       = lightGo.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = new Color(0.9f, 0.95f, 1f);
        lt.intensity = 1.5f;
        lt.range     = 3f;
        lt.shadows   = LightShadows.None;
    }

    // ── UI ────────────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var cObj = new GameObject("PCInteractionCanvas");
        var cv   = cObj.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 300;
        cObj.AddComponent<CanvasScaler>();
        cObj.AddComponent<GraphicRaycaster>();

        // Prompt text
        var pObj = new GameObject("Prompt");
        pObj.transform.SetParent(cObj.transform, false);
        _promptText           = pObj.AddComponent<TextMeshProUGUI>();
        _promptText.text      = "";
        _promptText.fontSize  = 22f;
        _promptText.color     = Color.white;
        _promptText.alignment = TextAlignmentOptions.Center;
        var pr            = _promptText.GetComponent<RectTransform>();
        pr.anchorMin      = new Vector2(0.5f, 0f);
        pr.anchorMax      = new Vector2(0.5f, 0f);
        pr.pivot          = new Vector2(0.5f, 0f);
        pr.anchoredPosition = new Vector2(0f, 80f);
        pr.sizeDelta      = new Vector2(600f, 50f);

        // Full-screen panel
        _screenPanel = new GameObject("ScreenPanel");
        _screenPanel.transform.SetParent(cObj.transform, false);
        _group = _screenPanel.AddComponent<CanvasGroup>();

        // Desktop background
        var bg    = _screenPanel.AddComponent<Image>();
        bg.color  = new Color(0.17f, 0.38f, 0.57f); // Win7 blue fallback
        if (desktopTexture != null)
        {
            bg.sprite = Sprite.Create(desktopTexture,
                new Rect(0, 0, desktopTexture.width, desktopTexture.height),
                new Vector2(0.5f, 0.5f));
            bg.type = Image.Type.Simple;
            bg.preserveAspect = false;
        }
        var bgR = bg.GetComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero;
        bgR.anchorMax = Vector2.one;
        bgR.offsetMin = bgR.offsetMax = Vector2.zero;

        // Notepad window (centre of screen)
        var notepad    = new GameObject("Notepad");
        notepad.transform.SetParent(_screenPanel.transform, false);
        var npImg      = notepad.AddComponent<Image>();
        npImg.color    = new Color(0.96f, 0.96f, 0.96f);
        var npR        = npImg.GetComponent<RectTransform>();
        npR.anchorMin  = new Vector2(0.5f, 0.5f);
        npR.anchorMax  = new Vector2(0.5f, 0.5f);
        npR.pivot      = new Vector2(0.5f, 0.5f);
        npR.sizeDelta  = new Vector2(700f, 200f);
        npR.anchoredPosition = Vector2.zero;

        // Title bar
        var titleBar   = new GameObject("TitleBar");
        titleBar.transform.SetParent(notepad.transform, false);
        var tbImg      = titleBar.AddComponent<Image>();
        tbImg.color    = new Color(0.07f, 0.36f, 0.65f);
        var tbR        = tbImg.GetComponent<RectTransform>();
        tbR.anchorMin  = new Vector2(0f, 1f);
        tbR.anchorMax  = new Vector2(1f, 1f);
        tbR.pivot      = new Vector2(0.5f, 1f);
        tbR.offsetMin  = new Vector2(0f, -32f);
        tbR.offsetMax  = Vector2.zero;


        // Message text
        var msgObj = new GameObject("MessageText");
        msgObj.transform.SetParent(notepad.transform, false);
        var msg            = msgObj.AddComponent<TextMeshProUGUI>();
        msg.text           = MESSAGE;
        msg.fontSize       = 16f;
        msg.color          = new Color(0.1f, 0.1f, 0.1f);
        msg.alignment      = TextAlignmentOptions.TopLeft;
        msg.enableWordWrapping = true;
        var msgR           = msg.GetComponent<RectTransform>();
        msgR.anchorMin     = new Vector2(0f, 0f);
        msgR.anchorMax     = new Vector2(1f, 1f);
        msgR.offsetMin     = new Vector2(16f, 16f);
        msgR.offsetMax     = new Vector2(-16f, -40f);

        // Exit button — bottom right
        var exitBtn  = new GameObject("ExitButton");
        exitBtn.transform.SetParent(_screenPanel.transform, false);
        var btnImg   = exitBtn.AddComponent<Image>();
        btnImg.color = new Color(0.07f, 0.36f, 0.65f);
        var btnR     = btnImg.GetComponent<RectTransform>();
        btnR.anchorMin      = new Vector2(1f, 0f);
        btnR.anchorMax      = new Vector2(1f, 0f);
        btnR.pivot          = new Vector2(1f, 0f);
        btnR.sizeDelta      = new Vector2(120f, 40f);
        btnR.anchoredPosition = new Vector2(-20f, 20f);

        var btn = exitBtn.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.2f, 0.5f, 0.8f);
        btn.colors = colors;
        btn.onClick.AddListener(() => StartCoroutine(CloseScreen()));

        AddLabel(exitBtn.transform, "Exit", 16f, Color.white, Vector2.zero, new Vector2(120f, 40f));

        _screenPanel.SetActive(false);
    }

    void AddLabel(Transform parent, string text, float size, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var obj       = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        var tmp       = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var r         = tmp.GetComponent<RectTransform>();
        r.anchorMin   = new Vector2(0.5f, 0.5f);
        r.anchorMax   = new Vector2(0.5f, 0.5f);
        r.pivot       = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta   = sizeDelta;
    }
}
