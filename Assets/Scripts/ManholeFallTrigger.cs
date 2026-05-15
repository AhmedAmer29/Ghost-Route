using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Sits just above the manhole hole. When the cover is OPEN and the player
// walks into this volume, smoothly fades the screen to pitch black and
// freezes the game. Spawns its own Canvas + black Image at runtime, so the
// scene doesn't need a ScreenFader prefab.
[RequireComponent(typeof(Collider))]
public class ManholeFallTrigger : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Time the fade-to-black takes (seconds).")]
    public float fadeDuration = 1.6f;

    [Tooltip("Two-stage fade: first ease to this alpha (glimpse of the sewer below), then go fully black. 0=disabled.")]
    [Range(0f, 1f)]
    public float glimpseAlpha = 0.35f;

    [Tooltip("Fraction of fadeDuration spent on the glimpse stage (0..1).")]
    [Range(0f, 1f)]
    public float glimpseFraction = 0.45f;

    [Tooltip("Freeze the game (Time.timeScale = 0) when fully black.")]
    public bool freezeGameWhenBlack = true;

    [Tooltip("Stay black forever after the fade. If false, fades back after holdDuration.")]
    public bool stayBlack = true;

    [Header("Scene Transition")]
    [Tooltip("Scene name to load after the fade completes. Leave empty to skip. If the scene isn't in Build Settings, the player just stays in black.")]
    public string nextSceneName = "sewer_maze";

    [Tooltip("Wait this long while fully black before loading the next scene (seconds, unscaled).")]
    public float preLoadHold = 0.4f;

    [Tooltip("Only used if stayBlack = false. Hold black this long before fading back (seconds).")]
    public float holdDuration = 1.0f;

    [Tooltip("Only used if stayBlack = false. Time to fade back from black (seconds).")]
    public float fadeBackDuration = 1.0f;

    [Header("Gating")]
    [Tooltip("If set, the trigger only fires when this manhole reports IsOpen == true.")]
    public ManholeInteraction manhole;

    [Tooltip("Player tag. Default 'Player'.")]
    public string playerTag = "Player";

    [Tooltip("How many times can the trigger fire? -1 for unlimited.")]
    public int triggerLimit = 1;

    [Header("Fall Gating")]
    [Tooltip("Only fire while the player is airborne (jumping or falling). Prevents firing when the player merely opens the cover while standing on/next to it.")]
    public bool requireAirborne = true;

    [Tooltip("Only fire when the player's feet are at or below the trigger's Y by this much. Set to 0 to disable this check and rely on the airborne gate alone.")]
    public float minDropBelowRim = 0f;

    [Tooltip("Log gate decisions to the console so you can see why TryFire was rejected.")]
    public bool debugLog = true;

    bool firing = false;
    int firedCount = 0;
    Canvas canvas;
    Image blackImage;

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (manhole == null) manhole = GetComponentInParent<ManholeInteraction>();
        BuildOverlay();
    }

    void BuildOverlay()
    {
        // A persistent fullscreen black image we own.
        var go = new GameObject("FallFadeOverlay_RUNTIME");
        DontDestroyOnLoad(go);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760; // on top of basically everything

        var imgGO = new GameObject("Black");
        imgGO.transform.SetParent(go.transform, false);
        blackImage = imgGO.AddComponent<Image>();
        blackImage.color = new Color(0f, 0f, 0f, 0f);
        blackImage.raycastTarget = false;

        var rt = blackImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnTriggerEnter(Collider other)
    {
        TryFire(other);
    }

    void OnTriggerStay(Collider other)
    {
        // Catch the case where the player is already inside the trigger when
        // the cover opens, or moves slowly along the boundary.
        TryFire(other);
    }

    void TryFire(Collider other)
    {
        if (firing) return;
        if (triggerLimit >= 0 && firedCount >= triggerLimit) return;
        if (manhole != null && !manhole.IsOpen)
        {
            // Don't spam log — only when player overlaps but cover is closed
            return;
        }
        if (!IsPlayer(other)) return;
        if (!IsPlayerFallingIn(other))
        {
            if (debugLog) Debug.Log($"[ManholeFallTrigger] Rejected: player not falling in (other={other.name})", this);
            return;
        }
        if (debugLog) Debug.Log("[ManholeFallTrigger] FIRING fall sequence", this);
        StartCoroutine(FallSequence());
    }

    // Player must actually be dropping into the hole — not just standing on the cover
    // when it slides away or walking near the rim. Requires airborne + feet below rim.
    bool IsPlayerFallingIn(Collider other)
    {
        if (!requireAirborne && minDropBelowRim <= 0f) return true;

        Transform root = other.transform.root != null ? other.transform.root : other.transform;

        if (requireAirborne)
        {
            // Look up the CC from the collider first, then the root, then anywhere in the hierarchy.
            var cc = other.GetComponentInParent<CharacterController>();
            if (cc == null) cc = root.GetComponentInChildren<CharacterController>();

            if (cc != null)
            {
                if (cc.isGrounded)
                {
                    if (debugLog) Debug.Log("[ManholeFallTrigger] gate: CC is grounded — not airborne", this);
                    return false;
                }
            }
            else
            {
                var rb = other.attachedRigidbody;
                if (rb != null && !rb.isKinematic && rb.velocity.y > -0.1f)
                {
                    if (debugLog) Debug.Log("[ManholeFallTrigger] gate: rigidbody not falling", this);
                    return false;
                }
            }
        }

        if (minDropBelowRim > 0f)
        {
            float feetY = root.position.y;
            if (feetY > transform.position.y - minDropBelowRim) return false;
        }

        return true;
    }

    bool IsPlayer(Collider other)
    {
        if (other == null) return false;

        // 1) Tag match
        if (!string.IsNullOrEmpty(playerTag) && other.gameObject.CompareTag(playerTag)) return true;

        // 2) Root name contains 'player'
        var root = other.transform.root;
        if (root != null && root.name.ToLower().Contains("player")) return true;

        // 3) Has a CharacterController anywhere in the hierarchy
        if (other.GetComponentInParent<CharacterController>() != null) return true;

        // 4) Has a Rigidbody and is not static (likely the player capsule)
        var rb = other.attachedRigidbody;
        if (rb != null && !rb.isKinematic) return true;

        // 5) MainCamera tag — your scene has a MainCamera, fall back to that
        if (other.gameObject.CompareTag("MainCamera")) return true;
        if (root != null)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.gameObject.CompareTag("MainCamera")) return true;
            }
        }

        return false;
    }

    IEnumerator FallSequence()
    {
        firing = true;
        firedCount++;

        // Two-stage fade: first ease to glimpseAlpha (player sees a hint of the sewer),
        // then ease to full black. If glimpseAlpha is 0 or fraction is 0, single fade.
        if (glimpseAlpha > 0f && glimpseFraction > 0f && glimpseFraction < 1f)
        {
            float t1 = fadeDuration * glimpseFraction;
            float t2 = fadeDuration * (1f - glimpseFraction);
            yield return StartCoroutine(EaseAlpha(0f, glimpseAlpha, t1, useUnscaledTime: false));
            yield return StartCoroutine(EaseAlpha(glimpseAlpha, 1f, t2, useUnscaledTime: false));
        }
        else
        {
            yield return StartCoroutine(EaseAlpha(0f, 1f, fadeDuration, useUnscaledTime: false));
        }

        // Try to transition to the next scene
        if (!string.IsNullOrWhiteSpace(nextSceneName) && CanLoadScene(nextSceneName))
        {
            if (preLoadHold > 0f)
            {
                float t = 0f;
                while (t < preLoadHold) { t += Time.unscaledDeltaTime; yield return null; }
            }
            // Make sure time is normal before loading so the next scene starts running
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        // No next scene — either freeze in black or fade back
        if (freezeGameWhenBlack) Time.timeScale = 0f;

        if (stayBlack) yield break; // done — black forever

        // Hold then fade back (unscaled, since timeScale may be 0)
        if (holdDuration > 0f)
        {
            float t = 0f;
            while (t < holdDuration) { t += Time.unscaledDeltaTime; yield return null; }
        }

        if (freezeGameWhenBlack) Time.timeScale = 1f;
        yield return StartCoroutine(EaseAlpha(1f, 0f, fadeBackDuration, useUnscaledTime: false));
        firing = false;
    }

    static bool CanLoadScene(string sceneName)
    {
        // Check Build Settings — only scenes added there can be loaded by name.
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    IEnumerator EaseAlpha(float from, float to, float duration, bool useUnscaledTime)
    {
        if (blackImage == null) yield break;
        if (duration <= 0f) { SetAlpha(to); yield break; }
        float t = 0f;
        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            SetAlpha(Mathf.Lerp(from, to, s));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        if (blackImage == null) return;
        var c = blackImage.color;
        c.a = a;
        blackImage.color = c;
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;
        Gizmos.color = new Color(0.05f, 0.05f, 0.05f, 0.6f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider bc) Gizmos.DrawCube(bc.center, bc.size);
        else if (col is SphereCollider sc) Gizmos.DrawSphere(sc.center, sc.radius);
    }
}
