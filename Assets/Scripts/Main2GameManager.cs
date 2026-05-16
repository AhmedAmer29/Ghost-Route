using System.Collections;
using UnityEngine;
using UnityEngine.Playables; // Required for Timeline
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Main2GameManager : MonoBehaviour
{
    public static Main2GameManager Instance;

    [Header("References")]
    public ClockController   clock;
    public AlarmInteraction  alarm;

    [Header("Timeline")]
    [Tooltip("The GameObject that has a PlayableDirector component with your wakeup Timeline on it")]
    public PlayableDirector wakeupTimeline;

    [Header("Cameras")]
    [Tooltip("The camera looking at the planning board (main menu view)")]
    public Camera menuCamera;

    [Tooltip("The first-person camera at the desk")]
    public Camera playerCamera;

    [Header("UI")]
    [Tooltip("The parent GameObject holding the Play button and title text")]
    public GameObject mainMenuUI;

    [Header("Ambience")]
    [Tooltip("Rain background sound — starts when Play is pressed, loops forever")]
    public AudioSource rainAudioSource;

    [Header("Sleep Settings")]
    [Tooltip("Seconds of darkness before the alarm fires — each second plays one tick")]
    public int sleepTicks = 5;

    [Header("Cutscene End")]
    [Tooltip("Scene loaded once the wakeup cutscene finishes")]
    public string nextSceneName = "Scene1";

    [Tooltip("Fade-to-black duration when the cutscene ends")]
    public float endFadeDuration = 1.5f;

    void Awake() => Instance = this;

    void Start()
    {
        menuCamera.gameObject.SetActive(true);
        playerCamera.gameObject.SetActive(false);
        mainMenuUI.SetActive(true);
        ScreenFader.Instance.SetClear();
    }

    // ── Wired to the Play button OnClick() in the Inspector ──────────────────
    public void OnPlayPressed()
    {
        // Start rain the moment Play is clicked — runs forever from here
        if (rainAudioSource != null)
        {
            rainAudioSource.loop = true;
            rainAudioSource.Play();
        }

        StartCoroutine(SleepSequence());
    }

    // ── Called by AlarmInteraction when the player clicks the alarm ───────────
    public void OnAlarmStopped()
    {
        clock.StopAlarm();
        // Tick keeps going — no action needed here for the clock.
    }

    // ── Called by a Timeline Signal (or manually) to enable alarm clicking ───
    public void EnableAlarmInteraction()
    {
        alarm.EnableInteraction();
    }

    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator SleepSequence()
    {
        Debug.Log("STEP 1 - hiding menu");
        // 1. Hide menu
        mainMenuUI.SetActive(false);

        Debug.Log("STEP 2 - fading to black");
        // 2. Fade to black
        yield return StartCoroutine(ScreenFader.Instance.FadeToBlack(1.2f));

        // 3. Swap cameras while screen is still black
        menuCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);

        // Snap character to first keyframe while screen is still black
        // so when the screen fades in the character is already in correct pose
        if (wakeupTimeline != null)
        {
            wakeupTimeline.time = 0;
            wakeupTimeline.Evaluate();
        }

        // 4. Start the clock ticking — it will run forever from here.
        //    The tick plays every second (visuals + audio) even while the screen is black.
        clock.StartTicking();

        // 5. Wait for the sleep duration (one tick per second)
        yield return new WaitForSeconds(sleepTicks);

        // 6. Snap clock hands to 12:00 AM and fire the alarm
        clock.SnapToMidnight();
        clock.PlayAlarm();
        Debug.Log("ALARM FIRED");

        // 7. Wait 3 seconds while alarm rings before waking up
        yield return new WaitForSeconds(3f);

        // 8. Blink effect — eyes struggling to open
        yield return StartCoroutine(ScreenFader.Instance.BlinkWakeup());

        // 9. Play the wakeup Timeline — screen is now fully clear
        Debug.Log("WAKING UP - playing timeline");
        if (wakeupTimeline != null)
        {
            wakeupTimeline.time = 0;
            wakeupTimeline.Evaluate(); // Snap character to first keyframe immediately
            wakeupTimeline.Play();

            // 10. Wait for the cutscene to finish, then fade out and load Scene1
            yield return new WaitForSeconds((float)wakeupTimeline.duration);
            Debug.Log("[Main2GameManager] Wakeup cutscene finished — loading " + nextSceneName);
            yield return StartCoroutine(FadeAndLoadNextScene());
        }
        else
            Debug.LogWarning("Wakeup Timeline is not assigned in Main2GameManager!");
    }

    // ── Fade to black and load the next scene once the cutscene ends ─────────
    IEnumerator FadeAndLoadNextScene()
    {
        GameObject canvasGO = new GameObject("Main2FadeCanvas");
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
        while (t < endFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / endFadeDuration));
            yield return null;
        }
        img.color = Color.black;

        if (Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            // The overlay survives the scene load — SceneFadeReveal fades it back
            // in once the next scene is ready (no stuck black screen).
            canvasGO.AddComponent<SceneFadeReveal>().Init(img, endFadeDuration);
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[Main2GameManager] Scene '" + nextSceneName + "' is not in Build Settings.");
        }
    }
}
