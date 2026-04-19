using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Tooltip("The black full-screen Image (FadeOverlay)")]
    public Image fadeImage;

    void Awake()
    {
        Instance = this;
        SetFade(0f);
    }

    // ── Public controls ───────────────────────────────────────────────────────

    public void SetBlack() => SetFade(1f);
    public void SetClear() => SetFade(0f);

    public IEnumerator FadeToBlack(float duration)
    {
        float startAlpha = fadeImage != null ? fadeImage.color.a : 0f;
        yield return StartCoroutine(EaseFade(startAlpha, 1f, duration));
    }

    public IEnumerator FadeFromBlack(float duration)
    {
        float startAlpha = fadeImage != null ? fadeImage.color.a : 1f;
        yield return StartCoroutine(EaseFade(startAlpha, 0f, duration));
    }

    // ── Blink wakeup ──────────────────────────────────────────────────────────
    // Pattern: open → close → [LONG shut] → open → close → [MEDIUM shut]
    //        → open → close → [SHORT shut] → final slow open
    // Blur clears gradually across all blinks.
    public IEnumerator BlinkWakeup()
    {
        BlurController.Instance.SetBlur(1f);

        // ── Blink 1 ─────────────────────────────────────────────────────────
        // Heavy eyes, barely crack open then snap shut. Long pause after.
        yield return StartCoroutine(EaseFade(1f, 0.4f, 0.5f));    // crack open slowly
        yield return new WaitForSeconds(0.06f);                     // hold barely open
        yield return StartCoroutine(EaseFade(0.4f, 1f, 0.4f));    // drift shut
        // Eyes stay shut — brain is fighting to stay asleep
        yield return new WaitForSeconds(0.9f);
        yield return StartCoroutine(BlurController.Instance.FadeBlur(1f, 0.72f, 0.4f));

        // ── Blink 2 ─────────────────────────────────────────────────────────
        // Opens a bit more this time, still can't hold. Medium pause after.
        yield return StartCoroutine(EaseFade(1f, 0.12f, 0.55f));  // open wider
        yield return new WaitForSeconds(0.1f);                      // hold a moment
        yield return StartCoroutine(EaseFade(0.12f, 1f, 0.38f));  // slide shut
        // Medium pause — getting closer to being awake
        yield return new WaitForSeconds(0.55f);
        yield return StartCoroutine(BlurController.Instance.FadeBlur(0.72f, 0.38f, 0.3f));

        // ── Blink 3 ─────────────────────────────────────────────────────────
        // Opens properly, holds, then falls shut one last time. Short pause.
        yield return StartCoroutine(EaseFade(1f, 0.05f, 0.6f));   // fully open, slow
        yield return new WaitForSeconds(0.18f);                     // hold open longer
        yield return StartCoroutine(EaseFade(0.05f, 1f, 0.45f));  // heavy close
        // Short pause — almost awake now
        yield return new WaitForSeconds(0.28f);
        yield return StartCoroutine(BlurController.Instance.FadeBlur(0.38f, 0.1f, 0.25f));

        // ── Final open ───────────────────────────────────────────────────────
        // Eyes open for good, slow and groggy. Blur fades out last.
        yield return StartCoroutine(EaseFade(1f, 0f, 0.95f));
        yield return StartCoroutine(BlurController.Instance.FadeBlur(0.1f, 0f, 0.8f));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    IEnumerator EaseFade(float from, float to, float duration)
    {
        if (fadeImage == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetFade(Mathf.Lerp(from, to, s));
            yield return null;
        }
        SetFade(to);
    }

    void SetFade(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
