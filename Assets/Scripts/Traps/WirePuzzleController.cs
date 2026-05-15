using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Procedural wire-matching puzzle.
/// Left nodes are in config order; right nodes are colour-shuffled.
/// Player drags from a LEFT node and drops on the matching-colour RIGHT node.
/// </summary>
public class WirePuzzleController : MonoBehaviour
{
    // ─── data ────────────────────────────────────────────────────────────
    private class LeftNode
    {
        public Color  color;
        public bool   isReal;          // does completing this wire count toward the master?
        public RectTransform rt;
        public bool   connected;
        public GameObject drawnLine;
    }

    private class RightNode
    {
        public Color  color;
        public RectTransform rt;
    }

    // ─── public API ──────────────────────────────────────────────────────
    public Transform leverHandle;
    public Vector3   leverDownRotation = new Vector3(45, 0, 0);

    // ─── private state ───────────────────────────────────────────────────
    private List<LeftNode>  _left  = new List<LeftNode>();
    private List<RightNode> _right = new List<RightNode>();

    private LeftNode     _dragging;
    private RectTransform _dragLineRT;
    private Image         _dragLineImg;
    private GameObject    _dragLineObj;

    private bool    _isSolved;
    private Canvas  _canvas;
    private RectTransform _panelRT;
    private Sprite  _circleSpr;
    private Sprite  _whiteSpr;

    // ─── entry point called by SubPowerBox ───────────────────────────────
    public void InitializePuzzle(List<LeverInteraction.WireConfig> cfg)
    {
        _circleSpr = MakeCircle();
        _whiteSpr  = MakeWhite();
        BuildUI(cfg);
    }

    void Start() { }   // intentionally empty

    // ─── every frame ─────────────────────────────────────────────────────
    void Update()
    {
        if (_isSolved) return;

        // ① press – pick up a left node
        if (Input.GetMouseButtonDown(0) && _dragging == null)
            TryPickUp(Input.mousePosition);

        // ② hold – stretch the temp wire
        if (_dragging != null && _dragLineRT != null)
            StretchLine(_dragLineRT,
                        _dragging.rt.position,
                        Input.mousePosition);

        // ③ release – try to snap onto a right node
        if (Input.GetMouseButtonUp(0) && _dragging != null)
            TryDrop(Input.mousePosition);
    }

    // ─── drag logic ──────────────────────────────────────────────────────
    void TryPickUp(Vector2 mouse)
    {
        foreach (var L in _left)
        {
            if (L.connected) continue;
            if (Vector2.Distance(mouse, L.rt.position) > 35f) continue;

            _dragging = L;

            // create temporary drag wire
            _dragLineObj = new GameObject("DragWire");
            _dragLineObj.transform.SetParent(_panelRT, false);
            _dragLineObj.transform.SetAsFirstSibling();
            _dragLineRT  = _dragLineObj.AddComponent<RectTransform>();
            _dragLineImg = _dragLineObj.AddComponent<Image>();
            _dragLineImg.sprite = _whiteSpr;
            _dragLineImg.color  = L.color;
            _dragLineRT.pivot   = new Vector2(0f, 0.5f);
            break;
        }
    }

    void TryDrop(Vector2 mouse)
    {
        bool snapped = false;

        foreach (var R in _right)
        {
            // Must be close enough AND same colour
            if (Vector2.Distance(mouse, R.rt.position) > 45f) continue;
            if (!ColorsMatch(R.color, _dragging.color))        continue;

            // Already something connected here? skip
            bool rightOccupied = false;
            foreach (var L in _left)
                if (L.connected && ColorsMatch(L.color, R.color)) { rightOccupied = true; break; }
            if (rightOccupied) break;

            // ── SNAP! ──────────────────────────────────────────────────
            snapped = true;
            _dragging.connected  = true;
            _dragging.drawnLine  = _dragLineObj;
            StretchLine(_dragLineRT, _dragging.rt.position, R.rt.position);
            _dragLineRT.sizeDelta = new Vector2(_dragLineRT.sizeDelta.x, 10f);

            // Glow the connected nodes green
            _dragging.rt.GetComponent<Image>().color = Color.Lerp(_dragging.color, Color.white, 0.4f);
            R.rt.GetComponent<Image>().color          = Color.Lerp(R.color, Color.white, 0.4f);

            Debug.Log($"<color=cyan>[WirePuzzle] Connected {ColorName(_dragging.color)} wire!</color>");
            CheckWin();
            break;
        }

        if (!snapped)
            Destroy(_dragLineObj);

        _dragging    = null;
        _dragLineRT  = null;
        _dragLineImg = null;
        _dragLineObj = null;
    }

    // ─── win condition ────────────────────────────────────────────────────
    void CheckWin()
    {
        // All 5 wires must be connected to solve the panel
        foreach (var L in _left)
            if (!L.connected) return;

        StartCoroutine(WinSequence());
    }

