using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UVRevealController : MonoBehaviour
{
    public static UVRevealController Instance;

    // Reset to false on each player death so the button works again
    public static bool RevealUsed = false;

    [Header("Respawn")]
    [Tooltip("Where the player respawns after stepping on a wrong plate.")]
    public Transform spawnPoint;

    [Header("Timing")]
    public float revealDuration    = 10f;
    public float greenRevealDelay  = 2f;
    public float timeBetweenGreen  = 0.6f;

    [Header("UV Overlay")]
    public Color uvColor     = new Color(0.45f, 0f, 0.85f, 0.72f);
    public float fadeDuration = 0.6f;

    Image _overlay;
    bool  _running;

    void Awake()
    {
        Instance = this;
        BuildOverlay();
    }

    void BuildOverlay()
    {
        GameObject canvasGO = new GameObject("UVOverlayCanvas");
        DontDestroyOnLoad(canvasGO);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("UVOverlay");
        imgGO.transform.SetParent(canvasGO.transform, false);

        _overlay = imgGO.AddComponent<Image>();
        _overlay.color = new Color(uvColor.r, uvColor.g, uvColor.b, 0f);

        RectTransform rt = _overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void StartReveal()
    {
        if (_running || RevealUsed) return;
        RevealUsed = true;
        StartCoroutine(RevealSequence());
    }

    // Called by DetectionZone and PressurePlateReveal on player death
    public static void ResetOnDeath()
    {
        RevealUsed = false;
    }

    public void RespawnPlayer()
    {
        if (spawnPoint == null) return;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            player.transform.position = spawnPoint.position;
    }

    IEnumerator RevealSequence()
    {
        _running = true;

        PressurePlateReveal[] allPlates = FindObjectsOfType<PressurePlateReveal>();

        List<PressurePlateReveal> correctPlates = new List<PressurePlateReveal>();
        foreach (var p in allPlates)
            if (p.isCorrect) correctPlates.Add(p);

        // Fade UV in
        yield return StartCoroutine(FadeOverlay(0f, uvColor.a, fadeDuration));

        // All plates appear white
        foreach (var p in allPlates)
            p.ShowWhite();

        // Correct plates turn green one by one
        yield return new WaitForSeconds(greenRevealDelay);
        foreach (var p in correctPlates)
        {
            p.ShowGreen();
            yield return new WaitForSeconds(timeBetweenGreen);
        }

        // Hold for remainder
        float elapsed   = fadeDuration + greenRevealDelay + correctPlates.Count * timeBetweenGreen;
        float remaining = revealDuration - elapsed;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        // Fade UV out
        yield return StartCoroutine(FadeOverlay(uvColor.a, 0f, fadeDuration));

        // Hide all plates
        foreach (var p in allPlates)
            p.Hide();

        _running = false;
    }

    IEnumerator FadeOverlay(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _overlay.color = new Color(uvColor.r, uvColor.g, uvColor.b, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        _overlay.color = new Color(uvColor.r, uvColor.g, uvColor.b, to);
    }
}
