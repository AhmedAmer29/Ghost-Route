using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LadderTeleport : MonoBehaviour
{
    [Header("Trigger")]
    public float triggerDistance = 3f;
    public string playerTag = "Player";

    [Header("Target Scene")]
    public string targetSceneName = "Scene3";

    [Header("Fade")]
    public float fadeDuration = 1.5f;
    public float holdBlackDuration = 0.4f;

    [Header("Debug")]
    [Tooltip("Logs distance every 0.5s so you can see exactly when/why it triggers.")]
    public bool verboseLogging = true;

    private Transform _player;
    private bool _triggered;
    private float _logTimer;

    void Start()
    {
        Debug.Log($"<color=cyan>[LadderTeleport:{name}] Start. Target='{targetSceneName}', triggerDist={triggerDistance}, pos={transform.position}</color>");
        CachePlayer();

        // Pre-flight: is the target scene actually in the build?
        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            Debug.LogError($"<color=red>[LadderTeleport:{name}] Scene '{targetSceneName}' NOT in Build Settings. Open File > Build Settings and add it. Until then the fade will roll back.</color>");
        else
            Debug.Log($"<color=lime>[LadderTeleport:{name}] Target scene '{targetSceneName}' is in Build Settings. Good.</color>");
    }

    void CachePlayer()
    {
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p == null) p = GameObject.Find("Player");
        if (p != null)
        {
            _player = p.transform;
            Debug.Log($"<color=cyan>[LadderTeleport:{name}] Found player '{p.name}' at {p.transform.position}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[LadderTeleport:{name}] No player found (tag='{playerTag}' or name='Player'). Will retry.</color>");
        }
    }

    void Update()
    {
        if (_triggered) return;

        if (_player == null)
        {
            CachePlayer();
            if (_player == null) return;
        }

        float dist = Vector3.Distance(transform.position, _player.position);

        if (verboseLogging)
        {
            _logTimer += Time.deltaTime;
            if (_logTimer >= 0.5f)
            {
                _logTimer = 0f;
                Debug.Log($"[LadderTeleport:{name}] dist={dist:F2}m (need ≤{triggerDistance}) ladderPos={transform.position} playerPos={_player.position}");
            }
        }

        if (dist <= triggerDistance)
        {
            _triggered = true;
            Debug.Log($"<color=lime>[LadderTeleport:{name}] TRIGGERED at dist={dist:F2}. Loading '{targetSceneName}'...</color>");
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
        Debug.Log($"[LadderTeleport:{name}] FadeAndLoad coroutine started");

        GameObject canvasGO = new GameObject("LadderFadeCanvas");
        DontDestroyOnLoad(canvasGO);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        Image img = imgGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Debug.Log($"[LadderTeleport:{name}] Fading to black over {fadeDuration}s");
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            img.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        img.color = Color.black;

        Debug.Log($"[LadderTeleport:{name}] Holding black for {holdBlackDuration}s");
        yield return new WaitForSecondsRealtime(holdBlackDuration);

        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.Log($"<color=lime>[LadderTeleport:{name}] SceneManager.LoadScene('{targetSceneName}')</color>");
            // Attach a reveal handler BEFORE loading. Without this the black overlay
            // survives the load (DontDestroyOnLoad) and the player sits in Scene3
            // staring at a black screen with no fade-back.
            canvasGO.AddComponent<SceneFadeReveal>().Init(img, fadeDuration);

            // Make sure controls aren't left frozen if the previous scene paused them.
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"<color=red>[LadderTeleport:{name}] Scene '{targetSceneName}' NOT in Build Settings. Add it via File > Build Settings.</color>");
            Destroy(canvasGO); // remove the black overlay so the player isn't stuck staring at it
            _triggered = false;
        }
    }
}
