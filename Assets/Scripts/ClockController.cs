using System.Collections;
using UnityEngine;

public class ClockController : MonoBehaviour
{
    [Header("Clock Hands")]
    [Tooltip("Drag the 'Seconds' child object here")]
    public Transform secondsHand;

    [Tooltip("Drag the 'Hours' child object here")]
    public Transform hoursHand;

    [Header("Tick Audio  (loops forever after Play is pressed)")]
    public AudioSource tickAudioSource;
    public AudioClip   tickSound;

    [Header("Alarm Audio  (separate — stops when player clicks the clock)")]
    public AudioSource alarmAudioSource;
    public AudioClip   alarmSound;

    // Clock math:
    // At 12:00:00 AM  ->  Seconds Z = 180,  Hours Z = 90
    // At  3:00:00 AM  ->  Hours   Z = 180   (30 degrees per hour)
    // Each second: Seconds += 6 deg,  Hours += (0.5 / 60) deg
    //
    // Time is tracked as seconds offset from midnight (12:00:00 AM = 0).
    // 11:55 PM = -300 seconds.

    private int       secondsFromMidnight = -(5 * 60); // start at 11:55
    private Coroutine tickCoroutine;

    void Start()
    {
        UpdateClockVisuals(); // Show 11:55 on boot
    }

    // ── Called once by Main2GameManager when Play is pressed ──────────────────
    // The tick will run forever from this point — it is never stopped by gameplay.
    public void StartTicking()
    {
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        tickCoroutine = StartCoroutine(TickLoop());
    }

    // ── Called by Main2GameManager after the 5 sleep-ticks ───────────────────
    public void SnapToMidnight()
    {
        secondsFromMidnight = 0;
        UpdateClockVisuals();
    }

    // ── Alarm (separate audio source — does NOT affect the tick) ─────────────
    public void PlayAlarm()
    {
        if (alarmAudioSource && alarmSound)
        {
            alarmAudioSource.clip = alarmSound;
            alarmAudioSource.loop = true;
            alarmAudioSource.Play();
        }
    }

    public void StopAlarm()
    {
        if (alarmAudioSource)
        {
            alarmAudioSource.Stop();
            alarmAudioSource.loop = false;
        }
        // Tick is untouched — it keeps running.
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    IEnumerator TickLoop()
    {
        while (true) // runs forever — tick is ambient, always on
        {
            yield return new WaitForSeconds(1f);
            secondsFromMidnight++;
            UpdateClockVisuals();
            PlayOneTick();
        }
    }

    void PlayOneTick()
    {
        if (tickAudioSource && tickSound)
            tickAudioSource.PlayOneShot(tickSound);
    }

    void UpdateClockVisuals()
    {
        // Positive modulo so negatives wrap correctly
        float secPos    = ((secondsFromMidnight % 60) + 60) % 60;
        float secAngle  = 180f + (secPos * 6f);

        float hourAngle = 90f + (secondsFromMidnight * (0.5f / 60f));

        secondsHand.localEulerAngles = new Vector3(0f, 0f, secAngle);
        hoursHand.localEulerAngles   = new Vector3(0f, 0f, hourAngle);
    }
}
