using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton UI manager.
/// Handles: proximity prompt (animated fade), typewriter dialogue, screen fade.
/// Auto-builds its own canvas — no prefab needed.
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    private CanvasGroup         _promptGroup;
    private TextMeshProUGUI     _promptKeyText;
    private TextMeshProUGUI     _promptActionText;

    private CanvasGroup         _subtitleGroup;
    private TextMeshProUGUI     _subtitleText;

    private Image               _fadePanel;

    private Coroutine _promptFadeCo;
    private Coroutine _subtitleCo;
    private Coroutine _fadeCo;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        try { BuildUI(); }
        catch (System.Exception e)
        {
            Debug.LogError($"[InteractionManager] BuildUI failed: {e}");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  UI CONSTRUCTION
    // ══════════════════════════════════════════════════════════════════════

    void BuildUI()
    {
        // Root canvas (plain GO — Canvas component adds its own RectTransform)
        var cGO = new GameObject("_InteractionCanvas");
        cGO.transform.SetParent(transform, false);
        var canvas = cGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = cGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        cGO.AddComponent<GraphicRaycaster>();

        // Full-screen fade panel (black, starts hidden)
        var fpGO   = Child("FadePanel", cGO.transform);
        _fadePanel = fpGO.AddComponent<Image>();
        _fadePanel.color = Color.black;
        Stretch(fpGO);
        fpGO.SetActive(false);

        // ── Proximity Prompt ──────────────────────────────────────────────
        //  [E]  Open door
        //  Key badge (thin outline square) + plain action text
        var promptAnchor = Child("PromptAnchor", cGO.transform);
        var paRect = EnsureRect(promptAnchor);
        paRect.anchorMin        = new Vector2(0.5f, 0f);
        paRect.anchorMax        = new Vector2(0.5f, 0f);
        paRect.pivot            = new Vector2(0.5f, 0f);
        paRect.anchoredPosition = new Vector2(0f, 130f);
        paRect.sizeDelta        = new Vector2(380f, 52f);

        _promptGroup                = promptAnchor.AddComponent<CanvasGroup>();
        _promptGroup.alpha          = 0f;
        _promptGroup.blocksRaycasts = false;

        var hLayout = promptAnchor.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment         = TextAnchor.MiddleCenter;
        hLayout.spacing                = 14f;
        hLayout.childForceExpandWidth  = false;
        hLayout.childForceExpandHeight = false;

        // Key badge
        var badge    = Child("KeyBadge", promptAnchor.transform);
        var badgeImg = badge.AddComponent<Image>();
        badgeImg.color = new Color(1f, 1f, 1f, 0.08f);
        var badgeLE = badge.AddComponent<LayoutElement>();
        badgeLE.preferredWidth = badgeLE.preferredHeight = 46f;

        // Thin border (slightly inset negative child)
        var borderGO   = Child("Border", badge.transform);
        var borderImg  = borderGO.AddComponent<Image>();
        borderImg.color = new Color(1f, 1f, 1f, 0.5f);
        var borderRect = EnsureRect(borderGO);
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-1.5f, -1.5f);
        borderRect.offsetMax = new Vector2( 1.5f,  1.5f);
        borderGO.transform.SetSiblingIndex(0);

        // Key letter
        var keyLetterGO    = Child("KeyLetter", badge.transform);
        _promptKeyText     = keyLetterGO.AddComponent<TextMeshProUGUI>();
        _promptKeyText.text      = "E";
        _promptKeyText.fontSize  = 24f;
        _promptKeyText.fontStyle = FontStyles.Bold;
        _promptKeyText.color     = Color.white;
        _promptKeyText.alignment = TextAlignmentOptions.Center;
        Stretch(keyLetterGO);

        // Action label
        var actionGO        = Child("ActionLabel", promptAnchor.transform);
        _promptActionText   = actionGO.AddComponent<TextMeshProUGUI>();
        _promptActionText.fontSize  = 24f;
        _promptActionText.color     = new Color(1f, 1f, 1f, 0.9f);
        _promptActionText.alignment = TextAlignmentOptions.MidlineLeft;
        var actionLE = actionGO.AddComponent<LayoutElement>();
        actionLE.preferredWidth  = 280f;
        actionLE.preferredHeight = 46f;

        // ── Subtitle / Dialogue ───────────────────────────────────────────
        //  thin top rule + italic text, no heavy background box
        var subAnchor = Child("SubtitleAnchor", cGO.transform);
        var saRect    = EnsureRect(subAnchor);
        saRect.anchorMin        = new Vector2(0.5f, 0f);
        saRect.anchorMax        = new Vector2(0.5f, 0f);
        saRect.pivot            = new Vector2(0.5f, 0f);
        saRect.anchoredPosition = new Vector2(0f, 60f);
        saRect.sizeDelta        = new Vector2(820f, 58f);

        _subtitleGroup                = subAnchor.AddComponent<CanvasGroup>();
        _subtitleGroup.alpha          = 0f;
        _subtitleGroup.blocksRaycasts = false;

        // Thin horizontal rule above text
        var ruleGO   = Child("Rule", subAnchor.transform);
        var ruleRect = EnsureRect(ruleGO);
        ruleRect.anchorMin        = new Vector2(0.15f, 1f);
        ruleRect.anchorMax        = new Vector2(0.85f, 1f);
        ruleRect.pivot            = new Vector2(0.5f, 1f);
        ruleRect.anchoredPosition = Vector2.zero;
        ruleRect.sizeDelta        = new Vector2(0f, 1f);
        var ruleImg = ruleGO.AddComponent<Image>();
        ruleImg.color = new Color(1f, 1f, 1f, 0.2f);

        // Subtitle text
        var stGO            = Child("SubtitleText", subAnchor.transform);
        _subtitleText       = stGO.AddComponent<TextMeshProUGUI>();
        _subtitleText.fontSize          = 27f;
        _subtitleText.color             = new Color(0.96f, 0.94f, 0.88f, 1f);
        _subtitleText.alignment         = TextAlignmentOptions.Center;
        _subtitleText.enableWordWrapping = true;
        _subtitleText.fontStyle         = FontStyles.Italic;
        Stretch(stGO);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════════════════════════════

    public void ShowPrompt(string key, string action)
    {
        if (_promptKeyText == null || _promptActionText == null || _promptGroup == null)
        {
            Debug.LogError("[InteractionManager] ShowPrompt called but UI not ready. Rebuilding...");
            try { BuildUI(); } catch (System.Exception e) { Debug.LogError(e); return; }
        }
        _promptKeyText.text    = key;
        _promptActionText.text = action;
        FadeGroup(_promptGroup, 1f, 0.2f, ref _promptFadeCo);
    }

    public void HidePrompt()
    {
        if (_promptGroup != null) FadeGroup(_promptGroup, 0f, 0.15f, ref _promptFadeCo);
    }

    /// Play lines with typewriter effect. Each line fades in, types out, holds, fades out.
    public void ShowDialogue(string[] lines, float[] durations, System.Action onDone = null)
    {
        if (_subtitleCo != null) StopCoroutine(_subtitleCo);
        _subtitleCo = StartCoroutine(DialogueRoutine(lines, durations, onDone));
    }

    public void ShowNotification(string message, float duration, System.Action onDone = null)
        => ShowDialogue(new[] { message }, new[] { duration }, onDone);

    public void FadeToScene(string sceneName, float delay = 0f, float fadeDuration = 2f)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeSceneRoutine(sceneName, delay, fadeDuration));
    }

    /// Fade to black, call onBlack (use for teleports), then fade back in.
    public void FadeOutThenIn(System.Action onBlack, float fadeOut = 1f, float hold = 1f, float fadeIn = 1f)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeOutInRoutine(onBlack, fadeOut, hold, fadeIn));
    }

    IEnumerator FadeOutInRoutine(System.Action onBlack, float fadeOut, float hold, float fadeIn)
    {
        // Fade to black
        _fadePanel.gameObject.SetActive(true);
        float t = 0f;
        while (t < fadeOut)
        {
            t += Time.deltaTime;
            _fadePanel.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, t / fadeOut));
            yield return null;
        }
        _fadePanel.color = Color.black;

        // Do the action (teleport etc.) while screen is black
        onBlack?.Invoke();

        yield return new WaitForSeconds(hold);

        // Fade back in
        t = 0f;
        while (t < fadeIn)
        {
            t += Time.deltaTime;
            _fadePanel.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(1f, 0f, t / fadeIn));
            yield return null;
        }
        _fadePanel.color = new Color(0f, 0f, 0f, 0f);
        _fadePanel.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  COROUTINES
    // ══════════════════════════════════════════════════════════════════════

    IEnumerator DialogueRoutine(string[] lines, float[] durations, System.Action onDone)
    {
        const float FADE_IN        = 0.28f;
        const float FADE_OUT       = 0.22f;
        const float CHARS_PER_SEC  = 36f;
        const float LINE_GAP       = 0.18f;

        for (int i = 0; i < lines.Length; i++)
        {
            string full     = lines[i];
            float  holdSecs = (durations != null && i < durations.Length) ? durations[i] : 3f;

            // Fade in
            _subtitleText.text = "";
            yield return FadeGroupTo(_subtitleGroup, 1f, FADE_IN);

            // Typewriter
            float elapsed = 0f;
            int shown = 0;
            while (shown < full.Length)
            {
                elapsed += Time.deltaTime;
                shown    = Mathf.Min(full.Length, Mathf.FloorToInt(elapsed * CHARS_PER_SEC));
                _subtitleText.text = full.Substring(0, shown);
                yield return null;
            }
            _subtitleText.text = full;

            // Hold (subtract typing time so total on-screen ≈ holdSecs)
            float typeTime  = full.Length / CHARS_PER_SEC;
            float remaining = Mathf.Max(0.4f, holdSecs - typeTime);
            yield return new WaitForSeconds(remaining);

            // Fade out
            yield return FadeGroupTo(_subtitleGroup, 0f, FADE_OUT);
            _subtitleText.text = "";

            if (i < lines.Length - 1)
                yield return new WaitForSeconds(LINE_GAP);
        }

        onDone?.Invoke();
    }

    IEnumerator FadeGroupTo(CanvasGroup grp, float target, float dur)
    {
        if (grp == null) yield break;
        float start = grp.alpha;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            grp.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        grp.alpha = target;
    }

    IEnumerator FadeSceneRoutine(string sceneName, float delay, float duration)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        _fadePanel.gameObject.SetActive(true);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _fadePanel.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        _fadePanel.color = Color.black;
        SceneManager.LoadScene(sceneName);

        // This manager is DontDestroyOnLoad, so the coroutine survives the load.
        // Wait a frame for the new scene to be active, then fade back in —
        // otherwise the player is stuck on a black screen.
        yield return null;
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _fadePanel.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(1f, 0f, t / duration));
            yield return null;
        }
        _fadePanel.color = new Color(0f, 0f, 0f, 0f);
        _fadePanel.gameObject.SetActive(false);
    }

    void FadeGroup(CanvasGroup grp, float target, float dur, ref Coroutine handle)
    {
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(FadeGroupTo(grp, target, dur));
    }

    // ── helpers ───────────────────────────────────────────────────────────
    static GameObject Child(string name, Transform parent)
    {
        // Include RectTransform in the constructor so it's present before SetParent,
        // which is required for any child of a Canvas hierarchy.
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var r = EnsureRect(go);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    static RectTransform EnsureRect(GameObject go)
        => go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
}
