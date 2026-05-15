using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PressurePlateReveal : MonoBehaviour
{
    [Tooltip("Correct plates are safe. Unchecked plates kill the player.")]
    public bool isCorrect = false;

    Renderer[]  _renderers;
    Collider    _col;

    // Shared UI — created once, reused by all plates
    static Canvas          _canvas;
    static TextMeshProUGUI _msgText;
    static Coroutine       _hideRoutine;

    // ── Setup ────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb   = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }

        // Accept any collider type (Box or Mesh)
        _col = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (_col != null)
        {
            if (_col is MeshCollider mc) mc.convex = true;
            _col.isTrigger = true;
        }

        EnsureUI();
    }

    void Start()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        Hide();
    }

    static void EnsureUI()
    {
        if (_canvas != null) return;

        var cObj = new GameObject("PlateMessageCanvas");
        Object.DontDestroyOnLoad(cObj);
        _canvas               = cObj.AddComponent<Canvas>();
        _canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder  = 999;
        cObj.AddComponent<CanvasScaler>();
        cObj.AddComponent<GraphicRaycaster>();

        var tObj = new GameObject("PlateMsg");
        tObj.transform.SetParent(cObj.transform, false);
        _msgText               = tObj.AddComponent<TextMeshProUGUI>();
        _msgText.text          = "";
        _msgText.fontSize      = 36f;
        _msgText.color         = Color.white;
        _msgText.alignment     = TextAlignmentOptions.Center;

        var r              = _msgText.GetComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 0f);
        r.anchorMax        = new Vector2(0.5f, 0f);
        r.pivot            = new Vector2(0.5f, 0f);
        r.anchoredPosition = new Vector2(0f, 80f);
        r.sizeDelta        = new Vector2(800f, 80f);
    }

    // ── Detection ────────────────────────────────────────────────────────────

    void Update()
    {
        if (isCorrect || _col == null) return;

        Bounds  b      = _col.bounds;
        Vector3 center = b.center + Vector3.up * 0.25f;
        Vector3 half   = b.extents + new Vector3(0.05f, 0.25f, 0.05f);

        Collider[] hits = Physics.OverlapBox(center, half, transform.rotation,
                                             ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Kill(hit.gameObject);
                return;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isCorrect && other.CompareTag("Player")) Kill(other.gameObject);
    }

    void OnTriggerStay(Collider other)
    {
        if (!isCorrect && other.CompareTag("Player")) Kill(other.gameObject);
    }

    // ── Kill ─────────────────────────────────────────────────────────────────

    void Kill(GameObject player)
    {
        ShowMessage("You have been caught");
        UVRevealController.ResetOnDeath();
        if (UVRevealController.Instance != null)
            UVRevealController.Instance.RespawnPlayer();
    }

    static void ShowMessage(string text)
    {
        if (_msgText == null) return;
        _msgText.text = text;
        if (_hideRoutine != null && _canvas != null)
            _canvas.GetComponent<MonoBehaviour>()?.StopCoroutine(_hideRoutine);
        _hideRoutine = _canvas.GetComponent<MonoBehaviour>()
                              ?.StartCoroutine(HideAfter(3f));
    }

    static IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_msgText != null) _msgText.text = "";
    }

    // ── Visual states ────────────────────────────────────────────────────────

    public void ShowWhite() => SetEmission(true, new Color(1f, 1f, 1f, 1f));
    public void ShowGreen() => SetEmission(true, new Color(0f, 3f, 0.5f, 1f));

    public void Hide()
    {
        if (_renderers == null) return;
        foreach (Renderer r in _renderers) r.enabled = false;
    }

    void SetEmission(bool show, Color color)
    {
        if (_renderers == null) return;
        foreach (Renderer r in _renderers)
        {
            r.enabled = show;
            foreach (Material m in r.materials)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color);
            }
        }
    }
}