    IEnumerator WinSequence()
    {
        _isSolved = true;
        Debug.Log("<color=green>[WirePuzzle] Panel complete!</color>");
        yield return new WaitForSeconds(0.8f);
        if (leverHandle) StartCoroutine(RotateLever());
        Invoke(nameof(CloseUI), 1.2f);
    }

    IEnumerator RotateLever()
    {
        Quaternion s = leverHandle.localRotation;
        Quaternion e = s * Quaternion.Euler(leverDownRotation);
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            leverHandle.localRotation = Quaternion.Slerp(s, e, t);
            yield return null;
        }
    }

    public bool IsSolved() => _isSolved;
    public void CloseUI()   => Destroy(gameObject);

    // ─── UI construction ──────────────────────────────────────────────────
    void BuildUI(List<LeverInteraction.WireConfig> cfg)
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // panel background
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(transform, false);
        var bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.06f, 0.08f, 0.97f);
        _panelRT = bg.rectTransform;
        _panelRT.sizeDelta = new Vector2(640, 500);
        panelObj.AddComponent<Outline>().effectColor = new Color(0.25f, 0.25f, 0.25f);

        // labels
        Label(panelObj.transform, "⚡  ELECTRICAL OVERRIDE  ⚡", 26, new Vector2(0, 210), new Vector2(580, 45));
        Label(panelObj.transform, "Connect all matching wires to restore power", 15, new Vector2(0, 175), new Vector2(580, 28));
        Label(panelObj.transform, "PANEL",  18, new Vector2(-230, 140), new Vector2(100, 28));
        Label(panelObj.transform, "MAINS",  18, new Vector2( 230, 140), new Vector2(100, 28));

        // ── Left nodes (fixed order) ──────────────────────────────────────
        for (int i = 0; i < cfg.Count; i++)
        {
            float y = 100f - i * 55f;
            var L  = new LeftNode
            {
                color  = cfg[i].color,
                isReal = cfg[i].isReal,
                rt     = MakeNodeRT(panelObj.transform, cfg[i].color, new Vector2(-230, y))
            };
            _left.Add(L);
        }

        // ── Right nodes (shuffled) ────────────────────────────────────────
        List<Color> shuffledColors = new List<Color>();
        foreach (var c in cfg) shuffledColors.Add(c.color);
        Shuffle(shuffledColors);

        for (int i = 0; i < shuffledColors.Count; i++)
        {
            float y = 100f - i * 55f;
            var R = new RightNode
            {
                color = shuffledColors[i],
                rt    = MakeNodeRT(panelObj.transform, shuffledColors[i], new Vector2(230, y))
            };
            _right.Add(R);
        }
    }

    RectTransform MakeNodeRT(Transform parent, Color c, Vector2 pos)
    {
        GameObject go = new GameObject("Node");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = _circleSpr;
        img.color  = c;
        img.rectTransform.sizeDelta        = new Vector2(40, 40);
        img.rectTransform.anchoredPosition = pos;

        // subtle glow ring
        GameObject ring = new GameObject("Glow");
        ring.transform.SetParent(go.transform, false);
        var ri = ring.AddComponent<Image>();
        ri.sprite = _circleSpr;
        ri.color  = new Color(c.r, c.g, c.b, 0.25f);
        ri.rectTransform.sizeDelta = new Vector2(58, 58);
        ring.transform.SetAsFirstSibling();

        return img.rectTransform;
    }

    void Label(Transform p, string txt, int sz, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(p, false);
        var t = go.AddComponent<Text>();
        t.text      = txt;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = sz;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = Color.white;
        t.rectTransform.anchoredPosition = pos;
        t.rectTransform.sizeDelta        = size;
        go.AddComponent<Shadow>().effectColor = Color.black;
    }

    void StretchLine(RectTransform rt, Vector2 from, Vector2 to)
    {
        var dir      = to - from;
        rt.position  = from;
        rt.sizeDelta = new Vector2(dir.magnitude, 8f);
        rt.rotation  = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    // ─── helpers ──────────────────────────────────────────────────────────
    static bool ColorsMatch(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.05f &&
        Mathf.Abs(a.g - b.g) < 0.05f &&
        Mathf.Abs(a.b - b.b) < 0.05f;

    static string ColorName(Color c)
    {
        if (c == Color.red)    return "Red";
        if (c == Color.blue)   return "Blue";
        if (c == Color.yellow) return "Yellow";
        if (c == Color.green)  return "Green";
        return "White";
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }

    Sprite MakeCircle()
    {
        const int S = 64;
        var tex = new Texture2D(S, S) { filterMode = FilterMode.Bilinear };
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y,
                    Vector2.Distance(new Vector2(x, y), new Vector2(32, 32)) < 30f
                        ? Color.white : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
    }

    Sprite MakeWhite()
    {
        var tex = new Texture2D(2, 2);
        for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++) tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0f, 0.5f));
    }
}
