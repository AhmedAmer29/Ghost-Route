using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VaultCinematic : MonoBehaviour
{
    static VaultCinematic _instance;
    static bool           _played;

    // Lines of the monologue
    static readonly string LINE1 = "I think I finally understand how the thief managed to get into the vault.";
    static readonly string LINE2 = "It all makes sense now… every step they took,\nand how they stayed ahead of the security systems.";

    Canvas     _canvas;
    CanvasGroup _root;
    Image      _black;
    Image      _barTop, _barBottom;
    TextMeshProUGUI _line1Text, _line2Text, _cursor;

    // ── Entry point ───────────────────────────────────────────────────────────

    public static void Play()
    {
        if (_played) return;
        _played = true;

        var go = new GameObject("VaultCinematic");
        DontDestroyOnLoad(go);
        go.AddComponent<VaultCinematic>().StartCoroutine(
            go.GetComponent<VaultCinematic>().RunCinematic());
    }

    // ── Build UI ──────────────────────────────────────────────────────────────

    void Awake()
    {
        var cObj = new GameObject("CinCanvas");
        cObj.transform.SetParent(transform);
        _canvas              = cObj.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;
        cObj.AddComponent<CanvasScaler>();
        cObj.AddComponent<GraphicRaycaster>();

        _root       = cObj.AddComponent<CanvasGroup>();
        _root.alpha = 0f;

        // Full black background
        _black       = MakeImage(cObj.transform, Vector2.zero, Vector2.one, Color.black);
        StretchFull(_black.rectTransform);

        // Subtle vignette (dark radial edges)
        var vig  = MakeImage(cObj.transform, Vector2.zero, Vector2.one, new Color(0,0,0,0));
        StretchFull(vig.rectTransform);
        // We just keep it transparent — the black bg is enough

        // Letterbox bars
        _barTop    = MakeImage(cObj.transform, new Vector2(0f,1f), new Vector2(1f,1f), Color.black);
        _barTop.rectTransform.pivot          = new Vector2(0.5f, 1f);
        _barTop.rectTransform.anchoredPosition = Vector2.zero;
        _barTop.rectTransform.sizeDelta      = new Vector2(0f, 80f);

        _barBottom = MakeImage(cObj.transform, new Vector2(0f,0f), new Vector2(1f,0f), Color.black);
        _barBottom.rectTransform.pivot          = new Vector2(0.5f, 0f);
        _barBottom.rectTransform.anchoredPosition = Vector2.zero;
        _barBottom.rectTransform.sizeDelta      = new Vector2(0f, 80f);

        // Line 1 text
        _line1Text = MakeText(cObj.transform,
            new Vector2(0.5f, 0.55f), new Vector2(900f, 80f),
            22f, new Color(0.92f, 0.88f, 0.78f));

        // Line 2 text
        _line2Text = MakeText(cObj.transform,
            new Vector2(0.5f, 0.40f), new Vector2(900f, 90f),
            20f, new Color(0.70f, 0.66f, 0.58f));

        // Blinking cursor
        _cursor = MakeText(cObj.transform,
            new Vector2(0.5f, 0.55f), new Vector2(900f, 80f),
            22f, new Color(0.92f, 0.88f, 0.78f, 0.8f));
        _cursor.text      = "_";
        _cursor.alignment = TextAlignmentOptions.MidlineLeft;
    }

    // ── Cinematic sequence ───────────────────────────────────────────────────

    IEnumerator RunCinematic()
    {
        // Lock player input
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Phase 1 — Fade to black
        yield return Fade(_root, 0f, 1f, 1.2f);

        // Phase 2 — Bars slide in
        yield return SlideBars(0f, 80f, 0.6f);
        yield return new WaitForSeconds(0.8f);

        // Phase 3 — Typewrite line 1
        yield return TypeLine(_line1Text, LINE1, 0.045f);
        _cursor.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.4f);

        // Phase 4 — Typewrite line 2 (slower, more weight)
        UpdateCursorAnchor(0.40f);
        _cursor.gameObject.SetActive(true);
        yield return TypeLine(_line2Text, LINE2, 0.055f);
        _cursor.gameObject.SetActive(false);

        // Phase 5 — Hold with gentle breathing
        yield return Breathe(_line1Text, _line2Text, 3.5f);

        // Phase 6 — Fade everything out slowly
        yield return Fade(_root, 1f, 0f, 2.5f);

        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    IEnumerator Fade(CanvasGroup cg, float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t      += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator SlideBars(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float h = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur));
            _barTop.rectTransform.sizeDelta    = new Vector2(0f, h);
            _barBottom.rectTransform.sizeDelta = new Vector2(0f, h);
            yield return null;
        }
    }

    IEnumerator TypeLine(TextMeshProUGUI label, string fullText, float charDelay)
    {
        label.text = "";
        label.gameObject.SetActive(true);

        // Move cursor alongside the growing text
        for (int i = 0; i <= fullText.Length; i++)
        {
            label.text = fullText[..i];

            // Update cursor position to end of typed text
            _cursor.text = label.text + "_";
            _cursor.alpha = 1f;

            yield return new WaitForSeconds(charDelay);

            // Vary speed slightly at punctuation for drama
            if (i < fullText.Length && (fullText[i] == '.' || fullText[i] == ','))
                yield return new WaitForSeconds(0.25f);
        }
    }

    IEnumerator Breathe(TextMeshProUGUI a, TextMeshProUGUI b, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float alpha = 0.75f + 0.25f * Mathf.Sin(t * Mathf.PI);
            a.alpha = alpha;
            b.alpha = alpha;
            yield return null;
        }
    }

    void UpdateCursorAnchor(float anchorY)
    {
        var r = _cursor.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, anchorY);
        r.anchorMax = new Vector2(0.5f, anchorY);
        r.anchoredPosition = Vector2.zero;
    }

    // ── UI factories ─────────────────────────────────────────────────────────

    Image MakeImage(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go  = new GameObject("Img");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r   = img.rectTransform;
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        return img;
    }

    void StretchFull(RectTransform r)
    {
        r.anchorMin        = Vector2.zero;
        r.anchorMax        = Vector2.one;
        r.offsetMin        = Vector2.zero;
        r.offsetMax        = Vector2.zero;
    }

    TextMeshProUGUI MakeText(Transform parent, Vector2 anchor, Vector2 size, float fontSize, Color color)
    {
        var go  = new GameObject("Txt");
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize            = fontSize;
        tmp.color               = color;
        tmp.alignment           = TextAlignmentOptions.Center;
        tmp.enableWordWrapping  = true;
        tmp.fontStyle           = FontStyles.Italic;
        tmp.text                = "";
        var r = tmp.rectTransform;
        r.anchorMin         = anchor;
        r.anchorMax         = anchor;
        r.pivot             = new Vector2(0.5f, 0.5f);
        r.anchoredPosition  = Vector2.zero;
        r.sizeDelta         = size;
        go.SetActive(false);
        return tmp;
    }
}
