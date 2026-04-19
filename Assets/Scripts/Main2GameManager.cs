using System.Collections;
using UnityEngine;
using UnityEngine.Playables; // Required for Timeline

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

    [Header("Sleep Settings")]
    [Tooltip("Seconds of darkness before the alarm fires — each second plays one tick")]
    public int sleepTicks = 5;

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
        Debug.Log("PLAY PRESSED");
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
            wakeupTimeline.Play();
        else
            Debug.LogWarning("Wakeup Timeline is not assigned in Main2GameManager!");
    }
}
