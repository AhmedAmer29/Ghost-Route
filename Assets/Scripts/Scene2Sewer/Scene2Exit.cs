using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Place this on a trigger collider at Scene2's exit (next to the fake_ladder).
// When the player enters the trigger, fades to black and loads the next scene.
// Access is gated physically by the gate that opens after the boss + lever.
[RequireComponent(typeof(Collider))]
public class Scene2Exit : MonoBehaviour
{
    [Header("Target Scene")]
    public string nextSceneName = "Scene3";

    [Header("Fade")]
    public float fadeDuration = 1.5f;
    public float holdBlackDuration = 0.5f;

    private bool _triggered;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!IsPlayer(other)) return;
        _triggered = true;
        StartCoroutine(FadeAndLoad());
    }

    bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player")) return true;
        if (other.GetComponentInParent<CharacterController>() != null) return true;
        return false;
    }

    private IEnumerator FadeAndLoad()
    {
        GameObject canvasGO = new GameObject("Scene2ExitFadeCanvas");
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
            img.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        img.color = Color.black;

        yield return new WaitForSecondsRealtime(holdBlackDuration);

        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            // The overlay survives the scene load — attach a reveal handler so it
            // fades back in once the next scene is ready (no stuck black screen).
            canvasGO.AddComponent<SceneFadeReveal>().Init(img, fadeDuration);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError($"[Scene2Exit] Scene '{nextSceneName}' is not in Build Settings.");
        }
    }
}
