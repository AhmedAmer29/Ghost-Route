using UnityEngine;
using UnityEngine.UI;

public class ObjectiveBoard : MonoBehaviour
{
    [Header("Proximity")]
    public float showDistance = 5f;
    public string playerTag = "Player";

    [Header("References (auto-found if empty)")]
    public MasterPowerSystem masterSystem;
    public LeverInteraction  lever;

    // Legacy fields kept for SewerToolsMenu Auto-Setup compatibility.
    // The proximity HUD builds its own UI and ignores these.
    [HideInInspector] public Text circuitText;
    [HideInInspector] public Text keyText;

    private Transform _player;
    private bool      _playerNear;

    private GameObject _panelGO;
    private Text       _bossLine;
    private Text       _circuitLine;
    private Text       _doneLine;

    void Start()
    {
        if (masterSystem == null) masterSystem = Object.FindFirstObjectByType<MasterPowerSystem>();
        if (lever        == null) lever        = Object.FindFirstObjectByType<LeverInteraction>();

        GameObject p = GameObject.FindWithTag(playerTag);
        if (p == null) p = GameObject.Find("Player");
        if (p != null) _player = p.transform;

        BuildHUD();
    }

    void Update()
    {
        if (_player == null) return;

        bool near = Vector3.Distance(transform.position, _player.position) <= showDistance;

        if (near != _playerNear)
        {
            _playerNear = near;
            if (_panelGO != null) _panelGO.SetActive(near);
        }

        if (_playerNear) RefreshUI();
    }

    void RefreshUI()
    {
        bool bossDone = BossHealth.HasKey;
        int count = masterSystem != null ? masterSystem.fixedCount  : 0;
        int total = masterSystem != null ? masterSystem.targetCount : 3;
        bool circuitsDone = total > 0 && count >= total;

        _bossLine.text  = bossDone ? "[X]  Boss defeated — key obtained"
                                   : "[ ]  Defeat the boss to get the key";
        _bossLine.color = bossDone ? new Color(0.45f, 1f, 0.45f) : Color.white;

        _circuitLine.text  = circuitsDone ? "[X]  Electrical circuits overloaded"
                                          : $"[ ]  Overload electrical circuits  ({count}/{total})";
        _circuitLine.color = circuitsDone ? new Color(0.45f, 1f, 0.45f) : Color.white;

        _doneLine.gameObject.SetActive(bossDone && circuitsDone);
    }

    // ── HUD ──────────────────────────────────────────────────────────────────
    void BuildHUD()
    {
        GameObject canvasGO = new GameObject("ObjectiveBoardCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        _panelGO = new GameObject("ObjectivePanel");
        _panelGO.transform.SetParent(canvasGO.transform, false);
        Image bg = _panelGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        RectTransform panelRT = _panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = new Vector2(0.5f, 0.78f);
        panelRT.pivot     = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(780, 260);

        MakeLabel(_panelGO.transform, "── OBJECTIVES ──",
            new Vector2(0, 90), 32, new Color(1f, 0.85f, 0.2f), FontStyle.Bold);

        _bossLine = MakeLabel(_panelGO.transform,
            "[ ]  Defeat the boss to get the key",
            new Vector2(0, 30), 26, Color.white, FontStyle.Normal);

        _circuitLine = MakeLabel(_panelGO.transform,
            "[ ]  Overload electrical circuits",
            new Vector2(0, -10), 26, Color.white, FontStyle.Normal);

        _doneLine = MakeLabel(_panelGO.transform,
            "All objectives complete — the way out is open",
            new Vector2(0, -70), 22, new Color(0.45f, 1f, 0.45f), FontStyle.Italic);
        _doneLine.gameObject.SetActive(false);

        _panelGO.SetActive(false);
    }

    Text MakeLabel(Transform parent, string text, Vector2 pos, int size, Color color, FontStyle style)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        Text t = go.AddComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = size;
        t.color     = color;
        t.text      = text;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = style;

        Shadow s = go.AddComponent<Shadow>();
        s.effectColor    = Color.black;
        s.effectDistance = new Vector2(1, -1);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(740, 50);
        return t;
    }
}
