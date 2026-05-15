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
    public string targetSceneName = "scene3";

    [Header("Fade")]
    public float fadeDuration = 1.5f;
    public float holdBlackDuration = 0.4f;

    private Transform _player;
    private bool _triggered;

    void Start()
    {
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p == null) p = GameObject.Find("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        if (_triggered || _player == null) return;

        if (Vector3.Distance(transform.position, _player.position) <= triggerDistance)
        {
            _triggered = true;
            StartCoroutine(FadeAndLoad());
        }
    }

    IEnumerator FadeAndLoad()
    {
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

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeDuration);
            img.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        img.color = Color.black;

        yield return new WaitForSecondsRealtime(holdBlackDuration);

        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[LadderTeleport] Scene '{targetSceneName}' is not in Build Settings. Add it via File > Build Settings.");
            _triggered = false;
        }
    }
}
